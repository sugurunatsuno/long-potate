using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Input;
using System.IO;
using GlassToKey.PrintStudio.Core.Geometry;
using GlassToKey.PrintStudio.Core.Import;

namespace GlassToKey.PrintStudio.App;

public sealed class PreviewCanvas : Control, IDisposable
{
    private IReadOnlyList<LayoutKey> keys = [];
    private IReadOnlyList<KeymapButton> buttons = [];
    private Bitmap? background;
    private Bitmap? leftBackground, rightBackground;
    private IBrush keyFill = Brushes.LightGray;
    private IBrush keyBorder = Brushes.DarkSlateGray;
    private IBrush textColor = Brushes.Black;
    private IBrush pageBackground = Brushes.White;
    private double cornerRadiusMm = 1;
    private double textSizeMm = 3.5;
    private Typeface typeface = Typeface.Default;
    private double backgroundXmm, backgroundYmm, backgroundScale = 1, backgroundRotation;
    private bool showBackgroundInKeymap = true;
    private bool showOtherLayerLabels;
    private bool showDebugGrid;
    private double previewZoom = 1;
    private Vector previewPan;
    private bool panning;
    private Point panStart;
    private Vector panOrigin;
    private bool dragging;
    private Point dragStart;
    private double dragX, dragY;

    public event Action<double, double, double, double>? BackgroundPlacementChanged;

    public PreviewCanvas()
    {
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
    }

    public void SetKeys(IReadOnlyList<LayoutKey> value)
    {
        keys = value;
        InvalidateVisual();
    }

    public void SetButtons(IReadOnlyList<KeymapButton> value)
    {
        buttons = value;
        InvalidateVisual();
    }

    public void SetBackground(string? path)
    {
        background?.Dispose();
        background = null;
        if (path is not null)
        {
            try { background = new Bitmap(path); } catch (Exception) { }
        }
        InvalidateVisual();
    }

    public void SetSideBackground(string? leftPath, string? rightPath)
    {
        leftBackground?.Dispose(); rightBackground?.Dispose(); leftBackground = rightBackground = null;
        try { if (leftPath is not null) leftBackground = new Bitmap(leftPath); } catch (Exception) { }
        try { if (rightPath is not null) rightBackground = new Bitmap(rightPath); } catch (Exception) { }
        InvalidateVisual();
    }

    public void SetBackground(Stream stream)
    {
        background?.Dispose();
        background = new Bitmap(stream);
        InvalidateVisual();
    }

    public void SetBackgroundPlacement(double xMm, double yMm, double scale, double rotation)
    {
        backgroundXmm = xMm;
        backgroundYmm = yMm;
        backgroundScale = scale;
        backgroundRotation = rotation;
        InvalidateVisual();
    }

    public void SetBackgroundVisibility(bool visible)
    {
        showBackgroundInKeymap = visible;
        InvalidateVisual();
    }
    public void SetOtherLayerLabelsVisibility(bool visible) { showOtherLayerLabels = visible; InvalidateVisual(); }

    public void SetCompatibilityMode(bool compatibility) => SetBackgroundVisibility(!compatibility);
    public void SetDebugGrid(bool visible) { showDebugGrid = visible; InvalidateVisual(); }
    public void FitTrackpad() { previewZoom = 1; previewPan = default; InvalidateVisual(); }
    public void Set100Percent() { previewZoom = 1; InvalidateVisual(); }
    public void FitBackground()
    {
        backgroundXmm = backgroundYmm = backgroundRotation = 0;
        backgroundScale = 1;
        NotifyPlacementChanged();
    }

    public void SetStyle(string fill, string border, string text, double cornerRadiusMm, double textSizeMm, string fontFamily, double fillOpacity)
    {
        try
        {
            keyFill = new SolidColorBrush(Color.Parse(fill)) { Opacity = fillOpacity };
            keyBorder = new SolidColorBrush(Color.Parse(border));
            textColor = new SolidColorBrush(Color.Parse(text));
            this.cornerRadiusMm = cornerRadiusMm;
            this.textSizeMm = textSizeMm;
            typeface = new Typeface(fontFamily);
            InvalidateVisual();
        }
        catch (FormatException) { }
    }

