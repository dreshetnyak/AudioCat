namespace AudioCat.Controls;

/// <summary>
/// Represents a positional marker drawn as a vertical line on the <see cref="DataStripControl"/>.
/// Implement this interface on your own ViewModel to avoid a mapping layer.
/// </summary>
public interface IStripBookmark
{
    /// <summary>Data index within [0, Capacity). The data item at this index may not yet exist.</summary>
    int Index { get; }

    /// <summary>Human-readable description; reserved for future rendering use.</summary>
    string Description { get; }
}