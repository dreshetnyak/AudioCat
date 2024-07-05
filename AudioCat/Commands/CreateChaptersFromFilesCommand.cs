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
            if (parameter is not IConcatParams concatParams)
                throw new ArgumentException("Invalid parameter type");
            var outputChapters = concatParams.OutputChapters;
          
            if (outputChapters.Count > 0)
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

            var startTime = TimeSpan.Zero;
            var chapters = new List<IMediaChapterViewModel>(MediaFiles.Count);
            for (var index = 0; index < MediaFiles.Count; index++)
            {
                var file = MediaFiles[index];
                if (file.IsImage || file.Duration == null)
                    continue;

                var title = Path.GetFileNameWithoutExtension(file.File.Name);
                if (trimStartNonChars)
                    title = title.TrimStartNonChars();

                const decimal divident = 1m;
                const decimal divisor = 1000m;

                var endTime = startTime.Add(file.Duration.Value);
                var calculatedStart = (long)((decimal)startTime.TotalSeconds * divisor);
                var calculatedEnd = (long)((decimal)endTime.TotalSeconds * divisor);

                chapters.Add(new ChapterViewModel
                {
                    Id = index,
                    Start = calculatedStart,
                    End = calculatedEnd,
                    TimeBaseDivident = divident,
                    TimeBaseDivisor = divisor,
                    StartTime = startTime,
                    EndTime = endTime,
                    Title = title
                });

                startTime = endTime;
            }

            if (chapters.Count > 0)
            {
                outputChapters.Clear();
                foreach (var chapter in chapters) 
                    outputChapters.Add(chapter);
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