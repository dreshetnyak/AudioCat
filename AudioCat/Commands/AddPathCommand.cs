using System.IO;
using System.Windows;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.Windows;

namespace AudioCat.Commands;

public sealed class AddPathCommand(IMediaFilesService mediaFilesService) : CommandBase
{
    private IMediaFilesService MediaFilesService { get; } = mediaFilesService;

    protected override async Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            var path = SelectionDialog.ChooseFolder();
            if (path == "")
                return Response<object>.Success();

            // The recursive walk and the sort run synchronously and can take seconds on a large
            // tree or a network share, so they must stay off the UI thread
            var sortedFileNames = await Task.Run(() =>
                Files.Sort(Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToArray()));

            var response = await MediaFilesService.AddMediaFiles(sortedFileNames, false);
            if (response.SkippedFiles.Count > 0)
                new SkippedFilesWindow(response.SkippedFiles).ShowDialog();

            return Response<object>.Success();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Folder selection error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Response<object>.Failure(ex.Message);
        }
    }
}