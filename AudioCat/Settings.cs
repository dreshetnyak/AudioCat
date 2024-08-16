using AudioCat.Models;

namespace AudioCat;

internal static class Settings
{
    public static IReadOnlyList<string> ErrorsToIgnore { get; } =
    [
        "Invalid PNG signature",
        "    Last message repeated ",
        "Incorrect BOM value\r\nError reading comment frame, skipped",
        "Incorrect BOM value",
        "Error reading comment frame, skipped",
        "Error reading frame GEOB, skipped"
    ];
    public static IReadOnlyList<string> RemuxOnErrors { get; } =
    [
        "non monotonically increasing dts"
    ];

    public static IEnumerable<string> SupportedAudioCodecs { get; } = 
    [
        "mp3", 
        "aac",
        "opus",
        "vorbis",       // OGG Vorbis
        "wmav2",        // WMA
        "pcm_s16le",    // WAV; Most of the files with this format will have this codec
        "pcm_u8",       // WAV; Less common
        "flac"
    ];
    public static IEnumerable<string> SupportedImageCodecs { get; } = ["mjpeg", "png"];
    public static IEnumerable<string> CodecsWithTwoStepsConcat { get; } = ["vorbis"]; // Concat and embedding of metadata must be done in two separate steps
    public static IEnumerable<string> CodecsWithTagsInStream { get; } = ["vorbis"]; // In OGG Vorbis files the tags are placed in the stream
    public static IEnumerable<string> CodecsThatDoesNotSupportChapters { get; } = ["vorbis", "pcm_s16le", "pcm_u8", "flac"];
    public static IEnumerable<string> CodecsThatDoesNotSupportImages { get; } = ["vorbis", "pcm_s16le", "pcm_u8"];

    private static string DefaultEncodingCommand { get; } = "-c copy";
    private static IEnumerable<NameValue> CodecEncodingCommands { get; } =
    [
        new NameValue("flac", "-c:a flac")
    ];
    public static string GetEncodingCommand(string codec)
    {
        foreach (var codecCommand in CodecEncodingCommands)
        {
            if (codecCommand.Name.Is(codec))
                return codecCommand.Value;
        }

        return DefaultEncodingCommand;
    }

    public static string GetSaveFileExtensionFilter(string codec) =>
        codec switch
        {
            "aac" => "AAC Audio|*.m4b",
            "mp3" => "MP3 Audio|*.mp3",
            "opus" => "Opus Audio|*.opus",
            "wmav2" => "Windows Media Audio|*.wma",
            "flac" => "Free Lossless Audio Codec|*.flac",
            "pcm_s16le" => "Waveform Audio|*.wav",
            "pcm_u8" => "Waveform Audio|*.wav",
            "vorbis" => "OGG Vorbis|*.ogg",
            _ => "Other Files|*.*"
        };

    public static string GetSuggestedFileNameExtension(string codec) =>
        codec switch
        {
            "aac" => ".m4b",
            "mp3" => ".mp3",
            "opus" => ".opus",
            "wmav2" => ".wma",
            "flac" => ".flac",
            "pcm_s16le" => ".wav",
            "pcm_u8" => ".wav",
            "vorbis" => ".ogg",
            _ => ""
        };

    public static string GetAddFilesExtensionFilter(string codec) =>
        codec switch
        {
            "mp3" => "MP3 Audio|*.mp3|" +
                     "Other Audio|*.*",
            "aac" => "AAC Audio|*.m4b|" +
                     "AAC Audio|*.m4a|" +
                     "AAC Audio|*.aac|" +
                     "Other Audio|*.*",
            "opus" => "Opus Audio|*.opus|" +
                     "Other Audio|*.*",
            "wmav2" => "Windows Media Audio|*.wma|" +
                       "Other Audio|*.*",
            "flac" => "Free Lossless Audio Codec|*.flac|" +
                      "Other Audio|*.*",
            "pcm_s16le" => "Waveform Audio|*.wav|" +
                           "Other Audio|*.*",
            "pcm_u8" => "Waveform Audio|*.wav|" +
                        "Other Audio|*.*",
            "vorbis" => "OGG Vorbis|*.ogg|" +
                        "Other Audio|*.*",
            _ => "MP3 Audio|*.mp3|" +
                 "AAC Audio|*.m4b|" +
                 "AAC Audio|*.m4a|" +
                 "AAC Audio|*.aac|" +
                 "Opus Audio|*.opus|" +
                 "Windows Media Audio|*.wma|" +
                 "Free Lossless Audio Codec|*.flac|" +
                 "Waveform Audio|*.wav|" +
                 "OGG Vorbis|*.ogg|" +
                 "Other Audio|*.*"
        };
}