using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AudioCat.Controls;
using AudioCat.ViewModels;

namespace AudioCat.Windows;

/// <summary>
/// Interaction logic for CreateChaptersWindow.xaml
/// </summary>
public partial class CreateChaptersWindow : Window
{
    public CreateChaptersWindow(CreateChaptersViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Close += (_, _) => Close();
        viewModel.UseCreated += (_, _) => { DialogResult = true; Close(); };
        DataContext = viewModel;
        Owner = Application.Current.MainWindow;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is CreateChaptersViewModel { IsUserInputEnabled: false } viewModel) 
            viewModel.CancelScanForSilence.Execute(null);
    }

    private void OnSeekRequested(object? sender, PositionChangeRequestedEventArgs e)
    {
        if (DataContext is CreateChaptersViewModel viewModel)
            viewModel.HandleStripPositionRequest(e.RequestedIndex);
    }

    private void OnChapterRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not CreateChaptersViewModel viewModel)
            return;
        // Double-clicks on headers, scroll bars or empty space have no ancestor row and are ignored
        if (FindAncestorRow(e.OriginalSource as DependencyObject)?.Item is IMediaChapterViewModel chapter)
            viewModel.PlayFromChapter(chapter);
    }

    private static DataGridRow? FindAncestorRow(DependencyObject? source)
    {
        while (source is not null && source is not DataGridRow)
        {
            // Text content can surface ContentElements (e.g. Run) that live in the logical tree only
            source = source is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }

        return source as DataGridRow;
    }

    #region Waveform zoom & pan (view-only interaction state)

    private const double ZoomFactorPerNotch = 1.25;
    private const int MinZoomWindowSize = 10; // 10 indices = 1 second

    private bool _isPanning;
    private MouseButton _panButton;
    private double _panStartX;
    private int _panStartWindowStart;
    private int _panWindowSize;

    /// <summary>
    /// The data index window currently shown by the strip. Mirrors the control's
    /// zoom semantics: the sentinel {0,0} means the full range {0, Capacity}.
    /// </summary>
    private (int Start, int End) GetEffectiveWindow()
    {
        var capacity = WaveformStrip.Capacity;
        var zoom = WaveformStrip.ZoomRange;
        if (zoom.IsSentinel)
            return (0, capacity);

        var start = Math.Clamp(zoom.Start, 0, capacity);
        var end = Math.Clamp(zoom.End, 0, capacity);
        return end <= start ? (start, start + 1) : (start, end);
    }

    private void OnStripMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;

        var capacity = WaveformStrip.Capacity;
        var width = WaveformStrip.ActualWidth;
        if (capacity <= 0 || width <= 0 || e.Delta == 0)
            return;

        var (start, end) = GetEffectiveWindow();
        var windowSize = (double)(end - start);

        // Data index under the cursor and its relative position within the window
        var relative = e.GetPosition(WaveformStrip).X / width;
        var cursorIndex = start + relative * windowSize;

        // Wheel up (positive delta) zooms in: window / 1.25 per notch; wheel down zooms out: window * 1.25
        var scale = Math.Pow(ZoomFactorPerNotch, e.Delta / 120.0);
        var newSize = Math.Max(windowSize / scale, MinZoomWindowSize);

        if (newSize >= capacity)
        {
            // Zoomed out to the full range — exactly {0, Capacity} re-enables auto-follow
            WaveformStrip.ZoomRange = new ZoomRange(0, capacity);
            return;
        }

        var sizeIndices = (int)Math.Round(newSize);

        // Keep the cursor index at the same relative position within the new window
        var newStart = (int)Math.Round(cursorIndex - relative * newSize);
        newStart = Math.Clamp(newStart, 0, capacity - sizeIndices);

        WaveformStrip.ZoomRange = new ZoomRange(newStart, newStart + sizeIndices);
    }

    private void OnStripMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton is not (MouseButton.Right or MouseButton.Middle) || _isPanning)
            return;

        var capacity = WaveformStrip.Capacity;
        if (capacity <= 0 || WaveformStrip.ActualWidth <= 0)
            return;

        var (start, end) = GetEffectiveWindow();
        if (start == 0 && end == capacity)
            return; // Nothing to pan when the full range is visible

        _isPanning = true;
        _panButton = e.ChangedButton;
        _panStartX = e.GetPosition(WaveformStrip).X;
        _panStartWindowStart = start;
        _panWindowSize = end - start;
        WaveformStrip.CaptureMouse();
        e.Handled = true;
    }

    private void OnStripMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning)
            return;

        var width = WaveformStrip.ActualWidth;
        if (width <= 0)
            return;

        // Natural drag direction: dragging right moves the visible window left
        var deltaX = e.GetPosition(WaveformStrip).X - _panStartX;
        var shift = -deltaX / width * _panWindowSize;
        var newStart = (int)Math.Round(_panStartWindowStart + shift);
        newStart = Math.Clamp(newStart, 0, Math.Max(WaveformStrip.Capacity - _panWindowSize, 0));

        WaveformStrip.ZoomRange = new ZoomRange(newStart, newStart + _panWindowSize);
    }

    private void OnStripMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning || e.ChangedButton != _panButton)
            return;

        _isPanning = false;
        WaveformStrip.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void OnStripLostMouseCapture(object sender, MouseEventArgs e) => _isPanning = false;

    private void OnStripPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
            return;

        // Restore the full timeline; handling the preview event suppresses the
        // control's own left-button click-seek so the reset does not also seek
        WaveformStrip.ZoomRange = ZoomRange.Sentinel;
        e.Handled = true;
    }

    #endregion
}