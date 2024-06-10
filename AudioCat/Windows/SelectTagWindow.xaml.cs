using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AudioCat.Commands;
using System.Windows;
using System.Windows.Input;

namespace AudioCat.Windows;

/// <summary>
/// Interaction logic for SelectTagWindow.xaml
/// </summary>
public partial class SelectTagWindow : Window, INotifyPropertyChanged
{
    #region Backing Fields
    private bool _trimStartingNonChars;
    private string _selectedTagName = "";

    #endregion

    public string SelectedTagName
    {
        get => _selectedTagName;
        set
        {
            if (value == _selectedTagName) 
                return;
            _selectedTagName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCreateEnabled));
        }
    }
    public bool TrimStartingNonChars
    {
        get => _trimStartingNonChars;
        set
        {
            if (value == _trimStartingNonChars) 
                return;
            _trimStartingNonChars = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> TagNames { get; } = [];

    public bool IsCreateEnabled => !string.IsNullOrEmpty(SelectedTagName);

    public ICommand CloseDialog { get; }
    public ICommand Create { get; }

    public SelectTagWindow(IReadOnlyList<string> tagNames)
    {
        InitializeComponent();
        DataContext = this;
        Owner = Application.Current.MainWindow;
        CloseDialog = new RelayCommand(Close);
        Create = new RelayCommand(OnCreate);
        foreach (var tagName in tagNames)
            TagNames.Add(tagName);
    }

    private void OnCreate()
    {
        DialogResult = true;
        Close();
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}