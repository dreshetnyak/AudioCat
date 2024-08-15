namespace AudioCat.Cue;

internal interface ITag
{
    string Name { get; }
    string Value { get; }
}

internal interface IIndex
{
    int Number { get; }
    TimeSpan Time { get; }
}

internal interface ITrack
{
    int Number { get; }
    string Type { get; }
    string Title { get; }
    string Performer { get; }
    string Songwriter { get; }
    IIndex Index { get; }
    IReadOnlyList<ITag> Tags { get; }
}

internal interface IFile
{
    string Name { get; }
    string Type { get; }
    IReadOnlyList<ITrack> Tracks { get; }
}

internal interface ICue
{
    string Title { get; }
    string Performer { get; }
    string Songwriter { get; }
    IReadOnlyList<ITag> Tags { get; }
    IReadOnlyList<IFile> Files { get; }
}

public interface IFileCommand
{
    string File { get; }
    string Type { get; }
}

public interface ITrackCommand
{
    int Number { get; }
    string Type { get; }
}

public interface IIndexCommand
{
    int Number { get; }
    TimeSpan Time { get; }
}

public interface ITitleCommand
{
    string Title { get; }
}

public interface IPerformerCommand
{
    string Performer { get; }
}

public interface ISongwriterCommand
{
    string Songwriter { get; }
}

public interface ITagCommand
{
    string Name { get; }
    string Value { get; }
}

internal static class Command
{
    public const string FILE = "FILE";
    public const string TRACK = "TRACK";
    public const string INDEX = "INDEX";
    public const string TITLE = "TITLE";
    public const string PERFORMER = "PERFORMER";
    public const string SONGWRITER = "SONGWRITER";
    public const string REM = "REM";
}