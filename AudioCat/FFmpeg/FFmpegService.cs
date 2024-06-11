using AudioCat.Models;
using AudioCat.Services;
using System.IO;
using System.Text;
using AudioCat.ViewModels;
using System.Collections.ObjectModel;

namespace AudioCat.FFmpeg;

internal class FFmpegService : IMediaFileToolkitService
{
    private static IReadOnlyList<string> StatusLineContent { get; } = ["size=", "time=", "bitrate=", "speed="];
    
    public async Task<IResponse<IMediaFile>> Probe(string fileFullName, CancellationToken ctx)
    {
        try
        {
            var probeResponse = await Process.Run(
                "ffprobe.exe",
                $"-hide_banner -show_format -show_chapters -show_streams -show_private_data -print_format xml -i \"{fileFullName}\"",
                Process.OutputType.Standard,
                ctx);

            var createResponse = FFprobeMediaFile.Create(fileFullName, probeResponse);
            return createResponse.IsSuccess
                ? Response<IMediaFile>.Success(createResponse.Data!)
                : Response<IMediaFile>.Failure(createResponse);
        }
        catch (Exception ex)
        {
            return Response<IMediaFile>.Failure(ex.Message);
        }
    }

    public async Task<IResult> Concatenate(IReadOnlyList<IMediaFileViewModel> mediaFiles, IConcatParams concatParams, string outputFileName, Action<IProcessingStats> onStatusUpdate, CancellationToken ctx)
    {
        var errorMessage = new StringBuilder();
        try
        {
            var extractImagesTask = ExtractImages(mediaFiles, ctx);
            var metadataFileTask = CreateMetadataFile(mediaFiles, concatParams, ctx);
            var listFileTask = CreateFilesListFile(mediaFiles);

            var listFile = await listFileTask;
            var metadataFile = await metadataFileTask;
            var extractedImages = await extractImagesTask;

            var hasImages = extractedImages.Count > 0;
            var outputToFile = hasImages ? await GenerateTempOutputFileFrom(outputFileName) : outputFileName;
            var args = metadataFile != ""
                ? $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -i \"{metadataFile}\" -map_metadata 1 -vn -c copy -id3v2_version 3 -write_id3v1 1 -update true \"{outputToFile}\""
                : $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -vn -c copy -update true \"{outputToFile}\"";

            await Process.Run(
                "ffmpeg.exe",
                args,
                OnStatus,
                Process.OutputType.Error,
                ctx);

            if (hasImages)
            {
                var imagesResult = await AddImages(outputToFile, extractedImages, outputFileName, ctx);
                if (imagesResult.IsFailure)
                    errorMessage.AppendLine(imagesResult.Message);
            }

            #region Delete Temporary Files
            // Delete the list file
            try { await Task.Run(() => File.Delete(listFile), CancellationToken.None); }
            catch { /* ignore */ }

            // Delete the metadata file
            if (metadataFile != "")
            {
                try { await Task.Run(() => File.Delete(metadataFile), CancellationToken.None); }
                catch { /* ignore */ }
            }

            foreach (var extractedImage in extractedImages)
            {
                if (!extractedImage.IsTemporaryFile)
                    continue;
                try { await Task.Run(() => File.Delete(extractedImage.Path), CancellationToken.None); }
                catch { /* ignore */ }
            }

            #endregion

            if (errorMessage.Length == 0)
                return Result.Success();

            var outputFile = new FileInfo(outputFileName);
            if (outputFile is { Exists: true, Length: 0 })
            {
                try { await Task.Run(() => File.Delete(outputFileName), CancellationToken.None); }
                catch { /* ignore */ }
            }

            return Result.Failure(errorMessage.ToString());
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }

        void OnStatus(string status)
        {
            if (IsErrorToIgnore(status))
                return;
            if (IsErrorMessage(status))
                errorMessage.AppendLine(status);
            else
                onStatusUpdate(new FFmpegStats(status));
        }

        static bool IsErrorMessage(string status) =>
            status.StartsWith('[') || !StatusLineContent.Any(status.Contains);
    }

