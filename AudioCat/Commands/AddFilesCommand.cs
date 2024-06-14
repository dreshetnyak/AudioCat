using System.Collections.ObjectModel;
using System.Windows;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;
using AudioCat.Windows;

namespace AudioCat.Commands;

public sealed class AddFilesCommand(IMediaFilesService mediaFilesService, IMediaFilesContainer mediaFilesContainer) : CommandBase
{
    private IMediaFilesService MediaFilesService { get; } = mediaFilesService;
    private ObservableCollection<IMediaFileViewModel> MediaFiles { get; } = mediaFilesContainer.Files;

    protected override async Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            var selectedCodec = MediaFiles.Count > 0
                ? Services.MediaFilesService.GetAudioCodec(MediaFiles)
                : "";

            var fileNames = SelectionDialog.ChooseFilesToOpen(Settings.GetAddFilesExtensionFilter(selectedCodec), true);
            if (fileNames.Length == 0)
                return Response<object>.Success();

            var sortedFileNames = Files.Sort(fileNames);

            var response = await MediaFilesService.AddMediaFiles(sortedFileNames, false);
            if (response.SkipFiles.Count > 0)
                new SkippedFilesWindow(response.SkipFiles).ShowDialog();
            
            return Response<object>.Success();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Files selection error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Response<object>.Failure(ex.Message);
        }
    }
}