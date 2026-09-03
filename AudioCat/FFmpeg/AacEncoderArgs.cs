using AudioCat.Models;

namespace AudioCat.FFmpeg;

// Part of the planned re-encoding feature (on the roadmap, not yet wired up): builds the ffmpeg
// encoder arguments for user-configurable AAC re-encoding (VBR quality / CBR bitrate, optional
// frequency cutoff). Introduced in 4.0.0 together with the FLAC re-encode support; the shipped
// concatenation instead uses the fixed per-codec commands in Settings.EncodingCommands, so this
// class has no callers yet. Kept for the future re-encoding development.
internal sealed class AacEncoderArgs : IEncoderArgs
{
    public enum EncodingType { Vbr, Cbr }
    private EncodingType Encoding { get; init; }
    private int Bitrate { get; init; }           // b: Set bit rate in bits/s. Setting this automatically activates constant bit rate (CBR) mode. If this option is unspecified it is set to 128kbps.
    private int Quality { get; init; }           // q: Set quality for variable bit rate (VBR) mode. This option is valid only using the ffmpeg command-line tool. For library interface users, use global_quality.
    private int CutOff { get; init; }            // cutoff: Set cutoff frequency. If unspecified will allow the encoder to dynamically adjust the cutoff to improve clarity on low bitrates.
    
    private AacEncoderArgs() { }
    public static IEncoderArgs CreateVbr(int quality, int cutOff = 0) => new AacEncoderArgs { Encoding = EncodingType.Vbr, Quality = quality, CutOff = cutOff };
    public static IEncoderArgs CreateCbr(int bitrate, int cutOff = 0) => new AacEncoderArgs { Encoding = EncodingType.Cbr, Bitrate = bitrate, CutOff = cutOff };

    public string Build() => Encoding switch
    {
        EncodingType.Vbr => $"-q:a {Quality}{BuildCutOff()}",
        EncodingType.Cbr => $"-b:a {Bitrate}k{BuildCutOff()}",
        _ => ""
    };

    private string BuildCutOff() =>
        CutOff > 0 ? $" -cutoff {CutOff}" : "";
}