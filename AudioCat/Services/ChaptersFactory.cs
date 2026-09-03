using AudioCat.Models;
using AudioCat.ViewModels;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace AudioCat.Services;

internal static class ChaptersFactory
{
    #region Create from File Names
    public static ReadOnlyCollection<IMediaChapterViewModel> CreateFromFileNames(ReadOnlyCollection<IMediaFileViewModel> files, bool trimStartingNonChars) => CreateChapters(files, (file, _) =>
    {
        var title = Path.GetFileNameWithoutExtension(file.File.Name);
        return trimStartingNonChars ? title.TrimStartNonChars() : title;
    });
    #endregion

    #region Create from Metadata Tags
    public static ReadOnlyCollection<IMediaChapterViewModel> CreateFromMetadataTags(ReadOnlyCollection<IMediaFileViewModel> files, string selectedTagName, bool trimStartingNonChars) => CreateChapters(files, (file, _) =>
    {
        var title = file.Tags.GetTagValue(selectedTagName);
        return trimStartingNonChars ? title.TrimStartNonChars() : title;
    });
    #endregion

    #region Create from Cue Files
    private const int StandardCueFramesPerSecond = 75;
    private const int CentisecondsPerSecond = 100;
    private static readonly TimeSpan EmbeddedChapterMatchTolerance = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan DecisiveEmbeddedChapterAdvantage = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan StandardTerminalMarkerTolerance = TimeSpan.FromMilliseconds(1);
    private static readonly TimeSpan TerminalMarkerMaxDuration = TimeSpan.FromMilliseconds(100);

    private enum EmbeddedTimeBaseEvidence { Inconclusive, Standard, Centiseconds }

    private sealed record CueCreationFailure(Cue.IFile File, Cue.ITrack Track, string Message);

    private sealed record CueCreationResult(List<IMediaChapterViewModel> Chapters, TimeSpan EndTime, CueCreationFailure? Failure)
    {
        public bool IsSuccess => Failure == null;
    }

    private sealed record CentisecondEvidence(bool IsStrong, bool? HasTerminalMarker);

