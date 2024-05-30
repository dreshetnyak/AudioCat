using System.Collections.ObjectModel;
using AudioCat.ViewModels;

namespace AudioCat.Models;

public interface IMediaFilesContainer
{
    ObservableCollection<IMediaFileViewModel> Files { get; }
    IMediaFileViewModel? SelectedFile { get; set; }
}