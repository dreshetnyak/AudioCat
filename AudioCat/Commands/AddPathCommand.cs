using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using AudioCat.Models;
using AudioCat.Services;
using AudioCat.ViewModels;
using AudioCat.Windows;

namespace AudioCat.Commands;

public class AddPathCommand(IAudioFileService audioFileService, IAudioFilesContainer audioFilesContainer) : CommandBase
{
    private IAudioFileService AudioFileService { get; } = audioFileService;
    private ObservableCollection<AudioFileViewModel> AudioFiles { get; } = audioFilesContainer.Files;

    protected override async Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            var path = SelectionDialog.ChooseFolder();
            if (path == "")
                return Response<object>.Success();

            var fileNames = Directory.EnumerateFiles(path, "*.mp3", SearchOption.AllDirectories).ToArray();
            var sortedFileNames = Files.Sort(fileNames);

            var (selectMetadata, selectCover) = SelectionFlags.GetFrom(AudioFiles);
            var (audioFiles, skippedFiles) = await AudioFileService.GetAudioFiles(sortedFileNames, !selectMetadata, !selectCover, CancellationToken.None); // TODO Cancellation support
            foreach (var file in audioFiles)
                AudioFiles.Add(file);

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