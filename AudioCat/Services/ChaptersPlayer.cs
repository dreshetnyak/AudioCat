using AudioCat.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AudioCat.Models;

namespace AudioCat.Services;

#region Event Args
internal sealed class ChaptersPlayerPositionEventArgs(TimeSpan globalPosition, IMediaChapterViewModel? activeChapter) : EventArgs
{
    public TimeSpan GlobalPosition { get; } = globalPosition;
    public IMediaChapterViewModel? ActiveChapter { get; } = activeChapter;
}
#endregion

internal sealed class ChaptersPlayer : IDisposable
{
    /// <summary>
    /// Raised when the chapter derived from the playback position differs from the previous one,
    /// including transitions to and from null (a gap not covered by any chapter).
    /// Raised on the player's poller thread; UI marshaling is the subscriber's responsibility.
    /// </summary>
    public event EventHandler<IMediaChapterViewModel?>? ChapterChanged;
    /// <summary>
    /// Raised on every position tick while playing.
    /// Raised on the player's poller thread; UI marshaling is the subscriber's responsibility.
    /// </summary>
    public event EventHandler<ChaptersPlayerPositionEventArgs>? PositionChanged;
    /// <summary>
    /// Raised when the engine transitions between Stopped, Playing and Paused.
    /// May be raised on the player's poller thread; UI marshaling is the subscriber's responsibility.
    /// </summary>
    public event EventHandler<AudioPlayerState>? StateChanged;
    public event EventHandler<string>? PlaybackError;

    // A file participating in the raw global timeline, with its precomputed start offset on that timeline
    private sealed record PlayableFile(IMediaFileViewModel File, TimeSpan GlobalOffset, TimeSpan Duration);

    private IReadOnlyList<PlayableFile> PlayableFiles { get; }
    private ObservableCollection<IMediaChapterViewModel> CreatedChapters { get; }

    /// <summary>Total length of the raw global timeline: the sum of all playable files' durations.</summary>
    public TimeSpan TotalDuration { get; }

    private SemaphoreSlim Sync { get; } = new(1, 1);
    private IAudioFilePlayer? ActivePlayer { get; set; }

    /// <summary>Current engine state: Stopped, Playing or Paused. Writes are guarded by <see cref="Sync"/>.</summary>
    public AudioPlayerState State { get; private set; } = AudioPlayerState.Stopped;

    /// <summary>
    /// Playback volume in the [0, 1] range, default 0.75. Applied to the live player immediately and
    /// re-applied to every newly created player (play, rollover, cross-file seek), so the level survives
    /// file transitions.
    /// </summary>
    public float Volume
    {
        get
        {
            Sync.Wait();
            try { return VolumeLevel; }
            finally { Sync.Release(); }
        }
        set
        {
            var volume = Math.Clamp(value, 0f, 1f);
            Sync.Wait();
            try
            {
                VolumeLevel = volume;
                ActivePlayer?.SetVolume(volume);
            }
            finally { Sync.Release(); }
        }
    }

    // Backing store for Volume. Guarded by Sync.
    private float VolumeLevel { get; set; } = 0.75f;

    // Last known position on the raw global timeline; while stopped it is the only position the engine
    // has (no device is open), and a Stopped-state seek moves just this value. Guarded by Sync.
    private TimeSpan CurrentGlobalPosition { get; set; }

    // Global timeline offset of the start of the currently playing file
    private TimeSpan ActiveFileGlobalOffset { get; set; }
    // Index into PlayableFiles of the currently playing file
    private int ActiveFileIndex { get; set; }
    // The chapter derived from the last observed position; null while stopped or in a gap between chapters
    private IMediaChapterViewModel? ActiveChapter { get; set; }

    // Timestamp of the last successful in-place playback recovery; rate-limits TryRecoverPlayback. Guarded by Sync.
    private DateTime LastRecoveryAtUtc { get; set; } = DateTime.MinValue;
    private static TimeSpan RecoveryCooldown { get; } = TimeSpan.FromSeconds(2);

    private bool IsDisposed { get; set; }

