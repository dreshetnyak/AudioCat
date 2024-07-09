using AudioCat.Models;
using AudioCat.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AudioCat.Services;

internal sealed class MediaFilesContainer : IMediaFilesContainer, INotifyPropertyChanged
{
    private IMediaFileViewModel? _selectedFile;
    public ObservableCollection<IMediaFileViewModel> Files { get; } = [];

    public IMediaFileViewModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            _selectedFile = value;
            OnPropertyChanged();
        }
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}