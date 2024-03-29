﻿using System.IO;

namespace AudioCat.Models;

public interface IAudioStream
{
    int Index { get; }
    string? CodecName { get; }
    string? CodecDescription { get; }
    string? CodecType { get; }
    string? CodecTag { get; }
    decimal? SampleRate { get; } //44100
    decimal? Channels { get; }
    string? ChannelLayout { get; }
    decimal? StartTime { get; }
    TimeSpan? Duration { get; }
    int? Width { get; }
    int? Height { get; }
    IReadOnlyList<KeyValuePair<string, string>> Tags { get; }
}

public interface IAudioChapter
{
    int Id { get; }
    long? Start { get; }
    long? End { get; }
    TimeSpan? StartTime { get; }
    TimeSpan? EndTime { get; }
    IReadOnlyList<KeyValuePair<string, string>> Tags { get; }
}

public interface IAudioFile
{
    FileInfo File { get; }
    string FileName { get; }
    string FilePath { get; }
    string? FormatName { get; }
    string? FormatDescription { get; }
    decimal? StartTime { get; }
    TimeSpan? Duration { get; }
    decimal? Bitrate { get; }
    IReadOnlyList<KeyValuePair<string, string>> Tags { get; }
    IReadOnlyList<IAudioChapter> Chapters { get; }
    IReadOnlyList<IAudioStream> Streams { get; }
}