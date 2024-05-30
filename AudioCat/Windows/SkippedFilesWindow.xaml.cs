using System.Collections.ObjectModel;
using System.IO;
using AudioCat.Commands;
using System.Windows;
using System.Windows.Input;
using AudioCat.Services;

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

    public SkippedFilesWindow(IReadOnlyList<IMediaFilesService.ISkipFile> skippedFiles)
    {
        InitializeComponent();
        DataContext = this;
        Owner = Application.Current.MainWindow;
        CloseDialog = new RelayCommand(Close);
        foreach (var file in GetSkippedFiles(skippedFiles))
            SkippedFiles.Add(file);
    }

    private static IEnumerable<IFileError> GetSkippedFiles(IReadOnlyList<IMediaFilesService.ISkipFile> skippedFiles)
    {
        foreach (var skippedFile in skippedFiles)
        {
            string fileName;
            try { fileName = Path.GetFileName(skippedFile.Path); }
            catch { fileName = skippedFile.Path; }

            string path;
            try { path = Path.GetDirectoryName(skippedFile.Path) ?? skippedFile.Path; }
            catch { path = skippedFile.Path; }
                
            yield return new SkippedFile
            {
                FileName = fileName,
                Error = skippedFile.Reason,
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