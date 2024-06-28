using System.Text;
using AudioCat.Models;
using AudioCat.ViewModels;

namespace AudioCat.Services;

internal static class Cue
{
    public static string Create(IReadOnlyList<IMediaFileViewModel> mediaFiles, string codec, string audioFileName)
    {
        var cue = new StringBuilder();
        AppendHeader(cue, mediaFiles, codec, audioFileName);
        AppendTracks(cue, mediaFiles);
        return cue.ToString();
    }

    #region Header
    private static void AppendHeader(StringBuilder cue, IReadOnlyList<IMediaFileViewModel> mediaFiles, string codec, string audioFileName)
    {
        var tagsSourceFile = mediaFiles.GetTagsSourceFile();
        if (tagsSourceFile != null)
        {
            cue.AppendCommands(GetRemCommands(tagsSourceFile.Tags));
            cue.AppendCommands(GetMainCommands(tagsSourceFile.Tags));
        }

        cue.AppendLine(GetFileCommand(audioFileName, codec));
    }

    private static string GetFileCommand(string fileName, string codec) =>
        $"FILE \"{fileName}\" {GetFileType(codec)}";

    private static string GetFileType(string codec) => codec switch
    {
        "aac" => "MP4",
        "mp3" => "MP3",
        _ => "WAVE"
    };

    #endregion

    #region Tracks

    private static void AppendTracks(StringBuilder cue, IReadOnlyList<IMediaFileViewModel> mediaFiles)
    {
        var trackIndex = 1; 
        var startTime = TimeSpan.Zero;
        foreach (var file in mediaFiles)
        {
            var fileDuration = file.Duration;
            if (fileDuration == null)
                continue;

            foreach (var chapter in file.Chapters)
            {
                var chapterStartTime = chapter.StartTime;
                if (chapterStartTime == null)
                    continue;
                var cueChapterStart = startTime.Add(chapterStartTime.Value);
                cue.AppendLine(GetTrackCommand(trackIndex++));
                cue.AppendCommands(GetMainCommands(chapter.Tags), 4);
                cue.AppendLine(GetIndexCommand(1, cueChapterStart));
            }

            startTime += fileDuration.Value;
        }
    }

    private static string GetTrackCommand(int index) => 
        $"  TRACK {index:00} AUDIO";

    private static string GetIndexCommand(int index, TimeSpan indexStart) => 
        $"    INDEX {index:00} {ToTrackIndexTime(indexStart)}";

    private const decimal FRAME_SIZE = 1000m / 75m;
    private static string ToTrackIndexTime(TimeSpan startTime) => 
        $"{startTime.TotalMinutes:00}:{startTime.Seconds:00}:{(int)(startTime.Milliseconds / FRAME_SIZE):00}";

    #endregion

    #region Commands
    private static IReadOnlyList<string> GetMainCommands(IReadOnlyList<IMediaTag> tags)
    {
        var commands = new List<string>(tags.Count);
        foreach (var mainCommand in MainCommands)
        {
            var tag = tags.GetTag(mainCommand.Name);
            if (tag == null)
                continue;
            var command = GetMainCommand(mainCommand.Value, tag.Value);
            if (command != null)
                commands.Add(command);
        }

        return commands;
    }

    private static IReadOnlyList<string> GetRemCommands(IReadOnlyCollection<IMediaTagViewModel> tags)
    {
        var commands = new List<string>(tags.Count);
        foreach (var tag in tags)
        {
            if (!MainCommands.Has(tag.Name))
                commands.Add(GetRemCommand(tag));
        }

        return commands;
    }

    private static IEnumerable<NameValue> MainCommands =>
    [
        new NameValue("title", "TITLE"),
        new NameValue("album_artist", "PERFORMER"),
        new NameValue("artist", "SONGWRITER")
    ];

    private static string? GetMainCommand(string commandName, string tagValue) =>
        !string.IsNullOrEmpty(tagValue)
            ? $"{commandName} {tagValue.ToQuote()}"
            : null;

    private static string GetRemCommand(IMediaTagViewModel tag) =>
        $"REM {tag.Name.ToQuote()} {tag.Value.ToQuote()}";

    private static void AppendCommands(this StringBuilder commands, IEnumerable<string> commandsToAppend, int indent = 0)
    {
        foreach (var commandToAppend in commandsToAppend)
        {
            if (indent > 0)
                commands.Append(' ', indent);
            commands.AppendLine(commandToAppend);
        }
    }

    #endregion

    private static string ToQuote(this string str) =>
        str.Contains(' ') ? $"\"{str}\"" : str;
    
    //  TRACK 01 AUDIO
    //    TITLE "Reverence"
    //    PERFORMER "Faithless"
    //    INDEX 01 00:00:00

    //REM GENRE Electronica
    //REM DATE 1998
    //PERFORMER "Faithless"
    //TITLE "Live in Berlin"
    //FILE "Faithless - Live in Berlin.mp3" MP3
    //  TRACK 01 AUDIO
    //    TITLE "Reverence"
    //    PERFORMER "Faithless"
    //    INDEX 01 00:00:00
    //  TRACK 02 AUDIO
    //    TITLE "She's My Baby"
    //    PERFORMER "Faithless"
    //    INDEX 01 06:42:00
    //  TRACK 03 AUDIO
    //    TITLE "Take the Long Way Home"
    //    PERFORMER "Faithless"
    //    INDEX 01 10:54:00
}