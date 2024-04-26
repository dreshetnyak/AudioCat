using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using AudioCat.Models;

namespace AudioCat.ViewModels;

public sealed class MediaFileViewModel : IMediaFile, INotifyPropertyChanged
{
    private bool _isTagsSource;
    private bool _isCoverSource;
    private IMediaFile MediaFile { get; }

    public FileInfo File => MediaFile.File;
    public string FileName => MediaFile.FileName;
    public string FilePath => MediaFile.FilePath;
    public string? FormatName { get; }
    public string? FormatDescription => MediaFile.FormatDescription;
    public decimal? StartTime => MediaFile.StartTime;
    public TimeSpan? Duration => MediaFile.Duration;
    public decimal? Bitrate => MediaFile.Bitrate;
    public IReadOnlyList<KeyValuePair<string, string>> Tags => MediaFile.Tags;
    public IReadOnlyList<IMediaChapter> Chapters => MediaFile.Chapters;
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
    public bool HasTags => MediaFile.Tags.Count > 0;

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

    public MediaFileViewModel(IMediaFile mediaFile, bool isTagsSource = false, bool isCoverSource = false)
    {
        _isTagsSource = isTagsSource;
        _isCoverSource = isCoverSource;
        MediaFile = mediaFile;
        Streams = GetStreams(mediaFile);
        HasCover = HasImageStream(Streams);
        FormatName = Streams.Count == 1 && HasCover // Only one stream and has a cover - it is an image file
            ? MediaFile.Streams[0].CodecName
            : MediaFile.FormatName;
    }

    private static IReadOnlyList<string> SupportedImageCodecs { get; } = ["mjpeg", "png"];
    private static bool HasImageStream(IEnumerable<IMediaStream> streams)
    {
        foreach (var stream in streams)
        {
            if (stream.CodecName != null && SupportedImageCodecs.Contains(stream.CodecName))
                return true;
        }

        return false;
    }

    private static IMediaStream[] GetStreams(IMediaFile mediaFile)
    {
        var streamsCount = mediaFile.Streams.Count;
        var streams = new IMediaStream[streamsCount];
        for (var i = 0; i < streamsCount; i++) 
            streams[i] = new MediaStreamViewModel(mediaFile.Streams[i]);
        return streams;
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}