using AudioCat.FFmpeg;
using AudioCat.Models;
using System.IO;
using System.Text;
using AudioCat.ViewModels;

namespace AudioCat.Services;

internal class FFmpegService : IMediaFileService
{
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

    private static IReadOnlyList<string> StatusLineContent { get; } = ["size=", "time=", "bitrate=", "speed="];
    public async Task<IResult> Concatenate(IReadOnlyList<MediaFileViewModel> mediaFiles, string outputFileName, Action<IProcessingStats> onStatusUpdate, CancellationToken ctx)
    {
        var errorMessage = new StringBuilder();
        try
        {
            var extractImagesTask = ExtractImages(mediaFiles, ctx);
            var metadataFileTask = CreateMetadataFile(mediaFiles, false, ctx); //TODO Adding chapters
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

            return errorMessage.Length == 0
                ? Result.Success()
                : Result.Failure(errorMessage.ToString());
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

    private static async Task<string> CreateFilesListFile(IEnumerable<IMediaFile> mediaFiles)
    {
        var listFile = Path.GetTempFileName();
        var sb = new StringBuilder();
        sb.AppendLine("ffconcat version 1.0");
        foreach (var mediaFile in mediaFiles)
        {
            if (IsImageFile(mediaFile))
                continue;
            var fileName = EscapeFilePath(mediaFile.File.FullName);
            sb.AppendLine($"file \'{fileName}\'");
        }

        await File.WriteAllTextAsync(listFile, sb.ToString());
        return listFile;

        static string EscapeFilePath(string path) => path.Replace("\\", "/").Replace("'", "'\\''");
    }

    private static async Task<string> CreateMetadataFile(IReadOnlyList<MediaFileViewModel> mediaFiles, bool addChapters, CancellationToken ctx)
    {
        var file = mediaFiles.FirstOrDefault(file => file.IsTagsSource);
        if (file == null)
            return "";

        var metadataFile = Path.GetTempFileName();
        var extractResult = await ExtractMetadata(file.FilePath, metadataFile, ctx);
        if (extractResult.IsSuccess)
        {
            if (addChapters)
                await AddChapters(mediaFiles, metadataFile, ctx);
            return metadataFile;
        }

        try { await Task.Run(() => File.Delete(metadataFile), CancellationToken.None); }
        catch { /* ignore */ }

        return "";
    }

    private static async Task<IResult> ExtractMetadata(string fileFullName, string outputFile, CancellationToken ctx)
    {
        var errorMessage = new StringBuilder();
        await Process.Run(
            "ffmpeg.exe",
            $"-hide_banner -y -loglevel error -i \"{fileFullName}\" -f ffmetadata \"{outputFile}\"",
            OnError,
            Process.OutputType.Error,
            ctx);

        if (errorMessage.Length != 0 && 
            !IsErrorToIgnore(errorMessage.ToString()))
            return Result.Failure(errorMessage.ToString());

        var fileInfo = new FileInfo(outputFile);
        if (fileInfo.Length == 0)
            return Result.Failure();

        await DiscardChapters(outputFile);

        return Result.Success();

        void OnError(string statusLine)
        {
            if (errorMessage.Length != 0)
                errorMessage.Append(Environment.NewLine);
            errorMessage.Append(statusLine.Trim());
        }
    }

    private static async Task DiscardChapters(string metadataFile)
    {
        string[] fileLines;
        try { fileLines = await File.ReadAllLinesAsync(metadataFile); }
        catch { return; }

        var chapterFound = false;
        var fileContent = new StringBuilder();
        foreach (var fileLine in fileLines)
        {
            if (fileLine == "[CHAPTER]")
            {
                chapterFound = true;
                break;
            }

            fileContent.AppendLine(fileLine);
        }

        if (!chapterFound)
            return;

        try { await File.WriteAllTextAsync(metadataFile, fileContent.ToString()); }
        catch { /* ignore */ }
    }

    private static async Task AddChapters(IReadOnlyList<MediaFileViewModel> mediaFiles, string metadataFile, CancellationToken ctx)
    {
        var startTime = TimeSpan.Zero;
        var chapters = new StringBuilder();
        foreach (var file in mediaFiles)
        {
            if (!file.Duration.HasValue)
                continue;

            foreach (var chapter in file.Chapters)
            {
                ctx.ThrowIfCancellationRequested();
                AddChapter(chapter);
            }

            startTime = startTime.Add(file.Duration.Value);
            ctx.ThrowIfCancellationRequested();
        }

        if (chapters.Length != 0)
            await File.AppendAllTextAsync(metadataFile, chapters.ToString(), CancellationToken.None);
        
        return;

        void AddChapter(IMediaChapter chapter)
        {
            if (!chapter.Start.HasValue || 
                !chapter.End.HasValue || 
                chapter.TimeBaseDivident is not > 0 || 
                chapter.TimeBaseDivisor is not > 0 ||
                chapter.Tags.Count == 0)
                return;

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
                
            chapters.AppendLine("[CHAPTER]");
            chapters.AppendLine("TIMEBASE=1/1000");
            chapters.AppendLine($"START={calculatedStart}");
            chapters.AppendLine($"END={calculatedEnd}");
            foreach (var tag in chapter.Tags) 
                chapters.AppendLine($"{tag.Key}={tag.Value}");
        }
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
                $"-hide_banner -y -loglevel error -i \"{audioFile}\"{imageFilesQuery} -c copy -map 0{mappingQuery}{metadataQuery} -disposition:v attached_pic \"{outputFile}\"",
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
                if (tag.Key != "comment")
                    query.Append($" -metadata:s:v:{i} {tag.Key}=\"{tag.Value.Replace("\"", "\\\"")}\"");
            }
        }

        return query.ToString();
    }

    private sealed class ImageFile(IMediaStream mediaStream, string path, bool isTemporaryFile)
    {
        public IMediaStream MediaStream { get; private init; } = mediaStream;
        public string Path { get; private init; } = path;
        public bool IsTemporaryFile { get; private init; } = isTemporaryFile;
    }

    private static async Task<IReadOnlyList<ImageFile>> ExtractImages(IEnumerable<MediaFileViewModel> mediaFiles, CancellationToken ctx)
    {
        var imageFiles = new List<ImageFile>();
        foreach (var file in mediaFiles)
        {
            if (IsImageFile(file))
                imageFiles.Add(new ImageFile(file.Streams[0], file.FilePath, false));
            else if (file.IsCoverSource)
                imageFiles.AddRange(await ExtractImages(file, ctx));
        }

        return imageFiles;
    }

    private static async Task<IReadOnlyList<ImageFile>> ExtractImages(IMediaFile mediaFile, CancellationToken ctx)
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
    private static IReadOnlyList<IMediaStream> GetImageStreams(IMediaFile mediaFile)
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

    private static IReadOnlyList<string> SupportedAudioCodecs { get; } = ["mp3", "aac"];
    private static IReadOnlyList<string> SupportedImageCodecs { get; } = ["mjpeg", "png"];
    public async Task<(IReadOnlyList<MediaFileViewModel> mediaFiles, IReadOnlyList<(string filePath, string skipReason)> skippedFiles)> GetMediaFiles(IReadOnlyList<string> fileNames, bool selectMetadata, bool selectCover, CancellationToken ctx)
    {
        var sortedFiles = Files.Sort(fileNames);

        var codec = "";
        var isTagsSourceSelected = false;
        var isCoverSourceSelected = false;

        var mediaFiles = new List<MediaFileViewModel>(fileNames.Count);
        var skippedFiles = new List<(string filePath, string skipReason)>();

        // Start the tasks concurrently
        var probeTasks = new List<Task<IResponse<IMediaFile>>>(fileNames.Count);
        foreach (var filePath in sortedFiles) 
            probeTasks.Add(Probe(filePath, ctx));
        await Task.WhenAll(probeTasks);

        for (var index = 0; index < probeTasks.Count; index++)
        {
            //IResponse<IMediaFile> probeResponse = await Probe(fileName, ctx);
            var probeResponse = await probeTasks[index];
            if (probeResponse.IsFailure)
            {
                skippedFiles.Add((sortedFiles[index], probeResponse.Message));
                continue;
            }

            var isTagsSource = false;
            var file = probeResponse.Data!;
            var isImageFile = IsImageFile(file);
            if (!isImageFile)
            {
                var codecSelectResult = SelectCodec(file, ref codec);
                if (codecSelectResult.IsFailure)
                {
                    skippedFiles.Add((sortedFiles[index], codecSelectResult.Message));
                    continue;
                }

                if (selectMetadata)
                {
                    isTagsSource = !isTagsSourceSelected && file.Tags.Count > 0;
                    if (isTagsSource)
                        isTagsSourceSelected = true;
                }
            }

            var mediaFileViewModel = new MediaFileViewModel(probeResponse.Data!, isTagsSource, isImageFile);

            if (!isImageFile && selectCover && !isCoverSourceSelected && mediaFileViewModel.HasCover)
            {
                mediaFileViewModel.IsCoverSource = true;
                isCoverSourceSelected = true;
            }

            mediaFiles.Add(mediaFileViewModel);
        }

        return (mediaFiles, skippedFiles);
    }

    private static IResult SelectCodec(IMediaFile mediaFile, ref string selectedCodec)
    {
        if (selectedCodec == "") // Acceptable codec has not been selected yet
            return (selectedCodec = GetCodecName(mediaFile)) != ""
                ? Result.Success()
                : Result.Failure("Doesn't contain any supported audio streams");

        return HasStreamWithCodec(mediaFile, selectedCodec)
            ? Result.Success()
            : Result.Failure($"Doesn't contain any audio stream encoded with '{selectedCodec}' codec");
    }

    private static string GetCodecName(IMediaFile mediaFile)
    {
        foreach (var stream in mediaFile.Streams)
        {
            if (SupportedAudioCodecs.Contains(stream.CodecName))
                return stream.CodecName ?? "";
        }

        return "";
    }

    private static bool IsImageFile(IMediaFile mediaFile) => 
        mediaFile.Streams.Count == 1 && SupportedImageCodecs.Contains(mediaFile.Streams[0].CodecName);

    private static bool HasStreamWithCodec(IMediaFile mediaFile, string codecName)
    {
        foreach (var stream in mediaFile.Streams)
        {
            if (stream.CodecName == codecName)
                return true;
        }

        return false;
    }

    public string GetAudioCodec(IReadOnlyCollection<MediaFileViewModel> mediaFiles)
    {
        foreach (var audioFile in mediaFiles)
        {
            string codec;
            if ((codec = GetCodecName(audioFile)) != "")
                return codec;
        }

        return "";
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
