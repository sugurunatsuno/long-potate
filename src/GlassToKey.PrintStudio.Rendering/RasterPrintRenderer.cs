using GlassToKey.PrintStudio.Core.Geometry;
using GlassToKey.PrintStudio.Core.Import;
using SkiaSharp;

namespace GlassToKey.PrintStudio.Rendering;

public static class RasterPrintRenderer
{
    public static void RenderPdfPages(GlassToKeyExport export, SvgPrintOptions options, string path)
    {
        using var document = SKDocument.CreatePdf(path);
        var layers = export.Layouts[options.LayoutName].Mappings.Keys
            .Select(key => int.TryParse(key, out var value) ? value : -1)
            .Where(value => value >= 0).Distinct().Order().DefaultIfEmpty(0);
        foreach (var layer in layers)
        {
            using var canvas = document.BeginPage((float)(options.PageWidthMm / 25.4 * 72), (float)(options.PageHeightMm / 25.4 * 72));
            Draw(export, options with { Layer = layer }, canvas, 72 / 25.4f);
            document.EndPage();
        }
        document.Close();
    }

    public static string? GetResolutionWarning(string? path, double physicalWidthMm, int dpi)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        using var bitmap = SKBitmap.Decode(path);
        if (bitmap is null || physicalWidthMm <= 0 || dpi <= 0) return null;
        var actualDpi = bitmap.Width / (physicalWidthMm / 25.4);
        return actualDpi < dpi ? $"画像の解像度が不足しています（実解像度 約{actualDpi:0} DPI / 出力 {dpi} DPI）" : null;
    }

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
        Draw(export, options with { BackgroundImagePath = options.RightBackgroundImagePath ?? options.BackgroundImagePath }, canvas, 72 / 25.4f, 1, true);
        Draw(export, options with
        {
            TrackpadYmm = options.TrackpadYmm + DeviceProfile.GlassToKeyMagicTrackpad.HeightMm + 10,
            BackgroundImagePath = options.LeftBackgroundImagePath ?? options.BackgroundImagePath
        }, canvas, 72 / 25.4f, 0, false);
        document.EndPage();
        document.Close();
    }

    private static void Draw(GlassToKeyExport export, SvgPrintOptions options, SKCanvas canvas, float scale, int side = -1, bool clear = true)
    {
        if (clear) canvas.Clear(SKColor.Parse(options.PageBackground));
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
            var height = width * bitmap.Height / (float)bitmap.Width;
            canvas.Save();
            canvas.ClipRect(trackpad);
            canvas.RotateDegrees((float)options.BackgroundRotationDegrees, x + width / 2, y + height / 2);
            using var imagePaint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(Math.Clamp(options.BackgroundOpacity, 0, 1) * 255)) };
            canvas.DrawBitmap(bitmap, new SKRect(x, y, x + width, y + height), new SKSamplingOptions(), imagePaint);
            canvas.Restore();
        }

        foreach (var key in LayoutBuilder.Build(export, options.LayoutName, side, options.Layer))
            DrawKey(canvas, key.Rect, key.Label, key.HoldLabel, key.OtherLayerLabels, options, scale, device);
        if (export.Layouts[options.LayoutName].CustomButtons.TryGetValue("0", out var buttons))
            foreach (var button in buttons.Where(button => button.Side is null || button.Side == side || side < 0))
                if ((button.Layer is null || button.Layer == options.Layer) && button.Rect is { } rect) DrawKey(canvas, new NormalizedRect(rect.X, rect.Y, rect.Width, rect.Height), button.Primary?.Label, button.Hold?.Label, null, options, scale, device);
        if (options.ShowCalibration) DrawCalibration(canvas, scale);
    }

    private static void DrawCalibration(SKCanvas canvas, float scale)
    {
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 0.25f * scale };
        canvas.DrawRect(10 * scale, 10 * scale, 10 * scale, 10 * scale, paint);
        canvas.DrawLine(10 * scale, 25 * scale, 110 * scale, 25 * scale, paint);
        canvas.DrawLine(10 * scale, 45 * scale, 60 * scale, 45 * scale, paint);
        canvas.DrawLine(90 * scale, 5 * scale, 90 * scale, 15 * scale, paint);
        canvas.DrawLine(85 * scale, 10 * scale, 95 * scale, 10 * scale, paint);
        using var text = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var font = new SKFont(SKTypeface.Default, 4 * scale);
        canvas.DrawText("10 mm", 21 * scale, 18 * scale, SKTextAlign.Left, font, text);
        canvas.DrawText("100 mm", 10 * scale, 31 * scale, SKTextAlign.Left, font, text);
        canvas.DrawText("50 mm", 10 * scale, 51 * scale, SKTextAlign.Left, font, text);
        canvas.DrawText("印刷倍率: 100%（用紙に合わせない）", 10 * scale, 38 * scale, SKTextAlign.Left, font, text);
    }

    private static void DrawKey(SKCanvas canvas, NormalizedRect rect, string? label, string? holdLabel, IReadOnlyDictionary<int, string>? otherLayerLabels, SvgPrintOptions options, float scale, DeviceProfile device)
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
        if (holdLabel is { Length: > 0 }) canvas.DrawText(holdLabel, x + width / 2, y + height - 2 * scale, SKTextAlign.Center, new SKFont(SKTypeface.Default, (float)(options.KeyTextSizeMm * scale * 0.7)), text);
        if (options.ShowOtherLayerLabels && otherLayerLabels is not null)
            foreach (var item in otherLayerLabels)
                canvas.DrawText(item.Value, item.Key switch { 1 => x + 2 * scale, 2 => x + width - 2 * scale, 3 => x + width - 2 * scale, _ => x + 2 * scale }, item.Key == 3 ? y + height - 2 * scale : y + 8 * scale, item.Key is 2 or 3 ? SKTextAlign.Right : SKTextAlign.Left, new SKFont(SKTypeface.Default, (float)(options.KeyTextSizeMm * scale * 0.55)), text);
        canvas.Restore();
    }
}
