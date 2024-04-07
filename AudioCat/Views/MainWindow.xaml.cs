using System.Windows;
using AudioCat.Models;
using AudioCat.ViewModels;
using AudioCat.Windows;

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

    // Code that accepts drag and drop audioFiles
    private void OnDataGridDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop, true) is string[] fileNames && fileNames.Length != 0) 
            _ = AddFilesAsync(fileNames); // Long operation, we fire the task and forget
    }

    private async Task AddFilesAsync(IReadOnlyList<string> fileNames)
    {
        try
        {
            ViewModel.IsUserEntryEnabled = false;

            ViewModel.Files.Clear();
            var (audioFiles, skippedFiles) = await AudioFileService.GetAudioFiles(fileNames, CancellationToken.None);
            foreach (var audioFile in audioFiles)
                ViewModel.Files.Add(audioFile);

            if (ViewModel.Files.Count > 0)
                ViewModel.SelectedFile = ViewModel.Files.First();

            if (skippedFiles.Count > 0) 
                new SkippedFilesWindow(skippedFiles).ShowDialog();
        }
        finally
        {
            ViewModel.IsUserEntryEnabled = true;
        }
    }
}