using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;
using AudioCat.Windows;

namespace AudioCat;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel ViewModel { get; }
    private IMediaFileToolkitService MediaFileToolkitService { get; }
    private IMediaFilesService MediaFilesService { get; }

    public MainWindow(MainViewModel viewModel, IMediaFileToolkitService mediaFileToolkitService, IMediaFilesService mediaFilesService)
    {
        viewModel.FocusFileDataGrid = FocusFileDataGrid;
        ViewModel = viewModel;
        MediaFileToolkitService = mediaFileToolkitService;
        MediaFilesService = mediaFilesService;
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
        try
        {
            if (e.Data.GetData(DataFormats.FileDrop, true) is not string[] fileNames || fileNames.Length == 0) 
                return;
            var ctrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            Task.Run(async () => await AddDragFiles(fileNames, !ctrlDown));
        }
        catch (COMException ex) when (ex.ErrorCode == unchecked((int)0x8007007A))
        {
            MessageBox.Show(Application.Current.MainWindow!, "The path of the file is too long, please shorten it or drop a different file. Or alternatively configure your OS to allow long paths.", "Path Too Long", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch 
        { /* ignore */ }
    }

    private async Task AddDragFiles(IReadOnlyList<string> fileNames, bool clearExisting)
    {
        try
        {
            ViewModel.IsUserEntryEnabled = false;
            var response = await MediaFilesService.AddMediaFiles(fileNames, clearExisting); // Long operation, we fire the task and forget

            if (response.SkipFiles.Count > 0) 
                await Application.Current.Dispatcher.InvokeAsync(() => new SkippedFilesWindow(response.SkipFiles).ShowDialog());
        }
        catch
        { /* ignore */ }
        finally
        {
            ViewModel.IsUserEntryEnabled = true;
        }
    }
    
    private void OnTagsDataGridKeyUp(object sender, KeyEventArgs eventArgs)
    {
        if (sender is not DataGrid { ItemsSource: ObservableCollection<IMediaTagViewModel> tags } dataGrid)
            return;

        switch (eventArgs.Key)
        {
            case Key.Insert:
                if (dataGrid.SelectedIndex >= 0)
                    tags.Insert(dataGrid.SelectedIndex, new TagViewModel());
                else
                    tags.Add(new TagViewModel());
                EnsureRowSelection(dataGrid);
                break;
            case Key.Up:
                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || dataGrid.SelectedIndex <= 0)
                    break;
                tags.Move(dataGrid.SelectedIndex, dataGrid.SelectedIndex - 1);
                EnsureRowSelection(dataGrid);
                break;
            case Key.Down:
                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || dataGrid.SelectedIndex < 0 || dataGrid.SelectedIndex + 1 >= tags.Count)
                    break;
                tags.Move(dataGrid.SelectedIndex, dataGrid.SelectedIndex + 1);
                EnsureRowSelection(dataGrid);
                break;
        }
    }

    private void OnTagsDataGridPreviewKeyDown(object sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key is Key.Up or Key.Down && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            eventArgs.Handled = true;
    }
}