namespace GlassToKey.PrintStudio.Core.Geometry;

public sealed record DeviceProfile(
    string Id,
    string DisplayName,
    double WidthMm,
    double HeightMm,
    double BaseKeyWidthMm,
    double BaseKeyHeightMm)
{
    public static DeviceProfile GlassToKeyMagicTrackpad { get; } = new(
        Id: "glass-to-key-magic-trackpad",
        DisplayName: "Magic Trackpad (GlassToKey compatibility)",
        WidthMm: 160.0,
        HeightMm: 114.9,
        BaseKeyWidthMm: 18.0,
        BaseKeyHeightMm: 17.0);
}
