using AudioCat.Models;
using System.Globalization;
using System.Xml.Linq;

namespace AudioCat.FFmpeg;

public class FFprobeMediaChapter : IMediaChapter
{
    public int Id { get; private init; }
    public long? Start { get; private init; }
    public long? End { get; private init; }
    public decimal? TimeBaseDivident { get; private init; }
    public decimal? TimeBaseDivisor { get; private init; }
    public TimeSpan? StartTime { get; private init; }
    public TimeSpan? EndTime { get; private init; }
    public IReadOnlyList<IMediaTag> Tags { get; private init; } = [];

    private FFprobeMediaChapter() { }

    public static IResponse<IMediaChapter> Create(XElement chapterElement)
    {
        var idStr = chapterElement.Attribute("id")?.Value;
        if (string.IsNullOrEmpty(idStr))
            return Response<IMediaChapter>.Failure("The 'id' attribute is missing in the chapter element");
        if (!int.TryParse(idStr, NumberStyles.Integer, CultureInfo.CurrentCulture, out var id))
            return Response<IMediaChapter>.Failure("The 'id' attribute of a chapter element can't be parsed to an integer");

        var (timeBaseDivident, timeBadeDivisor) = GetTimeBase(chapterElement.Attribute("time_base")?.Value);
        return Response<IMediaChapter>.Success(new FFprobeMediaChapter
        {
            Id = id,
            Start = chapterElement.Attribute("start")?.Value.ToLong(),
            End = chapterElement.Attribute("end")?.Value.ToLong(),
            TimeBaseDivident = timeBaseDivident,
            TimeBaseDivisor = timeBadeDivisor,
            StartTime = chapterElement.Attribute("start_time")?.Value.SecondsToTimeSpan(),
            EndTime = chapterElement.Attribute("end_time")?.Value.SecondsToTimeSpan(),
            Tags = chapterElement.GetTags()
        });
    }

    private static (decimal? divident, decimal? divisor) GetTimeBase(string? timeBase)
    {
        if (timeBase == null)
            return (null, null);
        var splitTimeBase = timeBase.Split('/');
        return
            splitTimeBase.Length == 2 &&
            decimal.TryParse(splitTimeBase[0], out var divident) &&
            decimal.TryParse(splitTimeBase[1], out var divisor)
                ? (divident, divisor)
                : (null, null);
    }
}