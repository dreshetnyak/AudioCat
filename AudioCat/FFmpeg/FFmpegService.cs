﻿using System.Collections.Concurrent;
using System.Diagnostics;
using AudioCat.Models;
using AudioCat.Services;
using System.IO;
using System.Text;
using AudioCat.ViewModels;
using Process = AudioCat.Services.Process;

namespace AudioCat.FFmpeg;

internal sealed class FFmpegService : IMediaFileToolkitService
{
    private static IReadOnlyList<string> StatusLineContent { get; } = ["size=", "time=", "bitrate=", "speed="];

    public event ProgressEventHandler? Progress;
    public event MessageEventHandler? Status;
    public event MessageEventHandler? Error;

    public async Task<IResponse<IMediaFile>> Probe(string fileFullName, CancellationToken ctx)
    {
        try
        {
            var args = $"-hide_banner -show_format -show_chapters -show_streams -show_private_data -print_format xml -i \"{fileFullName}\"";
            Debug.WriteLine($"ffmpeg.exe {args}");
            var probeResponse = await Process.Run(
                "ffprobe.exe",
                args,
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

    public async Task Concatenate(IReadOnlyList<IMediaFileViewModel> mediaFiles, IConcatParams concatParams, string outputFileName, CancellationToken ctx)
    {
        TimeSpan totalDuration;
        var concatErrors = new StringBuilder();
        try
        {
            await OnStatus("Starting...");

            var listFileTask = CreateFilesListFile(mediaFiles);
            var extractImagesTask = ExtractImages(mediaFiles, ctx);
            var metadataFileTask = CreateMetadataFile(mediaFiles, concatParams, ctx);
            var totalDurationTask = Task.Run(mediaFiles.GetTotalDuration, ctx);

            var codec = MediaFilesService.GetAudioCodec(mediaFiles);

            var listFile = await listFileTask;
            var (extractedImages, imageExtractionErrors) = await extractImagesTask;
            if (!string.IsNullOrEmpty(imageExtractionErrors))
                await OnError($"Image extraction errors:{Environment.NewLine}{imageExtractionErrors}");

            var metadataFile = await metadataFileTask;
            var twoStepsConcat = Settings.CodecsWithTwoStepsConcat.Has(codec) && metadataFile != "";

            var hasImages = extractedImages.Count > 0;
            var outputToFile = hasImages || twoStepsConcat
                ? await GenerateTempOutputFileFrom(Path.GetExtension(outputFileName)) 
                : outputFileName;

            IReadOnlyList<string>? remuxedFiles = null;
            totalDuration = await totalDurationTask;
            do
            {
                var args1 = GetFFmpegArgs(codec, listFile, !twoStepsConcat ? metadataFile : "", outputToFile);
                Debug.WriteLine($"ffmpeg.exe {args1}");
                await Process.Run("ffmpeg.exe", args1, status => OnConcatStatus(status, totalDuration), Process.OutputType.Error, ctx);

                var concatErrorsStr = concatErrors.ToString();
                concatErrors.Clear();

                if (concatErrorsStr == "")
                    break;
                
                if (remuxedFiles != null || !Settings.RemuxOnErrors.IsIn(concatErrorsStr)) //If not a remuxable error 
                {
                    await OnError(concatErrorsStr);
                    break;
                }

                #region Remuxing files
                // Some of the audio files have minor issues in them, like "non-monotonically increasing dts", if we try to concatenate them 'as is' FFMpeg will return errors
                // and I don't really know if the resulting output will play well. We need to remux the files to fix these errors. By remuxing I mean to copy the streams to
                // a new file for each file individually, this will fix the errors and allow to concatenation to terminate clean. To remux we run concatenation command with
                // a single input file, we output it to a temporary file. Then we concatenate the temporary files.

                await OnStatus("Remuxing files...");
                var remuxResponse = await RemuxFiles(mediaFiles, OnProgress, ctx);
                if (remuxResponse.IsFailure)
                {
                    //concatErrors.AppendMessage(remuxResponse.Message);
                    await OnError($"Remuxing errors:{Environment.NewLine}{remuxResponse.Message}");
                }
                if (remuxResponse.Data == null)
                {
                    await OnError("Remuxing failed with an unrecoverable error, aborting.");
                    break;
                }

                remuxedFiles = remuxResponse.Data!;
                try { await Task.Run(() => File.Delete(listFile), CancellationToken.None); }
                catch { /* ignore */ }
                listFile = await CreateFilesListFile(remuxedFiles);

                #endregion
            } while (true);

            #region Second Step of Concatenation
            if (twoStepsConcat)
            {
                try { await Task.Run(() => File.Delete(listFile), CancellationToken.None); }
                catch { /* ignore */ }

                listFile = await CreateFilesListFile(outputToFile);
                outputToFile = hasImages
                    ? await GenerateTempOutputFileFrom(Path.GetExtension(outputFileName))
                    : outputFileName;

                var args2 = GetFFmpegArgs(codec, listFile, metadataFile, outputToFile);
                Debug.WriteLine($"ffmpeg.exe {args2}");
                await Process.Run("ffmpeg.exe", args2, status => OnConcatStatus(status, totalDuration), Process.OutputType.Error, ctx);
            }
            #endregion

            #region Attach Images
            if (hasImages)
            {
                await OnStatus(extractedImages.Count == 1 ? "Appending cover image..." : "Appending cover images...");
                var imagesResult = await AddImages(outputToFile, extractedImages, outputFileName, ctx);
                if (imagesResult.IsFailure)
                    await OnError($"Image embedding errors:{Environment.NewLine}{imagesResult.Message}");
            }
            #endregion

            //var cue = Cue.Create(mediaFiles, codec, outputFileName);
            //var dir = new FileInfo(outputFileName).Directory!.FullName;
            //var fileName = Path.GetFileNameWithoutExtension(outputFileName);
            //var filePath = Path.Combine(dir, fileName + ".cue");
            //await File.WriteAllTextAsync(filePath, cue, new UTF8Encoding(false), ctx);

            #region Delete Temporary Files
            await OnStatus("Cleaning up...");
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

            if (remuxedFiles != null)
            {
                foreach (var remuxedFile in remuxedFiles)
                {
                    try { await Task.Run(() => File.Delete(remuxedFile), CancellationToken.None); }
                    catch { /* ignore */ }
                }
            }

            #endregion

            #region Delete Output File is it is Empty
            var outputFile = new FileInfo(outputFileName);
            if (outputFile is { Exists: true, Length: 0 })
            {
                try { await Task.Run(() => File.Delete(outputFileName), CancellationToken.None); }
                catch { /* ignore */ }
            }
            #endregion
        }
        catch (Exception ex)
        {
            await OnError($"Concatenation exception:{Environment.NewLine}{ex.Message}");
            return;
        }

        return;

        async Task OnConcatStatus(string status, TimeSpan total)
        {
            if (Settings.ErrorsToIgnore.IsIn(status))
                return;
            if (IsErrorMessage(status))
                concatErrors.AppendMessage(status);
            else
            {
                var stats = new FFmpegStats(status);
                await Task.Run(() => OnProgress(new Progress(total, stats.Time)), CancellationToken.None);
                await Task.Run(() => OnStatus(stats.ToString()), CancellationToken.None);
            }
        }
    }
    

    private static bool IsErrorMessage(string status) =>
        status.StartsWith('[') || !StatusLineContent.Any(status.Contains);

    private class RemuxProgress(IMediaFileViewModel file, IProcessingStats? stats = null)
    {
        public IMediaFileViewModel File { get; } = file;
        public IProcessingStats? Stats { get; set; } = stats;
    }

    private static async Task<IResponse<IReadOnlyList<string>>> RemuxFiles(IReadOnlyList<IMediaFileViewModel> mediaFiles, Func<Progress, Task> onProgress, CancellationToken ctx)
    {
        var sync = new object();
        var errors = new StringBuilder();
        var remuxedFiles = new ConcurrentBag<(IMediaFileViewModel, string)>();
        using var statusMessages = new BlockingCollection<RemuxProgress>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx);
        // ReSharper disable AccessToDisposedClosure
        using var progressTrackingTask = Task.Run(async () => await ProgressTracking(mediaFiles, statusMessages, onProgress, cts.Token), CancellationToken.None);
        // ReSharper restore AccessToDisposedClosure
        
        foreach (var file in mediaFiles.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount))
        {
            if (file.IsImage)
                continue;
            var remuxResponse = await RemuxFile(file, status => statusMessages.Add(status, CancellationToken.None), ctx);
            remuxedFiles.Add((file, remuxResponse.Data!));
            if (remuxResponse.IsFailure)
                lock (sync) errors.AppendMessage(remuxResponse.Message);
        }

        await cts.CancelAsync();

        if (errors.Length > 0 && IsUnrecoverableError()) 
            await DeleteAllTempFiles();

        var sortedRemuxedFiles = SortRemuxedFiles();

        try { await progressTrackingTask; }
        catch { /* ignore */ }
        
        return errors.Length == 0
            ? Response<IReadOnlyList<string>>.Success(sortedRemuxedFiles)
            : Response<IReadOnlyList<string>>.Failure(sortedRemuxedFiles, errors.ToString());

        #region Local Methods
        bool IsUnrecoverableError()
        {
            foreach (var (_, remuxedFile) in remuxedFiles)
            {
                var fileInfo = new FileInfo(remuxedFile);
                if (!fileInfo.Exists || fileInfo.Length == 0)
                    return true;
            }

            return false;
        }

        async Task DeleteAllTempFiles()
        {
            foreach (var (_, remuxedFile) in remuxedFiles)
            {
                var fileInfo = new FileInfo(remuxedFile);
                if (!fileInfo.Exists)
                    continue;
                try { await Task.Run(() => File.Delete(fileInfo.FullName), CancellationToken.None); }
                catch { /* ignore */ }
            }
        }

        IReadOnlyList<string> SortRemuxedFiles()
        {
            var sortedFiles = new List<string>(mediaFiles.Count);
            foreach (var mediaFile in mediaFiles)
            {
                foreach (var (remuxedMediaFile, remuxedFile) in remuxedFiles)
                {
                    if (remuxedMediaFile != mediaFile)
                        continue;
                    sortedFiles.Add(remuxedFile);
                    break;
                }
            }

            return sortedFiles;
        }
        #endregion
    }
    
    private static async Task ProgressTracking(IReadOnlyList<IMediaFileViewModel> mediaFiles, BlockingCollection<RemuxProgress> statusMessages, Func<Progress, Task> onProgress, CancellationToken ctx)
    {
        var totalDuration = mediaFiles.GetTotalDuration();
        var filesTracking = new RemuxProgress[mediaFiles.Count];
        for (var index = 0; index < mediaFiles.Count; index++) 
            filesTracking[index] = new RemuxProgress(mediaFiles[index]);
        
        do 
        {
            var statusUpdate = statusMessages.Take(ctx);
            foreach (var fileTracking in filesTracking)
            {
                if (fileTracking.File != statusUpdate.File)
                    continue;
                fileTracking.Stats = statusUpdate.Stats;
                var completedDuration = GetCompletedDuration(filesTracking);
                await onProgress(new Progress(totalDuration, completedDuration));
                break;
            }
        } while (!ctx.IsCancellationRequested);
        ctx.ThrowIfCancellationRequested();
    }

    private static TimeSpan GetCompletedDuration(IEnumerable<RemuxProgress> filesTracking)
    {
        var completedDuration = TimeSpan.Zero;
        foreach (var fileTacking in filesTracking)
        {
            var fileStats = fileTacking.Stats;
            if (fileStats != null)
                completedDuration = completedDuration.Add(fileStats.Time);
        }

        return completedDuration;
    }
    
    private static async Task<IResponse<string>> RemuxFile(IMediaFileViewModel mediaFile, Action<RemuxProgress> onStatus, CancellationToken ctx)
    {
        var errors = new StringBuilder();
        var filesList = await CreateFilesListFile([mediaFile]);
        var outputToFile = await GenerateTempOutputFileFrom(mediaFile.File.Extension);
        var args = $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{filesList}\" -vn -c:a copy -update true \"{outputToFile}\"";
        Debug.WriteLine($"ffmpeg.exe {args}");
        await Process.Run("ffmpeg.exe", args, OnStatus, Process.OutputType.Error, ctx);

        return errors.Length == 0
            ? Response<string>.Success(outputToFile)
            : Response<string>.Failure(outputToFile, errors.ToString());

        async Task OnStatus(string status)
        {
            if (Settings.ErrorsToIgnore.IsIn(status))
                return;
            if (IsErrorMessage(status))
                errors.AppendMessage(status);
            else
                await Task.Run(() => onStatus(new RemuxProgress(mediaFile, new FFmpegStats(status))), CancellationToken.None);
        }
    }

    private static string GetFFmpegArgs(string codec, string listFile, string metadataFile, string outputToFile)
    {
        var encodingCommand = Settings.GetEncodingCommand(codec);
        if (Settings.CodecsWithTwoStepsConcat.Has(codec)) // For Vorbis we first save it discarding tags, then in the second step we add the tags
            return metadataFile == ""
                ? $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -map_metadata -1 -vn {encodingCommand} -update true \"{outputToFile}\""
                : $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -i \"{metadataFile}\" -map_metadata 1 -vn {encodingCommand} -update true \"{outputToFile}\"";
        
        return metadataFile != ""
            ? $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -i \"{metadataFile}\" -map_metadata 1 -vn {encodingCommand} -id3v2_version 3 -write_id3v1 1 -update true \"{outputToFile}\""
            : $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -vn {encodingCommand} -update true \"{outputToFile}\"";
    }

    private static async Task<string> GenerateTempOutputFileFrom(string fileExtension)
    {
        var tryCount = 0;
        while (tryCount++ < 3)
        {
            try
            {
                var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + fileExtension);
                await File.WriteAllBytesAsync(filePath, []);
                return filePath;
            }
            catch { /* ignore */ }
        }

        return "";
    }