    public ChaptersPlayer(
        ReadOnlyCollection<IMediaFileViewModel> files,
        ObservableCollection<IMediaChapterViewModel> createdChapters)
    {
        // The playable list excludes images and files without a known duration; all timeline math uses only this list
        var playableFiles = new List<PlayableFile>(files.Count);
        var accumulated = TimeSpan.Zero;
        foreach (var file in files)
        {
            if (file.IsImage || file.Duration is not { } duration)
                continue;
            playableFiles.Add(new PlayableFile(file, accumulated, duration));
            accumulated += duration;
        }

        PlayableFiles = playableFiles;
        TotalDuration = accumulated;
        CreatedChapters = createdChapters;
        CreatedChapters.CollectionChanged += OnCreatedChaptersChanged;
    }

    /// <summary>
    /// Starts playing from the given position on the raw global timeline.
    /// If already playing, stops first and starts from the new position immediately.
    /// Playback continues to the end of the timeline, rolling over from file to file.
    /// </summary>
    public void Play(TimeSpan globalPosition)
    {
        string? error = null;
        AudioPlayerState? notifyState = null;

        Sync.Wait();
        try
        {
            var stateBefore = State;
            StopInternal();

            // Clamp to the [Zero, TotalDuration) timeline; at or beyond the end there is nothing to play
            if (globalPosition < TimeSpan.Zero)
                globalPosition = TimeSpan.Zero;

            if (globalPosition < TotalDuration && TryFindFile(globalPosition, out var fileIndex))
            {
                var playableFile = PlayableFiles[fileIndex];
                var positionInFile = globalPosition - playableFile.GlobalOffset;

                var createResult = CreatePlayer(playableFile.File.FilePath);
                if (createResult.IsFailure)
                    error = createResult.Message ?? "Failed to create audio player.";
                else
                {
                    ActivePlayer = createResult.Data!;
                    ActiveFileIndex = fileIndex;
                    ActiveFileGlobalOffset = playableFile.GlobalOffset;
                    CurrentGlobalPosition = globalPosition;
                    State = AudioPlayerState.Playing;

                    SubscribeToPlayer(ActivePlayer);
                    ActivePlayer.SetVolume(VolumeLevel);
                    ActivePlayer.SetPosition(playableFile.File.FilePath, positionInFile);
                    ActivePlayer.Play();
                }
            }

            if (State != stateBefore)
                notifyState = State;
        }
        finally
        {
            Sync.Release();
        }

        if (notifyState is { } state)
            StateChanged?.Invoke(this, state);
        if (error is not null)
            PlaybackError?.Invoke(this, error);
    }

    /// <summary>
    /// Pauses playback on the live player instance; the player, its file position and the derived
    /// chapter all survive. Valid only while Playing — otherwise a silent no-op.
    /// </summary>
    public void Pause()
    {
        AudioPlayerState? notifyState = null;

        Sync.Wait();
        try
        {
            if (State == AudioPlayerState.Playing && ActivePlayer is not null)
            {
                ActivePlayer.Pause();
                State = AudioPlayerState.Paused;
                notifyState = State;
            }
        }
        finally { Sync.Release(); }

        if (notifyState is { } state)
            StateChanged?.Invoke(this, state);
    }

    /// <summary>
    /// Resumes playback on the live paused player instance — no new player is created.
    /// Valid only while Paused — otherwise a silent no-op.
    /// </summary>
    public void Resume()
    {
        AudioPlayerState? notifyState = null;

        Sync.Wait();
        try
        {
            if (State == AudioPlayerState.Paused && ActivePlayer is not null)
            {
                ActivePlayer.Play();
                State = AudioPlayerState.Playing;
                notifyState = State;
            }
        }
        finally { Sync.Release(); }

        if (notifyState is { } state)
            StateChanged?.Invoke(this, state);
    }

