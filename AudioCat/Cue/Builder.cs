using System.Diagnostics;
using AudioCat.Models;

namespace AudioCat.Cue;

internal sealed class Builder
{
    #region Internal Types
    [DebuggerDisplay("Title: {Title}; Performer: {Performer}; Songwriter: {Songwriter}; Tags Count: {Tags.Count,nq}; Files Count: {Files.Count,nq}")]
    private sealed class CueImpl(string title, string performer, string songwriter, IReadOnlyList<ITag> tags, IReadOnlyList<IFile> files) : ICue
    {
        public string Title { get; } = title;
        public string Performer { get; } = performer;
        public string Songwriter { get; } = songwriter;
        public IReadOnlyList<ITag> Tags { get; } = tags;
        public IReadOnlyList<IFile> Files { get; } = files;
    }
    #endregion

    private string Title { get; set; } = "";
    private string Performer { get; set; } = "";
    private string Songwriter { get; set; } = "";
    private List<ITag> Tags { get; } = [];
    private List<IFile> Files { get; } = [];

    public void SetTitle(string trackTitle) => Title = trackTitle;
    public void SetPerformer(string trackPerformer) => Performer = trackPerformer;
    public void SetSongwriter(string trackSongwriter) => Songwriter = trackSongwriter;
    public void Add(ITag tagCommand) => Tags.Add(tagCommand);
    public void Add(IFile file) => Files.Add(file);

    public IResponse<ICue> Build() => Files.Count != 0
        ? Response<ICue>.Success(new CueImpl(Title, Performer, Songwriter, Tags.ToArray(), Files.ToArray()))
        : Response<ICue>.Failure("No FILE commands found in the cue file");
}