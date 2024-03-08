using System.IO;
using System.Windows;
using AudioCat.Models;
using AudioCat.ViewModels;

namespace AudioCat;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel ViewModel { get; }
    private IAudioFileService AudioFileService { get; }

    public MainWindow(MainViewModel viewModel, IAudioFileService audioFileService)
    {
        ViewModel = viewModel;
        AudioFileService = audioFileService;
        InitializeComponent();
        DataContext = viewModel;
    }

    // Code that accepts drag and drop files
    private void OnDataGridDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop, true) is string[] fileNames && fileNames.Length != 0) 
            _ = AddFilesAsync(fileNames); // Long operation, we fire the task and forget
    }

    private async Task AddFilesAsync(IEnumerable<string> fileNames)
    {
        try
        {
            ViewModel.IsUserEntryEnabled = false;

            var sortedFileNames = fileNames.OrderBy(s => s).ToArray();
            ViewModel.Files.Clear();
            foreach (var fileName in sortedFileNames)
            {
                var probeResponse = await AudioFileService.Probe(fileName, CancellationToken.None);
                if (probeResponse.IsSuccess)
                    ViewModel.Files.Add(probeResponse.Data!);
            }

        }
        finally
        {
            ViewModel.IsUserEntryEnabled = true;
        }
    }
}