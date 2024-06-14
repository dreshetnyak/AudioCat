using System.Text;
using AudioCat.Models;
using AudioCat.ViewModels;

namespace AudioCat.Services;

internal class Cue
{
    //https://exiftool.org/TagNames/RIFF.html
    //https://wiki.multimedia.cx/index.php/FFmpeg_Metadata

    //Save image stream to a file. Extract stream to a file. Binary. Adding binary streams? Subtitles streams.


    //public static string Create(IReadOnlyList<IMediaFileViewModel> mediaFiles, string codec, string audioFileName)
    //{
    //    var cue = new StringBuilder();
    //    AppendHeader(cue, mediaFiles, codec, audioFileName);
    //    AppendTracks(cue, mediaFiles, codec, audioFileName);
    //    return cue.ToString();
    //}

    //#region Header
    //private static void AppendHeader(StringBuilder chapters, IReadOnlyList<IMediaFileViewModel> mediaFiles, string codec, string audioFileName)
    //{
    //    var tagsSourceFile = mediaFiles.GetTagsSourceFile();
    //    if (tagsSourceFile != null)
    //        chapters.Append(GetTagCommands(tagsSourceFile));
    //    chapters.AppendLine(GetFileCommand(audioFileName, codec));
    //}

    //private static string GetTagCommands(IMediaFileViewModel file)
    //{
    //    var commands = new StringBuilder();
    //    var mainCommands = new StringBuilder();

    //    foreach (var tag in file.Tags)
    //    {
    //        var mainCommand = GetMainCommand(tag);
    //        if (mainCommand != null)
    //            mainCommands.AppendLine(mainCommand);
    //        else
    //            commands.Append(GetRemCommand(tag));
    //    }

    //    return commands.Append(mainCommands).ToString();
    //}

    //private static IEnumerable<NameValue> MainCommands =>
    //[
    //    new NameValue("title", "TITLE"),
    //    new NameValue("album_artist", "PERFORMER"),
    //    new NameValue("artist", "SONGWRITER")
    //];
    //private static string? GetMainCommand(IMediaTagViewModel tag)
    //{
    //    var commandName = MainCommands.GetValue(tag.Name);
    //    return commandName != null
    //        ? $"{commandName} {tag.Value}"
    //        : null;
    //}

    //private static string GetRemCommand(IMediaTagViewModel tag) => 
    //    $"REM {tag.Name} {tag.Value}";

    //private static string GetFileCommand(string fileName, string codec) => 
    //    $"FILE \"{fileName}\" {GetFileType(codec)}";

    //private static string GetFileType(string codec) => codec switch
    //{
    //    "aac" => "MP4",
    //    "mp3" => "MP3",
    //    _ => "WAVE"
    //};

    //#endregion

    //#region Tracks
    ////private static void AppendTracks(StringBuilder cue, IReadOnlyList<IMediaFileViewModel> mediaFiles, string codec, string audioFileName)
    ////{
    ////    foreach (var file in mediaFiles)
    ////    {
    ////        file.Chapters




    ////    }


    ////}

    //#endregion


    // PERFORMER
    // SONGWRITER
    // TITLE
    // 

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