using AudioCat.ViewModels;

namespace AudioCat.Models;

public interface IMediaFileToolkitService
{
    Task<IResponse<IMediaFile>> Probe(string fileFullName, CancellationToken ctx);
    Task<IResult> Concatenate(IReadOnlyList<IMediaFileViewModel> mediaFiles, string outputFileName, Action<IProcessingStats> onStatusUpdate, CancellationToken ctx);
    public Task<IResult> IsAccessible();
}