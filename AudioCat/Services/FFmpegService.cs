using AudioCat.FFmpeg;
using AudioCat.Models;
using System.IO;
using System.Text;
using AudioCat.ViewModels;

namespace AudioCat.Services;

internal class FFmpegService : IAudioFileService
{
    public async Task<IResponse<IAudioFile>> Probe(string fileFullName, CancellationToken ctx)
    {
        try
        {
            var probeResponse = await Process.Run(
                "ffprobe.exe",
                $"-hide_banner -show_format -show_chapters -show_streams -show_private_data -print_format xml -i \"{fileFullName}\"",
                Process.OutputType.Standard,
                ctx);

            var createResponse = FFprobeAudioFile.Create(fileFullName, probeResponse);
            return createResponse.IsSuccess
                ? Response<IAudioFile>.Success(createResponse.Data!)
                : Response<IAudioFile>.Failure(createResponse);
        }
        catch (Exception ex)
        {
            return Response<IAudioFile>.Failure(ex.Message);
        }
    }

    public async Task<IResult> Concatenate(IReadOnlyList<AudioFileViewModel> audioFiles, string outputFileName, Action<IProcessingStats> onStatusUpdate, CancellationToken ctx)
    {
        var errorMessage = new StringBuilder();
        try
        {
            var extractImagesTask = ExtractImages(audioFiles, ctx);
            var metadataFileTask = CreateMetadataFile(audioFiles, ctx);
            var listFileTask = Task.Run(() => CreateFilesListFile(audioFiles), ctx);

            var listFile = await listFileTask;
            var metadataFile = await metadataFileTask;
            var extractedImages = await extractImagesTask;

            var hasImages = extractedImages.Count > 0;
            var outputToFile = hasImages ? GenerateTempOutputFileFrom(outputFileName) : outputFileName;
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
                try { await Task.Run(() => File.Delete(extractedImage.imageFile), CancellationToken.None); }
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
            if (IsErrorMessage(status))
                errorMessage.AppendLine(status);
            else
                onStatusUpdate(new FFmpegStats(status));
        }

        static bool IsErrorMessage(string status) => 
            status.StartsWith('[') || !new[] { "size=", "time=", "bitrate=", "speed=" }.Any(status.Contains);
    }

    private static string GenerateTempOutputFileFrom(string outputFileName)
    {
        var tryCount = 0;
        while (tryCount++ < 3)
        {
            try
            {
                var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(outputFileName));
                File.WriteAllBytes(filePath, []);
                return filePath;
            }
            catch { /* ignore */ }
        }

