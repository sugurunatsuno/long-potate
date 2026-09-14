using System.Globalization;
using System.Text;
using System.Xml.Linq;
using GlassToKey.PrintStudio.Core.Geometry;
using GlassToKey.PrintStudio.Core.Import;

namespace GlassToKey.PrintStudio.Rendering;

public sealed record SvgPrintOptions(
    string LayoutName = "6x4",
    string? BackgroundImagePath = null,
    double PageWidthMm = 210,
    double PageHeightMm = 297,
    double TrackpadXmm = 25,
    double TrackpadYmm = 25,
    string KeyFill = "#f7f7f7",
    string KeyBorder = "#333333",
    string TextColor = "#000000",
    double KeyBorderWidthMm = 0.35,
    string PageBackground = "#ffffff",
    double KeyCornerRadiusMm = 1,
    double KeyTextSizeMm = 3.5,
    string KeyFontFamily = "Arial",
    double BackgroundXmm = 0,
    double BackgroundYmm = 0,
    double BackgroundScale = 1,
    double BackgroundRotationDegrees = 0,
    bool ShowBackgroundInKeymap = true,
    double KeyFillOpacity = 0.85,
    bool ShowCalibration = false,
    int Layer = 0,
    double BackgroundOpacity = 1)
{
    public string? LeftBackgroundImagePath { get; init; }
    public string? RightBackgroundImagePath { get; init; }
    public bool ShowOtherLayerLabels { get; init; }
}

