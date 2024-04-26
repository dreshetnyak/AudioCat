using System.Collections.ObjectModel;
using System.IO;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;

namespace AudioCat.Commands;

public sealed class StatusEventArgs(IProcessingStats stats) : EventArgs
{
    public IProcessingStats Stats { get; } = stats;
}
public delegate void StatusEventHandler(object sender, StatusEventArgs eventArgs);

public sealed class ConcatenateCommand(IMediaFileService mediaFileService, IMediaFilesContainer mediaFilesContainer) : CommandBase
{
    public IMediaFileService MediaFileService { get; } = mediaFileService;
    private ObservableCollection<MediaFileViewModel> MediaFiles { get; } = mediaFilesContainer.Files;

    private CancellationTokenSource? Cts { get; set; }

    public event StatusEventHandler? StatusUpdate;

    protected override async Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            Cts = new CancellationTokenSource();

            if (MediaFiles.Count == 0)
                return Response<object>.Failure("No files to concatenate");

            var codec = MediaFileService.GetAudioCodec(MediaFiles);

            var outputFileName = SelectionDialog.ChooseFileToSave(
                codec == "aac" ? "AAC Audio|*.m4b" : "MP3 Audio|*.mp3", 
                GetSuggestedFileName(codec), 
                GetInitialDirectory());
            if (outputFileName == "")
                return Response<object>.Success();
            
            var concatResult = await MediaFileService.Concatenate(MediaFiles, outputFileName, OnStatusUpdate, CancellationToken.None);
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

    private string GetSuggestedFileName(string codec)
    {
        var firstFile = MediaFiles.First().FilePath;

        var extension = codec == "aac" ? ".m4b" : ".mp3";

        var fileInfo = new FileInfo(firstFile);
        var suggestedName = fileInfo.Directory?.Name ?? "";
        if (suggestedName != "")
            return suggestedName + extension;
            
        suggestedName = fileInfo.Name;
        if (suggestedName == "")
            return extension;
        var withoutExtension = Path.GetFileNameWithoutExtension(suggestedName);

        return withoutExtension + ".Cat" + extension;
    }

    private string GetInitialDirectory() => 
        new FileInfo(MediaFiles.First().FilePath).Directory?.FullName ?? "";

    private void OnStatusUpdate(IProcessingStats stats) => StatusUpdate?.Invoke(this, new StatusEventArgs(stats));
}