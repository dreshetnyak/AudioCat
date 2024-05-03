using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private IMediaFileService MediaFileService { get; }

    public MainWindow(MainViewModel viewModel, IMediaFileService mediaFileService)
    {
        viewModel.FocusFileDataGrid = FocusFileDataGrid;
        ViewModel = viewModel;
        MediaFileService = mediaFileService;
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
    
    // Code that accepts drag and drop mediaFiles
    private void OnDataGridDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop, true) is not string[] fileNames || fileNames.Length == 0) 
            return;
        var isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl);
        _ = AddFilesAsync(fileNames, isCtrlPressed); // Long operation, we fire the task and forget
    }

    private async Task AddFilesAsync(IReadOnlyList<string> fileNames, bool isCtrlPressed)
    {
        try
        {
            ViewModel.IsUserEntryEnabled = false;

            if (!isCtrlPressed)
                ViewModel.Files.Clear();

            var (mediaFiles, skippedFiles) = await MediaFileService.GetMediaFiles(fileNames, IsSelectMetadata(), IsSelectCover(), CancellationToken.None);
            foreach (var audioFile in mediaFiles)
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

    private bool IsSelectMetadata()
    {
        foreach (var file in ViewModel.Files)
        {
            if (file.IsTagsSource)
                return false;
        }

        return true;
    }

    private bool IsSelectCover()
    {
        foreach (var file in ViewModel.Files)
        {
            if (file.IsCoverSource)
                return false;
        }

        return true;
    }
}