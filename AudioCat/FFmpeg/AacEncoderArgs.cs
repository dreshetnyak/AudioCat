using AudioCat.Models;

namespace AudioCat.FFmpeg;

internal sealed class AacEncoderArgs : IEncoderArgs
{
    public enum EncodingType { Vbr, Cbr }
    private EncodingType Encoding { get; init; }
    private int Bitrate { get; init; }           // b: Set bit rate in bits/s. Setting this automatically activates constant bit rate (CBR) mode. If this option is unspecified it is set to 128kbps.
    private int Quality { get; init; }           // q: Set quality for variable bit rate (VBR) mode. This option is valid only using the ffmpeg command-line tool. For library interface users, use global_quality.
    private int CutOff { get; init; }            // cutoff: Set cutoff frequency. If unspecified will allow the encoder to dynamically adjust the cutoff to improve clarity on low bitrates.
    
    private AacEncoderArgs() { }
    public IEncoderArgs CreateVbr(int quality, int cutOff = 0) => new AacEncoderArgs { Encoding = EncodingType.Vbr, Quality = quality, CutOff = cutOff };
    public IEncoderArgs CreateCbr(int bitrate, int cutOff = 0) => new AacEncoderArgs { Encoding = EncodingType.Cbr, Bitrate = bitrate, CutOff = cutOff };

    public string Build() => Encoding switch
    {
        EncodingType.Vbr => $"-q:a {Quality}{BuildCutOff()}",
        EncodingType.Cbr => $"-b:a {Bitrate}{BuildCutOff()}k",
        _ => ""
    };

    private string BuildCutOff() =>
        CutOff > 0 ? $" -cutoff {CutOff}" : "";
}