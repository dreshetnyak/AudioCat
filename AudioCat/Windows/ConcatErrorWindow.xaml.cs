using System.IO;
using System.Windows;
using System.Windows.Input;
using AudioCat.Commands;

namespace AudioCat.Windows;

/// <summary>
/// Interaction logic for ConcatError.xaml
/// </summary>
public partial class ConcatErrorWindow : Window
{
    private string OutputFilePath { get; }
    public string Errors { get; }
    public Visibility DeleteOutputFileVisibility { get; }

    public ICommand DeleteOutputFile { get; }
    public ICommand CloseDialog { get; }
    
    public ConcatErrorWindow(string errors, string outputFilePath)
    {
        Errors = errors;
        OutputFilePath = outputFilePath;
        DeleteOutputFileVisibility = File.Exists(outputFilePath) ? Visibility.Visible : Visibility.Collapsed;
        InitializeComponent();
        DataContext = this;
        CloseDialog = new RelayCommand(Close);
        DeleteOutputFile = new RelayCommand(OnDeleteOutputFile);
        Owner = Application.Current.MainWindow;
    }

    private void OnDeleteOutputFile()
    {
        try { File.Delete(OutputFilePath); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Output File Deletion Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        Close();
    }
}