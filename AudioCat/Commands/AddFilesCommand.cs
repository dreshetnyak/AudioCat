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

            var fileNames = SelectionDialog.ChooseFilesToOpen(GetExtensionFilter(selectedCodec), true);
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

    private static string GetExtensionFilter(string codec) =>
        codec switch
        {
            "mp3" => "MP3 Audio|*.mp3|Other Audio|*.*",
            "aac" => "AAC Audio|*.m4b|AAC Audio|*.m4a|AAC Audio|*.aac|Other Audio|*.*",
            "wmav2" => "Windows Media Audio|*.wma|Other Audio|*.*",
            "vorbis" => "OGG Vorbis|*.ogg|Other Audio|*.*",
            _ => "MP3 Audio|*.mp3|AAC Audio|*.m4b|AAC Audio|*.m4a|AAC Audio|*.aac|Windows Media Audio|*.wma|OGG Vorbis|*.ogg|Other Audio|*.*"
        };
}