    /// <summary>
    /// Seeks to the given position on the raw global timeline, clamped to [Zero, TotalDuration).
    /// Valid in any state: while Playing the audio continues from the new position (swapping files if
    /// needed); while Paused the position moves but the engine stays paused and no audio starts;
    /// while Stopped only the stored position moves — no device is opened.
    /// </summary>
    public void Seek(TimeSpan globalPosition)
    {
        string? error = null;
        AudioPlayerState? notifyState = null;
        var notifyPosition = false;
        var chapterChanged = false;
        IMediaChapterViewModel? activeChapter = null;

        Sync.Wait();
        try
        {
            var stateBefore = State;

            // Clamp to the [Zero, TotalDuration) timeline; the end itself is not a seekable position
            if (globalPosition >= TotalDuration)
                globalPosition = TotalDuration - TimeSpan.FromMilliseconds(1);
            if (globalPosition < TimeSpan.Zero)
                globalPosition = TimeSpan.Zero;

            if (State == AudioPlayerState.Stopped)
            {
                // No device is opened; only the stored position (and the chapter derived from it) moves
                CurrentGlobalPosition = globalPosition;
                notifyPosition = true;
            }
            else if (TryFindFile(globalPosition, out var fileIndex))
            {
                var playableFile = PlayableFiles[fileIndex];
                var positionInFile = globalPosition - playableFile.GlobalOffset;

                if (fileIndex == ActiveFileIndex && ActivePlayer is not null)
                {
                    // Target falls in the currently open file; a paused player stays paused
                    ActivePlayer.SetPosition(playableFile.File.FilePath, positionInFile);
                    CurrentGlobalPosition = globalPosition;
                    notifyPosition = true;
                }
                else
                {
                    var createResult = CreatePlayer(playableFile.File.FilePath);
                    if (createResult.IsFailure)
                    {
                        StopInternal();
                        error = createResult.Message ?? "Failed to create audio player.";
                    }
                    else
                    {
                        var oldPlayer = ActivePlayer;
                        if (oldPlayer is not null)
                        {
                            UnsubscribeFromPlayer(oldPlayer);
                            DisposePlayer(oldPlayer);
                        }

                        ActivePlayer = createResult.Data!;
                        ActiveFileIndex = fileIndex;
                        ActiveFileGlobalOffset = playableFile.GlobalOffset;
                        CurrentGlobalPosition = globalPosition;

                        SubscribeToPlayer(ActivePlayer);
                        ActivePlayer.SetVolume(VolumeLevel);
                        ActivePlayer.SetPosition(playableFile.File.FilePath, positionInFile);
                        if (State == AudioPlayerState.Playing)
                            ActivePlayer.Play(); // A paused engine keeps the new player silent until Resume
                        notifyPosition = true;
                    }
                }
            }

            if (notifyPosition)
            {
                // Re-derive the active chapter from the new position
                activeChapter = FindChapter(globalPosition);
                chapterChanged = !ReferenceEquals(activeChapter, ActiveChapter);
                ActiveChapter = activeChapter;
            }

            if (State != stateBefore)
                notifyState = State;
        }
        finally
        {
            Sync.Release();
        }

        if (notifyState is { } state)
            StateChanged?.Invoke(this, state);
        if (chapterChanged)
            ChapterChanged?.Invoke(this, activeChapter);
        if (notifyPosition)
            PositionChanged?.Invoke(this, new ChaptersPlayerPositionEventArgs(globalPosition, activeChapter));
        if (error is not null)
            PlaybackError?.Invoke(this, error);
    }

    /// <summary>
    /// Stops playback and disposes the active player.
    /// </summary>
    public void Stop()
    {
        AudioPlayerState? notifyState = null;

        Sync.Wait();
        try
        {
            if (State != AudioPlayerState.Stopped)
            {
                StopInternal();
                notifyState = AudioPlayerState.Stopped;
            }
        }
        finally { Sync.Release(); }

        if (notifyState is { } state)
            StateChanged?.Invoke(this, state);
    }

    // Must be called with Sync held.
    private void StopInternal()
    {
        State = AudioPlayerState.Stopped;
        ActiveChapter = null;

        if (ActivePlayer is null)
            return;

        var player = ActivePlayer;
        ActivePlayer = null;
        UnsubscribeFromPlayer(player);
        DisposePlayer(player);
    }

    // MediaFoundation occasionally fails transiently (E_UNEXPECTED "Catastrophic failure") on
    // operations that succeed when repeated, so a failed create gets one immediate retry.
    private static IResponse<IAudioFilePlayer> CreatePlayer(string filePath)
    {
        var createResult = AudioFilePlayer.Create(filePath);
        return createResult.IsSuccess ? createResult : AudioFilePlayer.Create(filePath);
    }

    // Disposal is deferred to the thread pool: StopInternal can run inside the player's own
    // event callback with Sync held, and AudioFilePlayer.Dispose joins its status-poller loop.
    private static void DisposePlayer(IAudioFilePlayer player)
    {
        // Silence the device immediately; the actual disposal may lag a poll cycle. A device that
        // already died on a playback error may refuse the pause — disposal is the real cleanup.
        try { player.Pause(); }
        catch { /* ignore */ }
        if (player is IDisposable disposable)
            _ = Task.Run(disposable.Dispose);
    }

