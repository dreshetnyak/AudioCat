using System.Diagnostics;

namespace AudioCat.Models;

[DebuggerDisplay("{Name,nq}: {Value}")]
internal class NameValue(string name, string value)
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}