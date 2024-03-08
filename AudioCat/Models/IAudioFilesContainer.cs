using System.Collections.ObjectModel;

namespace AudioCat.Models;

public interface IAudioFilesContainer
{
    ObservableCollection<IAudioFile> Files { get; }
    IAudioFile? SelectedFile { get; set; }
}