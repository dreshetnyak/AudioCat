using AudioCat.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AudioCat.Cue;

internal sealed class TrackBuilder
{
    [DebuggerDisplay("Number: {Number,nq}; Type: {Type,nq}; Title: {Title}; Performer: {Performer}; Songwriter: {Songwriter}; Tags Count: {Tags.Count,nq}")]
    private sealed class CueTrack(int number, string type, string title, string performer, string songwriter, IIndex index, ReadOnlyCollection<ITag> tags) : ITrack
    {
        public int Number { get; } = number;
        public string Type { get; } = type;
        public string Title { get; } = title;
        public string Performer { get; } = performer;
        public string Songwriter { get; } = songwriter;
        public IIndex Index { get; } = index;
        public ReadOnlyCollection<ITag> Tags { get; } = tags;
    }

    // Null means "TRACK command not seen yet"; 0 is a legal parsed number and must not read as unset
    private int? Number { get; set; }
    private string Type { get; set; } = "";
    private string Title { get; set; } = "";
    private string Performer { get; set; } = "";
    private string Songwriter { get; set; } = "";
    private IIndex? Index { get; set; }
    private List<ITag> Tags { get; } = [];

    public void SetNumber(int trackNumber) => Number = trackNumber;
    public void SetType(string trackType) => Type = trackType;
    public void SetTitle(string trackTitle) => Title = trackTitle;
    public void SetPerformer(string trackPerformer) => Performer = trackPerformer;
    public void SetSongwriter(string trackSongwriter) => Songwriter = trackSongwriter;
    public void SetIndex(IIndex trackIndex) => Index = trackIndex;
    public void Add(ITag tag) => Tags.Add(tag);

    public IResponse<ITrack> Build()
    {
        if (Number == null)
            return Response<ITrack>.Failure("The track is missing the number");
        if (Index == null)
            return Response<ITrack>.Failure("The track is missing the index command");

        return Response<ITrack>.Success(new CueTrack(Number.Value, Type, Title, Performer, Songwriter, Index, Tags.ToArray().AsReadOnly())); // Do not remove ToArray() here, it is intended to make a copy of the list
    }

    public void Clear()
    {
        Number = null;
        Type = "";
        Title = "";
        Performer = "";
        Songwriter = "";
        Index = null;
        Tags.Clear();
    }
}