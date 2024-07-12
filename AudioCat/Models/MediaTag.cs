using System.Diagnostics;

namespace AudioCat.Models;

[DebuggerDisplay("{Name,nq}: {Value}")]
internal class MediaTag(string name, string value) : IMediaTag
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}