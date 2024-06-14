namespace AudioCat.Models;

internal class NameValue(string name, string value)
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}