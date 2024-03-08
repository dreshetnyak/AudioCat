namespace AudioCat.Models;

public interface IAudioFileService
{
    Task<IResponse<IAudioFile>> Probe(string fileFullName, CancellationToken ctx);
    Task<IResult> Concatenate(IEnumerable<IAudioFile> audioFiles, string outputFileName, Action<IProcessingStats> onStatusUpdate, CancellationToken ctx);
}