    public void SetPageBackground(string color)
    {
        try { pageBackground = new SolidColorBrush(Color.Parse(color)); InvalidateVisual(); }
        catch (FormatException) { }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var page = new Rect(Bounds.Size);
        context.DrawRectangle(pageBackground, new Pen(Brushes.Black), page);
        if (showDebugGrid)
        {
            var gridBrush = new SolidColorBrush(Colors.Gray) { Opacity = 0.35 };
            var gridPen = new Pen(gridBrush, 1);
            for (var x = 0; x <= 30; x++) context.DrawLine(gridPen, new Point(Bounds.Width * x / 30, 0), new Point(Bounds.Width * x / 30, Bounds.Height));
            for (var y = 0; y <= 22; y++) context.DrawLine(gridPen, new Point(0, Bounds.Height * y / 22), new Point(Bounds.Width, Bounds.Height * y / 22));
        }
        var scale = Math.Min((Bounds.Width - 32) / (160 * 2 + 10), (Bounds.Height - 32) / 114.9) * previewZoom;
        if (scale <= 0) return;
        var totalWidth = (160 * 2 + 10) * scale;
        var origin = new Point((Bounds.Width - totalWidth) / 2 + previewPan.X, (Bounds.Height - 114.9 * scale) / 2 + previewPan.Y);
        DrawTrackpad(context, origin, scale, keys.Where(key => key.MappingId.StartsWith("left:", StringComparison.Ordinal)), buttons.Where(button => button.Side == 0), leftBackground ?? background);
        DrawTrackpad(context, new Point(origin.X + (160 + 10) * scale, origin.Y),
            scale, keys.Where(key => key.MappingId.StartsWith("right:", StringComparison.Ordinal)), buttons.Where(button => button.Side == 1), rightBackground ?? background);
    }

