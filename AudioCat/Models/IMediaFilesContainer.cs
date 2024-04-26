using System.Collections.ObjectModel;
using AudioCat.ViewModels;

namespace AudioCat.Models;

public interface IMediaFilesContainer
{
    ObservableCollection<MediaFileViewModel> Files { get; }
    MediaFileViewModel? SelectedFile { get; set; }
}