    private static async Task<string> GenerateTempOutputFileFrom(string outputFileName)
    {
        var tryCount = 0;
        while (tryCount++ < 3)
        {
            try
            {
                var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(outputFileName));
                await File.WriteAllBytesAsync(filePath, []);
                return filePath;
            }
            catch { /* ignore */ }
        }

        return "";
    }

    private static async Task<string> CreateFilesListFile(IEnumerable<IMediaFileViewModel> mediaFiles)
    {
        var listFile = Path.GetTempFileName();
        var sb = new StringBuilder();
        sb.AppendLine("ffconcat version 1.0");
        foreach (var mediaFile in mediaFiles)
        {
            if (mediaFile.IsImage)
                continue;
            var fileName = EscapeFilePath(mediaFile.File.FullName);
            sb.AppendLine($"file \'{fileName}\'");
        }

        await File.WriteAllTextAsync(listFile, sb.ToString());
        return listFile;

        static string EscapeFilePath(string path) => path.Replace("\\", "/").Replace("'", "'\\''");
    }

    private const string METADATA_FILE_START = ";FFMETADATA1\n";
    private static async Task<string> CreateMetadataFile(IReadOnlyList<IMediaFileViewModel> mediaFiles, IConcatParams concatParams, CancellationToken ctx)
    {
        var tagsMetadata = concatParams.TagsEnabled ? GetTagsMetadata(mediaFiles) : ""; 
        var chaptersMetadata = concatParams.ChaptersEnabled ? GetChaptersMetadata(mediaFiles) : "";
        if (tagsMetadata.Length == 0 && chaptersMetadata.Length == 0)
            return "";

        var metadataFile = Path.GetTempFileName();
        try
        {
            var utf8WithoutBom = new UTF8Encoding(false);
            await using var writer = new StreamWriter(metadataFile, false, utf8WithoutBom);
            await writer.WriteAsync(METADATA_FILE_START);
            if (tagsMetadata.Length > 0)
                await writer.WriteAsync(tagsMetadata);
            if (chaptersMetadata.Length > 0)
                await writer.WriteAsync(chaptersMetadata);

            return metadataFile;
        }
        catch
        {
            try { await Task.Run(() => File.Delete(metadataFile), CancellationToken.None); }
            catch { /* ignore */ }
            return "";
        }
    }

    private static string GetTagsMetadata(IEnumerable<IMediaFileViewModel> mediaFiles)
    {
        var mediaFile = mediaFiles.FirstOrDefault(mediaFile => mediaFile.IsTagsSource);
        return mediaFile != null
            ? GetTagsMetadata(mediaFile.Tags)
            : "";
    }

    private static string GetTagsMetadata(IEnumerable<IMediaTagViewModel> tags)
    {
        var tagsMetadata = new StringBuilder(8192);
        foreach (var tag in tags)
        {
            if (!tag.IsEnabled)
                continue;
            var name = tag.Name.FilterPrintable().Trim();
            if (name == "")
                continue;

            tagsMetadata.Append(name);
            tagsMetadata.Append('=');
            tagsMetadata.Append(FilterMetadataValue(tag.Value));
            tagsMetadata.Append('\n');
        }

        return tagsMetadata.ToString();
    }

    private static string GetChaptersMetadata(IEnumerable<IMediaFileViewModel> mediaFiles)
    {
        var startTime = TimeSpan.Zero;
        var chapters = new StringBuilder();
        foreach (var file in mediaFiles)
        {
            if (!file.Duration.HasValue)
                continue;

            foreach (var chapter in file.Chapters)
                chapters.Append(GetChapterMetadata(chapter, startTime));

            startTime = startTime.Add(file.Duration.Value);
        }

        return chapters.ToString();
    }

    private static string GetChapterMetadata(IMediaChapter chapter, TimeSpan startTime)
    {
        if (!chapter.Start.HasValue ||
            !chapter.End.HasValue ||
            chapter.TimeBaseDivident is not > 0 ||
            chapter.TimeBaseDivisor is not > 0 ||
            chapter.Tags.Count == 0)
            return "";

        var divident = chapter.TimeBaseDivident.Value;
        var divisor = chapter.TimeBaseDivisor.Value;
        var multiplier = divident / divisor;

        var startSeconds = chapter.Start.Value * multiplier;
        var relativeStart = TimeSpan.FromSeconds((double)startSeconds);
        var absoluteStart = relativeStart.Add(startTime);

        var endSeconds = chapter.End.Value * multiplier;
        var relativeEnd = TimeSpan.FromSeconds((double)endSeconds);
        var absoluteEnd = relativeEnd.Add(startTime);

        var calculatedStart = (long)((decimal)absoluteStart.TotalSeconds * 1000m);
        var calculatedEnd = (long)((decimal)absoluteEnd.TotalSeconds * 1000m);

        var chapters = new StringBuilder(256);
        chapters.Append("[CHAPTER]\n");
        chapters.Append("TIMEBASE=1/1000\n");
        chapters.Append($"START={calculatedStart}\n");
        chapters.Append($"END={calculatedEnd}\n");
        foreach (var tag in chapter.Tags)
            chapters.Append($"{tag.Name}={tag.Value}\n");

        return chapters.ToString();
    }

    private static string FilterMetadataValue(string name)
    {
        var valueBuilder = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            switch (ch)
            {
                case '\r': break;
                case '\n': valueBuilder.Append("\\\n"); break;
                case '\t': valueBuilder.Append('\t'); break;
                case '=': valueBuilder.Append("\\="); break;
                case ';': valueBuilder.Append("\\;"); break;
                case '#': valueBuilder.Append("\\#"); break;
                case '\\': valueBuilder.Append(@"\\"); break;
                default:
                    if (ch.IsPrintable())
                        valueBuilder.Append(ch);
                    break;
            }
        }

        return valueBuilder.ToString();
    }

    private static async Task<IResult> AddImages(string audioFile, IReadOnlyList<ImageFile> audioFileImages, string outputFile, CancellationToken ctx)
    {
        try
        {
            var imageFilesQuery = GetImageFileQuery(audioFileImages);
            var mappingQuery = GetMappingQuery(audioFileImages.Count);
            var metadataQuery = GetMetadataQuery(audioFileImages);
            var response = await Process.Run(
                "ffmpeg.exe",
                $"-hide_banner -y -loglevel error -i \"{audioFile}\"{imageFilesQuery} -c copy -map 0:a{mappingQuery}{metadataQuery} -id3v2_version 3 -write_id3v1 1 -disposition:v attached_pic \"{outputFile}\"",
                Process.OutputType.Error,
                ctx);

            return string.IsNullOrEmpty(response)
                ? Response<IResult>.Success()
                : Response<IResult>.Failure(response);
        }
        catch (Exception ex)
        {
            return Response<IResult>.Failure(ex.Message);
        }
    }

    private static string GetImageFileQuery(IReadOnlyList<ImageFile> audioFileImages)
    {
        var query = new StringBuilder();
        foreach (var imageFile in audioFileImages)
            query.Append($" -i \"{imageFile.Path}\"");
        return query.ToString();
    }

    private static string GetMappingQuery(int mediaFilesCount)
    {
        var query = new StringBuilder();
        for (var i = 0; i < mediaFilesCount; i++)
            query.Append($" -map {i + 1}");
        return query.ToString();
    }

    private static string GetMetadataQuery(IReadOnlyList<ImageFile> audioFileImages)
    {
        var query = new StringBuilder();

        for (var i = 0; i < audioFileImages.Count; i++)
        {
            var tags = audioFileImages[i].MediaStream.Tags;
            query.Append($" -metadata:s:v:{i} comment=\"Cover (front)\""); // comment has a special meaning for FFmpeg, setting to any not predefined value will cause FFmpeg to change it to "Other".
            if (tags.Count == 0)
                continue;

            foreach (var tag in tags)
            {
                if (tag.Name != "comment")
                    query.Append($" -metadata:s:v:{i} {tag.Name}=\"{tag.Value.Replace("\"", "\\\"")}\"");
            }
        }

        return query.ToString();
    }

    private sealed class ImageFile(IMediaStream mediaStream, string path, bool isTemporaryFile)
    {
        public IMediaStream MediaStream { get; } = mediaStream;
        public string Path { get; } = path;
        public bool IsTemporaryFile { get; } = isTemporaryFile;
    }

    private static async Task<IReadOnlyList<ImageFile>> ExtractImages(IEnumerable<IMediaFileViewModel> mediaFiles, CancellationToken ctx)
    {
        var imageFiles = new List<ImageFile>();
        foreach (var mediaFile in mediaFiles)
        {
            if (mediaFile.IsImage)
            {
                if (mediaFile.IsCoverSource)
                    imageFiles.Add(new ImageFile(mediaFile.Streams[0], mediaFile.FilePath, false));
            }
            else if (mediaFile.IsCoverSource)
                imageFiles.AddRange(await ExtractImages(mediaFile, ctx));
        }

        return imageFiles;
    }

    private static async Task<IReadOnlyList<ImageFile>> ExtractImages(IMediaFileViewModel mediaFile, CancellationToken ctx)
    {
        var imageStreams = GetImageStreams(mediaFile);
        if (imageStreams.Count == 0)
            return [];

        var imageFiles = new List<ImageFile>();
        foreach (var imageStream in imageStreams)
        {
            var outputFileName = Path.GetTempFileName();
            var extractResult = await ExtractImageStream(mediaFile.FilePath, outputFileName, imageStream.Index, ctx);
            if (extractResult.IsSuccess)
                imageFiles.Add(new ImageFile(imageStream, outputFileName, true));
            else
            {
                try { await Task.Run(() => File.Delete(outputFileName), CancellationToken.None); }
                catch { /* ignore */ }
            }
        }

        return imageFiles;
    }

    private static IReadOnlyList<string> KnownImageCodecs { get; } = ["mjpeg", "png"];
    private static IReadOnlyList<IMediaStream> GetImageStreams(IMediaFileViewModel mediaFile)
    {
        var streams = new List<IMediaStream>();
        foreach (var stream in mediaFile.Streams)
        {
            if (KnownImageCodecs.Contains(stream.CodecName))
                streams.Add(stream);
        }

        return streams;
    }

    private static async Task<IResult> ExtractImageStream(string sourceFileName, string outputFileName, int sourceStreamIndex, CancellationToken ctx)
    {
        try
        {
            var response = await Process.Run(
                "ffmpeg.exe",
                $"-hide_banner -y -loglevel error -i \"{sourceFileName}\" -map 0:{sourceStreamIndex} -update true -c copy -f image2 \"{outputFileName}\"",
                Process.OutputType.Error,
                ctx);

            if (!string.IsNullOrEmpty(response) && !IsErrorToIgnore(response))
                return Response<IResult>.Failure(response);

            var fileInfo = new FileInfo(outputFileName);
            return fileInfo.Length != 0
                ? Response<IResult>.Success()
                : Response<IResult>.Failure($"Unable to extract the image from the stream #{sourceStreamIndex}, the image will not be present in the output file");
        }
        catch (Exception ex)
        {
            return Response<IResult>.Failure(ex.Message);
        }
    }

    private static IReadOnlyList<string> ErrorsToIgnore { get; } =
    [
        "Invalid PNG signature",
        "    Last message repeated ",
        "Incorrect BOM value\r\nError reading comment frame, skipped",
        "Incorrect BOM value",
        "Error reading comment frame, skipped"
    ];
    private static bool IsErrorToIgnore(string response)
    {
        foreach (var error in response.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries))
        {
            if (ErrorsToIgnore.All(errorToIgnore => !error.Contains(errorToIgnore)))
                return false;
        }

        return true;
    }

    public async Task<IResult> IsAccessible()
    {
        try
        {
            var response = await Process.Run(
                "ffmpeg.exe",
                "-version",
                Process.OutputType.Standard,
                CancellationToken.None);

            if (!response.StartsWith("ffmpeg version"))
                return Result.Failure("The tool 'ffmpeg.exe' is not found");

            response = await Process.Run(
                "ffprobe.exe",
                "-version",
                Process.OutputType.Standard,
                CancellationToken.None);

            return response.StartsWith("ffprobe version")
                ? Result.Success()
                : Result.Failure("The tool 'ffprobe.exe' is not found");
        }
        catch (Exception ex)
        {
            return Result.Failure("Unable to check the accessibility of the tools ffmpeg and ffprobe. " + ex.Message);
        }
    }
}
