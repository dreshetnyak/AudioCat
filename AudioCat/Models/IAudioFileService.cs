using AudioCat.ViewModels;

namespace AudioCat.Models;

public interface IAudioFileService
{
    Task<IResponse<IAudioFile>> Probe(string fileFullName, CancellationToken ctx);
    Task<IResult> Concatenate(IReadOnlyList<AudioFileViewModel> audioFiles, string outputFileName, Action<IProcessingStats> onStatusUpdate, CancellationToken ctx);
    Task<(IReadOnlyCollection<AudioFileViewModel> audioFiles, IReadOnlyList<(string filePath, string skipReason)> skippedFiles)> GetAudioFiles(IReadOnlyList<string> fileNames, CancellationToken ctx);
}