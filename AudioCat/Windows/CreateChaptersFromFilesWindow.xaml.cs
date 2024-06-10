using AudioCat.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AudioCat.Windows;

/// <summary>
/// Interaction logic for CreateChaptersFromFilesWindow.xaml
/// </summary>
public partial class CreateChaptersFromFilesWindow : Window, INotifyPropertyChanged
{
    private bool _trimStartingNonChars;

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

    public ICommand CloseDialog { get; }
    public ICommand Create { get; }

    public CreateChaptersFromFilesWindow()
    {
        InitializeComponent();
        DataContext = this;
        Owner = Application.Current.MainWindow;
        CloseDialog = new RelayCommand(Close);
        Create = new RelayCommand(OnCreate);
    }

    private void OnCreate()
    {
        DialogResult = true;
        Close();
    }

    #region INotifyPropertyChanged Implementation
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}