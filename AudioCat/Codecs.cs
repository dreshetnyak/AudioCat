namespace AudioCat;

internal static class Codecs
{
    public const string MP3 = "mp3";
    public const string AAC = "aac";
    public const string OPUS = "opus";
    public const string VORBIS = "vorbis";        // OGG Vorbis
    public const string WMAV2 = "wmav2";          // WMA
    public const string PCM_S16_LE = "pcm_s16le"; // WAV; Most of the files with this format will have this codec
    public const string PCM_U8 = "pcm_u8";        // WAV; Less common
    public const string FLAC = "flac";
    public const string MJPEG = "mjpeg";
    public const string PNG = "png";
}