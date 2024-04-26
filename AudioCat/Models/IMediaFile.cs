using System.IO;

namespace AudioCat.Models;

public interface IMediaStream
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

public interface IMediaChapter
{
    int Id { get; }
    long? Start { get; }
    long? End { get; }
    decimal? TimeBaseDivident { get; }
    decimal? TimeBaseDivisor { get; }
    TimeSpan? StartTime { get; }
    TimeSpan? EndTime { get; }
    IReadOnlyList<KeyValuePair<string, string>> Tags { get; }
}

public interface IMediaFile
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
    IReadOnlyList<IMediaChapter> Chapters { get; }
    IReadOnlyList<IMediaStream> Streams { get; }
}