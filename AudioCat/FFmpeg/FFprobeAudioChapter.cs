using AudioCat.Models;
using System.Globalization;
using System.Xml.Linq;

namespace AudioCat.FFmpeg;

public class FFprobeAudioChapter : IAudioChapter
{
    public int Id { get; private init; }
    public long? Start { get; private init; }
    public long? End { get; private init; }
    public TimeSpan? StartTime { get; private init; }
    public TimeSpan? EndTime { get; private init; }
    public IReadOnlyList<KeyValuePair<string, string>> Tags { get; private init; } = [];

    private FFprobeAudioChapter() { }

    public static IResponse<IAudioChapter> Create(XElement chapterElement)
    {
        var idStr = chapterElement.Attribute("id")?.Value;
        if (string.IsNullOrEmpty(idStr))
            return Response<IAudioChapter>.Failure("The 'id' attribute is missing in the chapter element");
        if (!int.TryParse(idStr, NumberStyles.Integer, CultureInfo.CurrentCulture, out var id))
            return Response<IAudioChapter>.Failure("The 'id' attribute of a chapter element can't be parsed to an integer");
        return Response<IAudioChapter>.Success(new FFprobeAudioChapter
        {
            Id = id,
            Start = chapterElement.Attribute("start")?.Value.ToLong(),
            End = chapterElement.Attribute("end")?.Value.ToLong(),
            StartTime = chapterElement.Attribute("start_time")?.Value.SecondsToTimeSpan(),
            EndTime = chapterElement.Attribute("end_time")?.Value.SecondsToTimeSpan(),
            Tags = chapterElement.GetTags()
        });
    }
}