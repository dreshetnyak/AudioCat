using System.Collections.ObjectModel;
using System.Windows;

namespace AudioCat.Commands
{
    internal sealed class AddFilesCommand(ObservableCollection<AudioFile> audioFiles) : CommandBase
    {
        private ObservableCollection<AudioFile> AudioFiles { get; } = audioFiles;

        protected override Task Command(object? parameter)
        {
            try
            {
                var fileNames = FileSystemSelect.FilesToOpen("MP3 Audio|*.mp3", true);
                if (fileNames.Length == 0)
                    return Task.CompletedTask;

                foreach (var fileName in fileNames)
                    AudioFiles.Add(new AudioFile(fileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Files selection error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return Task.CompletedTask;
        }
    }
}
