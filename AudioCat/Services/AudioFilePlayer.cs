using AudioCat.Models;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace AudioCat.Services;

#region Internal Types
internal enum AudioPlayerState
{
    Stopped,
    Playing,
    Paused
}

internal class AudioPlayStateEventArgs(AudioPlayerState state) : EventArgs
{
    public AudioPlayerState State { get; } = state;
}

internal class PlaybackPositionEventArgs(TimeSpan duration, TimeSpan currentPosition) : EventArgs
{
    public TimeSpan Duration { get; } = duration;
    public TimeSpan CurrentPosition { get; } = currentPosition;
}

internal class StreamVolumeEventArgs(float[] maxSampleValues) : EventArgs
{
    public float[] MaxSampleValues { get; } = maxSampleValues;
}

internal interface IAudioFilePlayer
{
    void Play();
    void Pause();
    void SetVolume(float volume);
    void SetPosition(TimeSpan position);
    event EventHandler<StreamVolumeEventArgs>? PlaybackVolume;
    event EventHandler<AudioPlayStateEventArgs>? PlaybackStateChanged;
    event EventHandler<PlaybackPositionEventArgs>? PlaybackPositionChanged;
    event EventHandler<MessageEventArgs>? PlaybackError;
}
#endregion

internal sealed class AudioFilePlayer : IAudioFilePlayer, IAsyncDisposable, IDisposable
{
    private bool IsDisposed { get; set; } // Guarded by Sync
    private Lock Sync { get; } = new();
    private WaveOutEvent OutputDevice { get; }
    private AudioFileReader AudioFileReader { get; }
    private DisposalGuardedSampleProvider GuardedReader { get; }
    private MeteringSampleProvider MeteringProvider { get; }
    private PeriodicInvoker PlayerStatusInvoker { get; }
    private bool IsStatusPollerStarted { get; set; } // Guarded by Sync; the status poller must be started at most once per player lifetime

    private AudioPlayerState CurrentState { get; set; } = AudioPlayerState.Stopped; // Guarded by Sync
    private TimeSpan CurrentPosition { get; set; } = TimeSpan.Zero; // Guarded by Sync

    private TimeSpan Duration { get; }

    public event EventHandler<StreamVolumeEventArgs>? PlaybackVolume;
    public event EventHandler<AudioPlayStateEventArgs>? PlaybackStateChanged;
    public event EventHandler<PlaybackPositionEventArgs>? PlaybackPositionChanged;
    public event EventHandler<MessageEventArgs>? PlaybackError;

    private AudioFilePlayer(string audioFile)
    {
        OutputDevice = new WaveOutEvent();
        AudioFileReader = new AudioFileReader(audioFile);
        GuardedReader = new DisposalGuardedSampleProvider(AudioFileReader);
        MeteringProvider = new MeteringSampleProvider(GuardedReader);
        // NAudio reuses (and Array.Clears) the MaxSampleValues buffer across callbacks, so it must be
        // cloned before forwarding; '?.' skips the clone entirely while there are no subscribers.
        MeteringProvider.StreamVolume += (_, e) => PlaybackVolume?.Invoke(this, new StreamVolumeEventArgs((float[])e.MaxSampleValues.Clone()));
        OutputDevice.PlaybackStopped += OnPlaybackStopped;
        OutputDevice.Init(MeteringProvider);
        Duration = AudioFileReader.TotalTime;
        PlayerStatusInvoker = new PeriodicInvoker(OnPlaybackStateUpdate, TimeSpan.FromMilliseconds(100));
    }

    public static IResponse<IAudioFilePlayer> Create(string audioFile)
    {
        try
        { return Response<IAudioFilePlayer>.Success(new AudioFilePlayer(audioFile)); }
        catch (Exception ex)
        { return Response<IAudioFilePlayer>.Failure(ex.Message); }
    }

