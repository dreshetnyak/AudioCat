using AudioCat.Models;
using AudioCat.ViewModels;

namespace AudioCat.Services;

public interface IMediaFilesService
{
    #region Sub Types
    public interface ISkipFile
    {
        string Path { get; }
        string Reason { get; }
    }

    public interface IGetMediaFilesResponse
    {
        IReadOnlyList<IMediaFileViewModel> MediaFiles { get; }
        IReadOnlyList<ISkipFile> SkipFiles { get; }
    }

    #endregion

    Task<IGetMediaFilesResponse> GetMediaFiles(IReadOnlyList<string> fileNames, bool selectMetadata, bool selectCover, CancellationToken ctx);
    Task<IGetMediaFilesResponse> AddMediaFiles(IReadOnlyList<string> fileNames, bool clearExisting);
}

internal sealed class MediaFilesService(IMediaFilesContainer mediaFilesContainer, IMediaFileToolkitService mediaFileToolkitService) : IMediaFilesService
{
    #region Internal Types
    private sealed class SkipFile(string path, string reason) : IMediaFilesService.ISkipFile
    {
        public string Path { get; } = path;
        public string Reason { get; } = reason;
    }

    private sealed class GetMediaFilesResponse(IReadOnlyList<IMediaFileViewModel> mediaFiles, IReadOnlyList<IMediaFilesService.ISkipFile> skipFiles) : IMediaFilesService.IGetMediaFilesResponse
    {
        public IReadOnlyList<IMediaFileViewModel> MediaFiles { get; } = mediaFiles;
        public IReadOnlyList<IMediaFilesService.ISkipFile> SkipFiles { get; } = skipFiles;
    }

    #endregion

    private IMediaFilesContainer MediaFilesContainer { get; } = mediaFilesContainer;
    private IMediaFileToolkitService MediaFileToolkitService { get; } = mediaFileToolkitService;

    private static IEnumerable<string> SupportedAudioCodecs { get; } = ["mp3", "aac"];
    private static IEnumerable<string> SupportedImageCodecs { get; } = ["mjpeg", "png"];

    #region GetMediaFiles
    public async Task<IMediaFilesService.IGetMediaFilesResponse> GetMediaFiles(IReadOnlyList<string> fileNames, bool selectMetadata, bool selectCover, CancellationToken ctx)
    {
        var codec = "";
        var isTagsSourceSelected = false;
        var isCoverSourceSelected = false;

        var mediaFiles = new List<IMediaFileViewModel>(fileNames.Count);
        var skippedFiles = new List<IMediaFilesService.ISkipFile>();

        var sortedFiles = Files.Sort(fileNames);
        var probeFiles = await ProbeFiles(sortedFiles, ctx);
        
        for (var index = 0; index < probeFiles.Count; index++)
        {
            var probeResponse = probeFiles[index];
            if (probeResponse.IsFailure)
            {
                skippedFiles.Add(new SkipFile(sortedFiles[index], probeResponse.Message));
                continue;
            }

            var isTagsSource = false;
            var file = probeResponse.Data!;
            var isImageFile = IsImageFile(file);
            if (!isImageFile)
            {
                var codecSelectResult = SelectCodec(file, ref codec);
                if (codecSelectResult.IsFailure)
                {
                    skippedFiles.Add(new SkipFile(sortedFiles[index], codecSelectResult.Message));
                    continue;
                }

                if (selectMetadata)
                {
                    isTagsSource = !isTagsSourceSelected && file.Tags.Count > 0;
                    if (isTagsSource)
                        isTagsSourceSelected = true;
                }
            }

            var mediaFileViewModel = new MediaFileViewModel(probeResponse.Data!, isTagsSource, isImageFile);

            if (!isImageFile && selectCover && !isCoverSourceSelected && mediaFileViewModel.HasCover)
            {
                mediaFileViewModel.IsCoverSource = true;
                isCoverSourceSelected = true;
            }

            mediaFiles.Add(mediaFileViewModel);
        }

        return new GetMediaFilesResponse(mediaFiles, skippedFiles);
    }

    private async Task<IReadOnlyList<IResponse<IMediaFile>>> ProbeFiles(IReadOnlyList<string> fileNames, CancellationToken ctx)
    {
        var sortedFiles = Files.Sort(fileNames);

        var probeTasks = new List<Task<IResponse<IMediaFile>>>(fileNames.Count);
        foreach (var filePath in sortedFiles)
            probeTasks.Add(MediaFileToolkitService.Probe(filePath, ctx));

        var mediaFiles = new List<IResponse<IMediaFile>>(fileNames.Count);
        foreach (var probeTask in probeTasks)
            mediaFiles.Add(await probeTask);

        return mediaFiles;
    }

    private static IResult SelectCodec(IMediaFile mediaFile, ref string selectedCodec)
    {
        if (selectedCodec == "") // Acceptable codec has not been selected yet
            return (selectedCodec = GetCodecName(mediaFile.Streams)) != ""
                ? Result.Success()
                : Result.Failure("Doesn't contain any supported audio streams");

        return HasStreamWithCodec(mediaFile, selectedCodec)
            ? Result.Success()
            : Result.Failure($"Doesn't contain any audio stream encoded with '{selectedCodec}' codec");
    }

    private static string GetCodecName(IEnumerable<IMediaStream> mediaFileStreams)
    {
        foreach (var stream in mediaFileStreams)
        {
            if (SupportedAudioCodecs.Contains(stream.CodecName))
                return stream.CodecName ?? "";
        }

        return "";
    }

    private static bool HasStreamWithCodec(IMediaFile mediaFile, string codecName)
    {
        foreach (var stream in mediaFile.Streams)
        {
            if (stream.CodecName == codecName)
                return true;
        }

        return false;
    }

    private static bool IsImageFile(IMediaFile mediaFile) =>
        mediaFile.Streams.Count == 1 && SupportedImageCodecs.Contains(mediaFile.Streams[0].CodecName);
    #endregion

    #region AddMediaFiles

    public async Task<IMediaFilesService.IGetMediaFilesResponse> AddMediaFiles(IReadOnlyList<string> fileNames, bool clearExisting)
    {
        var uiDispatcher = System.Windows.Application.Current.Dispatcher;

        var files = MediaFilesContainer.Files;
        if (clearExisting)
            uiDispatcher.Invoke(files.Clear);

        var response = await GetMediaFiles(fileNames, IsSelectMetadata(), IsSelectCover(), CancellationToken.None);
        foreach (var audioFile in response.MediaFiles)
            uiDispatcher.Invoke(() => files.Add(audioFile));

        if (files.Count > 0)
            uiDispatcher.Invoke(() => MediaFilesContainer.SelectedFile = files.First());

        return response;
    }

    private bool IsSelectMetadata()
    {
        foreach (var file in MediaFilesContainer.Files)
        {
            if (file.IsTagsSource)
                return false;
        }

        return true;
    }

    private bool IsSelectCover()
    {
        foreach (var file in MediaFilesContainer.Files)
        {
            if (file.IsCoverSource)
                return false;
        }

        return true;
    }

    #endregion
    
    public static string GetAudioCodec(IReadOnlyCollection<IMediaFileViewModel> mediaFiles)
    {
        foreach (var audioFile in mediaFiles)
        {
            string codec;
            if ((codec = GetCodecName(audioFile.Streams)) != "")
                return codec;
        }

        return "";
    }
}