    private void DrawTrackpad(DrawingContext context, Point origin, double scale, IEnumerable<LayoutKey> deviceKeys, IEnumerable<KeymapButton> deviceButtons, Bitmap? image)
    {
        var trackpad = new Rect(origin.X, origin.Y, 160 * scale, 114.9 * scale);
        context.DrawRectangle(Brushes.WhiteSmoke, new Pen(Brushes.Gray), trackpad);
        if (showBackgroundInKeymap && image is not null)
        {
            var width = trackpad.Width * backgroundScale;
            var height = width * image.PixelSize.Height / (double)image.PixelSize.Width;
            var imageRect = new Rect(trackpad.X + backgroundXmm * scale, trackpad.Y + backgroundYmm * scale, width, height);
            using (context.PushClip(trackpad))
            using (context.PushTransform(Matrix.CreateRotation(backgroundRotation * Math.PI / 180, imageRect.Center)))
                context.DrawImage(image, new Rect(0, 0, image.PixelSize.Width, image.PixelSize.Height), imageRect);
        }
        foreach (var key in deviceKeys)
        {
            var rect = new Rect(origin.X + key.Rect.X * trackpad.Width, origin.Y + key.Rect.Y * trackpad.Height,
                key.Rect.Width * trackpad.Width, key.Rect.Height * trackpad.Height);
            context.DrawRectangle(keyFill, new Pen(keyBorder), rect, cornerRadiusMm * trackpad.Width / 160, cornerRadiusMm * trackpad.Width / 160);
            if (key.Label is { Length: > 0 })
                context.DrawText(new FormattedText(key.Label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, Math.Max(8, textSizeMm * trackpad.Width / 160), textColor), rect.Position + new Vector(4, 4));
            if (key.HoldLabel is { Length: > 0 } hold)
                context.DrawText(new FormattedText(hold, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, Math.Max(7, textSizeMm * trackpad.Width / 160 * 0.7), textColor), rect.BottomLeft - new Vector(-4, 4));
            if (showOtherLayerLabels && key.OtherLayerLabels is not null)
                foreach (var layer in key.OtherLayerLabels)
                {
                    var position = layer.Key switch { 1 => rect.TopLeft + new Vector(3, 10), 2 => rect.TopRight + new Vector(-3, 10), 3 => rect.BottomRight + new Vector(-3, -3), _ => rect.TopLeft + new Vector(3, 10) };
                    context.DrawText(new FormattedText(layer.Value, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        typeface, Math.Max(6, textSizeMm * trackpad.Width / 160 * 0.55), textColor), position);
                }
        }
        foreach (var button in deviceButtons)
        {
            if (button.Rect is not { } source) continue;
            var rect = new Rect(origin.X + source.X * trackpad.Width, origin.Y + source.Y * trackpad.Height,
                source.Width * trackpad.Width, source.Height * trackpad.Height);
            context.DrawRectangle(keyFill, new Pen(keyBorder, 2), rect, cornerRadiusMm * trackpad.Width / 160, cornerRadiusMm * trackpad.Width / 160);
            if (button.Primary?.Label is { Length: > 0 } label)
                context.DrawText(new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, Math.Max(8, textSizeMm * trackpad.Width / 160), textColor), rect.Position + new Vector(4, 4));
            if (button.Hold?.Label is { Length: > 0 } hold)
                context.DrawText(new FormattedText(hold, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, Math.Max(7, textSizeMm * trackpad.Width / 160 * 0.7), textColor), rect.BottomLeft - new Vector(-4, 4));
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonPressed)
        {
            panning = true;
            panStart = e.GetPosition(this);
            panOrigin = previewPan;
            e.Pointer.Capture(this);
            return;
        }
        if (background is null || !showBackgroundInKeymap) return;
        if (!IsOnTrackpad(e.GetPosition(this))) return;
        if (e.ClickCount == 2)
        {
            backgroundXmm = backgroundYmm = backgroundRotation = 0;
            backgroundScale = 1;
            NotifyPlacementChanged();
            return;
        }
        dragging = true;
        dragStart = e.GetPosition(this);
        dragX = backgroundXmm;
        dragY = backgroundYmm;
        e.Pointer.Capture(this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (panning)
        {
            previewPan = panOrigin + e.GetPosition(this) - panStart;
            InvalidateVisual();
            return;
        }
        if (!dragging) return;
        var scale = PreviewScale();
        var delta = e.GetPosition(this) - dragStart;
        backgroundXmm = dragX + delta.X / scale;
        backgroundYmm = dragY + delta.Y / scale;
        NotifyPlacementChanged();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (panning)
        {
            panning = false;
            e.Pointer.Capture(null);
            return;
        }
        if (!dragging) return;
        dragging = false;
        e.Pointer.Capture(null);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            previewZoom = Math.Clamp(previewZoom * (e.Delta.Y > 0 ? 1.1 : 0.9), 0.25, 4);
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (background is null || !IsOnTrackpad(e.GetPosition(this))) return;
        if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            backgroundRotation = Math.Clamp(backgroundRotation + e.Delta.Y * 5, -360, 360);
        else
            backgroundScale = Math.Clamp(backgroundScale * (e.Delta.Y > 0 ? 1.05 : 0.95), 0.01, 10);
        NotifyPlacementChanged();
        e.Handled = true;
    }

    private bool IsOnTrackpad(Point point)
    {
        var scale = PreviewScale();
        var origin = PreviewOrigin(scale);
        return new Rect(origin.X, origin.Y, 160 * scale, 114.9 * scale).Contains(point)
            || new Rect(origin.X + (160 + 10) * scale, origin.Y, 160 * scale, 114.9 * scale).Contains(point);
    }

    private double PreviewScale() => Math.Min((Bounds.Width - 32) / (160 * 2 + 10), (Bounds.Height - 32) / 114.9) * previewZoom;

    private Point PreviewOrigin(double scale) => new((Bounds.Width - (160 * 2 + 10) * scale) / 2, (Bounds.Height - 114.9 * scale) / 2);

    private void NotifyPlacementChanged() => BackgroundPlacementChanged?.Invoke(backgroundXmm, backgroundYmm, backgroundScale, backgroundRotation);

    public void Dispose() { background?.Dispose(); leftBackground?.Dispose(); rightBackground?.Dispose(); }
}