    public void Dispose()
    {
        lock (Sync)
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
        }
        // Join the status poller first so it cannot touch the disposed device/reader.
        // Must not hold Sync here: the poll callback takes Sync, so joining it under the lock deadlocks.
        PlayerStatusInvoker.Dispose();
        lock (Sync)
        {
            try { OutputDevice.Dispose(); }
            finally { GuardedReader.Dispose(); }
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (Sync)
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
        }
        // Join the status poller first so it cannot touch the disposed device/reader.
        // Must not hold Sync here: the poll callback takes Sync, so joining it under the lock deadlocks.
        await PlayerStatusInvoker.DisposeAsync();
        lock (Sync)
        {
            try { OutputDevice.Dispose(); }
            finally { GuardedReader.Dispose(); }
        }
    }

    public void Play()
    {
        lock (Sync)
        {
            // Guard on the device's real state, not the polled CurrentState — the cache
            // is stale for up to one poll tick after Pause(), which would swallow a
            // resume issued within that window (issue #28).
            if (IsDisposed || OutputDevice.PlaybackState == PlaybackState.Playing)
                return;
            if (!IsStatusPollerStarted)
            {
                IsStatusPollerStarted = true; // Start the poller exactly once; PeriodicInvoker.Start spawns a new loop on every call
                PlayerStatusInvoker.Start();
            }
            OutputDevice.Play();
        }
    }

    public void Pause()
    {
        lock (Sync)
        {
            if (IsDisposed)
                return;
            OutputDevice.Pause();
        }
    }

    public void SetVolume(float volume)
    {
        lock (Sync)
        {
            if (IsDisposed)
                return;
            OutputDevice.Volume = volume;
        }
    }

    public void SetPosition(TimeSpan position)
    {
        if (position < TimeSpan.Zero)
            position = TimeSpan.Zero;
        else if (position > Duration)
            position = Duration;
        lock (Sync)
        {
            if (IsDisposed)
                return;
            AudioFileReader.CurrentTime = position;
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
            OnPlaybackError(e.Exception.Message);
    }

    private Task OnPlaybackStateUpdate()
    {
        // Compute state/position changes under the lock, then raise the events after releasing it.
        // Consumers (ChaptersPlayer) hold their own lock while calling into this player, and our
        // events call back into their handlers; raising while holding Sync inverts the lock order.
        AudioPlayerState? changedState = null;
        TimeSpan? changedPosition = null;
        lock (Sync)
        {
            if (IsDisposed)
                return Task.CompletedTask;
            var playerState = ToAudioPlayerState(OutputDevice.PlaybackState);
            if (playerState != CurrentState)
            {
                CurrentState = playerState;
                changedState = playerState;
            }
            var currentTime = AudioFileReader.CurrentTime;
            if (currentTime != CurrentPosition)
            {
                CurrentPosition = currentTime;
                changedPosition = currentTime;
            }
        }
        if (changedState.HasValue)
            PlaybackStateChanged?.Invoke(this, new AudioPlayStateEventArgs(changedState.Value));
        if (changedPosition.HasValue)
            PlaybackPositionChanged?.Invoke(this, new PlaybackPositionEventArgs(Duration, changedPosition.Value));
        return Task.CompletedTask;
    }

    private void OnPlaybackError(string message) =>
        PlaybackError?.Invoke(this, new MessageEventArgs(message));

    private static AudioPlayerState ToAudioPlayerState(PlaybackState playbackState) => playbackState switch
    {
        PlaybackState.Stopped => AudioPlayerState.Stopped,
        PlaybackState.Playing => AudioPlayerState.Playing,
        PlaybackState.Paused => AudioPlayerState.Paused,
        _ => throw new ArgumentOutOfRangeException(nameof(playbackState))
    };

    // WaveOutEvent's playback thread pulls samples on its own schedule and offers no way to wait
    // for it to go idle, so the reader can be disposed while one of its reads is still in flight;
    // MediaFoundation reacts to that with undefined behavior (COMException 0x8000FFFF
    // "Catastrophic failure"). This wrapper serializes reads against disposal and reports
    // end-of-stream once disposed.
    private sealed class DisposalGuardedSampleProvider(AudioFileReader source) : ISampleProvider, IDisposable
    {
        private AudioFileReader Source { get; } = source;
        private Lock Sync { get; } = new();
        private bool IsDisposed { get; set; } // Guarded by Sync

        public WaveFormat WaveFormat { get; } = source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            lock (Sync)
                return IsDisposed ? 0 : Source.Read(buffer, offset, count);
        }

        public void Dispose()
        {
            lock (Sync)
            {
                if (IsDisposed)
                    return;
                IsDisposed = true;
                Source.Dispose();
            }
        }
    }
}
