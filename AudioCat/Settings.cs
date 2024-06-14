namespace AudioCat;

internal static class Settings
{
    public static IEnumerable<string> SupportedAudioCodecs { get; } = 
    [
        "mp3", 
        "aac", 
        "vorbis",       // OGG Vorbis
        "wmav2",        // WMA
        "pcm_s16le",    // WAV; Most of the files with this format will have this codec
        "pcm_u8",       // WAV; Less common
        "flac"
    ];
    public static IEnumerable<string> SupportedImageCodecs { get; } = ["mjpeg", "png"];
    public static IEnumerable<string> CodecsWithTwoStepsConcat { get; } = ["vorbis"];
    public static IEnumerable<string> CodecsWithTagsInStream { get; } = ["vorbis"]; // In OGG Vorbis files the tags are placed in the stream
    public static IEnumerable<string> CodecsThatDoesNotSupportChapters { get; } = ["vorbis", "pcm_s16le", "pcm_u8"];
    public static IEnumerable<string> CodecsThatDoesNotSupportImages { get; } = ["vorbis", "pcm_s16le", "pcm_u8"];

    public static string GetSaveFileExtensionFilter(string codec) =>
        codec switch
        {
            "aac" => "AAC Audio|*.m4b",
            "mp3" => "MP3 Audio|*.mp3",
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
                 "Windows Media Audio|*.wma|" +
                 "Free Lossless Audio Codec|*.flac|" +
                 "Waveform Audio|*.wav|" +
                 "OGG Vorbis|*.ogg|" +
                 "Other Audio|*.*"
        };
}