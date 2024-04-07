using System.Collections.ObjectModel;
using System.Windows;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;
using AudioCat.Windows;

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
                var fileNames = SelectionDialog.ChooseFilesToOpen("MP3 Audio|*.mp3|M4B Audio|*.m4b|M4A Audio|*.m4a|AAC Audio|*.aac|Other Audio|*.*", true);
                if (fileNames.Length == 0)
                    return Response<object>.Success();

                var sortedFileNames = Files.Sort(fileNames);

                var (audioFiles, skippedFiles) = await AudioFileService.GetAudioFiles(sortedFileNames, CancellationToken.None); // TODO Cancellation support
                foreach (var file in audioFiles) 
                    AudioFiles.Add(file);

                if (skippedFiles.Count > 0)
                    new SkippedFilesWindow(skippedFiles).ShowDialog();

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