    private void SubscribeToPlayer(IAudioFilePlayer player)
    {
        player.PlaybackStateChanged += OnPlaybackStateChanged;
        player.PlaybackPositionChanged += OnPlaybackPositionChanged;
        player.PlaybackError += OnPlayerPlaybackError;
    }

    private void UnsubscribeFromPlayer(IAudioFilePlayer player)
    {
        player.PlaybackStateChanged -= OnPlaybackStateChanged;
        player.PlaybackPositionChanged -= OnPlaybackPositionChanged;
        player.PlaybackError -= OnPlayerPlaybackError;
    }

    private void OnPlaybackPositionChanged(object? sender, PlaybackPositionEventArgs e)
    {
        bool chapterChanged;
        IMediaChapterViewModel? activeChapter;
        TimeSpan globalPosition;

        Sync.Wait();
        try
        {
            if (State != AudioPlayerState.Playing || !ReferenceEquals(sender, ActivePlayer))
                return; // Not playing, or a stale tick from a replaced player

            globalPosition = ActiveFileGlobalOffset + e.CurrentPosition;
            CurrentGlobalPosition = globalPosition;

            activeChapter = FindChapter(globalPosition);
            chapterChanged = !ReferenceEquals(activeChapter, ActiveChapter);
            ActiveChapter = activeChapter;
        }
        finally
        {
            Sync.Release();
        }

        // Events are raised outside the lock, on the player's poller thread
        if (chapterChanged)
            ChapterChanged?.Invoke(this, activeChapter);
        PositionChanged?.Invoke(this, new ChaptersPlayerPositionEventArgs(globalPosition, activeChapter));
    }

    private void OnPlaybackStateChanged(object? sender, AudioPlayStateEventArgs e)
    {
        if (e.State != AudioPlayerState.Stopped)
            return;

        string? error = null;
        var notifyStopped = false;

        Sync.Wait();
        try
        {
            // Rollover applies only to a natural file end while the engine is Playing. A device Stopped
            // event observed while the engine is Paused or Stopped, or coming from a replaced player,
            // is ignored — pausing or stopping must never trigger a file advance.
            if (State == AudioPlayerState.Playing && ReferenceEquals(sender, ActivePlayer))
            {
                // File ended naturally — advance to the next playable file
                var nextFileIndex = ActiveFileIndex + 1;
                if (nextFileIndex >= PlayableFiles.Count)
                {
                    // Reached the end of the timeline — stop
                    StopInternal();
                    notifyStopped = true;
                }
                else
                {
                    var oldPlayer = ActivePlayer;
                    var nextFile = PlayableFiles[nextFileIndex];
                    var createResult = CreatePlayer(nextFile.File.FilePath);
                    if (createResult.IsFailure)
                    {
                        StopInternal();
                        notifyStopped = true;
                        error = createResult.Message ?? "Failed to create audio player.";
                    }
                    else
                    {
                        // Unsubscribe and dispose the old player before switching
                        if (oldPlayer is not null)
                        {
                            UnsubscribeFromPlayer(oldPlayer);
                            DisposePlayer(oldPlayer);
                        }

                        ActivePlayer = createResult.Data!;
                        ActiveFileIndex = nextFileIndex;
                        ActiveFileGlobalOffset = nextFile.GlobalOffset;
                        CurrentGlobalPosition = nextFile.GlobalOffset;

                        SubscribeToPlayer(ActivePlayer);
                        ActivePlayer.SetVolume(VolumeLevel);
                        ActivePlayer.SetPosition(nextFile.File.FilePath, TimeSpan.Zero);
                        ActivePlayer.Play();
                    }
                }
            }
        }
        finally
        {
            Sync.Release();
        }

        if (notifyStopped)
            StateChanged?.Invoke(this, AudioPlayerState.Stopped);
        if (error is not null)
            PlaybackError?.Invoke(this, error);
    }

