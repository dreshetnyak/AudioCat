using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using AudioCat.Models;

namespace AudioCat.ViewModels;

public interface IMediaFileViewModel : INotifyPropertyChanged
{
    FileInfo File { get; }
    string FileName { get; }
    string FilePath { get; }
    string? FormatName { get; }
    string? FormatDescription { get; }
    decimal? StartTime { get; }
    TimeSpan? Duration { get; }
    decimal? Bitrate { get; }
    ObservableCollection<IMediaTagViewModel> Tags { get; }
    ObservableCollection<IMediaChapterViewModel> Chapters { get; }
    IReadOnlyList<IMediaStream> Streams { get; }

    bool IsImage { get; }

    bool HasTags { get; }

    bool IsCoverSource { get; set; }
    bool HasCover { get; }
}

[DebuggerDisplay("{FileName}: FormatName: {FormatName,nq}; Duration: {Duration,nq}; Bitrate: {Bitrate,nq}; IsImage: {IsImage,nq}")]
public sealed class MediaFileViewModel : IMediaFileViewModel
{
    private bool _isCoverSource;
    private IMediaFile MediaFile { get; }

    public FileInfo File => MediaFile.File;
    public string FileName => MediaFile.FileName;
    public string FilePath => MediaFile.FilePath;
    public string? FormatName { get; }
    public string? FormatDescription => MediaFile.FormatDescription;
    public decimal? StartTime => MediaFile.StartTime;
    public TimeSpan? Duration { get; } 
    public decimal? Bitrate { get; }
    public ObservableCollection<IMediaTagViewModel> Tags { get; }
    public ObservableCollection<IMediaChapterViewModel> Chapters { get; }
    public IReadOnlyList<IMediaStream> Streams { get; }

    public bool IsImage { get; }

    public bool HasTags => Tags.Count > 0;

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

    public MediaFileViewModel(IMediaFile mediaFile, bool isCoverSource = false)
    {
        _isCoverSource = isCoverSource;
        MediaFile = mediaFile;
        Streams = GetStreams(mediaFile);
        HasCover = HasImageStream(Streams);
        IsImage = Streams.Count == 1 && HasCover;
        if (IsImage)
        {
            FormatName = MediaFile.Streams[0].CodecName;
            Bitrate = null;
            Duration = null;
        }
        else
        {
            FormatName = MediaFile.FormatName;
            Bitrate = MediaFile.Bitrate;
            Duration = MediaFile.Duration;
        }

        Tags = new ObservableCollection<IMediaTagViewModel>();
        foreach (var tag in MediaFile.Tags) 
            Tags.Add(TagViewModel.CreateFrom(tag));

        Chapters = new ObservableCollection<IMediaChapterViewModel>();
        foreach (var chapter in MediaFile.Chapters)
            Chapters.Add(ChapterViewModel.CreateFrom(chapter));

        Tags.CollectionChanged += OnTagsCollectionChanged;
    }

    private void OnTagsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs _)
    {
        OnPropertyChanged(nameof(HasTags));
    }

    private static IEnumerable<string> SupportedImageCodecs { get; } = ["mjpeg", "png"];
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