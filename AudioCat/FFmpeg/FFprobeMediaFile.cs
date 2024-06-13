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
    public IReadOnlyList<IMediaTag> Tags { get; private init; } = [];
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

        var formatElement = response.Element("format");
        var streamsElement = response.Element("streams");
        var streams = GetStreams(streamsElement);
        return Response<IMediaFile>.Success(new FFprobeMediaFile(fileInfo)
        {
            FormatName = formatElement?.Attribute("format_name")?.Value,
            FormatDescription = formatElement?.Attribute("format_long_name")?.Value,
            StartTime = formatElement?.Attribute("start_time")?.Value.ToDecimal(),
            Duration = formatElement?.Attribute("duration")?.Value.SecondsToTimeSpan(),
            Bitrate = formatElement?.Attribute("bit_rate")?.Value.ToDecimal(),
            Tags = GetFileTags(formatElement, streamsElement, streams),
            Chapters = GetChapters(response.Element("chapters")),
            Streams = streams
        });
    }

    private static IReadOnlyList<IMediaTag> GetFileTags(XElement? formatElement, XElement? streamsElement, IReadOnlyList<IMediaStream> streams)
    {
        var tags = formatElement.GetTags();
        if (tags.Count > 0 || streamsElement == null)
            return tags;

        foreach (var streamElement in streamsElement.Elements("stream"))
        {
            if (Settings.CodecsWithTagsInStream.Has(streamElement.Attribute("codec_name")?.Value ?? ""))
                return streamElement.GetTags();
        }

        return tags;
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