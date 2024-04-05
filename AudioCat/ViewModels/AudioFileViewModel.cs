using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using AudioCat.Models;

namespace AudioCat.ViewModels;

public sealed class AudioFileViewModel : IAudioFile, INotifyPropertyChanged
{
    private bool _isTagsSource;
    private bool _isCoverSource;
    private IAudioFile AudioFile { get; }

    public FileInfo File => AudioFile.File;
    public string FileName => AudioFile.FileName;
    public string FilePath => AudioFile.FilePath;
    public string? FormatName => AudioFile.FormatName;
    public string? FormatDescription => AudioFile.FormatDescription;
    public decimal? StartTime => AudioFile.StartTime;
    public TimeSpan? Duration => AudioFile.Duration;
    public decimal? Bitrate => AudioFile.Bitrate;
    public IReadOnlyList<KeyValuePair<string, string>> Tags => AudioFile.Tags;
    public IReadOnlyList<IMediaChapter> Chapters => AudioFile.Chapters;
    public IReadOnlyList<IMediaStream> Streams { get; }

    public bool IsTagsSource
    {
        get => _isTagsSource;
        set
        {
            if (value == _isTagsSource) 
                return;
            _isTagsSource = value;
            OnPropertyChanged();
        }
    }
    public bool HasTags => AudioFile.Tags.Count > 0;

    public bool IsCoverSource
    {
        get => _isCoverSource;
        set
        {
            if (value == _isCoverSource) 
                return;
            _isCoverSource = value;
            OnPropertyChanged();
        }
    }
    public bool HasCover { get; }

    public AudioFileViewModel(IAudioFile audioFile, bool isTagsSource = false, bool isCoverSource = false)
    {
        _isTagsSource = isTagsSource;
        _isCoverSource = isCoverSource;
        AudioFile = audioFile;
        Streams = GetStreams(audioFile);
        HasCover = HasCoverStream(Streams);
    }

    private static bool HasCoverStream(IEnumerable<IMediaStream> streams)
    {
        foreach (var stream in streams)
        {
            if (stream.CodecName != null && stream.CodecName.Equals("mjpeg", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static IMediaStream[] GetStreams(IAudioFile audioFile)
    {
        var streamsCount = audioFile.Streams.Count;
        var streams = new IMediaStream[streamsCount];
        for (var i = 0; i < streamsCount; i++) 
            streams[i] = new MediaStreamViewModel(audioFile.Streams[i]);
        return streams;
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}