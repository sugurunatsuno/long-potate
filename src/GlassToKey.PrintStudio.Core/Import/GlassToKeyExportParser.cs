using System.Text.Json;

namespace GlassToKey.PrintStudio.Core.Import;

public sealed record GlassToKeyExport(
    int Version,
    int KeymapVersion,
    IReadOnlyDictionary<string, KeymapLayout> Layouts,
    JsonElement Settings);

public static class GlassToKeyExportParser
{
    public static GlassToKeyExport Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var version = RequiredInt(root, "Version");
        var keymapJson = RequiredString(root, "KeymapJson");
        var settings = Required(root, "Settings").Clone();

        using var keymapDocument = JsonDocument.Parse(keymapJson);
        var keymap = keymapDocument.RootElement;
        var keymapVersion = RequiredInt(keymap, "Version");
        var layoutsElement = Required(keymap, "Layouts");
        if (layoutsElement.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("KeymapJson.Layouts must be an object.");
        }

        var layouts = layoutsElement.EnumerateObject()
            .ToDictionary(
                property => property.Name,
                property => JsonSerializer.Deserialize(property.Value, KeymapJsonContext.Default.KeymapLayout)
                    ?? throw new FormatException($"Layout '{property.Name}' is null."));

        return new(version, keymapVersion, layouts, settings);
    }

    private static JsonElement Required(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value)
            ? value
            : throw new FormatException($"Missing required property '{name}'.");

    private static int RequiredInt(JsonElement parent, string name) =>
        Required(parent, name).TryGetInt32(out var value)
            ? value
            : throw new FormatException($"Property '{name}' must be an integer.");

    private static string RequiredString(JsonElement parent, string name) =>
        Required(parent, name).ValueKind == JsonValueKind.String
            ? Required(parent, name).GetString()!
            : throw new FormatException($"Property '{name}' must be a string.");
}
