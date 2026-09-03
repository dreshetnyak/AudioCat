using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AudioCat.Controls;

public class VolumeLevel : FrameworkElement
{
    static VolumeLevel()
    {
        IsEnabledProperty.OverrideMetadata(
            typeof(VolumeLevel),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
    }

    #region Routed Events

    public static readonly RoutedEvent VolumeChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(VolumeChanged),
        RoutingStrategy.Bubble,
        typeof(EventHandler<VolumeChangedEventArgs>),
        typeof(VolumeLevel));

    public event EventHandler<VolumeChangedEventArgs> VolumeChanged
    {
        add => AddHandler(VolumeChangedEvent, value);
        remove => RemoveHandler(VolumeChangedEvent, value);
    }

    #endregion

    #region Dependency Properties

    public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
        nameof(Volume),
        typeof(float),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(
            0.0f,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnVolumeChanged,
            CoerceVolume));

    private static object CoerceVolume(DependencyObject d, object baseValue) =>
        Math.Clamp((float)baseValue, 0.0f, 1.0f);

    private static void OnVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VolumeLevel control)
            control.RaiseEvent(new VolumeChangedEventArgs(VolumeChangedEvent, (float)e.NewValue));
    }

    public float Volume
    {
        get => (float)GetValue(VolumeProperty);
        set => SetValue(VolumeProperty, value);
    }

    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
        nameof(Background),
        typeof(Brush),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush Background
    {
        get => (Brush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly DependencyProperty TriangleColorProperty = DependencyProperty.Register(
        nameof(TriangleColor),
        typeof(Brush),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(Brushes.DarkSlateGray, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush TriangleColor
    {
        get => (Brush)GetValue(TriangleColorProperty);
        set => SetValue(TriangleColorProperty, value);
    }

    public static readonly DependencyProperty TriangleThicknessProperty = DependencyProperty.Register(
        nameof(TriangleThickness),
        typeof(double),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double TriangleThickness
    {
        get => (double)GetValue(TriangleThicknessProperty);
        set => SetValue(TriangleThicknessProperty, value);
    }

    public static readonly DependencyProperty VolumeFillColorProperty = DependencyProperty.Register(
        nameof(VolumeFillColor),
        typeof(Brush),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(Brushes.SteelBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush VolumeFillColor
    {
        get => (Brush)GetValue(VolumeFillColorProperty);
        set => SetValue(VolumeFillColorProperty, value);
    }

    public static readonly DependencyProperty VolumeFillDisabledColorProperty = DependencyProperty.Register(
        nameof(VolumeFillDisabledColor),
        typeof(Brush),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush VolumeFillDisabledColor
    {
        get => (Brush)GetValue(VolumeFillDisabledColorProperty);
        set => SetValue(VolumeFillDisabledColorProperty, value);
    }

    public static readonly DependencyProperty IsPercentageVisibleProperty = DependencyProperty.Register(
        nameof(IsPercentageVisible),
        typeof(bool),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsPercentageVisible
    {
        get => (bool)GetValue(IsPercentageVisibleProperty);
        set => SetValue(IsPercentageVisibleProperty, value);
    }

    public static readonly DependencyProperty PercentageColorProperty = DependencyProperty.Register(
        nameof(PercentageColor),
        typeof(Brush),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(Brushes.DarkSlateGray, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush PercentageColor
    {
        get => (Brush)GetValue(PercentageColorProperty);
        set => SetValue(PercentageColorProperty, value);
    }

    public static readonly DependencyProperty PercentageFontFamilyProperty = DependencyProperty.Register(
        nameof(PercentageFontFamily),
        typeof(FontFamily),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(new FontFamily("Consolas"), FrameworkPropertyMetadataOptions.AffectsRender));

    public FontFamily PercentageFontFamily
    {
        get => (FontFamily)GetValue(PercentageFontFamilyProperty);
        set => SetValue(PercentageFontFamilyProperty, value);
    }

    public static readonly DependencyProperty PercentageFontSizeProperty = DependencyProperty.Register(
        nameof(PercentageFontSize),
        typeof(double),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double PercentageFontSize
    {
        get => (double)GetValue(PercentageFontSizeProperty);
        set => SetValue(PercentageFontSizeProperty, value);
    }

    public static readonly DependencyProperty PercentageOffsetXProperty = DependencyProperty.Register(
        nameof(PercentageOffsetX),
        typeof(double),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double PercentageOffsetX
    {
        get => (double)GetValue(PercentageOffsetXProperty);
        set => SetValue(PercentageOffsetXProperty, value);
    }

    public static readonly DependencyProperty PercentageOffsetYProperty = DependencyProperty.Register(
        nameof(PercentageOffsetY),
        typeof(double),
        typeof(VolumeLevel),
        new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double PercentageOffsetY
    {
        get => (double)GetValue(PercentageOffsetYProperty);
        set => SetValue(PercentageOffsetYProperty, value);
    }

    #endregion

    #region Rendering

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        var volume = (double)Volume;
        var fillBrush = IsEnabled ? VolumeFillColor : VolumeFillDisabledColor;

        // --- 1. Level triangle filled with Background (opaque base, no stroke) ---
        var levelGeometry = BuildLevelTriangle(width, height);
        dc.DrawGeometry(Background, null, levelGeometry);

        // --- 2. Volume fill triangle ---
        if (volume > 0.0)
        {
            var fillGeometry = BuildFillTriangle(width, height, volume);
            dc.DrawGeometry(fillBrush, null, fillGeometry);
        }

        // --- 3. Level triangle outline — stroked, rounded corners, no fill ---
        var outlinePen = new Pen(TriangleColor, TriangleThickness)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        dc.DrawGeometry(null, outlinePen, levelGeometry);

        // --- 4. Percentage text drawn on top of everything ---
        if (IsPercentageVisible)
            DrawPercentageText(dc, volume);
    }

    private static StreamGeometry BuildLevelTriangle(double width, double height)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(0, height), isFilled: true, isClosed: true);
        ctx.LineTo(new Point(width, height), isStroked: true, isSmoothJoin: false);
        ctx.LineTo(new Point(width, 0), isStroked: true, isSmoothJoin: false);
        geometry.Freeze();
        return geometry;
    }

    private static StreamGeometry BuildFillTriangle(double width, double height, double volume)
    {
        var fillX = volume * width;
        var fillY = height * (1.0 - volume);
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(0, height), isFilled: true, isClosed: true);
        ctx.LineTo(new Point(fillX, height), isStroked: false, isSmoothJoin: false);
        ctx.LineTo(new Point(fillX, fillY), isStroked: false, isSmoothJoin: false);
        geometry.Freeze();
        return geometry;
    }

    private void DrawPercentageText(DrawingContext dc, double volume)
    {
        var text = $"{(int)Math.Round(volume * 100)}%";
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(PercentageFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            PercentageFontSize,
            PercentageColor,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formattedText, new Point(PercentageOffsetX, PercentageOffsetY));
    }

    #endregion

    #region Mouse Interaction

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        CaptureMouse();
        UpdateVolumeFromMousePosition(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            UpdateVolumeFromMousePosition(e.GetPosition(this));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        e.Handled = true;
    }

    private void UpdateVolumeFromMousePosition(Point position)
    {
        var width = ActualWidth;
        if (width <= 0)
            return;
        Volume = (float)(position.X / width);
    }

    #endregion
}