        return "";
    }

    private static string CreateFilesListFile(IEnumerable<IAudioFile> audioFiles)
    {
        var listFile = Path.GetTempFileName();
        var sb = new StringBuilder();
        sb.AppendLine("ffconcat version 1.0");
        foreach (var audioFile in audioFiles)
        {
            var fileName = EscapeFilePath(audioFile.File.FullName);
            sb.AppendLine($"file \'{fileName}\'");
        }

        File.WriteAllText(listFile, sb.ToString());
        return listFile;

        static string EscapeFilePath(string path) => path.Replace("\\", "/").Replace("'", "'\\''");
    }

    private static async Task<string> CreateMetadataFile(IEnumerable<AudioFileViewModel> audioFiles, CancellationToken ctx)
    {
        var file = audioFiles.FirstOrDefault(file => file.IsTagsSource);
        if (file == null)
            return "";

        var metadataFile = Path.GetTempFileName();
        var extractResult = await ExtractMetadata(file.FilePath, metadataFile, ctx);

        if (extractResult.IsSuccess)
            return metadataFile;

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

        return errorMessage.Length == 0 
            ? Result.Success()
            : Result.Failure(errorMessage.ToString());

        void OnError(string statusLine)
        {
            if (errorMessage.Length != 0)
                errorMessage.Append(Environment.NewLine);
            errorMessage.Append(statusLine.Trim());
        }
    }

    private static async Task<IResult> AddImages(string audioFile, IReadOnlyList<(IMediaStream imageStream, string imageFile)> audioFileImages, string outputFile, CancellationToken ctx)
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

    private static string GetImageFileQuery(IReadOnlyList<(IMediaStream imageStream, string imageFile)> audioFileImages)
    {
        var query = new StringBuilder();
        foreach (var (_, imageFile) in audioFileImages) 
            query.Append($" -i \"{imageFile}\"");
        return query.ToString();
    }

    private static string GetMappingQuery(int audioFilesCount)
    {
        var query = new StringBuilder();
        for (var i = 0; i < audioFilesCount; i++) 
            query.Append($" -map {i + 1}");
        return query.ToString();
    }

    private static string GetMetadataQuery(IReadOnlyList<(IMediaStream imageStream, string imageFile)> audioFileImages)
    {
        var query = new StringBuilder();

        for (var i = 0; i < audioFileImages.Count; i++)
        {
            var (imageStream, _) = audioFileImages[i];
            var tags = imageStream.Tags;
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

    private static async Task<IReadOnlyList<(IMediaStream imageStream, string imageFile)>> ExtractImages(IEnumerable<AudioFileViewModel> audioFiles, CancellationToken ctx)
    {
        var imageFiles = new List<(IMediaStream imageStream, string imageFile)>();
        foreach (var file in audioFiles)
        {
            if (file.IsCoverSource)
                imageFiles.AddRange(await ExtractImages(file, ctx));
        }

        return imageFiles;
    }

    private static async Task<IReadOnlyList<(IMediaStream imageStream, string imageFile)>> ExtractImages(IAudioFile audioFile, CancellationToken ctx)
    {
        var imageStreams = GetImageStreams(audioFile);
        if (imageStreams.Count == 0)
            return [];

        var imageFiles = new List<(IMediaStream imageStream, string imageFile)>();
        foreach (var imageStream in imageStreams)
        {
            var outputFileName = Path.GetTempFileName();
            var extractResult = await ExtractImageStream(audioFile.FilePath, outputFileName, imageStream.Index, ctx);
            if (extractResult.IsSuccess)
                imageFiles.Add((imageStream, outputFileName));
            else
            {
                try { await Task.Run(() => File.Delete(outputFileName), CancellationToken.None); }
                catch { /* ignore */ }
            }
        }

        return imageFiles;
    }

    private static IReadOnlyList<string> KnownImageCodecs { get; } = ["mjpeg", "png"];
    private static IReadOnlyList<IMediaStream> GetImageStreams(IAudioFile audioFile)
    {
        var streams = new List<IMediaStream>();
        foreach (var stream in audioFile.Streams)
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

            return string.IsNullOrEmpty(response)
                ? Response<IResult>.Success()
                : Response<IResult>.Failure(response);
        }
        catch (Exception ex)
        {
            return Response<IResult>.Failure(ex.Message);
        }
    }

    private static IReadOnlyList<string> SupportedAudioCodecs { get; } = ["mp3", "aac"];
    public async Task<(IReadOnlyCollection<AudioFileViewModel> audioFiles, IReadOnlyList<(string filePath, string skipReason)> skippedFiles)> GetAudioFiles(IReadOnlyList<string> fileNames, CancellationToken ctx)
    {
        var sortedFileNames = Files.Sort(fileNames);

        var codec = "";
        var isTagsSourceSelected = false;
        var isCoverSourceSelected = false;

        var audioFiles = new List<AudioFileViewModel>(fileNames.Count);
        var skippedFiles = new List<(string filePath, string skipReason)>();

        foreach (var fileName in sortedFileNames)
        {
            var probeResponse = await Probe(fileName, ctx);
            if (probeResponse.IsFailure)
            {
                skippedFiles.Add((fileName, probeResponse.Message));
                continue;
            }

            var file = probeResponse.Data!;

            var codecSelectResult = SelectCodec(file, ref codec);
            if (codecSelectResult.IsFailure)
            {
                skippedFiles.Add((fileName, codecSelectResult.Message));
                continue;
            }

            var isTagsSource = !isTagsSourceSelected && file.Tags.Count > 0;
            if (isTagsSource)
                isTagsSourceSelected = true;

            var audioFileViewModel = new AudioFileViewModel(probeResponse.Data!, isTagsSource);
            if (!isCoverSourceSelected && audioFileViewModel.HasCover)
            {
                audioFileViewModel.IsCoverSource = true;
                isCoverSourceSelected = true;
            }

            audioFiles.Add(audioFileViewModel);
        }

        return (audioFiles, skippedFiles);

        static string GetCodecName(IAudioFile audioFile)
        {
            foreach (var stream in audioFile.Streams)
            {
                if (SupportedAudioCodecs.Contains(stream.CodecName))
                    return stream.CodecName ?? "";
            }

            return "";
        }

        static bool HasStreamWithCodec(IAudioFile audioFile, string codecName)
        {
            foreach (var stream in audioFile.Streams)
            {
                if (stream.CodecName == codecName)
                    return true;
            }

            return false;
        }

        static IResult SelectCodec(IAudioFile audioFile, ref string selectedCodec)
        {
            if (selectedCodec == "")
                return (selectedCodec = GetCodecName(audioFile)) != ""
                    ? Result.Success()
                    : Result.Failure("Doesn't contain any supported audio streams");

            return HasStreamWithCodec(audioFile, selectedCodec)
                ? Result.Success()
                : Result.Failure($"Doesn't contain any audio stream encoded with '{selectedCodec}' codec");
        }
    }

}
