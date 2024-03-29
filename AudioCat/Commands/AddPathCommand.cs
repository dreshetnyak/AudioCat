using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;

namespace AudioCat.Commands;

public class AddPathCommand(IAudioFileService audioFileService, IAudioFilesContainer audioFilesContainer) : CommandBase
{
    private IAudioFileService AudioFileService { get; } = audioFileService;
    private ObservableCollection<AudioFileViewModel> AudioFiles { get; } = audioFilesContainer.Files;

    protected override async Task<IResult> Command(object? parameter)
    {
        try
        {
            var path = SelectionDialog.ChooseFolder();
            if (path == "")
                return Result.Success();

            foreach (var fileName in Directory.EnumerateFiles(path, "*.mp3", SearchOption.AllDirectories))
            {
                var probeResponse = await AudioFileService.Probe(fileName, CancellationToken.None); // TODO Cancellation support
                if (probeResponse.IsFailure) //TODO Log the error
                    continue;
                AudioFiles.Add(new AudioFileViewModel(probeResponse.Data!, AudioFiles.Count == 0));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Folder selection error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Result.Failure(ex.Message);
        }
    }
}