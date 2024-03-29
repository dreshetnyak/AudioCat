﻿using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using AudioCat.Models;

namespace AudioCat.ViewModels;

public sealed class AudioFileViewModel(IAudioFile audioFile, bool isTagsSource = false) : IAudioFile, INotifyPropertyChanged
{
    private bool _isTagsSource = isTagsSource;
    private IAudioFile AudioFile { get; } = audioFile;

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
    public IReadOnlyList<IMediaStream> Streams { get; } = GetStreams(audioFile);

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