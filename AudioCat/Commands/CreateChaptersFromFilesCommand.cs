using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using AudioCat.Models;
using AudioCat.ViewModels;
using AudioCat.Windows;

namespace AudioCat.Commands;

public sealed class CreateChaptersFromFilesCommand(IMediaFilesContainer mediaFilesContainer) : CommandBase
{
    private ObservableCollection<IMediaFileViewModel> MediaFiles { get; } = mediaFilesContainer.Files;

    protected override Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            if (MediaFiles.ChaptersAlreadyExist())
            {
                var confirmResult = MessageBox.Show("Creating chapters based on file names will delete any existing chapters. Do you want to proceed?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirmResult == MessageBoxResult.No)
                    return Task.FromResult(Response<object>.Success());
            }

            var createChaptersWindow = new CreateChaptersFromFilesWindow();
            var result = createChaptersWindow.ShowDialog();
            if (!result.HasValue || !result.Value)
                return Task.FromResult(Response<object>.Success());
            var trimStartNonChars = createChaptersWindow.TrimStartingNonChars;
            
            foreach (var file in MediaFiles)
            {
                if (file.IsImage || file.Duration == null)
                    continue;

                var title = Path.GetFileNameWithoutExtension(file.File.Name);
                if (trimStartNonChars)
                    title = title.TrimStartNonChars();

                if (file.Chapters.Count > 0)
                    file.Chapters.Clear();

                file.Chapters.Add(new ChapterViewModel
                {
                    Id = 0,
                    Start = 0,
                    End = (long)file.Duration.Value.TotalMilliseconds,
                    TimeBaseDivident = 1,
                    TimeBaseDivisor = 1000,
                    StartTime = TimeSpan.Zero,
                    EndTime = file.Duration,
                    Title = title
                });
            }

            return Task.FromResult(Response<object>.Success());
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Chapters creation error", MessageBoxButton.OK, MessageBoxImage.Error);
            return Task.FromResult(Response<object>.Failure(ex.Message));
        }
    }
}