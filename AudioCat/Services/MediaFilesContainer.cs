using AudioCat.Models;
using AudioCat.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AudioCat.Services;

internal sealed class MediaFilesContainer : IMediaFilesContainer, INotifyPropertyChanged
{
    private IMediaFileViewModel? _selectedFile;
    public ObservableCollection<IMediaFileViewModel> Files { get; }
    public IMediaFileViewModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            _selectedFile = value;
            OnPropertyChanged();
        }
    }

    public MediaFilesContainer()
    {
        Files = [];
        Files.CollectionChanged += OnFilesCollectionChanged;
    }

    private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.Action != NotifyCollectionChangedAction.Add || eventArgs.NewItems == null)
            return;
        foreach (IMediaFileViewModel item in eventArgs.NewItems)
            item.PropertyChanged += OnFilePropertyChanged;
    }

    private void OnFilePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(IMediaFileViewModel.HasTags) &&
            TagsSourceIsNotSelected() &&
            sender is IMediaFileViewModel { IsImage: false } fileViewModel)
            fileViewModel.IsTagsSource = fileViewModel.HasTags;
    }

    private bool TagsSourceIsNotSelected()
    {
        foreach (var file in Files)
        {
            if (file.IsImage)
                continue;
            if (file.IsTagsSource)
                return false;
        }

        return true;
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}