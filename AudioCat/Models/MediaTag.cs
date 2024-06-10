namespace AudioCat.Models;

internal class MediaTag(string name, string value) : IMediaTag
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}