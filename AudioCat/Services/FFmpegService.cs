using System.Diagnostics;
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

    public async Task<IResult> Concatenate(IEnumerable<AudioFileViewModel> audioFiles, string outputFileName, Action<IProcessingStats> onStatusUpdate, CancellationToken ctx)
    {
        var errorMessage = new StringBuilder();
        try
        {
            var listFileTask = Task.Run(() => CreateFilesListFile(audioFiles), ctx);
            var metadataFileTask = CreateMetadataFile(audioFiles, ctx);

            var listFile = await listFileTask;
            var metadataFile = await metadataFileTask;

            var args = metadataFile != ""
                ? $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -i \"{metadataFile}\" -map_metadata 1 -c copy -id3v2_version 3 -write_id3v1 1 \"{outputFileName}\""
                : $"-hide_banner -y -loglevel error -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -c copy \"{outputFileName}\"";

            await Process.Run(
                "ffmpeg.exe", 
                args,
                OnStatus, 
                Process.OutputType.Error, 
                ctx);

            // TODO Register errors
            // TODO If there was errors offer to remove the output file if such was created

            // Delete the list file
            try { await Task.Run(() => File.Delete(listFile), CancellationToken.None); }
            catch { /* ignore */ }

            // Delete the metadata file
            if (metadataFile != "")
            {
                try { await Task.Run(() => File.Delete(metadataFile), CancellationToken.None); }
                catch { /* ignore */ }
            }

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

    private static string CreateFilesListFile(IEnumerable<IAudioFile> audioFiles)
    {
        var listFile = Path.GetTempFileName();
        var sb = new StringBuilder();
        foreach (var audioFile in audioFiles)
        {
            var fileName = audioFile.File.FullName;
            if (fileName.Contains('\'')) // Escape quotation marks contained in the file name
                fileName = fileName.Replace("\'", "\\\'", StringComparison.Ordinal); //TODO Test if it actually works
            sb.AppendLine($"file \'{fileName}\'");
        }

        File.WriteAllText(listFile, sb.ToString());
        return listFile;
    }

    private static async Task<string> CreateMetadataFile(IEnumerable<AudioFileViewModel> audioFiles, CancellationToken ctx)
    {
        var file = audioFiles.FirstOrDefault(file => file.IsTagsSource);
        if (file == null)
            return "";

        var metadataFile = Path.GetTempFileName();
        var extractResult = await ExtractMetadata(file.FilePath, metadataFile, ctx);
        
        return extractResult.IsSuccess 
            ? metadataFile 
            : "";
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
}