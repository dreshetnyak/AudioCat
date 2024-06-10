using AudioCat.Models;
using AudioCat.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using AudioCat.Windows;
using AudioCat.Services;

namespace AudioCat.Commands;

public sealed class CreateChaptersFromTagsCommand(IMediaFilesContainer mediaFilesContainer) : CommandBase
{
    private ObservableCollection<IMediaFileViewModel> MediaFiles { get; } = mediaFilesContainer.Files;

    protected override Task<IResponse<object>> Command(object? parameter)
    {
        try
        {
            if (MediaFiles.ChaptersAlreadyExist())
            {
                var confirmResult = MessageBox.Show("Creating chapters based on media tags will delete any existing chapters. Do you want to proceed?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirmResult == MessageBoxResult.No)
                    return Task.FromResult(Response<object>.Success());
            }

            var tagNames = GetTagNames();
            if (tagNames.Count == 0)
            {
                MessageBox.Show("There are no media tags that can be used as a source for chapter names.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return Task.FromResult(Response<object>.Success());
            }

            var selectTagWindow = new SelectTagWindow(tagNames);
            var result = selectTagWindow.ShowDialog();
            var selectedTagName = selectTagWindow.SelectedTagName;
            if (!result.HasValue || !result.Value || string.IsNullOrEmpty(selectedTagName))
                return Task.FromResult(Response<object>.Success());
            var trimStartNonChars = selectTagWindow.TrimStartingNonChars;

            foreach (var file in MediaFiles)
            {
                if (file.IsImage || file.Duration == null)
                    continue;

                var title = file.Tags.GetTagValue(selectedTagName);
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

    private IReadOnlyList<string> GetTagNames()
    {
        var tagNames = new List<string>(128);
        foreach (var file in MediaFiles)
        {
            foreach (var tag in file.Tags)
            {
                if (!tagNames.Has(tag.Name))
                    tagNames.Add(tag.Name);
            }
        }

        return tagNames.Count > 0 
            ? Files.Sort(tagNames)
            : tagNames;
    }
}