    private const string FILES_LIST_HEADER = "ffconcat version 1.0\n";
    private static async Task<string> CreateFilesListFile(IEnumerable<IMediaFileViewModel> mediaFiles)
    {
        var listFile = Path.GetTempFileName();
        var sb = new StringBuilder(FILES_LIST_HEADER);
        foreach (var mediaFile in mediaFiles)
        {
            if (mediaFile.IsImage)
                continue;
            var fileName = EscapeFileListFilePath(mediaFile.File.FullName);
            sb.Append($"file \'{fileName}\'\n");
        }
        
        await File.WriteAllTextAsync(listFile, sb.ToString(), new UTF8Encoding(false));
        return listFile;
    }
    private static async Task<string> CreateFilesListFile(IReadOnlyList<string> remuxedFiles)
    {
        var listFile = Path.GetTempFileName();
        var sb = new StringBuilder(FILES_LIST_HEADER);
        foreach (var remuxedFile in remuxedFiles)
        {
            var fileName = EscapeFileListFilePath(remuxedFile);
            sb.Append($"file \'{fileName}\'\n");
        }

        await File.WriteAllTextAsync(listFile, sb.ToString(), new UTF8Encoding(false));
        return listFile;
    }

    private static async Task<string> CreateFilesListFile(string file)
    {
        var fileInfo = new FileInfo(file);
        if (!fileInfo.Exists)
            return "";
        var listFile = Path.GetTempFileName();
        var fileContent = $"{FILES_LIST_HEADER}file '{EscapeFileListFilePath(fileInfo.FullName)}'\n";
        await File.WriteAllTextAsync(listFile, fileContent);
        return listFile;
    }

