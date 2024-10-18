using AudioCat.Models;

namespace AudioCat;

internal static class Settings
{
    public static string FFmpegName { get; } = "ffmpeg.exe";
    public static string FFprobeName { get; } = "ffprobe.exe";

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
        Codecs.MP3, 
        Codecs.AAC,
        Codecs.OPUS,
        Codecs.VORBIS,      // OGG Vorbis
        Codecs.WMAV2,       // WMA
        Codecs.PCM_S16_LE,  // WAV; Most of the files with this format will have this codec
        Codecs.PCM_U8,      // WAV; Less common
        Codecs.FLAC
    ];
    public static IEnumerable<string> SupportedImageCodecs { get; } = [Codecs.MJPEG, Codecs.PNG];
    public static IEnumerable<string> CodecsWithTwoStepsConcat { get; } = [Codecs.VORBIS]; // Concat and embedding of metadata must be done in two separate steps
    public static IEnumerable<string> CodecsWithTagsInStream { get; } = [Codecs.OPUS, Codecs.VORBIS]; // In OPUS and OGG Vorbis files the tags are placed in the stream
    public static IEnumerable<string> CodecsThatDoesNotSupportChapters { get; } = [Codecs.VORBIS, Codecs.PCM_S16_LE, Codecs.PCM_U8, Codecs.FLAC];
    public static IEnumerable<string> CodecsThatDoesNotSupportImages { get; } = [Codecs.VORBIS, Codecs.PCM_S16_LE, Codecs.PCM_U8];

    private static string DefaultEncodingCommand => "-c copy";
    private static IEnumerable<NameValue> CodecEncodingCommands { get; } =
    [
        new NameValue(Codecs.FLAC, "-c:a flac")
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
            Codecs.AAC => "AAC Audio|*.m4b",
            Codecs.MP3 => "MP3 Audio|*.mp3",
            Codecs.OPUS => "Opus Audio|*.opus",
            Codecs.WMAV2 => "Windows Media Audio|*.wma",
            Codecs.FLAC => "Free Lossless Audio Codec|*.flac",
            Codecs.PCM_S16_LE => "Waveform Audio|*.wav",
            Codecs.PCM_U8 => "Waveform Audio|*.wav",
            Codecs.VORBIS => "OGG Vorbis|*.ogg",
            _ => "Other Files|*.*"
        };

    public static string GetSuggestedFileNameExtension(string codec) =>
        codec switch
        {
            Codecs.AAC => ".m4b",
            Codecs.MP3 => ".mp3",
            Codecs.OPUS => ".opus",
            Codecs.WMAV2 => ".wma",
            Codecs.FLAC => ".flac",
            Codecs.PCM_S16_LE => ".wav",
            Codecs.PCM_U8 => ".wav",
            Codecs.VORBIS => ".ogg",
            _ => ""
        };

    private const string OTHER_AUDIO_EXTENSION = "Other Audio|*.*";
    public static string GetAddFilesExtensionFilter(string codec) => codec switch
    {
        Codecs.MP3 => "MP3 Audio|*.mp3|" +
                      OTHER_AUDIO_EXTENSION,
        Codecs.AAC => "AAC Audio|*.m4b|" +
                      "AAC Audio|*.m4a|" +
                      "AAC Audio|*.aac|" +
                      OTHER_AUDIO_EXTENSION,
        Codecs.OPUS => "Opus Audio|*.opus|" +
                       OTHER_AUDIO_EXTENSION,
        Codecs.WMAV2 => "Windows Media Audio|*.wma|" +
                        OTHER_AUDIO_EXTENSION,
        Codecs.FLAC => "Free Lossless Audio Codec|*.flac|" +
                       OTHER_AUDIO_EXTENSION,
        Codecs.PCM_S16_LE => "Waveform Audio|*.wav|" +
                             OTHER_AUDIO_EXTENSION,
        Codecs.PCM_U8 => "Waveform Audio|*.wav|" +
                         OTHER_AUDIO_EXTENSION,
        Codecs.VORBIS => "OGG Vorbis|*.ogg|" +
                         OTHER_AUDIO_EXTENSION,
        _ => "MP3 Audio|*.mp3|" +
             "AAC Audio|*.m4b|" +
             "AAC Audio|*.m4a|" +
             "AAC Audio|*.aac|" +
             "Opus Audio|*.opus|" +
             "Windows Media Audio|*.wma|" +
             "Free Lossless Audio Codec|*.flac|" +
             "Waveform Audio|*.wav|" +
             "OGG Vorbis|*.ogg|" +
             OTHER_AUDIO_EXTENSION
    };
}