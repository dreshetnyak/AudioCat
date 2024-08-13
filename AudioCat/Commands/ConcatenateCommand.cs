using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Input;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;

namespace AudioCat.Commands;

public sealed class StatsEventArgs(IProcessingStats stats) : EventArgs
{
    public IProcessingStats Stats { get; } = stats;
}
public delegate void StatsEventHandler(object sender, StatsEventArgs eventArgs);

public sealed class StatusEventArgs(string status) : EventArgs
{
    public string Status { get; } = status;
}
public delegate void StatusEventHandler(object sender, StatusEventArgs eventArgs);

public sealed class ConcatenateCommand(IMediaFileToolkitService mediaFileToolkitService, IMediaFilesContainer mediaFilesContainer) : CommandBase
{
    #region Internal Types
    private class ConcatParams(bool tagsEnabled, bool chaptersEnabled) : IConcatParams
    {
        public bool TagsEnabled { get; } = tagsEnabled;
        public bool ChaptersEnabled { get; } = chaptersEnabled;
        public ObservableCollection<IMediaTagViewModel> OutputTags { get; } = [];
        public ObservableCollection<IMediaChapterViewModel> OutputChapters { get; } = [];
    }
    #endregion

    private IMediaFileToolkitService MediaFileToolkitService { get; } = mediaFileToolkitService;

    private ObservableCollection<IMediaFileViewModel> MediaFiles { get; } = mediaFilesContainer.Files;

    private CancellationTokenSource? Cts { get; set; }

    protected override async Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            Cts = new CancellationTokenSource();

            if (MediaFiles.Count == 0)
                return Response<object>.Failure("No files to concatenate");

            var codec = MediaFilesService.GetAudioCodec(MediaFiles);

            var firstFile = new FileInfo((MediaFiles.FirstOrDefault(file => !file.IsImage) ?? MediaFiles.First()).FilePath);
            var initialDirectory = GetFileDirectory(firstFile);
            var outputFileName = SelectionDialog.ChooseFileToSave(
                Settings.GetSaveFileExtensionFilter(codec), 
                GetSuggestedFileName(codec, firstFile),
                initialDirectory);
            if (outputFileName == "")
                return Response<object>.Success();

            var errors = new StringBuilder();
            var concatParams = parameter as IConcatParams ?? new ConcatParams(true, true);
            
            MessageEventHandler onConcatErrors = (_, args) => errors.AppendMessage(args.Message);
            try
            {
                MediaFileToolkitService.Error += onConcatErrors;
                await MediaFileToolkitService.Concatenate(MediaFiles, concatParams, outputFileName, CancellationToken.None);
            }
            finally { MediaFileToolkitService.Error -= onConcatErrors; }

            return errors.Length == 0
                ? Response<object>.Success()
                : Response<object>.Failure(outputFileName, errors.ToString());
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
            ? (Path.GetFileNameWithoutExtension(firstFile.Name)).Trim()
            : firstFile.Directory?.Name.Trim() ?? "") + Settings.GetSuggestedFileNameExtension(codec);

    private static string GetFileDirectory(FileInfo firstFile) =>
        firstFile.Directory?.FullName ?? "";
}