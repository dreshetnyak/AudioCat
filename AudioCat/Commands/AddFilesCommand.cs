using System.Collections.ObjectModel;
using System.Windows;
using AudioCat.Models;
using AudioCat.Services;

namespace AudioCat.Commands
{
    public sealed class AddFilesCommand(IAudioFileService audioFileService, IAudioFilesContainer audioFilesContainer) : CommandBase
    {
        private IAudioFileService AudioFileService { get; } = audioFileService;
        private ObservableCollection<IAudioFile> AudioFiles { get; } = audioFilesContainer.Files;

        protected override async Task<IResult> Command(object? parameter)
        {
            try
            {
                var fileNames = SelectionDialog.ChooseFilesToOpen("MP3 Audio|*.mp3", true);
                if (fileNames.Length == 0)
                    return Result.Success();

                foreach (var fileName in fileNames)
                {
                    var probeResponse = await AudioFileService.Probe(fileName, CancellationToken.None); // TODO Cancellation support
                    if (probeResponse.IsFailure) //TODO Log the error
                        continue;
                    AudioFiles.Add(probeResponse.Data!);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Files selection error", MessageBoxButton.OK, MessageBoxImage.Error);
                return Result.Failure(ex.Message);
            }
        }
    }
}
