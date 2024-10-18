using System.Diagnostics;
using AudioCat.Models;

namespace AudioCat.Cue;

internal sealed class TrackBuilder
{
    [DebuggerDisplay("Number: {Number,nq}; Type: {Type,nq}; Title: {Title}; Performer: {Performer}; Songwriter: {Songwriter}; Tags Count: {Tags.Count,nq}")]
    private sealed class CueTrack(int number, string type, string title, string performer, string songwriter, IIndex index, IReadOnlyList<ITag> tags) : ITrack
    {
        public int Number { get; } = number;
        public string Type { get; } = type;
        public string Title { get; } = title;
        public string Performer { get; } = performer;
        public string Songwriter { get; } = songwriter;
        public IIndex Index { get; } = index;
        public IReadOnlyList<ITag> Tags { get; } = tags;
    }

    private int Number { get; set; }
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
        if (Number == 0)
            return Response<ITrack>.Failure("The track is missing the number");
        if (Index == null)
            return Response<ITrack>.Failure("The track is missing the index command");

        return Response<ITrack>.Success(new CueTrack(Number, Type, Title, Performer, Songwriter, Index, Tags.ToArray())); // Do not  remove ToArray() here, it is intended to make a copy of the list
    }

    public void Clear()
    {
        Number = 0;
        Type = "";
        Title = "";
        Performer = "";
        Songwriter = "";
        Index = null;
        Tags.Clear();
    }
}