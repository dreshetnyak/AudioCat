using AudioCat.Models;
using AudioCat.ViewModels;
using System.IO;
using System.Windows;

namespace AudioCat.Services;

internal static class ChaptersFactory
{
    #region Create from File Names
    public static IReadOnlyList<IMediaChapterViewModel> CreateFromFileNames(IReadOnlyList<IMediaFileViewModel> files, bool trimStartingNonChars) => CreateChapters(files, (file, _) =>
    {
        var title = Path.GetFileNameWithoutExtension(file.File.Name);
        return trimStartingNonChars ? title.TrimStartNonChars() : title;
    });
    #endregion

    #region Create from Metadata Tags
    public static IReadOnlyList<IMediaChapterViewModel> CreateFromMetadataTags(IReadOnlyList<IMediaFileViewModel> files, string selectedTagName, bool trimStartingNonChars) => CreateChapters(files, (file, _) =>
    {
        var title = file.Tags.GetTagValue(selectedTagName);
        return trimStartingNonChars ? title.TrimStartNonChars() : title;
    });
    #endregion

    #region Create from Cue Files
    public static IReadOnlyList<IMediaChapterViewModel> CreateFromCueFiles(IReadOnlyList<IMediaFileViewModel> files, IReadOnlyList<Cue.ICue> cueFiles)
    {
        if (cueFiles.Count == 0)
            return [];
        
        var chapterIndex = 0;
        var fileStartTime = TimeSpan.Zero;          // Current file start time
        var absoluteTrackStartTime = TimeSpan.Zero; // Current track absolute start time (previous files duration included)
        var chapters = new List<IMediaChapterViewModel>();
        foreach (var cueFile in cueFiles)
        {
            foreach (var file in cueFile.Files)
            {
                var trackDuration = TimeSpan.Zero;
                for (var trackIndex = 0; trackIndex < file.Tracks.Count; trackIndex++)
                {
                    var track = file.Tracks[trackIndex];
                    absoluteTrackStartTime = fileStartTime + track.Index.Time;

                    try { trackDuration = GetTrackDuration(files, file, track, absoluteTrackStartTime, trackIndex); }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to create chapter for file '{file.Name}', track '{track.Title}'; Error: {ex.Message}", "Chapters Creation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return [];
                    }

                    var chapter = CreateChapter(absoluteTrackStartTime, trackDuration, track.Title, chapterIndex++);
                    chapters.Add(chapter);
                }

                fileStartTime = absoluteTrackStartTime + trackDuration;
            }
        }

        return chapters;
    }

    private static TimeSpan GetTrackDuration(IReadOnlyList<IMediaFileViewModel> files, Cue.IFile file, Cue.ITrack track, TimeSpan trackStartTime, int trackIndex)
    {
        TimeSpan trackDuration;
        if (trackIndex != file.Tracks.Count - 1)
        {
            if (file.Tracks[trackIndex + 1].Index.Time < track.Index.Time)
                throw new InvalidOperationException("The next track start time is less than the current track start time");
            trackDuration = file.Tracks[trackIndex + 1].Index.Time - track.Index.Time;
        }
        else
        {
            trackDuration = GetTimespanToEndOfFileFrom(files, trackStartTime);
            if (trackDuration == TimeSpan.Zero)
                throw new InvalidOperationException("The track start is out of range");
        }

        return trackDuration;
    }

    private static TimeSpan GetTimespanToEndOfFileFrom(IReadOnlyList<IMediaFileViewModel> files, TimeSpan trackStart)
    {
        var totalDuration = TimeSpan.Zero;
        foreach (var file in files)
        {
            if (file is not { IsImage: false, Duration: not null })
                continue;
            if (trackStart >= totalDuration && trackStart <= totalDuration + file.Duration.Value)
                return totalDuration + file.Duration.Value - trackStart;
            totalDuration += file.Duration.Value;
        }

        return TimeSpan.Zero; // The track is not in the files
    }
    #endregion

    #region Create from Template
    public static IReadOnlyList<IMediaChapterViewModel> CreateFromTemplate(
        IReadOnlyList<IMediaFileViewModel> files,
        string template,
        int templateStartNumberValue,
        string templateStartNumber,
        bool isTemplateStartNumberValid) =>
        CreateChapters(files, (_, index) => isTemplateStartNumberValid
            ? template.Replace("{}", (templateStartNumberValue + index).ToString(new string('0', templateStartNumber.Length)))
            : template);
    #endregion

    #region Create from Existing Chapters
    public static IReadOnlyList<IMediaChapterViewModel> CreateFromExisting(IReadOnlyList<IMediaFileViewModel> files, bool trimStartingNonChars)
    {
        var startTime = TimeSpan.Zero;
        var chapters = new List<IMediaChapterViewModel>();
        foreach (var file in files)
        {
            if (file.IsImage || !file.Duration.HasValue)
                continue;

            if (file.Chapters.Count == 0)
            {
                var chapter = CreateChapter(startTime, file.Duration.Value, "", chapters.Count);
                chapters.Add(chapter);
                startTime += file.Duration.Value;
                continue;
            }

            foreach (var sourceChapter in file.Chapters)
            {
                var duration = sourceChapter.EndTime!.Value - sourceChapter.StartTime!.Value;
                var title = trimStartingNonChars ? sourceChapter.Title.TrimStartNonChars() : sourceChapter.Title;
                var chapter = CreateChapter(startTime, duration, title, chapters.Count);
                chapters.Add(chapter);
                startTime += duration;
            }
        }

        return chapters;
    }
    #endregion

    #region Create from Silence Intervals
    public static IReadOnlyList<IMediaChapterViewModel> CreateFromIntervals(IReadOnlyList<IInterval> intervals)
    {
        var startTime = TimeSpan.Zero;
        var chapters = new List<IMediaChapterViewModel>();
        foreach (var interval in intervals)
        {
            var chapter = CreateChapter(startTime, interval.Start - startTime, chapters.Count.ToString(), chapters.Count);
            chapters.Add(chapter);
            startTime += interval.End - startTime;
        }

        return chapters;
    }
    #endregion

    private static IReadOnlyList<IMediaChapterViewModel> CreateChapters(IReadOnlyList<IMediaFileViewModel> files, Func<IMediaFileViewModel, int, string> getTitle)
    {
        var startTime = TimeSpan.Zero;
        var chapters = new List<IMediaChapterViewModel>(files.Count);
        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            if (file.IsImage || file.Duration == null)
                continue;
            var title = getTitle(file, index);
            var chapter = CreateChapter(startTime, file.Duration.Value, title, index);
            chapters.Add(chapter);
            startTime = chapter.EndTime!.Value;
        }

        return chapters;
    }

    private static IMediaChapterViewModel CreateChapter(TimeSpan startTime, TimeSpan duration, string title, int index)
    {
        const decimal divident = 1m;
        const decimal divisor = 1000m;

        var endTime = startTime.Add(duration);
        var calculatedStart = (long)((decimal)startTime.TotalSeconds * divisor);
        var calculatedEnd = (long)((decimal)endTime.TotalSeconds * divisor);

        return new ChapterViewModel
        {
            Id = index,
            Start = calculatedStart,
            End = calculatedEnd,
            TimeBaseDivident = divident,
            TimeBaseDivisor = divisor,
            StartTime = startTime,
            EndTime = endTime,
            Title = title
        };
    }
}