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

public sealed class ConcatenateCommand(IAudioFileService audioFileService, IAudioFilesContainer audioFilesContainer) : CommandBase
{
    public IAudioFileService AudioFileService { get; } = audioFileService;
    private ObservableCollection<AudioFileViewModel> AudioFiles { get; } = audioFilesContainer.Files;

    private CancellationTokenSource? Cts { get; set; }

    public event StatusEventHandler? StatusUpdate;

    protected override async Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            Cts = new CancellationTokenSource();

            if (AudioFiles.Count == 0)
                return Response<object>.Failure("No files to concatenate");

            var outputFileName = SelectionDialog.ChooseFileToSave("MP3 Audio|*.mp3", GetSuggestedFileName(), GetInitialDirectory());
            if (outputFileName == "")
                return Response<object>.Success();
            
            var concatResult = await AudioFileService.Concatenate(AudioFiles, outputFileName, OnStatusUpdate, CancellationToken.None);
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

    private string GetSuggestedFileName()
    {
        var firstFile = AudioFiles.First().FilePath;

        var fileInfo = new FileInfo(firstFile);
        var suggestedName = fileInfo.Directory?.Name ?? "";
        if (suggestedName != "")
            return suggestedName + ".mp3";
            
        suggestedName = fileInfo.Name;
        if (suggestedName == "")
            return ".mp3";
        var withoutExtension = Path.GetFileNameWithoutExtension(suggestedName);

        return withoutExtension + ".Cat.mp3";
    }

    private string GetInitialDirectory() => 
        new FileInfo(AudioFiles.First().FilePath).Directory?.FullName ?? "";

    private void OnStatusUpdate(IProcessingStats stats) => StatusUpdate?.Invoke(this, new StatusEventArgs(stats));
}