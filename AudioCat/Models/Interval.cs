using System.Diagnostics;

namespace AudioCat.Models;

public interface IInterval
{
    string FileFullName { get; }
    TimeSpan Start { get; }
    TimeSpan End { get; }
    TimeSpan Duration { get; }
}

[DebuggerDisplay("Start: {Start,nq}; End: {End,nq}; Duration: {Duration,nq}; {FileFullName}")]
internal sealed class Interval(string fileFullName, TimeSpan startTime, TimeSpan endTime) : IInterval
{
    public string FileFullName { get; } = fileFullName;
    public TimeSpan Start { get; } = startTime;
    public TimeSpan End { get; } = endTime;
    public TimeSpan Duration { get; } = endTime - startTime;
}