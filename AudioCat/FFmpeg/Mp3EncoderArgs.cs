using AudioCat.Models;

namespace AudioCat.FFmpeg;

internal sealed class Mp3EncoderArgs : IEncoderArgs
{
    public enum EncodingType { Vbr, Cbr, Abr }
    private EncodingType Encoding { get; init; }
    private int Bitrate { get; init; }           // b (-b): Set bitrate expressed in bits/s for CBR or ABR. LAME bitrate is expressed in kilobits/s.
    private int Quality { get; init; }           // q (-V): Set constant quality setting for VBR.
    private int CutOff { get; init; }            // cutoff (--lowpass): Set lowpass cutoff frequency. If unspecified, the encoder dynamically adjusts the cutoff.

    private Mp3EncoderArgs() { }
    public IEncoderArgs CreateVbr(int quality, int cutOff = 0) => new Mp3EncoderArgs { Encoding = EncodingType.Vbr, Quality = quality, CutOff = cutOff };
    public IEncoderArgs CreateCbr(int bitrate, int cutOff = 0) => new Mp3EncoderArgs { Encoding = EncodingType.Cbr, Bitrate = bitrate, CutOff = cutOff };
    public IEncoderArgs CreateAbr(int bitrate, int cutOff = 0) => new Mp3EncoderArgs { Encoding = EncodingType.Abr, Bitrate = bitrate, CutOff = cutOff };

    public string Build() => Encoding switch
    {
        EncodingType.Vbr => $"-q:a {Quality}{BuildCutOff()}",
        EncodingType.Cbr => $"-b:a {Bitrate}{BuildCutOff()}k",
        EncodingType.Abr => $"-abr 1 -b:a {Bitrate}{BuildCutOff()}k", // abr (--abr): Enable the encoder to use ABR when set to 1. The lame --abr sets the target bitrate, while this options only tells FFmpeg to use ABR still relies on b to set bitrate.
        _ => ""
    };

    private string BuildCutOff() => 
        CutOff > 0 ? $" -cutoff {CutOff}" : "";
}