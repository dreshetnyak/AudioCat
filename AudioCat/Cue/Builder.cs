using AudioCat.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace AudioCat.Cue;

internal sealed class Builder
{
    #region Internal Types
    [DebuggerDisplay("Title: {Title}; Performer: {Performer}; Songwriter: {Songwriter}; Tags Count: {Tags.Count,nq}; Files Count: {Files.Count,nq}")]
    private sealed class CueImpl(string sourceFileFullName, string title, string performer, string songwriter, ReadOnlyCollection<ITag> tags, ReadOnlyCollection<IFile> files) : ICue
    {
        public string SourceFileFullName { get; } = sourceFileFullName;
        public string Title { get; } = title;
        public string Performer { get; } = performer;
        public string Songwriter { get; } = songwriter;
        public ReadOnlyCollection<ITag> Tags { get; } = tags;
        public ReadOnlyCollection<IFile> Files { get; } = files;
        public override string ToString()
        {
            try { return Path.GetFileName(SourceFileFullName); }
            catch { return ""; }
        }
    }
    #endregion

    private string SourceFileFullName { get; set; } = "";
    private string Title { get; set; } = "";
    private string Performer { get; set; } = "";
    private string Songwriter { get; set; } = "";
    private List<ITag> Tags { get; } = [];
    private List<IFile> Files { get; } = [];

    public void SetSourceFileFullName(string cueSourceFileFullName) => SourceFileFullName = cueSourceFileFullName;
    public void SetTitle(string trackTitle) => Title = trackTitle;
    public void SetPerformer(string trackPerformer) => Performer = trackPerformer;
    public void SetSongwriter(string trackSongwriter) => Songwriter = trackSongwriter;
    public void Add(ITag tagCommand) => Tags.Add(tagCommand);
    public void Add(IFile file) => Files.Add(file);

    public IResponse<ICue> Build() => Files.Count != 0
        ? Response<ICue>.Success(new CueImpl(SourceFileFullName, Title, Performer, Songwriter, Tags.AsReadOnly(), Files.AsReadOnly())) // Do not remove ToArray() here, it is intended to make a copy of the list
        : Response<ICue>.Failure("No FILE commands found in the cue file");
}