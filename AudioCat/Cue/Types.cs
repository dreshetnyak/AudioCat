using System.Collections.ObjectModel;

namespace AudioCat.Cue;

public interface ITag
{
    string Name { get; }
    string Value { get; }
}

public interface IIndex
{
    int Number { get; }
    TimeSpan Time { get; }
}

internal interface IRawIndexTime
{
    int Minutes { get; }
    int Seconds { get; }
    int Frames { get; }
}

public interface ITrack
{
    int Number { get; }
    string Type { get; }
    string Title { get; }
    string Performer { get; }
    string Songwriter { get; }
    IIndex Index { get; }
    ReadOnlyCollection<ITag> Tags { get; }
}

public interface IFile
{
    string Name { get; }
    string Type { get; }
    ReadOnlyCollection<ITrack> Tracks { get; }
}

public interface ICue
{
    string SourceFileFullName { get; }
    string Title { get; }
    string Performer { get; }
    string Songwriter { get; }
    ReadOnlyCollection<ITag> Tags { get; }
    ReadOnlyCollection<IFile> Files { get; }
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
