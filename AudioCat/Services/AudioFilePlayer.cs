using System.Windows.Threading;
using AudioCat.Models;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace AudioCat.Services;

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

internal class StreamVolumeEventArgs(float[] maxSampleValues) : EventArgs
{
    public float[] MaxSampleValues { get; } = maxSampleValues;
}

internal interface IAudioFilePlayer
{
    void Play(string filePath, TimeSpan startTime);
    void Pause();
    void SetVolume(float volume);
    void SetPosition(string filePath, TimeSpan position);
    event EventHandler<StreamVolumeEventArgs>? StreamVolume;
    event EventHandler<AudioPlayStateEventArgs>? StateChanged;
    event EventHandler<MessageEventArgs>? PlaybackError;
}

internal sealed class AudioFilePlayer : IAudioFilePlayer, IAsyncDisposable, IDisposable
{
    private bool IsDisposed { get; set; }
    private SemaphoreSlim Sync { get; set; }
    private WaveOutEvent OutputDevice { get; set; }
    private AudioFileReader AudioFileReader { get; set; }
    private DispatcherTimer PlayerTimer { get; set; }
    private MeteringSampleProvider MeteringProvider { get; set; }

    public event EventHandler<StreamVolumeEventArgs>? StreamVolume;     // Play volume metering
    public event EventHandler<AudioPlayStateEventArgs>? StateChanged;
    public event EventHandler<MessageEventArgs>? PlaybackError;

    private AudioFilePlayer(string audioFile)
    {
        Sync = new SemaphoreSlim(1, 1);
        OutputDevice = new WaveOutEvent();
        AudioFileReader = new AudioFileReader(audioFile);
        PlayerTimer = new DispatcherTimer();
        MeteringProvider = new MeteringSampleProvider(AudioFileReader);
        MeteringProvider.StreamVolume += (sender, e) => StreamVolume?.Invoke(sender, new StreamVolumeEventArgs(e.MaxSampleValues));
        OutputDevice.PlaybackStopped += OnPlaybackStopped;
        OutputDevice.Init(MeteringProvider);
    }

    public static IResponse<IAudioFilePlayer> Create(string audioFile)
    {
        try 
        { return Response<IAudioFilePlayer>.Success(new AudioFilePlayer(audioFile)); }
        catch (Exception ex) 
        { return Response<IAudioFilePlayer>.Failure(ex.Message); }
    }

    public void Play(string filePath, TimeSpan startTime)
    {
        OutputDevice.Play();
    }

    public void Pause()
    {
        OutputDevice.Pause();
    }

    public void SetVolume(float volume) => 
        OutputDevice.Volume = volume;

    public void SetPosition(string filePath, TimeSpan position)
    {
        if (position < TimeSpan.Zero)
            position = TimeSpan.Zero;
        else if (position > AudioFileReader.TotalTime)
            position = AudioFileReader.TotalTime;
        AudioFileReader.CurrentTime = position;
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // if (e.Exception != null) e.Exception.Message
        // OnPlaybackError
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
    
    private void TimerTick(object sender, EventArgs e)
    {
        if (audioFile != null)
        {
            var currentTime = AudioFile.CurrentTime;
            var totalTime = AudioFile.TotalTime;
            //PositionLabel.Content = $"{currentTime:mm\\:ss} / {totalTime:mm\\:ss}";
        }
    }

    private void OnPlaybackError(string message) => 
        PlaybackError?.Invoke(this, new MessageEventArgs(message));
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