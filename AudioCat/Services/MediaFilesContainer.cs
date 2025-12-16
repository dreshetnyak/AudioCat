using AudioCat.Models;
using AudioCat.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AudioCat.Services;

internal sealed class MediaFilesContainer : IMediaFilesContainer, INotifyPropertyChanged
{
    public bool DoNotInvokeFilesCollectionChangedEvent { get; set; }
    public ObservableCollection<IMediaFileViewModel> Files { get; } = [];

    public IMediaFileViewModel? SelectedFile
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}