public static class SvgPrintRenderer
{
    public static string Render(GlassToKeyExport export, SvgPrintOptions options)
    {
        if (!export.Layouts.TryGetValue(options.LayoutName, out var layout))
        {
            throw new ArgumentException($"Unknown layout '{options.LayoutName}'.", nameof(options));
        }

        XNamespace svgNs = "http://www.w3.org/2000/svg";
        var svg = new XElement(svgNs + "svg",
            new XAttribute("width", Mm(options.PageWidthMm) + "mm"),
            new XAttribute("height", Mm(options.PageHeightMm) + "mm"),
            new XAttribute("viewBox", $"0 0 {Mm(options.PageWidthMm)} {Mm(options.PageHeightMm)}"));
        svg.Add(new XElement(svgNs + "rect", new XAttribute("width", Mm(options.PageWidthMm)),
            new XAttribute("height", Mm(options.PageHeightMm)), new XAttribute("fill", options.PageBackground)));

        var trackpad = new XElement(svgNs + "g", new XAttribute("id", "trackpad"),
            new XElement(svgNs + "rect",
                new XAttribute("x", Mm(options.TrackpadXmm)),
                new XAttribute("y", Mm(options.TrackpadYmm)),
                new XAttribute("width", Mm(DeviceProfile.GlassToKeyMagicTrackpad.WidthMm)),
                new XAttribute("height", Mm(DeviceProfile.GlassToKeyMagicTrackpad.HeightMm)),
                new XAttribute("fill", "white"), new XAttribute("stroke", "black")));

        if (options.ShowBackgroundInKeymap && options.BackgroundImagePath is { Length: > 0 } path)
        {
            var href = File.Exists(path)
                ? $"data:{ContentType(path)};base64,{Convert.ToBase64String(File.ReadAllBytes(path))}"
                : new Uri(Path.GetFullPath(path)).AbsoluteUri;
            var x = options.TrackpadXmm + options.BackgroundXmm;
            var y = options.TrackpadYmm + options.BackgroundYmm;
            var width = DeviceProfile.GlassToKeyMagicTrackpad.WidthMm * options.BackgroundScale;
            var height = DeviceProfile.GlassToKeyMagicTrackpad.HeightMm * options.BackgroundScale;
            trackpad.Add(new XElement(svgNs + "image", new XAttribute("href", href),
                new XAttribute("x", Mm(x)), new XAttribute("y", Mm(y)),
                new XAttribute("width", Mm(width)), new XAttribute("height", Mm(height)), new XAttribute("opacity", Mm(options.BackgroundOpacity)),
                new XAttribute("transform", $"rotate({Mm(options.BackgroundRotationDegrees)} {Mm(x + width / 2)} {Mm(y + height / 2)})")));
        }

        foreach (var key in LayoutBuilder.Build(export, options.LayoutName, layer: options.Layer))
        {
            var x = options.TrackpadXmm + key.Rect.X * DeviceProfile.GlassToKeyMagicTrackpad.WidthMm;
            var y = options.TrackpadYmm + key.Rect.Y * DeviceProfile.GlassToKeyMagicTrackpad.HeightMm;
            var width = key.Rect.Width * DeviceProfile.GlassToKeyMagicTrackpad.WidthMm;
            var height = key.Rect.Height * DeviceProfile.GlassToKeyMagicTrackpad.HeightMm;
            var transform = $"rotate({key.Rect.RotationDegrees.ToString("0.####", CultureInfo.InvariantCulture)} {Mm(x + width / 2)} {Mm(y + height / 2)})";
            trackpad.Add(new XElement(svgNs + "rect", new XAttribute("x", Mm(x)), new XAttribute("y", Mm(y)),
                new XAttribute("width", Mm(width)), new XAttribute("height", Mm(height)),
                new XAttribute("rx", Mm(options.KeyCornerRadiusMm)), new XAttribute("fill", options.KeyFill), new XAttribute("fill-opacity", options.KeyFillOpacity), new XAttribute("stroke", options.KeyBorder),
                new XAttribute("stroke-width", Mm(options.KeyBorderWidthMm)), new XAttribute("transform", transform)));
            if (key.Label is { Length: > 0 } label)
                trackpad.Add(new XElement(svgNs + "text", new XAttribute("x", Mm(x + width / 2)), new XAttribute("y", Mm(y + height / 2)),
                    new XAttribute("text-anchor", "middle"), new XAttribute("dominant-baseline", "middle"),
                    new XAttribute("fill", options.TextColor), new XAttribute("font-family", options.KeyFontFamily),
                    new XAttribute("font-size", Mm(options.KeyTextSizeMm)), new XAttribute("transform", transform), label));
            if (key.HoldLabel is { Length: > 0 } hold)
                trackpad.Add(new XElement(svgNs + "text", new XAttribute("x", Mm(x + width / 2)), new XAttribute("y", Mm(y + height - 1)),
                    new XAttribute("text-anchor", "middle"), new XAttribute("fill", options.TextColor), new XAttribute("font-family", options.KeyFontFamily),
                    new XAttribute("font-size", Mm(options.KeyTextSizeMm * 0.7)), new XAttribute("transform", transform), hold));
        }

        if (layout.CustomButtons.TryGetValue("0", out var buttons))
        {
            foreach (var button in buttons.Where(button => button.Layer is null || button.Layer == options.Layer))
            {
                if (button.Rect is not { } rect) continue;
                var x = options.TrackpadXmm + rect.X * DeviceProfile.GlassToKeyMagicTrackpad.WidthMm;
                var y = options.TrackpadYmm + (1 - rect.Y - rect.Height) * DeviceProfile.GlassToKeyMagicTrackpad.HeightMm;
                var width = rect.Width * DeviceProfile.GlassToKeyMagicTrackpad.WidthMm;
                var height = rect.Height * DeviceProfile.GlassToKeyMagicTrackpad.HeightMm;
                trackpad.Add(new XElement(svgNs + "rect", new XAttribute("x", Mm(x)), new XAttribute("y", Mm(y)),
                    new XAttribute("width", Mm(width)), new XAttribute("height", Mm(height)),
                    new XAttribute("fill", options.KeyFill), new XAttribute("stroke", options.KeyBorder),
                    new XAttribute("stroke-width", Mm(options.KeyBorderWidthMm))));
                if (button.Primary?.Label is { Length: > 0 } label)
                    trackpad.Add(new XElement(svgNs + "text", new XAttribute("x", Mm(x + width / 2)),
                        new XAttribute("y", Mm(y + height / 2)), new XAttribute("text-anchor", "middle"),
                        new XAttribute("dominant-baseline", "middle"), new XAttribute("fill", options.TextColor), label));
            }
        }

        svg.Add(trackpad);
        if (options.ShowCalibration) AddCalibration(svg, svgNs, options);
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), svg).ToString(SaveOptions.DisableFormatting);
    }

    private static void AddCalibration(XElement svg, XNamespace ns, SvgPrintOptions options)
    {
        var calibration = new XElement(ns + "g", new XAttribute("id", "calibration"),
            new XAttribute("fill", "none"), new XAttribute("stroke", "black"), new XAttribute("stroke-width", "0.25"));
        calibration.Add(new XElement(ns + "rect", new XAttribute("x", "10"), new XAttribute("y", "10"), new XAttribute("width", "10"), new XAttribute("height", "10")));
        calibration.Add(new XElement(ns + "text", new XAttribute("x", "21"), new XAttribute("y", "18"), new XAttribute("fill", "black"), new XAttribute("stroke", "none"), "10 mm"));
        calibration.Add(new XElement(ns + "line", new XAttribute("x1", "10"), new XAttribute("y1", "25"), new XAttribute("x2", "110"), new XAttribute("y2", "25")));
        calibration.Add(new XElement(ns + "line", new XAttribute("x1", "10"), new XAttribute("y1", "45"), new XAttribute("x2", "60"), new XAttribute("y2", "45")));
        calibration.Add(new XElement(ns + "line", new XAttribute("x1", "90"), new XAttribute("y1", "5"), new XAttribute("x2", "90"), new XAttribute("y2", "15")));
        calibration.Add(new XElement(ns + "line", new XAttribute("x1", "85"), new XAttribute("y1", "10"), new XAttribute("x2", "95"), new XAttribute("y2", "10")));
        calibration.Add(new XElement(ns + "text", new XAttribute("x", "10"), new XAttribute("y", "31"), new XAttribute("fill", "black"), new XAttribute("stroke", "none"), "100 mm"));
        calibration.Add(new XElement(ns + "text", new XAttribute("x", "10"), new XAttribute("y", "51"), new XAttribute("fill", "black"), new XAttribute("stroke", "none"), "50 mm"));
        calibration.Add(new XElement(ns + "text", new XAttribute("x", "10"), new XAttribute("y", "38"), new XAttribute("fill", "black"), new XAttribute("stroke", "none"), "印刷倍率: 100%（用紙に合わせない）"));
        svg.Add(calibration);
    }

    private static string Mm(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        _ => "image/png",
    };
}
