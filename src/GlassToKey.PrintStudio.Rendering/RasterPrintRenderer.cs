using GlassToKey.PrintStudio.Core.Geometry;
using GlassToKey.PrintStudio.Core.Import;
using SkiaSharp;

namespace GlassToKey.PrintStudio.Rendering;

public static class RasterPrintRenderer
{
    public static void RenderPng(GlassToKeyExport export, SvgPrintOptions options, string path, int dpi = 300)
    {
        using var surface = SKSurface.Create(new SKImageInfo((int)(options.PageWidthMm / 25.4 * dpi), (int)(options.PageHeightMm / 25.4 * dpi)));
        Draw(export, options, surface.Canvas, dpi / 25.4f);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    public static void RenderPdf(GlassToKeyExport export, SvgPrintOptions options, string path)
    {
        using var document = SKDocument.CreatePdf(path);
        using var canvas = document.BeginPage((float)(options.PageWidthMm / 25.4 * 72), (float)(options.PageHeightMm / 25.4 * 72));
        Draw(export, options, canvas, 72 / 25.4f);
        document.EndPage();
        document.Close();
    }

    private static void Draw(GlassToKeyExport export, SvgPrintOptions options, SKCanvas canvas, float scale)
    {
        canvas.Clear(SKColor.Parse(options.PageBackground));
        using var fill = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var stroke = new SKPaint { Color = SKColor.Parse(options.KeyBorder), Style = SKPaintStyle.Stroke, StrokeWidth = (float)(options.KeyBorderWidthMm * scale) };
        var device = DeviceProfile.GlassToKeyMagicTrackpad;
        var trackpad = new SKRect((float)(options.TrackpadXmm * scale), (float)(options.TrackpadYmm * scale),
            (float)((options.TrackpadXmm + device.WidthMm) * scale), (float)((options.TrackpadYmm + device.HeightMm) * scale));
        canvas.DrawRect(trackpad, fill);
        canvas.DrawRect(trackpad, stroke);
        if (options.ShowBackgroundInKeymap && options.BackgroundImagePath is { Length: > 0 } backgroundPath)
        {
            using var bitmap = SKBitmap.Decode(backgroundPath) ?? throw new InvalidDataException($"Unable to decode background image '{backgroundPath}'.");
            var x = (float)((options.TrackpadXmm + options.BackgroundXmm) * scale);
            var y = (float)((options.TrackpadYmm + options.BackgroundYmm) * scale);
            var width = (float)(device.WidthMm * options.BackgroundScale * scale);
            var height = (float)(device.HeightMm * options.BackgroundScale * scale);
            canvas.Save();
            canvas.ClipRect(trackpad);
            canvas.RotateDegrees((float)options.BackgroundRotationDegrees, x + width / 2, y + height / 2);
            canvas.DrawBitmap(bitmap, new SKRect(x, y, x + width, y + height), new SKSamplingOptions());
            canvas.Restore();
        }

        foreach (var key in LayoutBuilder.Build(export, options.LayoutName))
            DrawKey(canvas, key.Rect, key.Label, options, scale, device);
        if (export.Layouts[options.LayoutName].CustomButtons.TryGetValue("0", out var buttons))
            foreach (var button in buttons)
                if (button.Rect is { } rect) DrawKey(canvas, new NormalizedRect(rect.X, rect.Y, rect.Width, rect.Height), button.Primary?.Label, options, scale, device);
    }

    private static void DrawKey(SKCanvas canvas, NormalizedRect rect, string? label, SvgPrintOptions options, float scale, DeviceProfile device)
    {
        var x = (float)((options.TrackpadXmm + rect.X * device.WidthMm) * scale);
        var y = (float)((options.TrackpadYmm + rect.Y * device.HeightMm) * scale);
        var width = (float)(rect.Width * device.WidthMm * scale);
        var height = (float)(rect.Height * device.HeightMm * scale);
        canvas.Save();
        canvas.RotateDegrees((float)rect.RotationDegrees, x + width / 2, y + height / 2);
        using var fill = new SKPaint { Color = SKColor.Parse(options.KeyFill).WithAlpha((byte)(Math.Clamp(options.KeyFillOpacity, 0, 1) * 255)), IsAntialias = true };
        using var stroke = new SKPaint { Color = SKColor.Parse(options.KeyBorder), Style = SKPaintStyle.Stroke, StrokeWidth = (float)(options.KeyBorderWidthMm * scale) };
        var radius = (float)(options.KeyCornerRadiusMm * scale);
        canvas.DrawRoundRect(new SKRect(x, y, x + width, y + height), radius, radius, fill);
        canvas.DrawRoundRect(new SKRect(x, y, x + width, y + height), radius, radius, stroke);
        if (label is not { Length: > 0 })
        {
            canvas.Restore();
            return;
        }
        using var text = new SKPaint { Color = SKColor.Parse(options.TextColor), IsAntialias = true };
        using var font = new SKFont(SKTypeface.FromFamilyName(options.KeyFontFamily), (float)(options.KeyTextSizeMm * scale));
        var textWidth = font.MeasureText(label, text);
        canvas.DrawText(label, x + width / 2 - textWidth / 2, y + height / 2, SKTextAlign.Left, font, text);
        canvas.Restore();
    }
}
