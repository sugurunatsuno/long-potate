using GlassToKey.PrintStudio.Core.Import;
using GlassToKey.PrintStudio.Rendering;

if (args.Length < 3)
{
    Console.Error.WriteLine("使い方: gtprint <入力.json|入力フォルダ> <出力ファイル|出力フォルダ> <svg|pdf|pdf-pages|png> [レイアウト] [レイヤー]");
    return 2;
}

var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var format = args[2].ToLowerInvariant();
var layout = args.Length > 3 ? args[3] : "6x4";
var layerArgument = args.Length > 4 ? args[4] : "0";
var allLayers = layerArgument.Equals("all", StringComparison.OrdinalIgnoreCase);
var layer = int.TryParse(layerArgument, out var parsedLayer) ? parsedLayer : 0;
if (Directory.Exists(input))
{
    Directory.CreateDirectory(output);
    foreach (var file in Directory.EnumerateFiles(input, "*.json"))
    {
        var name = Path.GetFileNameWithoutExtension(file);
        if (allLayers)
        {
            var export = GlassToKeyExportParser.Parse(File.ReadAllText(file));
            var layers = export.Layouts.Values.FirstOrDefault()?.Mappings.Keys.Select(key => int.TryParse(key, out var value) ? value : -1).Where(value => value >= 0).Distinct().Order().ToArray() ?? new[] { 0 };
            foreach (var currentLayer in layers) Write(file, Path.Combine(output, $"{name}.layer-{currentLayer}.{format}"), format, layout, currentLayer);
        }
        else Write(file, Path.Combine(output, $"{name}.{format}"), format, layout, layer);
    }
    return 0;
}

Write(input, output, format, layout, layer);
return 0;

static void Write(string input, string output, string format, string layout, int layer)
{
    var export = GlassToKeyExportParser.Parse(File.ReadAllText(input));
    var options = new SvgPrintOptions(LayoutName: layout, Layer: layer);

    switch (format)
    {
        case "svg": File.WriteAllText(output, SvgPrintRenderer.Render(export, options)); break;
        case "pdf": RasterPrintRenderer.RenderPdf(export, options, output); break;
        case "pdf-pages": RasterPrintRenderer.RenderPdfPages(export, options, output); break;
        case "png": RasterPrintRenderer.RenderPng(export, options, output); break;
        default: throw new ArgumentException($"未対応の形式: {format}");
    }
    Console.WriteLine(output);
}
