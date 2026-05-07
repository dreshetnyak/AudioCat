using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AudioCat.Controls;

/// <summary>
/// Interaction logic for PlayerControl.xaml
/// </summary>
public partial class PlayerControl : UserControl
{
    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
        nameof(IsPlaying), typeof(bool), typeof(PlayerControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty CanRewindProperty = DependencyProperty.Register(
        nameof(CanRewind), typeof(bool), typeof(PlayerControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty CanForwardProperty = DependencyProperty.Register(
        nameof(CanForward), typeof(bool), typeof(PlayerControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsMutedProperty = DependencyProperty.Register(
        nameof(IsMuted), typeof(bool), typeof(PlayerControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
        nameof(Volume), typeof(float), typeof(PlayerControl),
        new PropertyMetadata(1.0f));

    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
        nameof(Position), typeof(TimeSpan), typeof(PlayerControl),
        new PropertyMetadata(TimeSpan.Zero));

    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration), typeof(TimeSpan), typeof(PlayerControl),
        new PropertyMetadata(TimeSpan.Zero));

    public static readonly DependencyProperty PlayPauseCommandProperty = DependencyProperty.Register(
        nameof(PlayPauseCommand), typeof(ICommand), typeof(PlayerControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty StopCommandProperty = DependencyProperty.Register(
        nameof(StopCommand), typeof(ICommand), typeof(PlayerControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty PreviousCommandProperty = DependencyProperty.Register(
        nameof(PreviousCommand), typeof(ICommand), typeof(PlayerControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty BackwardCommandProperty = DependencyProperty.Register(
        nameof(BackwardCommand), typeof(ICommand), typeof(PlayerControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ForwardCommandProperty = DependencyProperty.Register(
        nameof(ForwardCommand), typeof(ICommand), typeof(PlayerControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty NextCommandProperty = DependencyProperty.Register(
        nameof(NextCommand), typeof(ICommand), typeof(PlayerControl),
        new PropertyMetadata(null));

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public bool CanRewind
    {
        get => (bool)GetValue(CanRewindProperty);
        set => SetValue(CanRewindProperty, value);
    }

    public bool CanForward
    {
        get => (bool)GetValue(CanForwardProperty);
        set => SetValue(CanForwardProperty, value);
    }

    public bool IsMuted
    {
        get => (bool)GetValue(IsMutedProperty);
        set => SetValue(IsMutedProperty, value);
    }

    public float Volume
    {
        get => (float)GetValue(VolumeProperty);
        set => SetValue(VolumeProperty, value);
    }

    public TimeSpan Position
    {
        get => (TimeSpan)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public ICommand? PlayPauseCommand
    {
        get => (ICommand?)GetValue(PlayPauseCommandProperty);
        set => SetValue(PlayPauseCommandProperty, value);
    }

    public ICommand? StopCommand
    {
        get => (ICommand?)GetValue(StopCommandProperty);
        set => SetValue(StopCommandProperty, value);
    }

    public ICommand? PreviousCommand
    {
        get => (ICommand?)GetValue(PreviousCommandProperty);
        set => SetValue(PreviousCommandProperty, value);
    }

    public ICommand? BackwardCommand
    {
        get => (ICommand?)GetValue(BackwardCommandProperty);
        set => SetValue(BackwardCommandProperty, value);
    }

    public ICommand? ForwardCommand
    {
        get => (ICommand?)GetValue(ForwardCommandProperty);
        set => SetValue(ForwardCommandProperty, value);
    }

    public ICommand? NextCommand
    {
        get => (ICommand?)GetValue(NextCommandProperty);
        set => SetValue(NextCommandProperty, value);
    }

    public PlayerControl()
    {
        InitializeComponent();
    }

    private void VolumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsEnabled)
            IsMuted = !IsMuted;
    }
}