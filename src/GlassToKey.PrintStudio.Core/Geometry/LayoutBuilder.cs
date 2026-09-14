using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using GlassToKey.PrintStudio.Core.Import;

namespace GlassToKey.PrintStudio.Core.Geometry;

public sealed record LayoutKey(string MappingId, NormalizedRect Rect, string? Label, string? HoldLabel = null, IReadOnlyDictionary<int, string>? OtherLayerLabels = null);

public static class LayoutBuilder
{
    private static readonly (double X, double Y)[] ConventionalAnchors =
    [
        (35, 20.9), (53, 19.2), (71, 17.5), (89, 19.2), (107, 22.6), (125, 22.6),
    ];

    public static IReadOnlyList<LayoutKey> Build(GlassToKeyExport export, string layoutName, int side = -1, int layer = 0)
    {
        if (!export.Layouts.TryGetValue(layoutName, out var layout)) throw new ArgumentException($"Unknown layout '{layoutName}'.");
        var columns = ReadColumnSettings(export.Settings, layoutName);
        var result = new List<LayoutKey>();
        var layerName = layer.ToString(CultureInfo.InvariantCulture);
        var primaryMappings = layout.Mappings.GetValueOrDefault(layerName) ?? layout.Mappings.GetValueOrDefault("0") ?? new Dictionary<string, KeymapButton>();
        var allMappings = layout.Mappings.GetValueOrDefault(layerName) ?? layout.Mappings.GetValueOrDefault("0") ?? new Dictionary<string, KeymapButton>();
        var mappingEntries = allMappings
            .GroupBy(pair => pair.Key)
            .Select(group => (MappingId: group.Key, Button: primaryMappings.GetValueOrDefault(group.Key) ?? group.First().Value));
        if (layoutName == "6x3" && !allMappings.ContainsKey("right:0:4"))
            mappingEntries = mappingEntries.Append(("right:0:4", new KeymapButton(0, null, 1, layer, null, new KeyLabel(null), null)));
        foreach (var (mappingId, button) in mappingEntries)
        {
            var parts = mappingId.Split(':');
            if (parts.Length != 3 || (side >= 0 && parts[0] != (side == 0 ? "left" : "right")) || !int.TryParse(parts[1], out var row) || !int.TryParse(parts[2], out var column)) continue;
            if (column >= columns.Count || column >= ConventionalAnchors.Length) continue;
            var setting = columns[column];
            var width = DeviceProfile.GlassToKeyMagicTrackpad.BaseKeyWidthMm * setting.EffectiveScaleX;
            var height = DeviceProfile.GlassToKeyMagicTrackpad.BaseKeyHeightMm * setting.EffectiveScaleY;
            var (anchorX, anchorY) = ConventionalAnchors[column];
            var x = (anchorX + width * 0.0) / DeviceProfile.GlassToKeyMagicTrackpad.WidthMm + setting.OffsetXPercent / 100;
            var yMm = anchorY + row * (height + height * setting.RowSpacingPercent / 100 + height * setting.KeyPaddingPercent / 100);
            var y = yMm / DeviceProfile.GlassToKeyMagicTrackpad.HeightMm + setting.OffsetYPercent / 100;
            var rect = new NormalizedRect(x - width / DeviceProfile.GlassToKeyMagicTrackpad.WidthMm / 2,
                y, width / DeviceProfile.GlassToKeyMagicTrackpad.WidthMm, height / DeviceProfile.GlassToKeyMagicTrackpad.HeightMm,
                -Math.Clamp(setting.RotationDegrees, 0, 360));
            if (parts[0] == "left") rect = rect.MirrorHorizontally();
            var labels = layout.Mappings
                .Where(pair => int.TryParse(pair.Key, out var otherLayer) && otherLayer != layer)
                .Select(pair => (Layer: int.Parse(pair.Key, CultureInfo.InvariantCulture), Label: pair.Value.GetValueOrDefault(mappingId)?.Primary?.Label))
                .Where(item => !string.IsNullOrWhiteSpace(item.Label) && !string.Equals(item.Label, "None", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(item => item.Layer, item => item.Label!);
            if (layoutName == "6x3" && mappingId == "right:0:4" && button.Primary?.Label is null)
                result.Add(new(mappingId, rect, "P", button.Hold?.Label, labels));
            else
            result.Add(new(mappingId, rect, button.Primary?.Label, button.Hold?.Label, labels));
        }
        return result;
    }

    private static List<ColumnSetting> ReadColumnSettings(JsonElement settings, string layoutName)
    {
        if (!settings.TryGetProperty("ColumnSettingsByLayout", out var all) || !all.TryGetProperty(layoutName, out var selected))
            return [];
        var padding = settings.TryGetProperty("KeyPaddingPercentByLayout", out var paddings) && paddings.TryGetProperty(layoutName, out var p) ? p.GetDouble() : 0;
        return JsonSerializer.Deserialize(selected, LayoutJsonContext.Default.ListColumnSetting)!.Select(x => x with { KeyPaddingPercent = padding }).ToList();
    }

}

internal sealed record ColumnSetting
{
    public double OffsetXPercent { get; init; }
    public double OffsetYPercent { get; init; }
    public double RotationDegrees { get; init; }
    public double RowSpacingPercent { get; init; }
    public double Scale { get; init; }
    public double? ScaleX { get; init; }
    public double? ScaleY { get; init; }
    public double KeyPaddingPercent { get; init; }
    public double EffectiveScaleX => ScaleX ?? Scale;
    public double EffectiveScaleY => ScaleY ?? Scale;
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false, NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(List<ColumnSetting>))]
internal sealed partial class LayoutJsonContext : JsonSerializerContext;
