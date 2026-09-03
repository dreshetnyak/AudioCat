using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AudioCat.Controls;

/// <summary>
/// A horizontal strip that renders an interpolated line over a growing float data collection,
/// with optional bookmark markers and a current-position indicator.
/// The control is agnostic of the audio domain; all concepts are expressed in data indices.
/// </summary>
public sealed class DataStripControl : FrameworkElement
{
    #region Routed Events

    public static readonly RoutedEvent PositionChangeRequestedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(PositionChangeRequested),
            RoutingStrategy.Bubble,
            typeof(EventHandler<PositionChangeRequestedEventArgs>),
            typeof(DataStripControl));

    /// <summary>
    /// Raised when the user clicks the strip. The consumer decides whether to honour the request.
    /// </summary>
    public event EventHandler<PositionChangeRequestedEventArgs> PositionChangeRequested
    {
        add    => AddHandler(PositionChangeRequestedEvent, value);
        remove => RemoveHandler(PositionChangeRequestedEvent, value);
    }

    #endregion

    #region Dependency Properties — Appearance

    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(DataStripControl),
            new FrameworkPropertyMetadata(Brushes.White,
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Strip background color. Default: White.</summary>
    public Brush Background
    {
        get => (Brush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    // ── Line ────────────────────────────────────────────────────────────────

    public static readonly DependencyProperty LineColorProperty =
        DependencyProperty.Register(nameof(LineColor), typeof(Brush), typeof(DataStripControl),
            new FrameworkPropertyMetadata(Brushes.DarkGray,
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Color of the interpolated data line. Default: DarkGray.</summary>
    public Brush LineColor
    {
        get => (Brush)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public static readonly DependencyProperty LineThicknessProperty =
        DependencyProperty.Register(nameof(LineThickness), typeof(double), typeof(DataStripControl),
            new FrameworkPropertyMetadata(1.0,
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Thickness of the interpolated data line. Default: 1.</summary>
    public double LineThickness
    {
        get => (double)GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    public static readonly DependencyProperty FillColorProperty =
        DependencyProperty.Register(nameof(FillColor), typeof(Brush), typeof(DataStripControl),
            new FrameworkPropertyMetadata(Brushes.LightGray,
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Fill color for the area below the interpolated line. Default: LightGray.</summary>
    public Brush FillColor
    {
        get => (Brush)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public static readonly DependencyProperty InterpolationTensionProperty =
        DependencyProperty.Register(nameof(InterpolationTension), typeof(double), typeof(DataStripControl),
            new FrameworkPropertyMetadata(0.5,
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// Catmull-Rom interpolation blend strength in [0.0, 1.0].
    /// 0.0 = straight line segments; 1.0 = fully smooth spline. Default: 0.5.
    /// </summary>
    public double InterpolationTension
    {
        get => (double)GetValue(InterpolationTensionProperty);
        set => SetValue(InterpolationTensionProperty, value);
    }

    // ── Bookmarks ───────────────────────────────────────────────────────────

    public static readonly DependencyProperty BookmarkLineColorProperty =
        DependencyProperty.Register(nameof(BookmarkLineColor), typeof(Brush), typeof(DataStripControl),
            new FrameworkPropertyMetadata(Brushes.Gray,
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Color of bookmark vertical lines. Default: Gray.</summary>
    public Brush BookmarkLineColor
    {
        get => (Brush)GetValue(BookmarkLineColorProperty);
        set => SetValue(BookmarkLineColorProperty, value);
    }

    public static readonly DependencyProperty BookmarkLineThicknessProperty =
        DependencyProperty.Register(nameof(BookmarkLineThickness), typeof(double), typeof(DataStripControl),
            new FrameworkPropertyMetadata(1.0,
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Thickness of bookmark vertical lines. Default: 1.</summary>
    public double BookmarkLineThickness
    {
        get => (double)GetValue(BookmarkLineThicknessProperty);
        set => SetValue(BookmarkLineThicknessProperty, value);
    }

    // ── Position indicator ──────────────────────────────────────────────────

    public static readonly DependencyProperty PositionIndicatorColorProperty =
        DependencyProperty.Register(nameof(PositionIndicatorColor), typeof(Brush), typeof(DataStripControl),
            new FrameworkPropertyMetadata(Brushes.Black,
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Color of the current-position line and triangles. Default: Black.</summary>
    public Brush PositionIndicatorColor
    {
        get => (Brush)GetValue(PositionIndicatorColorProperty);
        set => SetValue(PositionIndicatorColorProperty, value);
    }

    public static readonly DependencyProperty PositionLineThicknessProperty =
        DependencyProperty.Register(nameof(PositionLineThickness), typeof(double), typeof(DataStripControl),
            new FrameworkPropertyMetadata(1.0,
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Thickness of the current-position vertical line. Default: 1.</summary>
    public double PositionLineThickness
    {
        get => (double)GetValue(PositionLineThicknessProperty);
        set => SetValue(PositionLineThicknessProperty, value);
    }

    public static readonly DependencyProperty PositionTriangleWidthProperty =
        DependencyProperty.Register(nameof(PositionTriangleWidth), typeof(double), typeof(DataStripControl),
            new FrameworkPropertyMetadata(8.0,
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// Width (and height) of the triangles at the top and bottom of the position indicator.
    /// Default: 8.
    /// </summary>
    public double PositionTriangleWidth
    {
        get => (double)GetValue(PositionTriangleWidthProperty);
        set => SetValue(PositionTriangleWidthProperty, value);
    }

    #endregion

    #region Dependency Properties — Data

    public static readonly DependencyProperty CapacityProperty =
        DependencyProperty.Register(nameof(Capacity), typeof(int), typeof(DataStripControl),
            new PropertyMetadata(0, OnCapacityChanged));

    /// <summary>
    /// Total expected number of items in <see cref="Data"/> when fully populated.
    /// Defines the logical width of the strip in data units.
    /// </summary>
    public int Capacity
    {
        get => (int)GetValue(CapacityProperty);
        set => SetValue(CapacityProperty, value);
    }

    private static void OnCapacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (DataStripControl)d;

        // When auto-following, re-evaluate whether the new full range {0, Capacity}
        // should now lock or unlock auto-follow (e.g. an explicit {0, oldCapacity} is
        // no longer equal to {0, newCapacity}).
        var zoom = ctrl.ZoomRange;
        if (!zoom.IsSentinel)
            ctrl._autoFollowZoom = zoom.Start == 0 && zoom.End == (int)e.NewValue;

        ctrl.InvalidateVisual();
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(ObservableCollection<float>), typeof(DataStripControl),
            new PropertyMetadata(null, OnDataChanged));

    /// <summary>
    /// The float data collection. A value of 0 maps to the bottom of the strip;
    /// 10 000 maps to the top. Items are drawn left-to-right in index order.
    /// </summary>
    public ObservableCollection<float>? Data
    {
        get => (ObservableCollection<float>?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (DataStripControl)d;
        if (e.OldValue is ObservableCollection<float> old)
            old.CollectionChanged -= ctrl.OnDataCollectionChanged;
        if (e.NewValue is ObservableCollection<float> fresh)
            fresh.CollectionChanged += ctrl.OnDataCollectionChanged;
        ctrl.InvalidateVisual();
    }

    private void OnDataCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        InvalidateVisual();

    public static readonly DependencyProperty BookmarksProperty =
        DependencyProperty.Register(nameof(Bookmarks), typeof(ObservableCollection<IStripBookmark>), typeof(DataStripControl),
            new PropertyMetadata(null, OnBookmarksChanged));

    /// <summary>
    /// Collection of bookmark markers. Each is drawn as a vertical line at its data index,
    /// even if that index has no data yet.
    /// </summary>
    public ObservableCollection<IStripBookmark>? Bookmarks
    {
        get => (ObservableCollection<IStripBookmark>?)GetValue(BookmarksProperty);
        set => SetValue(BookmarksProperty, value);
    }

    private static void OnBookmarksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (DataStripControl)d;
        if (e.OldValue is ObservableCollection<IStripBookmark> old)
            old.CollectionChanged -= ctrl.OnBookmarksCollectionChanged;
        if (e.NewValue is ObservableCollection<IStripBookmark> fresh)
            fresh.CollectionChanged += ctrl.OnBookmarksCollectionChanged;
        ctrl.InvalidateVisual();
    }

    private void OnBookmarksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        InvalidateVisual();

    public static readonly DependencyProperty CurrentPositionProperty =
        DependencyProperty.Register(nameof(CurrentPosition), typeof(int), typeof(DataStripControl),
            new PropertyMetadata(0, OnCurrentPositionChanged));

    /// <summary>
    /// Current playback / navigation position as a data index within [0, Capacity).
    /// Setting this may shift the zoom window to keep the position visible.
    /// </summary>
    public int CurrentPosition
    {
        get => (int)GetValue(CurrentPositionProperty);
        set => SetValue(CurrentPositionProperty, value);
    }

    private static void OnCurrentPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (DataStripControl)d;
        ctrl.AdjustZoomForCurrentPosition();
        ctrl.InvalidateVisual();
    }

    #endregion

    #region Dependency Properties — Zoom

    public static readonly DependencyProperty ZoomRangeProperty =
        DependencyProperty.Register(nameof(ZoomRange), typeof(ZoomRange), typeof(DataStripControl),
            new PropertyMetadata(ZoomRange.Sentinel, OnZoomRangeChanged));

    /// <summary>
    /// Visible data index range.
    /// <list type="bullet">
    ///   <item><see cref="ZoomRange.Sentinel"/> {0,0} — auto-follow Capacity (default).</item>
    ///   <item>{0, Capacity} — explicit 100 %; also auto-follows Capacity changes.</item>
    ///   <item>Any other value — locked; Capacity changes do not affect the window.</item>
    /// </list>
    /// Values are clamped to [0, Capacity] on render.
    /// </summary>
    public ZoomRange ZoomRange
    {
        get => (ZoomRange)GetValue(ZoomRangeProperty);
        set => SetValue(ZoomRangeProperty, value);
    }

    // true  → window tracks Capacity (sentinel OR explicitly set to {0, Capacity})
    // false → window is locked to whatever the consumer set
    private bool _autoFollowZoom = true;

    private static void OnZoomRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl     = (DataStripControl)d;
        var zoom     = (ZoomRange)e.NewValue;
        var capacity = ctrl.Capacity;

        ctrl._autoFollowZoom = zoom.IsSentinel || (zoom.Start == 0 && zoom.End == capacity);
        ctrl.InvalidateVisual();
    }

    #endregion

    #region Constructor

    public DataStripControl()
    {
        SizeChanged += (_, _) => InvalidateVisual();
    }

    #endregion

    #region Zoom logic

    private (int start, int end) GetEffectiveZoom()
    {
        var capacity = Math.Max(Capacity, 1);

        if (_autoFollowZoom)
            return (0, capacity);

        var zoom  = ZoomRange;
        var start = Math.Clamp(zoom.Start, 0, capacity);
        var end   = Math.Clamp(zoom.End,   0, capacity);

        if (end <= start)
            end = start + 1;

        return (start, end);
    }

    /// <summary>
    /// If CurrentPosition falls outside the current zoom window, shifts the window
    /// while preserving its size so that the position is visible with ~1/3 of the
    /// window range as breathing room beyond it.
    /// </summary>
    private void AdjustZoomForCurrentPosition()
    {
        // In auto-follow mode the full range is always visible — nothing to shift.
        if (_autoFollowZoom)
            return;

        var zoom       = ZoomRange;
        var pos        = CurrentPosition;
        var capacity   = Capacity;
        var windowSize = zoom.End - zoom.Start;

        if (windowSize <= 0)
            return;

        int newStart, newEnd;
        var padding = windowSize / 3;

        if (pos >= zoom.End)
        {
            // Shift right: keep ~1/3 of the window after CurrentPosition.
            newEnd   = Math.Min(pos + padding, capacity);
            newStart = Math.Max(newEnd - windowSize, 0);
        }
        else if (pos < zoom.Start)
        {
            // Shift left: keep ~1/3 of the window before CurrentPosition.
            newStart = Math.Max(pos - padding, 0);
            newEnd   = Math.Min(newStart + windowSize, capacity);
        }
        else
        {
            return; // Already within the window.
        }

        // Setting this property fires OnZoomRangeChanged, which updates _autoFollowZoom.
        SetValue(ZoomRangeProperty, new ZoomRange(newStart, newEnd));
    }

    #endregion

    #region Rendering

    protected override void OnRender(DrawingContext dc)
    {
        var width  = ActualWidth;
        var height = ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        var bounds = new Rect(0, 0, width, height);

        // ── 1. Background ──────────────────────────────────────────────────
        dc.DrawRectangle(Background, null, bounds);

        // Clip all subsequent drawing to the control bounds.
        dc.PushClip(new RectangleGeometry(bounds));

        var (zStart, zEnd) = GetEffectiveZoom();
        var zRange         = (double)(zEnd - zStart);

        // ── 2 & 3. Waveform fill + line ────────────────────────────────────
        var data = Data;
        if (data is { Count: >= 2 })
        {
            var points = BuildScreenPoints(data, zStart, zRange, width, height);
            if (points.Count >= 2)
            {
                dc.DrawGeometry(FillColor, null, BuildFillGeometry(points, height));
                dc.DrawGeometry(null, new Pen(LineColor, LineThickness), BuildLineGeometry(points));
            }
        }

        // ── 4. Bookmarks ───────────────────────────────────────────────────
        var bookmarks = Bookmarks;
        if (bookmarks is { Count: > 0 })
        {
            var bookmarkPen = new Pen(BookmarkLineColor, BookmarkLineThickness);
            bookmarkPen.Freeze();

            foreach (var bookmark in bookmarks)
            {
                var bx = IndexToX(bookmark.Index, zStart, zRange, width);
                if (bx < 0 || bx > width)
                    continue;
                dc.DrawLine(bookmarkPen, new Point(bx, 0), new Point(bx, height));
            }
        }

        // ── 5. Current position indicator ──────────────────────────────────
        DrawPositionIndicator(dc, IndexToX(CurrentPosition, zStart, zRange, width), width, height);

        dc.Pop(); // clip
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static double IndexToX(int index, int zStart, double zRange, double width) =>
        (index - zStart) / zRange * width;

    /// <summary>
    /// Samples the data collection at pixel resolution using Catmull-Rom blended with
    /// linear interpolation, controlled by <see cref="InterpolationTension"/>.
    /// Only pixels that correspond to existing data indices are included.
    /// </summary>
    private List<Point> BuildScreenPoints(
        ObservableCollection<float> data,
        int zStart, double zRange,
        double width, double height)
    {
        var tension    = InterpolationTension;
        var dataCount  = data.Count;
        var pixelCount = (int)Math.Ceiling(width) + 1;
        var points     = new List<Point>(pixelCount);

        for (var px = 0; px < pixelCount; px++)
        {
            // Fractional data index corresponding to this pixel column.
            var fi = zStart + px / width * zRange;

            if (fi < 0)
                continue;
            if (fi > dataCount - 1)
                break;

            var value = SampleCatmullRom(data, fi, tension);
            var y     = height - Math.Clamp(value, 0f, 10000f) / 10000.0 * height;

            points.Add(new Point(px, y));
        }

        return points;
    }

    /// <summary>
    /// Blends standard Catmull-Rom with linear interpolation.
    /// tension=0 → straight line segments; tension=1 → fully smooth spline.
    /// </summary>
    private static float SampleCatmullRom(ObservableCollection<float> data, double fi, double tension)
    {
        var n  = data.Count;
        var i  = (int)Math.Floor(fi);
        var t  = (float)(fi - i);

        if (i >= n - 1)
            return data[n - 1];

        var p1 = data[i];
        var p2 = data[Math.Min(i + 1, n - 1)];

        // Linear baseline — guaranteed by tension=0.
        var linear = p1 + t * (p2 - p1);

        if (tension <= 0.0)
            return linear;

        // Catmull-Rom tangents (standard α = 0.5).
        var p0 = data[Math.Max(i - 1, 0)];
        var p3 = data[Math.Min(i + 2, n - 1)];
        var m1 = 0.5f * (p2 - p0);
        var m2 = 0.5f * (p3 - p1);

        var t2     = t * t;
        var t3     = t2 * t;
        var spline = (2 * t3 - 3 * t2 + 1) * p1 +
                     (t3 - 2 * t2 + t)     * m1 +
                     (-2 * t3 + 3 * t2)    * p2 +
                     (t3 - t2)             * m2;

        // Blend: 0 = linear, 1 = full Catmull-Rom.
        return linear + (float)tension * (spline - linear);
    }

    private static Geometry BuildFillGeometry(List<Point> points, double height)
    {
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            // Start at the bottom-left corner of the data region.
            ctx.BeginFigure(new Point(points[0].X, height), isFilled: true, isClosed: true);
            ctx.LineTo(points[0], isStroked: true, isSmoothJoin: false);
            for (var i = 1; i < points.Count; i++)
                ctx.LineTo(points[i], isStroked: true, isSmoothJoin: false);
            // Drop back down to the bottom-right corner.
            ctx.LineTo(new Point(points[^1].X, height), isStroked: true, isSmoothJoin: false);
        }
        geo.Freeze();
        return geo;
    }

    private static Geometry BuildLineGeometry(List<Point> points)
    {
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(points[0], isFilled: false, isClosed: false);
            for (var i = 1; i < points.Count; i++)
                ctx.LineTo(points[i], isStroked: true, isSmoothJoin: false);
        }
        geo.Freeze();
        return geo;
    }

    private void DrawPositionIndicator(DrawingContext dc, double x, double width, double height)
    {
        var tw = PositionTriangleWidth;

        // Do not draw if the indicator is fully outside the control bounds.
        if (x + tw / 2.0 < 0 || x - tw / 2.0 > width)
            return;

        var fill = PositionIndicatorColor;
        var pen  = new Pen(fill, PositionLineThickness);
        pen.Freeze();

        // Full-height vertical line.
        dc.DrawLine(pen, new Point(x, 0), new Point(x, height));

        var half = tw / 2.0;

        // Top triangle — points downward, apex at (x, tw).
        var topGeo = new StreamGeometry();
        using (var ctx = topGeo.Open())
        {
            ctx.BeginFigure(new Point(x - half, 0), isFilled: true, isClosed: true);
            ctx.LineTo(new Point(x + half, 0),  isStroked: true, isSmoothJoin: false);
            ctx.LineTo(new Point(x,        tw), isStroked: true, isSmoothJoin: false);
        }
        topGeo.Freeze();
        dc.DrawGeometry(fill, null, topGeo);

        // Bottom triangle — points upward, apex at (x, height - tw).
        var bottomGeo = new StreamGeometry();
        using (var ctx = bottomGeo.Open())
        {
            ctx.BeginFigure(new Point(x - half, height),      isFilled: true, isClosed: true);
            ctx.LineTo(new Point(x + half, height),            isStroked: true, isSmoothJoin: false);
            ctx.LineTo(new Point(x,        height - tw),       isStroked: true, isSmoothJoin: false);
        }
        bottomGeo.Freeze();
        dc.DrawGeometry(fill, null, bottomGeo);
    }

    #endregion

    #region Mouse interaction

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (ActualWidth <= 0)
            return;

        var (zStart, zEnd) = GetEffectiveZoom();
        var clickX         = e.GetPosition(this).X;
        var index          = (int)Math.Round(zStart + clickX / ActualWidth * (zEnd - zStart));

        index = Math.Clamp(index, 0, Math.Max(Capacity - 1, 0));

        RaiseEvent(new PositionChangeRequestedEventArgs(PositionChangeRequestedEvent, index));
    }

    #endregion
}