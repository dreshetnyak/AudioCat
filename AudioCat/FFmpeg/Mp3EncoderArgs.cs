using AudioCat.Models;

namespace AudioCat.FFmpeg;

// Part of the planned re-encoding feature (on the roadmap, not yet wired up): builds the ffmpeg
// encoder arguments for user-configurable MP3 (libmp3lame) re-encoding (VBR quality / CBR / ABR
// bitrate, optional lowpass cutoff). Introduced in 4.0.0 together with the FLAC re-encode support;
// the shipped concatenation instead uses the fixed per-codec commands in Settings.EncodingCommands,
// so this class has no callers yet. Kept for the future re-encoding development.
internal sealed class Mp3EncoderArgs : IEncoderArgs
{
    public enum EncodingType { Vbr, Cbr, Abr }
    private EncodingType Encoding { get; init; }
    private int Bitrate { get; init; }           // b (-b): Set bitrate expressed in bits/s for CBR or ABR. LAME bitrate is expressed in kilobits/s.
    private int Quality { get; init; }           // q (-V): Set constant quality setting for VBR.
    private int CutOff { get; init; }            // cutoff (--lowpass): Set lowpass cutoff frequency. If unspecified, the encoder dynamically adjusts the cutoff.

    private Mp3EncoderArgs() { }
    public static IEncoderArgs CreateVbr(int quality, int cutOff = 0) => new Mp3EncoderArgs { Encoding = EncodingType.Vbr, Quality = quality, CutOff = cutOff };
    public static IEncoderArgs CreateCbr(int bitrate, int cutOff = 0) => new Mp3EncoderArgs { Encoding = EncodingType.Cbr, Bitrate = bitrate, CutOff = cutOff };
    public static IEncoderArgs CreateAbr(int bitrate, int cutOff = 0) => new Mp3EncoderArgs { Encoding = EncodingType.Abr, Bitrate = bitrate, CutOff = cutOff };

    public string Build() => Encoding switch
    {
        EncodingType.Vbr => $"-q:a {Quality}{BuildCutOff()}",
        EncodingType.Cbr => $"-b:a {Bitrate}k{BuildCutOff()}",
        EncodingType.Abr => $"-abr 1 -b:a {Bitrate}k{BuildCutOff()}", // abr (--abr): Enable the encoder to use ABR when set to 1. The lame --abr sets the target bitrate, while this options only tells FFmpeg to use ABR still relies on b to set bitrate.
        _ => ""
    };

    private string BuildCutOff() => 
        CutOff > 0 ? $" -cutoff {CutOff}" : "";
}