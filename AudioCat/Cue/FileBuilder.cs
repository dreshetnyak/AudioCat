using System.Diagnostics;
using AudioCat.Models;

namespace AudioCat.Cue;

internal sealed class FileBuilder
{
    [DebuggerDisplay("Name: {Name,nq}; Type: {Type,nq}; Tracks Count: {Tracks.Count,nq}")]
    private sealed class CueFile(string name, string type, IReadOnlyList<ITrack> tracks) : IFile
    {
        public string Name { get; } = name;
        public string Type { get; } = type;
        public IReadOnlyList<ITrack> Tracks { get; } = tracks;
    }

    private string Name { get; set; } = "";
    private string Type { get; set; } = "";
    private List<ITrack> Tracks { get; } = [];

    public void SetName(string fileName) => Name = fileName;
    public void SetType(string fileType) => Type = fileType;
    public void Add(ITrack track) => Tracks.Add(track);

    public IResponse<IFile> Build()
    {
        if (string.IsNullOrEmpty(Name))
            return Response<IFile>.Failure("The FILE command doesn't have the file name");
        if (Tracks.Count == 0)
            return Response<IFile>.Failure("The FILE command doesn't have any TRACK commands");
        
        return Response<IFile>.Success(new CueFile(Name, Type, Tracks.ToArray()));
    }

    public void Clear()
    {
        Name = "";
        Type = "";
        Tracks.Clear();
    }
}
