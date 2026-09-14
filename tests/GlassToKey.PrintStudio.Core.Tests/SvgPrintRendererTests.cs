using GlassToKey.PrintStudio.Core.Import;
using GlassToKey.PrintStudio.Rendering;
using Xunit;

namespace GlassToKey.PrintStudio.Core.Tests;

public sealed class SvgPrintRendererTests
{
    [Fact]
    public void RendersFixtureLayoutAsPhysicalSvg()
    {
        var export = GlassToKeyExportParser.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GlassToKey-mac-settings.json")));
        var svg = SvgPrintRenderer.Render(export, new SvgPrintOptions(BackgroundImagePath: "background.png"));

        Assert.Contains("width=\"210mm\"", svg);
        Assert.Contains("height=\"297mm\"", svg);
        Assert.Contains("background.png", svg);
        Assert.Contains("Space", svg);
    }

    [Fact]
    public void RendersPngAndPdfFiles()
    {
        var export = GlassToKeyExportParser.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GlassToKey-mac-settings.json")));
        var directory = Path.Combine(Path.GetTempPath(), "glass-to-key-tests");
        Directory.CreateDirectory(directory);
        var png = Path.Combine(directory, "layout.png");
        var pdf = Path.Combine(directory, "layout.pdf");
        var options = new SvgPrintOptions();

        RasterPrintRenderer.RenderPng(export, options, png, 72);
        RasterPrintRenderer.RenderPdf(export, options, pdf);

        Assert.True(new FileInfo(png).Length > 100);
        Assert.True(new FileInfo(pdf).Length > 100);
        Assert.Equal("89504E47", Convert.ToHexString(File.ReadAllBytes(png)[..4]));
        Assert.StartsWith("%PDF", File.ReadAllText(pdf)[..4]);
    }

    [Fact]
    public void EmbedsExistingBackgroundImageInSvg()
    {
        var export = GlassToKeyExportParser.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GlassToKey-mac-settings.json")));
        var image = Path.Combine(Path.GetTempPath(), "glass-to-key-background.png");
        File.WriteAllBytes(image, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));

        var svg = SvgPrintRenderer.Render(export, new SvgPrintOptions(BackgroundImagePath: image));

        Assert.Contains("data:image/png;base64,", svg);
    }

    [Fact]
    public void RendersCustomPageAndCalibrationMarks()
    {
        var export = GlassToKeyExportParser.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GlassToKey-mac-settings.json")));
        var svg = SvgPrintRenderer.Render(export, new SvgPrintOptions(PageWidthMm: 180, PageHeightMm: 240, ShowCalibration: true));

        Assert.Contains("width=\"180mm\"", svg);
        Assert.Contains("height=\"240mm\"", svg);
        Assert.Contains("id=\"calibration\"", svg);
        Assert.Contains("10 mm", svg);
        Assert.Contains("50 mm", svg);
        Assert.Contains("100%", svg);
    }

    [Fact]
    public void WarnsWhenRasterResolutionIsBelowRequestedDpi()
    {
        var image = Path.Combine(Path.GetTempPath(), "glass-to-key-low-resolution.png");
        File.WriteAllBytes(image, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));

        Assert.NotNull(RasterPrintRenderer.GetResolutionWarning(image, 160, 300));
        Assert.Null(RasterPrintRenderer.GetResolutionWarning(image, 0.01, 1));
    }
}
