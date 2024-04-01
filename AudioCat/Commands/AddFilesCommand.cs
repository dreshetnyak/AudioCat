using System.Collections.ObjectModel;
using System.Windows;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;

namespace AudioCat.Commands
{
    public sealed class AddFilesCommand(IAudioFileService audioFileService, IAudioFilesContainer audioFilesContainer) : CommandBase
    {
        private IAudioFileService AudioFileService { get; } = audioFileService;
        private ObservableCollection<AudioFileViewModel> AudioFiles { get; } = audioFilesContainer.Files;

        protected override async Task<IResponse<object>> Command(object? parameter)
        {
            try
            {
                var fileNames = SelectionDialog.ChooseFilesToOpen("MP3 Audio|*.mp3", true);
                if (fileNames.Length == 0)
                    return Response<object>.Success();

                foreach (var fileName in fileNames)
                {
                    var probeResponse = await AudioFileService.Probe(fileName, CancellationToken.None); // TODO Cancellation support
                    if (probeResponse.IsFailure) //TODO Log the error
                        continue;
                    AudioFiles.Add(new AudioFileViewModel(probeResponse.Data!, AudioFiles.Count == 0));
                }

                return Response<object>.Success();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Files selection error", MessageBoxButton.OK, MessageBoxImage.Error);
                return Response<object>.Failure(ex.Message);
            }
        }
    }
}
