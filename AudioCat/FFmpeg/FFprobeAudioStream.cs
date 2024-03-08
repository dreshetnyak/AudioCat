using AudioCat.Models;
using System.Globalization;
using System.Xml.Linq;

namespace AudioCat.FFmpeg;

public class FFprobeAudioStream : IAudioStream
{
    public int Index { get; private init; }
    public string? CodecName { get; private init; }
    public string? CodecDescription { get; private init; }
    public string? CodecType { get; private init; }
    public string? CodecTag { get; private init; }
    public decimal? SampleRate { get; private init; }
    public decimal? Channels { get; private init; }
    public string? ChannelLayout { get; private init; }
    public decimal? StartTime { get; private init; }
    public TimeSpan? Duration { get; private init; }
    public int? Width { get; private init; }
    public int? Height { get; private init; }
    public IReadOnlyList<KeyValuePair<string, string>> Tags { get; private init; } = [];

    private FFprobeAudioStream() { }

    public static IResponse<IAudioStream> Create(XElement streamElement)
    {
        var indexStr = streamElement.Attribute("index")?.Value;
        if (string.IsNullOrEmpty(indexStr))
            return Response<IAudioStream>.Failure("The 'index' attribute is missing in the stream element");
        if (!int.TryParse(indexStr, NumberStyles.Integer, CultureInfo.CurrentCulture, out var index))
            return Response<IAudioStream>.Failure("The 'index' attribute of a stream element can't be parsed to an integer");
        return Response<IAudioStream>.Success(new FFprobeAudioStream
        {
            Index = index,
            CodecName = streamElement.Attribute("codec_name")?.Value,
            CodecDescription = streamElement.Attribute("codec_long_name")?.Value,
            CodecType = streamElement.Attribute("codec_type")?.Value,
            CodecTag = streamElement.Attribute("codec_tag_string")?.Value,
            SampleRate = streamElement.Attribute("sample_rate")?.Value.ToDecimal(),
            Channels = streamElement.Attribute("channels")?.Value.ToDecimal(),
            ChannelLayout = streamElement.Attribute("channel_layout")?.Value,
            StartTime = streamElement.Attribute("start_time")?.Value.ToDecimal(),
            Duration = streamElement.Attribute("duration")?.Value.SecondsToTimeSpan(),
            Width = streamElement.Attribute("width")?.Value.ToInt(),
            Height = streamElement.Attribute("height")?.Value.ToInt(),
            Tags = streamElement.GetTags()
        });
    }
}