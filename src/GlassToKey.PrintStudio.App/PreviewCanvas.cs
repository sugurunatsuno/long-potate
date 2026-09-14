using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.IO;
using GlassToKey.PrintStudio.Core.Geometry;
using GlassToKey.PrintStudio.Core.Import;

namespace GlassToKey.PrintStudio.App;

public sealed class PreviewCanvas : Control, IDisposable
{
    private IReadOnlyList<LayoutKey> keys = [];
    private IReadOnlyList<KeymapButton> buttons = [];
    private Bitmap? background;
    private IBrush keyFill = Brushes.LightGray;
    private IBrush keyBorder = Brushes.DarkSlateGray;
    private IBrush textColor = Brushes.Black;
    private IBrush pageBackground = Brushes.White;
    private double cornerRadiusMm = 1;
    private double textSizeMm = 3.5;
    private Typeface typeface = Typeface.Default;
    private double backgroundXmm, backgroundYmm, backgroundScale = 1, backgroundRotation;
    private bool showBackgroundInKeymap = true;

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
        var scale = Math.Min((Bounds.Width - 32) / 160, (Bounds.Height - 32) / (114.9 * 2 + 10));
        if (scale <= 0) return;
        var totalHeight = (114.9 * 2 + 10) * scale;
        var origin = new Point((Bounds.Width - 160 * scale) / 2, (Bounds.Height - totalHeight) / 2);
        DrawTrackpad(context, origin, scale, keys.Where(key => key.MappingId.StartsWith("right:", StringComparison.Ordinal)), buttons.Where(button => button.Side == 1));
        DrawTrackpad(context, new Point(origin.X, origin.Y + (114.9 + 10) * scale),
            scale, keys.Where(key => key.MappingId.StartsWith("left:", StringComparison.Ordinal)), buttons.Where(button => button.Side == 0));
    }

    private void DrawTrackpad(DrawingContext context, Point origin, double scale, IEnumerable<LayoutKey> deviceKeys, IEnumerable<KeymapButton> deviceButtons)
    {
        var trackpad = new Rect(origin.X, origin.Y, 160 * scale, 114.9 * scale);
        context.DrawRectangle(Brushes.WhiteSmoke, new Pen(Brushes.Gray), trackpad);
        if (showBackgroundInKeymap && background is not null)
        {
            var imageRect = new Rect(trackpad.X + backgroundXmm * scale, trackpad.Y + backgroundYmm * scale,
                trackpad.Width * backgroundScale, trackpad.Height * backgroundScale);
            using (context.PushClip(trackpad))
            using (context.PushTransform(Matrix.CreateRotation(backgroundRotation * Math.PI / 180, imageRect.Center)))
                context.DrawImage(background, new Rect(0, 0, background.PixelSize.Width, background.PixelSize.Height), imageRect);
        }
        foreach (var key in deviceKeys)
        {
            var rect = new Rect(origin.X + key.Rect.X * trackpad.Width, origin.Y + key.Rect.Y * trackpad.Height,
                key.Rect.Width * trackpad.Width, key.Rect.Height * trackpad.Height);
            context.DrawRectangle(keyFill, new Pen(keyBorder), rect, cornerRadiusMm * trackpad.Width / 160, cornerRadiusMm * trackpad.Width / 160);
            if (key.Label is { Length: > 0 })
                context.DrawText(new FormattedText(key.Label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, Math.Max(8, textSizeMm * trackpad.Width / 160), textColor), rect.Position + new Vector(4, 4));
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
        }
    }

    public void Dispose() => background?.Dispose();
}
