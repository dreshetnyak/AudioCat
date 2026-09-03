using System.Windows;

namespace AudioCat.Controls;

public class VolumeChangedEventArgs(RoutedEvent routedEvent, float newValue) : RoutedEventArgs(routedEvent)
{
    public float NewValue { get; } = newValue;
}