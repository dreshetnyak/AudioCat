using System.Collections;
using AudioCat.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AudioCat.ViewModels;

namespace AudioCat.Windows;

/// <summary>
/// Interaction logic for DuplicateFilesWindow.xaml
/// </summary>
public partial class DuplicateFilesWindow : Window, INotifyPropertyChanged
{
    private bool _isAddEnabled;
    
    public ObservableCollection<IMediaFileViewModel> DuplicateFiles { get; } = [];

    public IReadOnlyList<IMediaFileViewModel> SelectedDuplicateFiles =>
        DialogResult.HasValue && DialogResult.Value 
            ? DuplicateFilesDataGrid.SelectedItems.Cast<IMediaFileViewModel>().ToArray()
            : [];

    public bool IsAddEnabled
    {
        get => _isAddEnabled;
        set
        {
            if (value == _isAddEnabled) 
                return;
            _isAddEnabled = value;
            OnPropertyChanged();
        }
    }

    public ICommand CloseDialog { get; }
    public ICommand AddSelected { get; }

    public DuplicateFilesWindow(IReadOnlyList<IMediaFileViewModel> duplicateFiles)
    {
        InitializeComponent();
        DataContext = this;
        Owner = Application.Current.MainWindow;
        CloseDialog = new RelayCommand(Close);
        AddSelected = new RelayCommand(OnAddSelected);
        foreach (var duplicateFile in duplicateFiles)
            DuplicateFiles.Add(duplicateFile);
    }

    private void OnAddSelected()
    {
        DialogResult = true;
        Close();
    }

    private void OnDuplicatesSelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        if (sender is DataGrid dataGrid)
            IsAddEnabled = dataGrid.SelectedItems.Count > 0;
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}