using AudioCat.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AudioCat.Models;

namespace AudioCat.Services;

internal sealed class ChaptersPlayer : IDisposable
{
    public event EventHandler<IMediaChapterViewModel>? ChapterChanged;
    public event EventHandler<PlaybackProgressEventArgs>? PlaybackProgress;
    public event EventHandler<string>? PlaybackError;

    private ReadOnlyCollection<IMediaFileViewModel> Files { get; }
    private ObservableCollection<IMediaChapterViewModel> CreatedChapters { get; }

    private SemaphoreSlim Sync { get; } = new(1, 1);
    private IAudioFilePlayer? ActivePlayer { get; set; }
    private bool IsPlaying { get; set; }

    // Global timeline offset of the start of the currently playing file
    private TimeSpan ActiveFileGlobalOffset { get; set; }
    // Index into Files of the currently playing file
    private int ActiveFileIndex { get; set; }
    // The chapter currently being played
    private IMediaChapterViewModel? ActiveChapter { get; set; }
    // The index of ActiveChapter within CreatedChapters
    private int ActiveChapterIndex { get; set; }

    private bool IsDisposed { get; set; }

    public ChaptersPlayer(
        ReadOnlyCollection<IMediaFileViewModel> files,
        ObservableCollection<IMediaChapterViewModel> createdChapters)
    {
        Files = files;
        CreatedChapters = createdChapters;
        CreatedChapters.CollectionChanged += OnCreatedChaptersChanged;
    }

    /// <summary>
    /// Starts playing from the beginning of <paramref name="chapter"/> plus the given <paramref name="offset"/> into that chapter.
    /// If already playing, stops first and starts the new chapter immediately.
    /// </summary>
    public void Play(IMediaChapterViewModel chapter, TimeSpan offset)
    {
        Sync.Wait();
        try
        {
            StopInternal();

            var chapterIndex = CreatedChapters.IndexOf(chapter);
            if (chapterIndex < 0)
                return;

            if (chapter.StartTime is not { } chapterStart)
                return;

            // Clamp offset to lower bound: negative offset snaps to chapter start
            if (offset < TimeSpan.Zero)
                offset = TimeSpan.Zero;

            // Clamp offset to upper bound: offset at or beyond chapter duration is out of range
            if (chapter.EndTime is { } chapterEnd)
            {
                var chapterDuration = chapterEnd - chapterStart;
                if (offset >= chapterDuration)
                    return;
            }

            var globalPosition = chapterStart + offset;

            if (!TryFindFile(globalPosition, out var fileIndex, out var fileGlobalOffset))
                return;

            var file = Files[fileIndex];
            var positionInFile = globalPosition - fileGlobalOffset;

            var createResult = AudioFilePlayer.Create(file.FilePath);
            if (createResult.IsFailure)
            {
                PlaybackError?.Invoke(this, createResult.Message ?? "Failed to create audio player.");
                return;
            }

            ActivePlayer = createResult.Data!;
            ActiveFileIndex = fileIndex;
            ActiveFileGlobalOffset = fileGlobalOffset;
            ActiveChapter = chapter;
            ActiveChapterIndex = chapterIndex;
            IsPlaying = true;

            SubscribeToPlayer(ActivePlayer);
            ActivePlayer.SetPosition(file.FilePath, positionInFile);
            ActivePlayer.Play();
        }
        finally
        {
            Sync.Release();
        }
    }

    /// <summary>
    /// Stops playback and disposes the active player.
    /// </summary>
    public void Stop()
    {
        Sync.Wait();
        try { StopInternal(); }
        finally { Sync.Release(); }
    }

