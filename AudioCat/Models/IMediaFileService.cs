using AudioCat.ViewModels;
using System.Collections.Generic;

namespace AudioCat.Models;

public interface IMediaFileService
{
    Task<IResponse<IMediaFile>> Probe(string fileFullName, CancellationToken ctx);
    Task<IResult> Concatenate(IReadOnlyList<MediaFileViewModel> mediaFiles, string outputFileName, Action<IProcessingStats> onStatusUpdate, CancellationToken ctx);
    Task<(IReadOnlyList<MediaFileViewModel> mediaFiles, IReadOnlyList<(string filePath, string skipReason)> skippedFiles)> GetMediaFiles(IReadOnlyList<string> fileNames, bool selectMetadata, bool selectCover, CancellationToken ctx);
    public string GetAudioCodec(IReadOnlyCollection<MediaFileViewModel> mediaFiles);
    public Task<IResult> IsAccessible();
}