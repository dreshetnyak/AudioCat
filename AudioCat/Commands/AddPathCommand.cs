using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;
using AudioCat.Windows;

namespace AudioCat.Commands;

public sealed class AddPathCommand(IMediaFileService mediaFileService, IMediaFilesContainer mediaFilesContainer) : CommandBase
{
    private IMediaFileService MediaFileService { get; } = mediaFileService;
    private ObservableCollection<MediaFileViewModel> MediaFiles { get; } = mediaFilesContainer.Files;

    protected override async Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            var path = SelectionDialog.ChooseFolder();
            if (path == "")
                return Response<object>.Success();

            var fileNames = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToArray();
            var sortedFileNames = Files.Sort(fileNames);

            var (selectMetadata, selectCover) = SelectionFlags.GetFrom(MediaFiles);
            var (mediaFiles, skippedFiles) = await MediaFileService.GetMediaFiles(sortedFileNames, !selectMetadata, !selectCover, CancellationToken.None); // TODO Cancellation support
            foreach (var file in mediaFiles)
                MediaFiles.Add(file);

            if (skippedFiles.Count > 0)
                new SkippedFilesWindow(skippedFiles).ShowDialog();

            return Response<object>.Success();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Folder selection error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Response<object>.Failure(ex.Message);
        }
    }
}