namespace AudioCat.Models;

// Part of the planned re-encoding feature (on the roadmap, not yet wired up): implementations
// build the codec-specific ffmpeg encoder arguments (see AacEncoderArgs, Mp3EncoderArgs). The
// shipped concatenation uses the fixed per-codec commands in Settings.EncodingCommands instead.
internal interface IEncoderArgs
{
    string Build();
}