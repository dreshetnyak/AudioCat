using AudioCat.ViewModels;

namespace AudioCat.Models;

public interface IMediaFileToolkitService
{
    Task<IResponse<IMediaFile>> Probe(string fileFullName, CancellationToken ctx);
    Task<IResponse<IReadOnlyList<IInterval>>> ScanForSilence(string fileFullName, int durationMilliseconds, int silenceThreshold, CancellationToken ctx);
    Task Concatenate(IReadOnlyList<IMediaFileViewModel> mediaFiles, IConcatParams concatParams, string outputFileName, CancellationToken ctx);
    public Task<IResult> IsAccessible();

    public event ProgressEventHandler? Progress;
    public event MessageEventHandler? Status;
    public event MessageEventHandler? Error;
}