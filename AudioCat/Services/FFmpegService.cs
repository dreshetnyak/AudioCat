using AudioCat.FFmpeg;
using AudioCat.Models;
using System.IO;
using System.Text;

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

    public async Task<IResult> Concatenate(IEnumerable<IAudioFile> audioFiles, string outputFileName, Action<IProcessingStats> onStatusUpdate, CancellationToken ctx)
    {
        try
        {
            var listFile = await Task.Run(() => CreateFilesListFile(audioFiles), ctx);
            
            // TODO Audio streams mapping
            // TODO Subtitle streams
            // TODO Cover image streams pass-through


            await Process.Run(
                "ffmpeg.exe", 
                $"-y -loglevel quiet -stats -stats_period 0.1 -f concat -safe 0 -i \"{listFile}\" -c copy \"{outputFileName}\"",
                statusLine => onStatusUpdate(new FFmpegStats(statusLine)), 
                Process.OutputType.Error, 
                ctx);

            try { await Task.Run(() => File.Delete(listFile), CancellationToken.None); }
            catch { /* ignore */ }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
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
}