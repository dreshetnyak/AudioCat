using AudioCat.Models;
using System.IO;
using System.Xml.Linq;

namespace AudioCat.FFmpeg;

public class FFprobeMediaFile : IMediaFile
{
    public FileInfo File { get; private init; }
    public string FileName => File.Name ?? "";
    public string FilePath => File.FullName ?? "";
    public string? FormatName { get; private init; }
    public string? FormatDescription { get; private init; }
    public decimal? StartTime { get; private init; }
    public TimeSpan? Duration { get; private init; }
    public decimal? Bitrate { get; private init; }
    public IReadOnlyList<KeyValuePair<string, string>> Tags { get; private init; } = [];
    public IReadOnlyList<IMediaChapter> Chapters { get; private init; } = [];
    public IReadOnlyList<IMediaStream> Streams { get; private init; } = [];

    private FFprobeMediaFile(FileInfo file) { File = file; }

    public static IResponse<IMediaFile> Create(string fileFullName, string probeXmlResponse)
    {
        if (string.IsNullOrEmpty(fileFullName))
            return Response<IMediaFile>.Failure("Missing the file name");
        var fileInfo = new FileInfo(fileFullName);
        if (!fileInfo.Exists)
            return Response<IMediaFile>.Failure($"The file {fileFullName.ToQuoted()} doesn't exist");

        XElement response;
        try { response = XElement.Parse(probeXmlResponse); }
        catch (Exception ex) { return Response<IMediaFile>.Failure($"Failed to parse FFprobe response to XML: {ex.Message}"); }

        var format = response.Element("format");
        return Response<IMediaFile>.Success(new FFprobeMediaFile(fileInfo)
        {
            FormatName = format?.Attribute("format_name")?.Value,
            FormatDescription = format?.Attribute("format_long_name")?.Value,
            StartTime = format?.Attribute("start_time")?.Value.ToDecimal(),
            Duration = format?.Attribute("duration")?.Value.SecondsToTimeSpan(),
            Bitrate = format?.Attribute("bit_rate")?.Value.ToDecimal(),
            Tags = format.GetTags(),
            Chapters = GetChapters(response.Element("chapters")),
            Streams = GetStreams(response.Element("streams"))
        });
    }

    private static IReadOnlyList<IMediaChapter> GetChapters(XElement? chaptersContainerElement)
    {
        if (chaptersContainerElement is not { HasElements: true })
            return [];

        var chapters = new List<IMediaChapter>();
        foreach (var chapterElement in chaptersContainerElement.Elements("chapter"))
        {
            var chapterResponse = FFprobeMediaChapter.Create(chapterElement);
            if (chapterResponse.IsSuccess)
                chapters.Add(chapterResponse.Data!);
        }

        return chapters;
    }

    private static IReadOnlyList<IMediaStream> GetStreams(XElement? streamsContainerElement)
    {
        if (streamsContainerElement is not { HasElements: true })
            return [];

        var chapters = new List<IMediaStream>();
        foreach (var chapterElement in streamsContainerElement.Elements("stream"))
        {
            var chapterResponse = FFprobeMediaStream.Create(chapterElement);
            if (chapterResponse.IsSuccess)
                chapters.Add(chapterResponse.Data!);
        }

        return chapters;
    }
}