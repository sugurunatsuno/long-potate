using GlassToKey.PrintStudio.Core.Import;
using GlassToKey.PrintStudio.Core.Geometry;
using Xunit;

namespace GlassToKey.PrintStudio.Core.Tests;

public sealed class GlassToKeyExportParserTests
{
    [Fact]
    public void ParsesExportFixtureAndNestedKeymap()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GlassToKey-mac-settings.json"));
        var export = GlassToKeyExportParser.Parse(json);

        Assert.Equal(1, export.Version);
        Assert.Equal(2, export.KeymapVersion);
        Assert.Contains("6x4", export.Layouts.Keys);
        Assert.Equal(4, export.Layouts["6x4"].CustomButtons.Values.Sum(buttons => buttons.Count));
        Assert.Equal(171, export.Layouts["6x4"].Mappings.Values.Sum(mapping => mapping.Count));
    }

    [Fact]
    public void BuildsNormalizedGeometryForFixtureKeys()
    {
        var export = GlassToKeyExportParser.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GlassToKey-mac-settings.json")));
        var keys = LayoutBuilder.Build(export, "6x4");

        Assert.NotEmpty(keys);
        Assert.All(keys, key =>
        {
            Assert.InRange(key.Rect.X, -1, 2);
            Assert.InRange(key.Rect.Y, -1, 2);
            Assert.InRange(key.Rect.Width, 0, 1);
            Assert.InRange(key.Rect.Height, 0, 1);
        });
    }

    [Fact]
    public void KeepsRightPKeyInFiveByFourLayout()
    {
        var export = GlassToKeyExportParser.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GlassToKey-mac-settings.json")));
        Assert.Contains(LayoutBuilder.Build(export, "5x4"), key => key.MappingId == "right:1:4" && key.Label == "P");
    }

    [Fact]
    public void AddsMissingPKeyToSixByThreeLayout()
    {
        var export = GlassToKeyExportParser.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GlassToKey-mac-settings.json")));
        Assert.Contains(LayoutBuilder.Build(export, "6x3"), key => key.MappingId == "right:0:4" && key.Label == "P");
    }
}
