using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;

namespace AudioCat.Commands;

public sealed class StatusEventArgs(IProcessingStats stats) : EventArgs
{
    public IProcessingStats Stats { get; } = stats;
}
public delegate void StatusEventHandler(object sender, StatusEventArgs eventArgs);

public sealed class ConcatenateCommand(IMediaFileToolkitService mediaFileToolkitService, IMediaFilesContainer mediaFilesContainer) : CommandBase
{
    #region Internal Types
    private class ConcatParams(bool tagsEnabled, bool chaptersEnabled) : IConcatParams
    {
        public bool TagsEnabled { get; } = tagsEnabled;
        public bool ChaptersEnabled { get; } = chaptersEnabled;
    }
    #endregion

    private IMediaFileToolkitService MediaFileToolkitService { get; } = mediaFileToolkitService;

    private ObservableCollection<IMediaFileViewModel> MediaFiles { get; } = mediaFilesContainer.Files;

    private CancellationTokenSource? Cts { get; set; }

    public event StatusEventHandler? StatusUpdate;

    protected override async Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            Cts = new CancellationTokenSource();

            if (MediaFiles.Count == 0)
                return Response<object>.Failure("No files to concatenate");

            var codec = MediaFilesService.GetAudioCodec(MediaFiles);

            var firstFile = new FileInfo((MediaFiles.FirstOrDefault(file => !file.IsImage) ?? MediaFiles.First()).FilePath);
            var initialDirectory = GetInitialDirectory(firstFile);
            var outputFileName = SelectionDialog.ChooseFileToSave(
                Settings.GetSaveFileExtensionFilter(codec), 
                GetSuggestedFileName(codec, firstFile),
                initialDirectory);
            if (outputFileName == "")
                return Response<object>.Success();

            var concatParams = parameter as IConcatParams ?? new ConcatParams(true, true);
            var concatResult = await MediaFileToolkitService.Concatenate(MediaFiles, concatParams, outputFileName, OnStatusUpdate, CancellationToken.None);
            return concatResult.IsSuccess
                ? Response<object>.Success()
                : Response<object>.Failure(outputFileName, concatResult.Message);
        }
        finally
        {
            var cts = Cts;
            Cts = null;
            try { cts?.Dispose(); }
            catch { /* ignore */ }
        }
    }

    public void Cancel()
    {
        try { Cts?.Cancel(); }
        catch { /* ignore */ }
    }

    private static string GetSuggestedFileName(string codec, FileInfo firstFile) =>
        (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
            ? Path.GetFileNameWithoutExtension(firstFile.Name)
            : firstFile.Directory?.Name ?? "") + Settings.GetSuggestedFileNameExtension(codec);

    private static string GetInitialDirectory(FileInfo firstFile) =>
        firstFile.Directory?.FullName ?? "";

    private void OnStatusUpdate(IProcessingStats stats) => 
        StatusUpdate?.Invoke(this, new StatusEventArgs(stats));
}