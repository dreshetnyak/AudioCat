using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace AudioCat.Commands
{
    internal class AddPathCommand(ObservableCollection<AudioFile> audioFiles) : CommandBase
    {
        private ObservableCollection<AudioFile> AudioFiles { get; } = audioFiles;
        
        protected override Task Command(object? parameter)
        {
            try
            {
                var path = FileSystemSelect.Folder();
                if (path == "")
                    return Task.CompletedTask;

                foreach (var fileName in Directory.EnumerateFiles(path, "*.mp3", SearchOption.AllDirectories))
                    AudioFiles.Add(new AudioFile(fileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Folder selection error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return Task.CompletedTask;
        }
    }
}
