using System.Windows;

namespace AudioCat.Controls;

public sealed class PositionChangeRequestedEventArgs(RoutedEvent routedEvent, int requestedIndex) : RoutedEventArgs(routedEvent)
{
    /// <summary>The data index the user clicked, clamped to [0, Capacity).</summary>
    public int RequestedIndex { get; } = requestedIndex;
}