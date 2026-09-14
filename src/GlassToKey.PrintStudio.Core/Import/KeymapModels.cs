using System.Text.Json;
using System.Text.Json.Serialization;

namespace GlassToKey.PrintStudio.Core.Import;

public sealed record KeymapLayout(
    Dictionary<string, JsonElement> KeyGeometry,
    Dictionary<string, List<KeymapButton>> CustomButtons,
    Dictionary<string, Dictionary<string, KeymapButton>> Mappings);

public sealed record KeymapButton(
    double HoldForceThreshold,
    NormalizedButtonRect? Rect,
    int? Side,
    int? Layer,
    string? Id,
    KeyLabel? Primary,
    KeyLabel? Hold);

public sealed record KeyLabel(
    string? Label);

public sealed record NormalizedButtonRect(
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation = 0);

internal static class KeymapJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = false,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false, NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(KeymapLayout))]
internal sealed partial class KeymapJsonContext : JsonSerializerContext;
