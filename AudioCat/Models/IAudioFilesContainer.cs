using System.Collections.ObjectModel;
using AudioCat.ViewModels;

namespace AudioCat.Models;

public interface IAudioFilesContainer
{
    ObservableCollection<AudioFileViewModel> Files { get; }
    AudioFileViewModel? SelectedFile { get; set; }
}