using AudioCat.Models;
using AudioCat.ViewModels;
using AudioCat.Windows;
using System.Diagnostics;

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

    Task<IGetMediaFilesResponse> GetMediaFiles(IReadOnlyList<string> fileNames, bool selectCover, string selectedCodec = "", CancellationToken ctx = default);
    Task<IGetMediaFilesResponse> AddMediaFiles(IReadOnlyList<string> fileNames, bool clearExisting);
}

internal sealed class MediaFilesService(IMediaFilesContainer mediaFilesContainer, IMediaFileToolkitService mediaFileToolkitService) : IMediaFilesService
{
    #region Internal Types
    [DebuggerDisplay("{Path}: Reason: {Reason}")]
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

    #region GetMediaFiles
    public async Task<IMediaFilesService.IGetMediaFilesResponse> GetMediaFiles(IReadOnlyList<string> fileNames, bool selectCover, string selectedCodec = "", CancellationToken ctx = default)
    {
        var codec = selectedCodec;
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
            }

            var mediaFileViewModel = new MediaFileViewModel(probeResponse.Data!, isImageFile);

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
            if (Settings.SupportedAudioCodecs.Contains(stream.CodecName))
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
        mediaFile.Streams.Count == 1 && Settings.SupportedImageCodecs.Contains(mediaFile.Streams[0].CodecName);
    #endregion

    #region AddMediaFiles
    public async Task<IMediaFilesService.IGetMediaFilesResponse> AddMediaFiles(IReadOnlyList<string> fileNames, bool clearExisting)
    {
        var uiDispatcher = System.Windows.Application.Current.Dispatcher;

        var files = MediaFilesContainer.Files;
        if (clearExisting)
            uiDispatcher.Invoke(files.Clear);

        var selectedCodec = files.Count > 0 
            ? GetAudioCodec(files) 
            : "";

        var coverSelected = SelectionFlags.GetCoverSelectedFrom(files);

        var response = await GetMediaFiles(fileNames, !coverSelected, selectedCodec, CancellationToken.None);

        if (selectedCodec == "")
            selectedCodec = GetAudioCodec(response.MediaFiles);
        if (Settings.CodecsThatDoesNotSupportImages.Has(selectedCodec))
        {
            if (files.Any(file => file.IsImage))
                response = SkipFiles(response, selectedCodec);
            else if (response.MediaFiles.Any(file => file.IsImage))
                response = SkipImages(response, selectedCodec);
        }

        var mediaFiles = response.MediaFiles;
        var duplicates = GetDuplicates(files, mediaFiles);
        if (duplicates.Count > 0)
        {
            var duplicatesToAdd = uiDispatcher.Invoke(() =>
            {
                var duplicateFilesWindow = new DuplicateFilesWindow(duplicates);
                duplicateFilesWindow.ShowDialog();
                return duplicateFilesWindow.SelectedDuplicateFiles;
            });

            if (duplicatesToAdd.Count != duplicates.Count)
            {
                var mediaFilesWithoutDuplicates = new List<IMediaFileViewModel>(mediaFiles.Count);
                foreach (var file in mediaFiles)
                {
                    if (duplicates.All(duplicate => duplicate.FilePath != file.FilePath) || duplicatesToAdd.Any(duplicate => duplicate.FilePath == file.FilePath))
                        mediaFilesWithoutDuplicates.Add(file);
                }

                mediaFiles = mediaFilesWithoutDuplicates;
            }
        }
        
        foreach (var audioFile in mediaFiles)
            uiDispatcher.Invoke(() => files.Add(audioFile));

        if (files.Count > 0)
            uiDispatcher.Invoke(() => MediaFilesContainer.SelectedFile = files.First());

        return response;
    }

    private static IMediaFilesService.IGetMediaFilesResponse SkipImages(IMediaFilesService.IGetMediaFilesResponse response, string codec)
    {
        var updatedSkipFiles = new List<IMediaFilesService.ISkipFile>(response.SkipFiles);
        var updatedFiles = new List<IMediaFileViewModel>(response.MediaFiles.Count);
        foreach (var file in response.MediaFiles)
        {
            if (!file.IsImage)
                updatedFiles.Add(file);
            else
                updatedSkipFiles.Add(new SkipFile(file.FileName, $"Images are not supported together with the '{codec}' codec media files."));
        }

        return new GetMediaFilesResponse(updatedFiles, updatedSkipFiles);
    }

    private static IMediaFilesService.IGetMediaFilesResponse SkipFiles(IMediaFilesService.IGetMediaFilesResponse response, string codec)
    {
        var updatedSkipFiles = new List<IMediaFilesService.ISkipFile>(response.SkipFiles);
        var updatedFiles = new List<IMediaFileViewModel>(response.MediaFiles.Count);
        foreach (var file in response.MediaFiles)
        {
            if (file.IsImage)
                updatedFiles.Add(file);
            else
                updatedSkipFiles.Add(new SkipFile(file.FileName, $"The '{codec}' codec media files are not supported together with images."));
        }

        return new GetMediaFilesResponse(updatedFiles, updatedSkipFiles);
    }

    private static IReadOnlyList<IMediaFileViewModel> GetDuplicates(IReadOnlyList<IMediaFileViewModel> existingFiles, IReadOnlyList<IMediaFileViewModel> mediaFiles)
    {
        var duplicateFiles = new List<IMediaFileViewModel>(mediaFiles.Count);
        foreach (var newFile in mediaFiles)
        {
            foreach (var existingFile in existingFiles)
            {
                if (newFile.File.FullName.IsNot(existingFile.File.FullName))
                    continue;
                duplicateFiles.Add(existingFile);
                break;
            }
        }

        return duplicateFiles;
    }



    #endregion

    public static string GetAudioCodec(IReadOnlyCollection<IMediaFileViewModel> mediaFiles)
    {
        foreach (var file in mediaFiles)
        {
            var codec = GetCodecName(file.Streams);
            if (!string.IsNullOrEmpty(codec))
                return codec;
        }

        return "";
    }
}