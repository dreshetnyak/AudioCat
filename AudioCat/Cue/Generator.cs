namespace AudioCat.Cue;

internal static class Generator
{
    public static IEnumerable<string> ToCommands(this ICue cue)
    {
        if (cue.Title != "")
            yield return $"TITLE \"{cue.Title}\"";
        if (cue.Performer != "")
            yield return $"PERFORMER \"{cue.Performer}\"";
        if (cue.Songwriter != "")
            yield return $"SONGWRITER \"{cue.Songwriter}\"";
        foreach (var tag in cue.Tags)
            yield return $"REM {tag.Name} {tag.Value}";

        foreach (var file in cue.Files)
        {
            yield return file.Type != ""
                ? $"FILE \"{file.Name}\" {file.Type}"
                : $"FILE \"{file.Name}\"";
                
            foreach (var track in file.Tracks)
            {
                yield return $"  TRACK {track.Number:00} {track.Type}";
                if (track.Title != "")
                    yield return $"    TITLE \"{track.Title}\"";
                if (track.Performer != "")
                    yield return $"    PERFORMER \"{track.Performer}\"";
                if (track.Songwriter != "")
                    yield return $"    SONGWRITER \"{track.Songwriter}\"";
                foreach (var tag in track.Tags)
                    yield return tag.Value != "" 
                        ? $"    REM {tag.Name} {tag.Value}" 
                        : $"    REM {tag.Name}";
                yield return $"    INDEX {track.Index.Number:00} {track.Index.Time.ToTrackIndexTime()}";
            }
        }
    }

    private const decimal FRAME_SIZE = 1000m / 75m;
    private static string ToTrackIndexTime(this TimeSpan startTime) =>
        $"{(int)startTime.TotalMinutes:00}:{startTime.Seconds:00}:{(int)(startTime.Milliseconds / FRAME_SIZE):00}";
}