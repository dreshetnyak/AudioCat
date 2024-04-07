using System.Collections.ObjectModel;
using System.IO;
using AudioCat.Commands;
using System.Windows;
using System.Windows.Input;

namespace AudioCat.Windows;

/// <summary>
/// Interaction logic for SkippedFilesWindow.xaml
/// </summary>
public partial class SkippedFilesWindow : Window
{
    private sealed class SkippedFile : IFileError
    {
        public string FileName { get; init; } = "";
        public string Error { get; init; } = "";
        public string FilePath { get; init; } = "";
    }

    public ICommand CloseDialog { get; }
    public ObservableCollection<IFileError> SkippedFiles { get; } = [];

    public SkippedFilesWindow(IReadOnlyList<(string filePath, string skipReason)> skippedFiles)
    {
        InitializeComponent();
        DataContext = this;
        Owner = Application.Current.MainWindow;
        CloseDialog = new RelayCommand(Close);
        foreach (var file in GetSkippedFiles(skippedFiles))
            SkippedFiles.Add(file);
    }

    private static IEnumerable<IFileError> GetSkippedFiles(
        IReadOnlyList<(string filePath, string skipReason)> skippedFiles)
    {
        foreach (var (filePath, skipReason) in skippedFiles)
        {
            string fileName;
            try { fileName = Path.GetFileName(filePath); }
            catch { fileName = filePath; }

            string path;
            try { path = Path.GetDirectoryName(filePath) ?? filePath; }
            catch { path = filePath; }
                
            yield return new SkippedFile
            {
                FileName = fileName,
                Error = skipReason,
                FilePath = path
            };
        }
    }
}

public interface IFileError
{
    string FileName { get; }
    string Error { get; }
    string FilePath { get; }
}