    public static ReadOnlyCollection<IMediaChapterViewModel> CreateFromCueFiles(ReadOnlyCollection<IMediaFileViewModel> files, ReadOnlyCollection<Cue.ICue> cueFiles)
    {
        if (cueFiles.Count == 0)
            return [];

        var cueStartTime = TimeSpan.Zero;
        var chapters = new List<IMediaChapterViewModel>();
        foreach (var cueFile in cueFiles)
        {
            var standardResult = TryCreateFromCueFile(files, cueFile, cueStartTime, chapters.Count, StandardCueFramesPerSecond);
            if (standardResult.IsSuccess && HasShortTerminalChapter(standardResult, StandardTerminalMarkerTolerance))
            {
                var withoutMarkerResult = TryCreateFromCueFile(files, cueFile, cueStartTime, chapters.Count, StandardCueFramesPerSecond, true);
                if (withoutMarkerResult.IsSuccess)
                    standardResult = withoutMarkerResult;
            }
            else if (!standardResult.IsSuccess && IsFinalTrackFailure(cueFile, standardResult))
            {
                var withoutMarkerResult = TryCreateFromCueFile(files, cueFile, cueStartTime, chapters.Count, StandardCueFramesPerSecond, true);
                if (withoutMarkerResult.IsSuccess && IsOmittedTerminalMarkerAtEnd(cueFile, withoutMarkerResult, StandardCueFramesPerSecond, StandardTerminalMarkerTolerance))
                    standardResult = withoutMarkerResult;
            }

            var selectedResult = standardResult;
            if (!standardResult.IsSuccess)
            {
                var centisecondResult = TryCreateFromCueFile(files, cueFile, cueStartTime, chapters.Count, CentisecondsPerSecond);
                var centisecondEvidence = GetCentisecondEvidence(files, cueFile, cueStartTime, centisecondResult);
                if (centisecondResult.IsSuccess && centisecondEvidence.IsStrong)
                {
                    selectedResult = centisecondResult;
                    if (centisecondEvidence.HasTerminalMarker ?? HasShortTerminalChapter(centisecondResult, TerminalMarkerMaxDuration))
                    {
                        var withoutMarkerResult = TryCreateFromCueFile(files, cueFile, cueStartTime, chapters.Count, CentisecondsPerSecond, true);
                        if (withoutMarkerResult.IsSuccess)
                            selectedResult = withoutMarkerResult;
                    }
                }
                else if (!centisecondResult.IsSuccess && IsFinalTrackFailure(cueFile, centisecondResult))
                {
                    var withoutMarkerResult = TryCreateFromCueFile(files, cueFile, cueStartTime, chapters.Count, CentisecondsPerSecond, true);
                    if (withoutMarkerResult.IsSuccess &&
                        IsOmittedTerminalMarkerAtEnd(cueFile, withoutMarkerResult, CentisecondsPerSecond, TerminalMarkerMaxDuration) &&
                        GetCentisecondEvidence(files, cueFile, cueStartTime, withoutMarkerResult).IsStrong)
                    {
                        selectedResult = withoutMarkerResult;
                    }
                }
            }

            if (!selectedResult.IsSuccess)
            {
                var failure = standardResult.Failure!;
                MessageBox.Show($"Failed to create chapter for file '{failure.File.Name}', track '{failure.Track.Title}'; Error: {failure.Message}", "Chapters Creation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return [];
            }

            chapters.AddRange(selectedResult.Chapters);
            cueStartTime = selectedResult.EndTime;
        }

        return chapters.AsReadOnly();
    }

    private static CueCreationResult TryCreateFromCueFile(
        ReadOnlyCollection<IMediaFileViewModel> files,
        Cue.ICue cueFile,
        TimeSpan cueStartTime,
        int chapterStartIndex,
        int fractionsPerSecond,
        bool omitTerminalTrack = false)
    {
        var fileStartTime = cueStartTime;
        var chapters = new List<IMediaChapterViewModel>();
        for (var fileIndex = 0; fileIndex < cueFile.Files.Count; fileIndex++)
        {
            var file = cueFile.Files[fileIndex];
            var mediaFileEndTime = GetMediaFileEndTime(files, fileStartTime);
            if (mediaFileEndTime == null)
                return Failure(file, file.Tracks[0], "The CUE file has no corresponding media file");

            var trackCount = file.Tracks.Count;
            if (omitTerminalTrack && fileIndex == cueFile.Files.Count - 1)
                trackCount--;
            if (trackCount <= 0)
                return Failure(file, file.Tracks[^1], "The CUE file contains no chapter before its terminal marker");

            var absoluteTrackStartTime = TimeSpan.Zero;
            var trackDuration = TimeSpan.Zero;
            for (var trackIndex = 0; trackIndex < trackCount; trackIndex++)
            {
                var track = file.Tracks[trackIndex];
                try
                {
                    absoluteTrackStartTime = fileStartTime + GetIndexTime(track.Index, fractionsPerSecond);
                    if (absoluteTrackStartTime < fileStartTime || absoluteTrackStartTime >= mediaFileEndTime.Value)
                        throw new InvalidOperationException("The track start is out of range");

                    trackDuration = GetTrackDuration(file, track, absoluteTrackStartTime, mediaFileEndTime.Value, trackIndex, trackCount, fractionsPerSecond);
                }
                catch (Exception ex)
                {
                    return Failure(file, track, ex.Message);
                }

                chapters.Add(CreateChapter(absoluteTrackStartTime, trackDuration, track.Title, chapterStartIndex + chapters.Count));
            }

            fileStartTime = mediaFileEndTime.Value;
        }

        return new CueCreationResult(chapters, fileStartTime, null);

        CueCreationResult Failure(Cue.IFile file, Cue.ITrack track, string message) =>
            new(chapters, cueStartTime, new CueCreationFailure(file, track, message));
    }

    private static TimeSpan GetTrackDuration(
        Cue.IFile file,
        Cue.ITrack track,
        TimeSpan trackStartTime,
        TimeSpan mediaFileEndTime,
        int trackIndex,
        int trackCount,
        int fractionsPerSecond)
    {
        if (trackIndex != trackCount - 1)
        {
            var trackStart = GetIndexTime(track.Index, fractionsPerSecond);
            var nextTrackStart = GetIndexTime(file.Tracks[trackIndex + 1].Index, fractionsPerSecond);
            if (nextTrackStart <= trackStart)
                throw new InvalidOperationException("The next track start time is not greater than the current track start time");
            return nextTrackStart - trackStart;
        }

        var trackDuration = mediaFileEndTime - trackStartTime;
        if (trackDuration <= TimeSpan.Zero)
            throw new InvalidOperationException("The track start is out of range");
        return trackDuration;
    }

    private static TimeSpan GetIndexTime(Cue.IIndex index, int fractionsPerSecond)
    {
        if (index is not Cue.IRawIndexTime rawTime)
        {
            if (fractionsPerSecond == StandardCueFramesPerSecond)
                return index.Time;
            throw new InvalidOperationException("The raw CUE track time is unavailable");
        }

        if (rawTime.Seconds is < 0 or >= 60 || rawTime.Frames < 0 || rawTime.Frames >= fractionsPerSecond)
        {
            var format = fractionsPerSecond == StandardCueFramesPerSecond
                ? "standard 75-frame"
                : "centisecond";
            throw new InvalidOperationException($"The track time is not valid for a {format} CUE sheet");
        }

        return TimeSpan.FromMinutes(rawTime.Minutes) + TimeSpan.FromSeconds(rawTime.Seconds + rawTime.Frames / (double)fractionsPerSecond);
    }

    private static TimeSpan? GetMediaFileEndTime(ReadOnlyCollection<IMediaFileViewModel> files, TimeSpan mediaFileStartTime)
    {
        var totalDuration = TimeSpan.Zero;
        foreach (var file in files)
        {
            if (file is not { IsImage: false, Duration: not null })
                continue;
            var fileEndTime = totalDuration + file.Duration.Value;
            if (mediaFileStartTime >= totalDuration && mediaFileStartTime < fileEndTime)
                return fileEndTime;
            totalDuration = fileEndTime;
        }

        return null;
    }

    private static CentisecondEvidence GetCentisecondEvidence(
        ReadOnlyCollection<IMediaFileViewModel> files,
        Cue.ICue cueFile,
        TimeSpan cueStartTime,
        CueCreationResult centisecondResult)
    {
        var hasNonStandardFrame = false;
        var tracks = new List<Cue.ITrack>();
        foreach (var file in cueFile.Files)
        {
            foreach (var track in file.Tracks)
            {
                tracks.Add(track);
                if (track.Index is not Cue.IRawIndexTime { Minutes: >= 0, Seconds: >= 0 and < 60, Frames: >= 0 and < 100 } rawTime)
                    return new CentisecondEvidence(false, null);
                if (rawTime.Frames >= StandardCueFramesPerSecond)
                    hasNonStandardFrame = true;
            }
        }

        var embeddedEvidence = CompareEmbeddedChapterTimeBases(files, tracks, cueStartTime, centisecondResult, out var hasTerminalMarker);
        var isStrong = embeddedEvidence != EmbeddedTimeBaseEvidence.Standard &&
                       (hasNonStandardFrame || embeddedEvidence == EmbeddedTimeBaseEvidence.Centiseconds);
        return new CentisecondEvidence(isStrong, hasTerminalMarker);
    }

    private static EmbeddedTimeBaseEvidence CompareEmbeddedChapterTimeBases(
        ReadOnlyCollection<IMediaFileViewModel> files,
        IReadOnlyList<Cue.ITrack> tracks,
        TimeSpan cueStartTime,
        CueCreationResult cueResult,
        out bool? hasTerminalMarker)
    {
        hasTerminalMarker = null;
        var embeddedStarts = GetEmbeddedChapterStarts(files, cueStartTime, cueResult.EndTime);
        if (embeddedStarts.Count < 2)
            return EmbeddedTimeBaseEvidence.Inconclusive;

        if (cueResult.Chapters.Count == embeddedStarts.Count + 1)
        {
            if (!HasShortTerminalChapter(cueResult, TerminalMarkerMaxDuration))
                return EmbeddedTimeBaseEvidence.Inconclusive;
            hasTerminalMarker = true;
        }
        else if (cueResult.Chapters.Count != embeddedStarts.Count)
            return EmbeddedTimeBaseEvidence.Inconclusive;
        else
            hasTerminalMarker = false;

        var centisecondMatches = true;
        var standardMatches = true;
        var centisecondAdvantageCount = 0;
        var standardAdvantageCount = 0;
        var hasDecisiveCentisecondAdvantage = false;
        var hasDecisiveStandardAdvantage = false;
        for (var index = 0; index < embeddedStarts.Count; index++)
        {
            if (cueResult.Chapters[index].StartTime is not { } centisecondStart || tracks[index].Index is not Cue.IRawIndexTime rawTime)
            {
                hasTerminalMarker = null;
                return EmbeddedTimeBaseEvidence.Inconclusive;
            }

            var centisecondError = (centisecondStart - embeddedStarts[index]).Duration();
            if (centisecondError > EmbeddedChapterMatchTolerance)
                centisecondMatches = false;

            var standardStart = centisecondStart + TimeSpan.FromSeconds(rawTime.Frames / (double)StandardCueFramesPerSecond - rawTime.Frames / (double)CentisecondsPerSecond);
            var standardError = (standardStart - embeddedStarts[index]).Duration();
            if (standardError > EmbeddedChapterMatchTolerance)
                standardMatches = false;

            var centisecondAdvantage = standardError - centisecondError;
            if (centisecondAdvantage >= EmbeddedChapterMatchTolerance)
                centisecondAdvantageCount++;
            if (centisecondAdvantage >= DecisiveEmbeddedChapterAdvantage)
                hasDecisiveCentisecondAdvantage = true;

            var standardAdvantage = centisecondError - standardError;
            if (standardAdvantage >= EmbeddedChapterMatchTolerance)
                standardAdvantageCount++;
            if (standardAdvantage >= DecisiveEmbeddedChapterAdvantage)
                hasDecisiveStandardAdvantage = true;
        }

        var strongCentisecondEvidence = centisecondMatches && (hasDecisiveCentisecondAdvantage || centisecondAdvantageCount >= 2);
        var strongStandardEvidence = standardMatches && (hasDecisiveStandardAdvantage || standardAdvantageCount >= 2);
        if (!centisecondMatches)
            hasTerminalMarker = null;
        if (strongCentisecondEvidence == strongStandardEvidence)
            return EmbeddedTimeBaseEvidence.Inconclusive;
        return strongCentisecondEvidence
            ? EmbeddedTimeBaseEvidence.Centiseconds
            : EmbeddedTimeBaseEvidence.Standard;
    }

    private static bool HasShortTerminalChapter(CueCreationResult result, TimeSpan maximumDuration) =>
        result.Chapters.Count > 1 &&
        result.Chapters[^1].Duration is { } duration &&
        duration > TimeSpan.Zero &&
        duration <= maximumDuration;

    private static bool IsFinalTrackFailure(Cue.ICue cueFile, CueCreationResult result)
    {
        if (result.Failure == null || cueFile.Files.Count == 0)
            return false;
        var finalFile = cueFile.Files[^1];
        return finalFile.Tracks.Count > 0 &&
               ReferenceEquals(result.Failure.File, finalFile) &&
               ReferenceEquals(result.Failure.Track, finalFile.Tracks[^1]);
    }

    private static bool IsOmittedTerminalMarkerAtEnd(
        Cue.ICue cueFile,
        CueCreationResult result,
        int fractionsPerSecond,
        TimeSpan maximumDistance)
    {
        if (cueFile.Files.Count == 0)
            return false;

        var finalFile = cueFile.Files[^1];
        if (finalFile.Tracks.Count < 2)
            return false;

        var precedingTrackCount = 0;
        for (var fileIndex = 0; fileIndex < cueFile.Files.Count - 1; fileIndex++)
            precedingTrackCount += cueFile.Files[fileIndex].Tracks.Count;
        var precedingTrackIndex = precedingTrackCount + finalFile.Tracks.Count - 2;
        if (result.Chapters.Count != precedingTrackIndex + 1 || result.Chapters[precedingTrackIndex].StartTime is not { } precedingTrackStart)
            return false;

        try
        {
            var precedingIndexTime = GetIndexTime(finalFile.Tracks[^2].Index, fractionsPerSecond);
            var terminalIndexTime = GetIndexTime(finalFile.Tracks[^1].Index, fractionsPerSecond);
            if (terminalIndexTime <= precedingIndexTime)
                return false;

            var finalFileStart = precedingTrackStart - precedingIndexTime;
            var terminalStart = finalFileStart + terminalIndexTime;
            return (terminalStart - result.EndTime).Duration() <= maximumDistance;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static List<TimeSpan> GetEmbeddedChapterStarts(
        ReadOnlyCollection<IMediaFileViewModel> files,
        TimeSpan cueStartTime,
        TimeSpan cueEndTime)
    {
        var starts = new List<TimeSpan>();
        var fileStartTime = TimeSpan.Zero;
        foreach (var file in files)
        {
            if (file is not { IsImage: false, Duration: not null })
                continue;

            var fileEndTime = fileStartTime + file.Duration.Value;
            if (fileEndTime > cueStartTime && fileStartTime < cueEndTime)
            {
                foreach (var chapter in file.Chapters)
                {
                    if (chapter.StartTime is not { } chapterStart)
                        continue;
                    var absoluteStart = fileStartTime + chapterStart;
                    if (absoluteStart >= cueStartTime && absoluteStart < cueEndTime)
                        starts.Add(absoluteStart);
                }
            }

            fileStartTime = fileEndTime;
        }

        return starts;
    }
    #endregion

    #region Create from Template
    public static ReadOnlyCollection<IMediaChapterViewModel> CreateFromTemplate(
        ReadOnlyCollection<IMediaFileViewModel> files,
        string template,
        int templateStartNumberValue,
        string templateStartNumber,
        bool isTemplateStartNumberValid) =>
        CreateChapters(files, (_, index) => isTemplateStartNumberValid
            ? template.Replace("{}", (templateStartNumberValue + index).ToString(new string('0', templateStartNumber.Length)))
            : template);
    #endregion

    #region Create from Existing Chapters
    public static ReadOnlyCollection<IMediaChapterViewModel> CreateFromExisting(ReadOnlyCollection<IMediaFileViewModel> files, bool trimStartingNonChars)
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

        return chapters.AsReadOnly();
    }
    #endregion

    #region Create from Silence Intervals
    public static ReadOnlyCollection<IMediaChapterViewModel> CreateFromIntervals(IReadOnlyList<IInterval> intervals)
    {
        var startTime = TimeSpan.Zero;
        var chapters = new List<IMediaChapterViewModel>();
        foreach (var interval in intervals)
        {
            // The intervals may touch or regress (a file ending in detected silence, ffprobe container
            // duration disagreeing with the ffmpeg decode timeline), so skip chapters that would come
            // out empty or negative and never move the timeline backwards.
            if (interval.Start > startTime)
                chapters.Add(CreateChapter(startTime, interval.Start - startTime, chapters.Count.ToString(), chapters.Count));
            if (interval.End > startTime)
                startTime = interval.End;
        }

        return chapters.AsReadOnly();
    }
    #endregion

    private static ReadOnlyCollection<IMediaChapterViewModel> CreateChapters(ReadOnlyCollection<IMediaFileViewModel> files, Func<IMediaFileViewModel, int, string> getTitle)
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

        return chapters.AsReadOnly();
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
