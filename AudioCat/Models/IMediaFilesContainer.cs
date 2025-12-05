using System.Collections.ObjectModel;
using AudioCat.ViewModels;

namespace AudioCat.Models;

public interface IMediaFilesContainer
{
    bool DoNotInvokeFilesCollectionChangedEvent { get; set; } // Set to true while adding multiple files to avoid multiple invocations of the Files.CollectionChanged event, set to false for the last file
    ObservableCollection<IMediaFileViewModel> Files { get; }
    IMediaFileViewModel? SelectedFile { get; set; }
}