    private void OnPlayerPlaybackError(object? sender, MessageEventArgs e)
    {
        var isStale = false;
        var recovered = false;
        var notifyStopped = false;

        Sync.Wait();
        try
        {
            if (!ReferenceEquals(sender, ActivePlayer))
                isStale = true; // A stale error from a replaced player — ignore
            else
            {
                recovered = TryRecoverPlayback();
                if (!recovered)
                {
                    notifyStopped = State != AudioPlayerState.Stopped;
                    StopInternal();
                }
            }
        }
        finally { Sync.Release(); }

        if (isStale || recovered)
            return;
        if (notifyStopped)
            StateChanged?.Invoke(this, AudioPlayerState.Stopped);
        PlaybackError?.Invoke(this, e.Message);
    }

    /// <summary>
    /// Recovers from a playback error by replacing the failed player with a fresh one at the last
    /// known position, so a transient decoder failure (MediaFoundation is known to sporadically
    /// fail a seek or a read with E_UNEXPECTED "Catastrophic failure") does not stop playback with
    /// a modal error. Rate-limited by <see cref="RecoveryCooldown"/> so a systematically failing
    /// file still surfaces its error instead of silently stuttering in a recovery loop.
    /// Must be called with Sync held. Returns false when recovery is not possible or not allowed —
    /// the caller then stops the engine and surfaces the error as before.
    /// </summary>
    private bool TryRecoverPlayback()
    {
        if (State == AudioPlayerState.Stopped)
            return false;
        var nowUtc = DateTime.UtcNow;
        if (nowUtc - LastRecoveryAtUtc < RecoveryCooldown)
            return false;
        if (!TryFindFile(CurrentGlobalPosition, out var fileIndex))
            return false;

        var playableFile = PlayableFiles[fileIndex];
        var createResult = CreatePlayer(playableFile.File.FilePath);
        if (createResult.IsFailure)
            return false;

        var oldPlayer = ActivePlayer;
        if (oldPlayer is not null)
        {
            UnsubscribeFromPlayer(oldPlayer);
            DisposePlayer(oldPlayer);
        }

        ActivePlayer = createResult.Data!;
        ActiveFileIndex = fileIndex;
        ActiveFileGlobalOffset = playableFile.GlobalOffset;

        SubscribeToPlayer(ActivePlayer);
        ActivePlayer.SetVolume(VolumeLevel);
        ActivePlayer.SetPosition(playableFile.File.FilePath, CurrentGlobalPosition - playableFile.GlobalOffset);
        if (State == AudioPlayerState.Playing)
            ActivePlayer.Play(); // A paused engine keeps the new player silent until Resume
        LastRecoveryAtUtc = nowUtc;
        return true;
    }

    private void OnCreatedChaptersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var notifyStopped = false;

        Sync.Wait();
        try
        {
            if (State != AudioPlayerState.Stopped)
            {
                StopInternal();
                notifyStopped = true;
            }
        }
        finally
        {
            Sync.Release();
        }

        if (notifyStopped)
            StateChanged?.Invoke(this, AudioPlayerState.Stopped);
    }

    /// <summary>
    /// Derives the active chapter purely from the global position: the first chapter whose
    /// [StartTime, EndTime) range contains it. Chapters missing either time are skipped.
    /// Returns null when no chapter contains the position (a gap).
    /// </summary>
    private IMediaChapterViewModel? FindChapter(TimeSpan globalPosition)
    {
        foreach (var chapter in CreatedChapters)
        {
            if (chapter.StartTime is { } start && chapter.EndTime is { } end &&
                start <= globalPosition && globalPosition < end)
                return chapter;
        }

        return null;
    }

    /// <summary>
    /// Finds the index of the playable file that contains the given global timeline position.
    /// Positions at or beyond the end of the timeline resolve to the last playable file.
    /// Fails only when there are no playable files.
    /// </summary>
    private bool TryFindFile(TimeSpan globalPosition, out int fileIndex)
    {
        for (var i = 0; i < PlayableFiles.Count; i++)
        {
            var file = PlayableFiles[i];
            if (globalPosition < file.GlobalOffset + file.Duration || i == PlayableFiles.Count - 1)
            {
                fileIndex = i;
                return true;
            }
        }

        fileIndex = -1;
        return false;
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        CreatedChapters.CollectionChanged -= OnCreatedChaptersChanged;
        Sync.Wait();
        try { StopInternal(); }
        finally
        {
            Sync.Release();
            Sync.Dispose();
        }
    }
}