    // Must be called with Sync held.
    private void StopInternal()
    {
        IsPlaying = false;
        ActiveChapter = null;

        if (ActivePlayer is null)
            return;

        UnsubscribeFromPlayer(ActivePlayer);
        (ActivePlayer as IDisposable)?.Dispose();
        ActivePlayer = null;
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
        Sync.Wait();
        try
        {
            if (!IsPlaying || ActiveChapter is null)
                return;

            var globalPosition = ActiveFileGlobalOffset + e.CurrentPosition;

            // Check if we've crossed into the next chapter
            var nextChapterIndex = ActiveChapterIndex + 1;
            if (nextChapterIndex < CreatedChapters.Count)
            {
                var nextChapter = CreatedChapters[nextChapterIndex];
                if (nextChapter.StartTime is { } nextStart && globalPosition >= nextStart)
                {
                    ActiveChapter = nextChapter;
                    ActiveChapterIndex = nextChapterIndex;
                    ChapterChanged?.Invoke(this, nextChapter);
                }
            }

            // Check if we've reached the end of the last chapter
            if (ActiveChapter.EndTime is { } chapterEnd && globalPosition >= chapterEnd)
            {
                // No more chapters — stop playing
                if (ActiveChapterIndex >= CreatedChapters.Count - 1)
                {
                    StopInternal();
                    return;
                }
            }

            // Fire playback progress — chapter position is global position relative to chapter start
            var chapterPosition = ActiveChapter.StartTime is { } chapterStart
                ? globalPosition - chapterStart
                : globalPosition;

            PlaybackProgress?.Invoke(this, new PlaybackProgressEventArgs(ActiveChapter, globalPosition, chapterPosition));
        }
        finally
        {
            Sync.Release();
        }
    }

    private void OnPlaybackStateChanged(object? sender, AudioPlayStateEventArgs e)
    {
        if (e.State != AudioPlayerState.Stopped)
            return;

        Sync.Wait();
        try
        {
            if (!IsPlaying)
                return; // We stopped intentionally via StopInternal — nothing to do

            // File ended naturally — move to the next file
            var nextFileIndex = ActiveFileIndex + 1;
            if (nextFileIndex >= Files.Count)
            {
                // No more files — stop
                StopInternal();
                return;
            }

            // Compute the global offset of the next file
            var nextFileGlobalOffset = ActiveFileGlobalOffset + (Files[ActiveFileIndex].Duration ?? TimeSpan.Zero);

            var oldPlayer = ActivePlayer;

            var nextFile = Files[nextFileIndex];
            var createResult = AudioFilePlayer.Create(nextFile.FilePath);
            if (createResult.IsFailure)
            {
                StopInternal();
                PlaybackError?.Invoke(this, createResult.Message ?? "Failed to create audio player.");
                return;
            }

            // Unsubscribe and dispose the old player before switching
            if (oldPlayer is not null)
            {
                UnsubscribeFromPlayer(oldPlayer);
                (oldPlayer as IDisposable)?.Dispose();
            }

            ActivePlayer = createResult.Data!;
            ActiveFileIndex = nextFileIndex;
            ActiveFileGlobalOffset = nextFileGlobalOffset;

            SubscribeToPlayer(ActivePlayer);
            ActivePlayer.SetPosition(nextFile.FilePath, TimeSpan.Zero);
            ActivePlayer.Play();
        }
        finally
        {
            Sync.Release();
        }
    }

    private void OnPlayerPlaybackError(object? sender, MessageEventArgs e)
    {
        Sync.Wait();
        try { StopInternal(); }
        finally { Sync.Release(); }

        PlaybackError?.Invoke(this, e.Message);
    }

    private void OnCreatedChaptersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Sync.Wait();
        try
        {
            if (IsPlaying)
                StopInternal();
        }
        finally
        {
            Sync.Release();
        }
    }

    /// <summary>
    /// Finds which file in <see cref="Files"/> contains the given global timeline position.
    /// </summary>
    private bool TryFindFile(TimeSpan globalPosition, out int fileIndex, out TimeSpan fileGlobalOffset)
    {
        var accumulated = TimeSpan.Zero;
        for (var i = 0; i < Files.Count; i++)
        {
            var duration = Files[i].Duration ?? TimeSpan.Zero;
            var fileEnd = accumulated + duration;

            if (globalPosition < fileEnd || i == Files.Count - 1)
            {
                fileIndex = i;
                fileGlobalOffset = accumulated;
                return true;
            }

            accumulated = fileEnd;
        }

        fileIndex = -1;
        fileGlobalOffset = TimeSpan.Zero;
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