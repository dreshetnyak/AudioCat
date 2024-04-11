using System.Windows;
using System.Windows.Controls;
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
        viewModel.FocusFileDataGrid = FocusFileDataGrid;
        ViewModel = viewModel;
        AudioFileService = audioFileService;
        InitializeComponent();
        DataContext = viewModel;
    }

    private void FocusFileDataGrid() => EnsureRowSelection(FileDataGrid);

    public static IResult EnsureRowSelection(DataGrid dataGrid)
    {
        if (!dataGrid.SelectionUnit.Equals(DataGridSelectionUnit.FullRow))
            return Result.Failure("The SelectionUnit of the DataGrid must be set to FullRow");

        var rowIndex = dataGrid.SelectedIndex;
        if (rowIndex < 0 || rowIndex > dataGrid.Items.Count - 1)
            return Result.Failure($"{rowIndex} is an invalid row index");

        try
        {
            if (dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) is DataGridRow row) // Called when the user press Up or Down arrows
                row.Focus();
            else // Called when user press Ctrl+Up or Ctrl+Down arrows
            {
                var item = dataGrid.Items[rowIndex];
                if (item != null)
                    dataGrid.ScrollIntoView(item);

                var cellContent = dataGrid.Columns[0].GetCellContent(dataGrid.SelectedItem);
                var cell = cellContent?.Parent as DataGridCell;
                cell?.Focus();
            }
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }

        return Result.Success();
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
            var (audioFiles, skippedFiles) = await AudioFileService.GetAudioFiles(fileNames, true, true, CancellationToken.None);
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