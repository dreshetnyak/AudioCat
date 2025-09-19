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
    void SetPosition(string filePath, TimeSpan position);
    event EventHandler<StreamVolumeEventArgs>? PlaybackVolume;
    event EventHandler<AudioPlayStateEventArgs>? PlaybackStateChanged;
    event EventHandler<PlaybackPositionEventArgs>? PlaybackPositionChanged;
    event EventHandler<MessageEventArgs>? PlaybackError;
}
#endregion

internal sealed class AudioFilePlayer : IAudioFilePlayer, IAsyncDisposable, IDisposable
{
    #region Backing Fields
    private AudioPlayerState _currentState = AudioPlayerState.Stopped;
    private TimeSpan _currentPosition = TimeSpan.Zero;

    #endregion

    private bool IsDisposed { get; set; }
    private SemaphoreSlim Sync { get; }
    private WaveOutEvent OutputDevice { get; }
    private AudioFileReader AudioFileReader { get; }
    private MeteringSampleProvider MeteringProvider { get; }
    private PeriodicInvoker PlayerStatusInvoker { get; }
    private AudioPlayerState CurrentState
    {
        get => _currentState;
        set
        {
            if (_currentState == value)
                return; 
            _currentState = value;
            OnPlaybackStateChanged();
        }
    }
    private TimeSpan CurrentPosition
    {
        get => _currentPosition;
        set
        {
            if (_currentPosition == value)
                return;
            _currentPosition = value;
            OnPlaybackPositionChanged();
        }
    }
    private TimeSpan Duration { get; }

    public event EventHandler<StreamVolumeEventArgs>? PlaybackVolume;
    public event EventHandler<AudioPlayStateEventArgs>? PlaybackStateChanged;
    public event EventHandler<PlaybackPositionEventArgs>? PlaybackPositionChanged;
    public event EventHandler<MessageEventArgs>? PlaybackError;

    private AudioFilePlayer(string audioFile)
    {
        Sync = new SemaphoreSlim(1, 1);
        OutputDevice = new WaveOutEvent();
        AudioFileReader = new AudioFileReader(audioFile);
        MeteringProvider = new MeteringSampleProvider(AudioFileReader);
        MeteringProvider.StreamVolume += (sender, e) => PlaybackVolume?.Invoke(sender, new StreamVolumeEventArgs(e.MaxSampleValues));
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
        if (IsDisposed)
            return;
        IsDisposed = true;
        Sync.Dispose();
        OutputDevice.Dispose();
        AudioFileReader.Dispose();
        PlayerStatusInvoker.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        await CastAndDispose(Sync);
        await CastAndDispose(OutputDevice);
        await AudioFileReader.DisposeAsync();
        await PlayerStatusInvoker.DisposeAsync();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    public void Play()
    {
        if (CurrentState == AudioPlayerState.Playing)
            return;
        PlayerStatusInvoker.Start();
        OutputDevice.Play();
    }

    public void Pause() => 
        OutputDevice.Pause();

    public void SetVolume(float volume) => 
        OutputDevice.Volume = volume;

    public void SetPosition(string filePath, TimeSpan position)
    {
        if (position < TimeSpan.Zero)
            position = TimeSpan.Zero;
        else if (position > Duration)
            position = Duration;
        AudioFileReader.CurrentTime = position;
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
            OnPlaybackError(e.Exception.Message);
    }
    
    private Task OnPlaybackStateUpdate()
    {
        var playerState = ToAudioPlayerState(OutputDevice.PlaybackState);
        if (playerState != CurrentState)
            CurrentState = playerState;
        var currentTime = AudioFileReader.CurrentTime;
        if (currentTime != CurrentPosition)
            CurrentPosition = currentTime;
        return Task.CompletedTask;
    }

    private void OnPlaybackStateChanged() => 
        PlaybackStateChanged?.Invoke(this, new AudioPlayStateEventArgs(CurrentState));
    
    private void OnPlaybackPositionChanged() =>
        PlaybackPositionChanged?.Invoke(this, new PlaybackPositionEventArgs(Duration, CurrentPosition));

    private void OnPlaybackError(string message) =>
        PlaybackError?.Invoke(this, new MessageEventArgs(message));

    private static AudioPlayerState ToAudioPlayerState(PlaybackState playbackState) => playbackState switch
    {
        PlaybackState.Stopped => AudioPlayerState.Stopped,
        PlaybackState.Playing => AudioPlayerState.Playing,
        PlaybackState.Paused => AudioPlayerState.Paused,
        _ => throw new ArgumentOutOfRangeException(nameof(playbackState))
    };
}


//internal interface IAudioChapter
//{
//    string Title { get; }
//    string FilePath { get; }
//    TimeSpan StartTime { get; }
//}

//public ObservableCollection<IAudioChapter> Chapters { get; } = new();
//float GetSystemVolume();

//internal interface IAudioPlayerSettings
//{
//    float TargetSystemVolume { get; }
//}

//// Start the timer to update the playback position
//timer.Start();

//// Display audio format information
//SampleRateLabel.Content = $"Sample Rate: {AudioFile.WaveFormat.SampleRate} Hz";
//ChannelsLabel.Content = $"Channels: {AudioFile.WaveFormat.Channels}";

//private float GetStartingVolume()
//{
//    // TODO Get the system volume and target the starting volume to be 60% of the system volume.
//    // TODO The calculated or then changed volume should be remembered and used while the application is running.
//}

//private float GetSystemVolume()
//{
//    using var deviceEnumerator = new MMDeviceEnumerator();
//    using var defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
//    return defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar; // Return the current master volume level (between 0.0 and 1.0)
//}


//public void Play()
//{
//    OutputDevice.Play();
//}

//private void Stop_Click(object sender, RoutedEventArgs e)
//{
//    OutputDevice?.Stop();
//    OutputDevice?.Dispose();
//    OutputDevice = null;
//    AudioFile?.Dispose();
//    AudioFile = null;
//    Timer.Stop();

//    // Reset labels
//    PositionLabel.Content = "00:00 / 00:00";
//}