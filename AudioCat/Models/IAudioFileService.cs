using AudioCat.ViewModels;

namespace AudioCat.Models;

public interface IAudioFileService
{
    Task<IResponse<IAudioFile>> Probe(string fileFullName, CancellationToken ctx);
    Task<IResult> Concatenate(IEnumerable<AudioFileViewModel> audioFiles, string outputFileName, Action<IProcessingStats> onStatusUpdate, CancellationToken ctx);
}