    private static string EscapeFileListFilePath(string path) => path.Replace("\\", "/").Replace("'", "'\\''");
    
    private const string METADATA_FILE_START = ";FFMETADATA1\n";
    private static async Task<string> CreateMetadataFile(IReadOnlyList<IMediaFileViewModel> mediaFiles, IConcatParams concatParams, CancellationToken ctx)
    {
        var tagsMetadata = concatParams.TagsEnabled 
            ? GetTagsMetadata(mediaFiles) 
            : ""; 
        var chaptersMetadata = concatParams.ChaptersEnabled 
            ? GetChaptersMetadata(mediaFiles) 
            : "";
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
        var mediaFile = mediaFiles.GetTagsSourceFile();
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
            var args = $"-hide_banner -y -loglevel error -i \"{audioFile}\"{imageFilesQuery} -c copy -map 0:a{mappingQuery}{metadataQuery} -id3v2_version 3 -write_id3v1 1 -disposition:v attached_pic \"{outputFile}\"";
            Debug.WriteLine($"ffmpeg.exe {args}");
            var response = await Process.Run(
                "ffmpeg.exe",
                args,
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

    private static async Task<(IReadOnlyList<ImageFile> imageFiles, string errors)> ExtractImages(IEnumerable<IMediaFileViewModel> mediaFiles, CancellationToken ctx)
    {
        var errors = new StringBuilder();
        var imageFiles = new List<ImageFile>();
        foreach (var mediaFile in mediaFiles)
        {
            if (mediaFile.IsImage)
            {
                if (mediaFile.IsCoverSource)
                    imageFiles.Add(new ImageFile(mediaFile.Streams[0], mediaFile.FilePath, false));
            }
            else if (mediaFile.IsCoverSource)
            {
                var (extractedImageFiles, extractionErrors) = await ExtractImages(mediaFile, ctx);
                imageFiles.AddRange(extractedImageFiles);
                errors.Append(extractionErrors);
            }
        }

        return (imageFiles, errors.ToString());
    }

    private static async Task<(IReadOnlyList<ImageFile> imageFiles, string errors)> ExtractImages(IMediaFileViewModel mediaFile, CancellationToken ctx)
    {
        var imageStreams = GetImageStreams(mediaFile);
        if (imageStreams.Count == 0)
            return ([], "");

        var errors = new StringBuilder();
        var imageFiles = new List<ImageFile>();
        foreach (var imageStream in imageStreams)
        {
            var outputFileName = Path.GetTempFileName();
            var extractResult = await ExtractImageStream(mediaFile.FilePath, outputFileName, imageStream.Index, ctx);
            if (extractResult.IsSuccess)
                imageFiles.Add(new ImageFile(imageStream, outputFileName, true));
            else
            {
                var imageFile = new FileInfo(outputFileName);
                if (!imageFile.Exists)
                {
                    errors.AppendMessage(extractResult.Message);
                    continue;
                }

                if (imageFile.Length > 0 && await IsValidMediaFile(outputFileName, ctx))
                {
                    // If we got error, but the extracted file validates just fine, use the extracted file.
                    imageFiles.Add(new ImageFile(imageStream, outputFileName, true));
                    continue;
                }

                errors.AppendMessage(extractResult.Message);
                try { await Task.Run(() => File.Delete(outputFileName), CancellationToken.None); }
                catch { /* ignore */ }
            }
        }

        return (imageFiles, errors.ToString());
    }

    private static IReadOnlyList<IMediaStream> GetImageStreams(IMediaFileViewModel mediaFile)
    {
        var streams = new List<IMediaStream>();
        foreach (var stream in mediaFile.Streams)
        {
            if (Settings.SupportedImageCodecs.Contains(stream.CodecName))
                streams.Add(stream);
        }

        return streams;
    }

    private static async Task<IResult> ExtractImageStream(string sourceFileName, string outputFileName, int sourceStreamIndex, CancellationToken ctx)
    {
        try
        {
            var args = $"-hide_banner -y -loglevel error -i \"{sourceFileName}\" -map 0:{sourceStreamIndex} -update true -c copy -f image2 \"{outputFileName}\"";
            Debug.WriteLine($"ffmpeg.exe {args}");
            var response = await Process.Run(
                "ffmpeg.exe",
                args,
                Process.OutputType.Error,
                ctx);

            if (!string.IsNullOrEmpty(response) && !Settings.ErrorsToIgnore.IsIn(response))
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

    private static async Task<bool> IsValidMediaFile(string sourceFileName, CancellationToken ctx)
    {
        try
        {
            var args = $"-hide_banner -y -loglevel error -i \"{sourceFileName}\" -f null -";
            var response = await Process.Run(
                "ffmpeg.exe",
                args,
                Process.OutputType.Error,
                ctx);

            return string.IsNullOrEmpty(response);
        }
        catch
        {
            return false;
        }
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

    private Task OnProgress(Progress progress) => Task.Run(() => Progress?.Invoke(this, new ProgressEventArgs(progress)));
    private Task OnStatus(string status) => Task.Run(() => Status?.Invoke(this, new MessageEventArgs(status)));
    private Task OnError(string message) => Task.Run(() => Error?.Invoke(this, new MessageEventArgs(message)));
}

