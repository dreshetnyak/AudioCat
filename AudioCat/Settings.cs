namespace AudioCat;

internal static class Settings
{
    public static IEnumerable<string> SupportedAudioCodecs { get; } = ["mp3", "aac", "vorbis", "wmav2"];
    public static IEnumerable<string> SupportedImageCodecs { get; } = ["mjpeg", "png"];

    public static IEnumerable<string> CodecsWithTwoStepsConcat { get; } = ["vorbis"];
    public static IEnumerable<string> CodecsWithTagsInStream { get; } = ["vorbis"]; // In OGG Vorbis files the tags are placed in the stream
    public static IEnumerable<string> CodecsThatDoesNotSupportChapters { get; } = ["vorbis"];
    public static IEnumerable<string> CodecsThatDoesNotSupportImages { get; } = ["vorbis"];
}