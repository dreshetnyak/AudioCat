using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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
            Debug.WriteLine($"{Settings.FFprobeName} {args}");
            var probeResponse = await Process.Run(
                Settings.FFprobeName,
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

    #region Silence Detection
    public async Task<IResponse<IReadOnlyList<IInterval>>> ScanForSilence(string fileFullName, int durationMilliseconds, int silenceThreshold, CancellationToken ctx)
    {
        var intervals = new List<IInterval>();
        using var statusQueue = new BlockingCollection<string>();

        try
        {
            var args = $"-hide_banner -stats -stats_period 0.1 -i \"{fileFullName}\" -af silencedetect=n=-{silenceThreshold}dB:d={durationMilliseconds}ms -f null -";
            Debug.WriteLine($"{Settings.FFmpegName} {args}");

            // ReSharper disable once AccessToDisposedClosure
            var intervalsTask = Task.Run(() => IntervalsProcessor(statusQueue, intervals, fileFullName));

            try { await Process.Run(Settings.FFmpegName, args, OnSilenceStatus, Process.OutputType.Error, ctx); }
            finally
            {
                statusQueue.CompleteAdding(); // The processor drains the queued tail lines and exits; also unblocks it on the cancellation/exception paths
                try { await intervalsTask; }
                catch { /* ignore */ }
            }

            return Response<IReadOnlyList<IInterval>>.Success(intervals);
        }
        catch (TaskCanceledException) { return Response<IReadOnlyList<IInterval>>.Failure(nameof(TaskCanceledException)); }
        catch (OperationCanceledException) { return Response<IReadOnlyList<IInterval>>.Failure(nameof(OperationCanceledException)); }
        catch (Exception ex) { return Response<IReadOnlyList<IInterval>>.Failure(ex.Message); }

        Task OnSilenceStatus(string status)
        {
            // ReSharper disable once AccessToDisposedClosure
            try { statusQueue.Add(status, CancellationToken.None); }
            catch { /* ignore */ }
            return Task.CompletedTask;
        }
    }

    private static void IntervalsProcessor(BlockingCollection<string> statusQueue, List<IInterval> silenceIntervals, string fileFullName)
    {
        var startTime = TimeSpan.Zero;
        foreach (var status in statusQueue.GetConsumingEnumerable())
        {
            if (!status.StartsWith("[silencedetect", StringComparison.Ordinal))
                continue;

            if (startTime == TimeSpan.Zero)
            {
                if (TryGetTime(status, "silence_start:", out var start))
                    startTime = start;
                continue;
            }
            
            if (!TryGetTime(status, "silence_end:", out var end))
            {
                if (TryGetTime(status, "silence_start:", out var start))
                    startTime = start;
                continue;
            }

            // End of the silence
            silenceIntervals.Add(new Interval(fileFullName, startTime, end));
            startTime = TimeSpan.Zero;
        }
    }

    private static bool TryGetTime(string status, string name, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;
        var index = status.IndexOf(name, StringComparison.Ordinal);
        if (index == -1)
            return false;
        var valueStart = status.IndexOfDigit(index + name.Length);
        if (valueStart == -1)
            return false;
        var valueEnd = status.IndexOfNotDigitOrDot(valueStart);
        if (valueEnd == -1)
            valueEnd = status.Length;
        if (!double.TryParse(status.AsSpan(valueStart, valueEnd - valueStart), NumberStyles.Float, CultureInfo.InvariantCulture, out var timeStamp))
            return false;
        timeSpan = TimeSpan.FromSeconds(timeStamp);
        return true;
    }

    #endregion

    public async Task Concatenate(IReadOnlyList<IMediaFileViewModel> mediaFiles, IConcatParams concatParams, string outputFileName, CancellationToken ctx)
    {
        var concatErrors = new StringBuilder();
        var tempDir = "";
        var stagedOutputFile = "";
        var outputFileWritten = false;
        var isCancelled = false;
        try
        {
            await OnStatus("Starting...");

            tempDir = TempDirectory.Create();

            var listFileTask = CreateFilesListFile(tempDir, mediaFiles);
            var extractImagesTask = ExtractImages(tempDir, mediaFiles, ctx);
            var metadataFileTask = CreateMetadataFile(tempDir, concatParams, ctx);
            var totalDurationTask = Task.Run(mediaFiles.GetTotalDuration, ctx);

            var codec = MediaFilesService.GetAudioCodec(mediaFiles);

            var listFile = await listFileTask;
            var (extractedImages, imageExtractionErrors) = await extractImagesTask;
            if (!string.IsNullOrEmpty(imageExtractionErrors))
                await OnError($"Image extraction errors:{Environment.NewLine}{imageExtractionErrors}");

            var metadataFile = await metadataFileTask;
            var twoStepsConcat = Settings.CodecsWithTwoStepsConcat.Has(codec) && metadataFile != "";

            var hasImages = extractedImages.Count > 0;
            var finalOutputFile = outputFileName;
            if (IsOutputFileAnInput(mediaFiles, outputFileName))
            {
                stagedOutputFile = await GenerateStagedOutputFileFrom(outputFileName);
                finalOutputFile = stagedOutputFile;
            }

            var outputToFile = hasImages || twoStepsConcat
                ? await GenerateTempOutputFileFrom(tempDir, Path.GetExtension(outputFileName))
                : finalOutputFile;

            ReadOnlyCollection<string>? remuxedFiles = null;
            var firstStepSucceeded = false;
            var totalDuration = await totalDurationTask;
            do
            {
                var args1 = GetFFmpegArgs(codec, listFile, !twoStepsConcat ? metadataFile : "", outputToFile);
                Debug.WriteLine($"{Settings.FFmpegName} {args1}");
                outputFileWritten |= outputToFile == outputFileName;
                await Process.Run(Settings.FFmpegName, args1, status => OnConcatStatus(status, totalDuration), Process.OutputType.Error, ctx);

                var concatErrorsStr = concatErrors.ToString();
                concatErrors.Clear();

                if (concatErrorsStr == "")
                {
                    firstStepSucceeded = true;
                    break;
                }

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
                var remuxResponse = await RemuxFiles(tempDir, mediaFiles, OnProgress, ctx);
                if (remuxResponse.IsFailure)
                {
                    await OnError($"Remuxing errors:{Environment.NewLine}{remuxResponse.Message}");
                }
                if (remuxResponse.Data == null)
                {
                    await OnError("Remuxing failed with an unrecoverable error, aborting.");
                    break;
                }

                remuxedFiles = remuxResponse.Data!;
                listFile = await CreateFilesListFile(tempDir, remuxedFiles);

                #endregion
            } while (true);

            if (!firstStepSucceeded && stagedOutputFile != "")
                return;

            #region Second Step of Concatenation
            if (twoStepsConcat)
            {
                listFile = await CreateFilesListFile(tempDir, outputToFile);
                if (listFile == "")
                {
                    // The first-step output is pre-created on disk, so it can only be missing through
                    // outside interference (antivirus quarantine, temp cleanup); without this guard
                    // the second step would run ffmpeg with an empty input path
                    await OnError($"The intermediate output file '{outputToFile}' is missing, the concatenation cannot be completed.");
                    return;
                }

                outputToFile = hasImages
                    ? await GenerateTempOutputFileFrom(tempDir, Path.GetExtension(outputFileName))
                    : finalOutputFile;

                var args2 = GetFFmpegArgs(codec, listFile, metadataFile, outputToFile);
                Debug.WriteLine($"{Settings.FFmpegName} {args2}");
                outputFileWritten |= outputToFile == outputFileName;
                await Process.Run(Settings.FFmpegName, args2, status => OnConcatStatus(status, totalDuration), Process.OutputType.Error, ctx);

                // The error check in the loop above only covers the first step; without this
                // check a second-step failure accumulates in concatErrors and never surfaces
                var secondStepErrors = concatErrors.ToString();
                if (secondStepErrors != "")
                {
                    await OnError(secondStepErrors);
                    if (stagedOutputFile != "")
                        return;
                }
            }
            #endregion

            #region Attach Images
            if (hasImages)
            {
                await OnStatus(extractedImages.Count == 1 ? "Embedding cover image..." : "Embedding cover images...");
                outputFileWritten |= finalOutputFile == outputFileName;
                var imagesResult = await AddImages(outputToFile, extractedImages, finalOutputFile, ctx);
                if (imagesResult.IsFailure)
                {
                    await OnError($"Image embedding errors:{Environment.NewLine}{imagesResult.Message}");
                    if (stagedOutputFile != "")
                        return;
                }
            }
            #endregion

            if (stagedOutputFile != "")
            {
                var stagedOutput = new FileInfo(stagedOutputFile);
                if (!stagedOutput.Exists || stagedOutput.Length == 0)
                {
                    await OnError("Concatenation did not produce a valid output file.");
                    return;
                }

                await Task.Run(() => File.Move(stagedOutputFile, outputFileName, true), CancellationToken.None);
                outputFileWritten = true;
            }
        }
        catch (OperationCanceledException)
        {
            isCancelled = true;
        }
        catch (Exception ex)
        {
            await OnError($"Concatenation exception:{Environment.NewLine}{ex.Message}");
        }
        finally
        {
            await OnStatus("Cleaning up...");

            if (tempDir != "")
                await Task.Run(() => TempDirectory.Delete(tempDir), CancellationToken.None);

            if (stagedOutputFile != "")
            {
                try { await Task.Run(() => File.Delete(stagedOutputFile), CancellationToken.None); }
                catch { /* ignore */ }
            }

            try
            {
                // A cancelled run leaves a partial output file, a failed one can leave an empty one; neither should survive.
                var outputFile = new FileInfo(outputFileName);
                if (outputFile.Exists && outputFileWritten && (isCancelled || outputFile.Length == 0))
                    await Task.Run(() => outputFile.Delete(), CancellationToken.None);
            }
            catch { /* ignore */ }

            if (isCancelled)
                await OnStatus("Cancelled");
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

    private sealed class RemuxProgress(IMediaFileViewModel file, IProcessingStats? stats = null)
    {
        public IMediaFileViewModel File { get; } = file;
        public IProcessingStats? Stats { get; set; } = stats;
    }

    private static async Task<IResponse<ReadOnlyCollection<string>>> RemuxFiles(string tempDir, IReadOnlyList<IMediaFileViewModel> mediaFiles, Func<Progress, Task> onProgress, CancellationToken ctx)
    {
        var sync = new object();
        var errors = new StringBuilder();
        var remuxedFiles = new ConcurrentBag<(IMediaFileViewModel, string)>();
        using var statusMessages = new BlockingCollection<RemuxProgress>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx);
        // ReSharper disable AccessToDisposedClosure
        using var progressTrackingTask = Task.Run(async () => await ProgressTracking(mediaFiles, statusMessages, onProgress, cts.Token), CancellationToken.None);
        // ReSharper restore AccessToDisposedClosure
        
        var remuxTasks = mediaFiles.AsParallel().Select(async file => 
        {
            if (file.IsImage)
                return;
            // ReSharper disable once AccessToDisposedClosure
            var remuxResponse = await RemuxFile(tempDir, file, status => statusMessages.Add(status, CancellationToken.None), ctx);
            remuxedFiles.Add((file, remuxResponse.Data!));
            if (remuxResponse.IsFailure)
                lock (sync) errors.AppendMessage(remuxResponse.Message);
        });
        try { await Task.WhenAll(remuxTasks); }
        finally
        {
            // The progress tracking task must be joined before the using blocks dispose it and its collections
            await cts.CancelAsync();
            try { await progressTrackingTask; }
            catch { /* ignore */ }
        }

        if (errors.Length > 0 && IsUnrecoverableError(remuxedFiles))
        {
            await DeleteAllTempFiles(remuxedFiles);
            // Data must stay null here: the caller aborts on Data == null (issue #43). Returning the
            // now-deleted paths would send the concatenation loop into a rerun against missing files.
            return Response<ReadOnlyCollection<string>>.Failure(errors.ToString());
        }

        var sortedRemuxedFiles = SortRemuxedFiles(mediaFiles, remuxedFiles);

        return errors.Length == 0
            ? Response<ReadOnlyCollection<string>>.Success(sortedRemuxedFiles)
            : Response<ReadOnlyCollection<string>>.Failure(sortedRemuxedFiles, errors.ToString());
    }

    private static bool IsUnrecoverableError(ConcurrentBag<(IMediaFileViewModel, string)> remuxedFiles)
    {
        foreach (var (_, remuxedFile) in remuxedFiles)
        {
            var fileInfo = new FileInfo(remuxedFile);
            if (!fileInfo.Exists || fileInfo.Length == 0)
                return true;
        }

        return false;
    }

    private static async Task DeleteAllTempFiles(ConcurrentBag<(IMediaFileViewModel, string)> remuxedFiles)
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

    private static ReadOnlyCollection<string> SortRemuxedFiles(IReadOnlyList<IMediaFileViewModel> mediaFiles, ConcurrentBag<(IMediaFileViewModel, string)> remuxedFiles)
    {
        // Match by reference, same as the ConcurrentBag scan this replaces; view-model Equals overrides must not affect pairing
        var remuxedByFile = new Dictionary<IMediaFileViewModel, string>(ReferenceEqualityComparer.Instance);
        foreach (var (remuxedMediaFile, remuxedFile) in remuxedFiles)
            remuxedByFile[remuxedMediaFile] = remuxedFile;

        var sortedFiles = new List<string>(mediaFiles.Count);
        foreach (var mediaFile in mediaFiles)
        {
            if (remuxedByFile.TryGetValue(mediaFile, out var remuxedFile))
                sortedFiles.Add(remuxedFile);
        }

        return sortedFiles.AsReadOnly();
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
    
    private static async Task<IResponse<string>> RemuxFile(string tempDir, IMediaFileViewModel mediaFile, Action<RemuxProgress> onStatus, CancellationToken ctx)
    {
        var errors = new StringBuilder();
        var filesList = await CreateFilesListFile(tempDir, [mediaFile]);
        var outputToFile = await GenerateTempOutputFileFrom(tempDir, mediaFile.File.Extension);
        var args = $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{filesList}\" -vn -c:a copy \"{outputToFile}\"";
        Debug.WriteLine($"{Settings.FFmpegName} {args}");
        await Process.Run(Settings.FFmpegName, args, OnStatus, Process.OutputType.Error, ctx);

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
                ? $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -map_metadata -1 -vn {encodingCommand} \"{outputToFile}\""
                : $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -i \"{metadataFile}\" -map_metadata 1 -vn {encodingCommand} \"{outputToFile}\"";
        
        return metadataFile != ""
            ? $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -i \"{metadataFile}\" -map_metadata 1 -vn {encodingCommand} -id3v2_version 3 -write_id3v1 1 \"{outputToFile}\""
            : $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -vn {encodingCommand} \"{outputToFile}\"";
    }

    private static bool IsOutputFileAnInput(IEnumerable<IMediaFileViewModel> mediaFiles, string outputFileName)
    {
        var outputFullName = Path.GetFullPath(outputFileName);
        return mediaFiles.Any(file => file.File.FullName.Equals(outputFullName, StringComparison.OrdinalIgnoreCase));
    }

    private static Task<string> GenerateStagedOutputFileFrom(string outputFileName)
    {
        var outputFile = new FileInfo(outputFileName);
        var outputDirectory = outputFile.Directory?.FullName
            ?? throw new IOException($"Cannot determine the output directory for '{outputFileName}'.");
        return GenerateTempOutputFileFrom(outputDirectory, outputFile.Extension);
    }

    private static async Task<string> GenerateTempOutputFileFrom(string tempDir, string fileExtension)
    {
        Exception? lastError = null;
        for (var tryCount = 0; tryCount < 3; tryCount++)
        {
            try
            {
                var filePath = Path.Combine(tempDir, Guid.NewGuid() + fileExtension);
                await File.WriteAllBytesAsync(filePath, []);
                return filePath;
            }
            catch (Exception ex) { lastError = ex; }
        }

        // Returning "" here would send ffmpeg an empty output path and produce a confusing
        // downstream error (issue #39); all callers run inside try/catch that surfaces the message.
        throw new IOException($"Failed to create a temporary output file: {lastError?.Message}", lastError);
    }

    private const string FILES_LIST_HEADER = "ffconcat version 1.0\n";
    private static async Task<string> CreateFilesListFile(string tempDir, IEnumerable<IMediaFileViewModel> mediaFiles)
    {
        var listFile = Path.Combine(tempDir, Path.GetRandomFileName());
        await using var fileStream = new FileStream(listFile, FileMode.Create, FileAccess.Write);
        await fileStream.WriteAsync(Encoding.UTF8.GetBytes(FILES_LIST_HEADER));
        foreach (var mediaFile in mediaFiles)
        {
            if (!mediaFile.IsImage) 
                await fileStream.WriteAsync(Encoding.UTF8.GetBytes($"file \'{EscapeFileListFilePath(mediaFile.File.FullName)}\'\n"));
        }

        return listFile;
    }

    private static async Task<string> CreateFilesListFile(string tempDir, IReadOnlyList<string> remuxedFiles)
    {
        var listFile = Path.Combine(tempDir, Path.GetRandomFileName());
        await using var fileStream = new FileStream(listFile, FileMode.Create, FileAccess.Write);
        await fileStream.WriteAsync(Encoding.UTF8.GetBytes(FILES_LIST_HEADER));
        
        foreach (var remuxedFile in remuxedFiles) 
            await fileStream.WriteAsync(Encoding.UTF8.GetBytes($"file \'{EscapeFileListFilePath(remuxedFile)}\'\n"));

        return listFile;
    }

    private static async Task<string> CreateFilesListFile(string tempDir, string file)
    {
        var fileInfo = new FileInfo(file);
        if (!fileInfo.Exists)
            return "";

        var listFile = Path.Combine(tempDir, Path.GetRandomFileName());
        await using var fileStream = new FileStream(listFile, FileMode.Create, FileAccess.Write);
        await fileStream.WriteAsync(Encoding.UTF8.GetBytes($"{FILES_LIST_HEADER}file '{EscapeFileListFilePath(fileInfo.FullName)}'\n"));

        return listFile;
    }

    private static string EscapeFileListFilePath(string path) => path.Replace("\\", "/").Replace("'", "'\\''");
    
    private const string METADATA_FILE_START = ";FFMETADATA1\n";
    private static async Task<string> CreateMetadataFile(string tempDir, IConcatParams concatParams, CancellationToken ctx)
    {
        var tagsMetadata = concatParams.TagsEnabled 
            ? GetTagsMetadata(concatParams.OutputTags) 
            : ""; 
        var chaptersMetadata = concatParams.ChaptersEnabled 
            ? GetChaptersMetadata(concatParams.OutputChapters) 
            : "";
        if (tagsMetadata.Length == 0 && chaptersMetadata.Length == 0)
            return "";

        var metadataFile = Path.Combine(tempDir, Path.GetRandomFileName());
        try
        {
            var utf8WithoutBom = new UTF8Encoding(false);
            await using var fileStream = new FileStream(metadataFile, FileMode.Create, FileAccess.Write);
            await using var writer = new StreamWriter(fileStream, utf8WithoutBom);

            await writer.WriteAsync(METADATA_FILE_START.AsMemory(), ctx);
            if (tagsMetadata.Length > 0)
                await writer.WriteAsync(tagsMetadata.AsMemory(), ctx);
            if (chaptersMetadata.Length > 0)
                await writer.WriteAsync(chaptersMetadata.AsMemory(), ctx);

            return metadataFile;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            try { await Task.Run(() => File.Delete(metadataFile), CancellationToken.None); }
            catch { /* ignore */ }
            throw new IOException($"Failed to create metadata file: {ex.Message}", ex);
        }
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

    private static string GetChaptersMetadata(IEnumerable<IMediaChapterViewModel> outputChapters)
    {
        var chapters = new StringBuilder();
        foreach (var chapter in outputChapters)
            AppendChapterMetadata(chapters, chapter);
        return chapters.ToString();
    }

    private static void AppendChapterMetadata(StringBuilder chapters, IMediaChapter chapter)
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
        var absoluteStart = TimeSpan.FromSeconds((double)startSeconds);

        var endSeconds = chapter.End.Value * multiplier;
        var absoluteEnd = TimeSpan.FromSeconds((double)endSeconds);

        var calculatedStart = (long)((decimal)absoluteStart.TotalSeconds * 1000m);
        var calculatedEnd = (long)((decimal)absoluteEnd.TotalSeconds * 1000m);

        chapters.Append("[CHAPTER]\n");
        chapters.Append("TIMEBASE=1/1000\n");
        chapters.Append($"START={calculatedStart}\n");
        chapters.Append($"END={calculatedEnd}\n");
        foreach (var tag in chapter.Tags)
            chapters.Append($"{tag.Name.FilterPrintable().Trim()}={FilterMetadataValue(tag.Value)}\n");
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
            Debug.WriteLine($"{Settings.FFmpegName} {args}");
            var response = await Process.Run(
                Settings.FFmpegName,
                args,
                Process.OutputType.Error,
                ctx);

            return string.IsNullOrEmpty(response)
                ? Response<IResult>.Success()
                : Response<IResult>.Failure(response);
        }
        catch (OperationCanceledException)
        {
            throw;
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
                if (tag.Name == "comment")
                    continue;
                var name = tag.Name.FilterPrintable().Trim();
                if (name.Length == 0 || name.Contains(' ') || name.Contains('"') || name.Contains('='))
                    continue; // Such a name cannot be expressed in name=value form on the command line; drop the tag rather than emit a broken argument
                query.Append($" -metadata:s:v:{i} {name}=\"{EscapeCliArgValue(tag.Value)}\"");
            }
        }

        return query.ToString();
    }

    // The value is placed inside double quotes in ProcessStartInfo.Arguments and parsed back by
    // ffmpeg's C runtime: a backslash run is literal unless it precedes a double quote, in which
    // case each backslash must be doubled and the quote itself escaped. Blanket-doubling every
    // backslash would corrupt interior ones (AC\DC would parse back as AC\\DC).
    private static string EscapeCliArgValue(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        var backslashes = 0;
        foreach (var ch in value)
        {
            if (ch == '\\')
            {
                backslashes++;
                continue;
            }
            if (ch == '"')
            {
                builder.Append('\\', backslashes * 2 + 1).Append('"'); // 2n+1 backslashes + quote -> n literal backslashes + one literal quote
                backslashes = 0;
                continue;
            }
            builder.Append('\\', backslashes).Append(ch);
            backslashes = 0;
        }
        builder.Append('\\', backslashes * 2); // Trailing backslashes precede our closing quote and must be doubled
        return builder.ToString();
    }

    private sealed class ImageFile(IMediaStream mediaStream, string path, bool isTemporaryFile)
    {
        public IMediaStream MediaStream { get; } = mediaStream;
        public string Path { get; } = path;
        public bool IsTemporaryFile { get; } = isTemporaryFile;
    }

    private static async Task<(IReadOnlyList<ImageFile> imageFiles, string errors)> ExtractImages(string tempDir, IEnumerable<IMediaFileViewModel> mediaFiles, CancellationToken ctx)
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
                var (extractedImageFiles, extractionErrors) = await ExtractImages(tempDir, mediaFile, ctx);
                imageFiles.AddRange(extractedImageFiles);
                errors.Append(extractionErrors);
            }
        }

        return (imageFiles, errors.ToString());
    }

    private static async Task<(IReadOnlyList<ImageFile> imageFiles, string errors)> ExtractImages(string tempDir, IMediaFileViewModel mediaFile, CancellationToken ctx)
    {
        var imageStreams = GetImageStreams(mediaFile);
        if (imageStreams.Count == 0)
            return ([], "");

        var errors = new StringBuilder();
        var imageFiles = new List<ImageFile>();
        foreach (var imageStream in imageStreams)
        {
            var outputFileName = Path.Combine(tempDir, Path.GetRandomFileName());
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

    private static ReadOnlyCollection<IMediaStream> GetImageStreams(IMediaFileViewModel mediaFile)
    {
        var streams = new List<IMediaStream>();
        foreach (var stream in mediaFile.Streams)
        {
            if (Settings.SupportedImageCodecs.Contains(stream.CodecName))
                streams.Add(stream);
        }

        return streams.AsReadOnly();
    }

    private static async Task<IResult> ExtractImageStream(string sourceFileName, string outputFileName, int sourceStreamIndex, CancellationToken ctx)
    {
        try
        {
            var args = $"-hide_banner -y -loglevel error -i \"{sourceFileName}\" -map 0:{sourceStreamIndex} -update true -c copy -f image2 \"{outputFileName}\"";
            Debug.WriteLine($"{Settings.FFmpegName} {args}");
            var response = await Process.Run(
                Settings.FFmpegName,
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
        catch (OperationCanceledException)
        {
            throw;
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
                Settings.FFmpegName,
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
                Settings.FFmpegName,
                "-version",
                Process.OutputType.Standard,
                CancellationToken.None);

            if (!response.StartsWith("ffmpeg version"))
                return Result.Failure($"The tool '{Settings.FFmpegName}' is not found");

            response = await Process.Run(
                Settings.FFprobeName,
                "-version",
                Process.OutputType.Standard,
                CancellationToken.None);

            return response.StartsWith("ffprobe version")
                ? Result.Success()
                : Result.Failure($"The tool '{Settings.FFprobeName}' is not found");
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
