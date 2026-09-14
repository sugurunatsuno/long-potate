using System.Collections.ObjectModel;
using System.Text.Json;
using GlassToKey.PrintStudio.Core.Geometry;
using GlassToKey.PrintStudio.Core.Import;
using CommunityToolkit.Mvvm.ComponentModel;
#if !PRINT_STUDIO_BROWSER
using GlassToKey.PrintStudio.Rendering;
#endif

namespace GlassToKey.PrintStudio.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions ProjectJsonOptions = new() { WriteIndented = true };
    public string Title { get; } = "GlassToKey Print Studio";
    public string Status { get; private set; } = "GlassToKeyの書き出しを開いてください";
    public ObservableCollection<string> Layouts { get; } = [];
    public ObservableCollection<string> RecentFiles { get; } = [];
    private string? selectedRecentFile;
    public string? SelectedRecentFile
    {
        get => selectedRecentFile;
        set
        {
            if (!SetProperty(ref selectedRecentFile, value) || value is null || !File.Exists(value)) return;
            LoadExport(value);
            selectedRecentFile = null;
            OnPropertyChanged();
        }
    }
    public IReadOnlyList<string> ThemePresets { get; } = ["標準", "ダークキー", "白黒"];
    private string selectedTheme = "標準";
    public string SelectedTheme
    {
        get => selectedTheme;
        set
        {
            if (!SetProperty(ref selectedTheme, value)) return;
            (KeyFill, KeyBorder, TextColor) = value switch
            {
                "ダークキー" => ("#303030", "#111111", "#ffffff"),
                "白黒" => ("#ffffff", "#000000", "#000000"),
                _ => ("#f7f7f7", "#333333", "#000000"),
            };
        }
    }
    public ObservableCollection<int> Layers { get; } = [];
    private int selectedLayer;
    public int SelectedLayer
    {
        get => selectedLayer;
        set
        {
            if (!SetProperty(ref selectedLayer, value) || Export is null || SelectedLayout is null) return;
            PreviewKeys = LayoutBuilder.Build(Export, SelectedLayout, layer: value);
            OnPropertyChanged(nameof(PreviewKeys));
        }
    }
    public IReadOnlyList<string> PageSizes { get; } = ["A4", "A3", "Letter", "Trackpad", "Custom"];
    private double customPageWidthMm = 210;
    public double CustomPageWidthMm { get => customPageWidthMm; set => SetProperty(ref customPageWidthMm, value); }
    private double customPageHeightMm = 297;
    public double CustomPageHeightMm { get => customPageHeightMm; set => SetProperty(ref customPageHeightMm, value); }
    private bool showCalibration;
    public bool ShowCalibration { get => showCalibration; set => SetProperty(ref showCalibration, value); }
    public IReadOnlyList<int> PngDpiOptions { get; } = [150, 300, 600];
    private int pngDpi = 300;
    public int PngDpi { get => pngDpi; set => SetProperty(ref pngDpi, value); }
    public string SelectedPageSize { get; set; } = "A4";
    private string keyFill = "#f7f7f7";
    public string KeyFill { get => keyFill; set => SetProperty(ref keyFill, value); }
    private string keyBorder = "#333333";
    public string KeyBorder { get => keyBorder; set => SetProperty(ref keyBorder, value); }
    private string textColor = "#000000";
    public string TextColor { get => textColor; set => SetProperty(ref textColor, value); }
    private double keyBorderWidthMm = 0.35;
    public double KeyBorderWidthMm { get => keyBorderWidthMm; set => SetProperty(ref keyBorderWidthMm, value); }
    private string pageBackground = "#ffffff";
    public string PageBackground { get => pageBackground; set => SetProperty(ref pageBackground, value); }
    private double keyCornerRadiusMm = 1;
    public double KeyCornerRadiusMm { get => keyCornerRadiusMm; set => SetProperty(ref keyCornerRadiusMm, value); }
    private double keyTextSizeMm = 3.5;
    public double KeyTextSizeMm { get => keyTextSizeMm; set => SetProperty(ref keyTextSizeMm, value); }
    private string keyFontFamily = "Arial";
    public string KeyFontFamily { get => keyFontFamily; set => SetProperty(ref keyFontFamily, value); }
    private double backgroundXmm;
    public double BackgroundXmm { get => backgroundXmm; set => SetProperty(ref backgroundXmm, value); }
    private double backgroundYmm;
    public double BackgroundYmm { get => backgroundYmm; set => SetProperty(ref backgroundYmm, value); }
    private double backgroundScale = 1;
    public double BackgroundScale { get => backgroundScale; set => SetProperty(ref backgroundScale, value); }
    private double backgroundRotationDegrees;
    public double BackgroundRotationDegrees { get => backgroundRotationDegrees; set => SetProperty(ref backgroundRotationDegrees, value); }
    private double backgroundOpacity = 1;
    public double BackgroundOpacity { get => backgroundOpacity; set => SetProperty(ref backgroundOpacity, value); }
    private bool showBackgroundInKeymap = true;
    public bool ShowBackgroundInKeymap { get => showBackgroundInKeymap; set => SetProperty(ref showBackgroundInKeymap, value); }
    private double keyFillOpacity = 0.85;
    public double KeyFillOpacity { get => keyFillOpacity; set => SetProperty(ref keyFillOpacity, value); }
    private double trackpadXmm = 25;
    public double TrackpadXmm { get => trackpadXmm; set => SetProperty(ref trackpadXmm, value); }
    private double trackpadYmm = 25;
    public double TrackpadYmm { get => trackpadYmm; set => SetProperty(ref trackpadYmm, value); }
    private string? selectedLayout;
    public string? SelectedLayout
    {
        get => selectedLayout;
        set
        {
            if (!SetProperty(ref selectedLayout, value) || Export is null || value is null) return;
            PreviewKeys = LayoutBuilder.Build(Export, value);
            PreviewButtons = Export.Layouts[value].CustomButtons.TryGetValue("0", out var buttons) ? buttons : [];
            OnPropertyChanged(nameof(PreviewKeys));
            OnPropertyChanged(nameof(PreviewButtons));
            OnPropertyChanged(nameof(CanExport));
        }
    }
    public string? BackgroundImagePath { get; private set; }
    public string? LeftBackgroundImagePath { get; private set; }
    public string? RightBackgroundImagePath { get; private set; }
    private bool showOtherLayerLabels;
    public bool ShowOtherLayerLabels { get => showOtherLayerLabels; set => SetProperty(ref showOtherLayerLabels, value); }
    public IReadOnlyList<LayoutKey> PreviewKeys { get; private set; } = [];
    public IReadOnlyList<KeymapButton> PreviewButtons { get; private set; } = [];
    public bool HasExport => Export is not null;
    public bool CanExport => Export is not null && SelectedLayout is not null;
    private GlassToKeyExport? Export { get; set; }
    private string? sourceExportPath;

    public void LoadExport(string path)
    {
        LoadExportText(File.ReadAllText(path), Path.GetFileName(path), Path.GetFullPath(path));
    }

    public void ReloadSource()
    {
        if (sourceExportPath is null) throw new InvalidOperationException("元JSONファイルがありません。");
        LoadExport(sourceExportPath);
    }

    public void LoadExportJson(string json, string displayName)
    {
        LoadExportText(json, displayName, null);
    }

    private void LoadExportText(string json, string displayName, string? sourcePath)
    {
        if (sourcePath is not null)
        {
            RecentFiles.Remove(sourcePath);
            RecentFiles.Insert(0, sourcePath);
            while (RecentFiles.Count > 5) RecentFiles.RemoveAt(5);
        }
        sourceExportPath = sourcePath;
        Export = GlassToKeyExportParser.Parse(json);
        Layouts.Clear();
        foreach (var name in Export.Layouts.Keys) Layouts.Add(name);
        Layers.Clear();
        var layerKeys = Export.Layouts.Values.FirstOrDefault()?.Mappings.Keys ?? Enumerable.Empty<string>();
        foreach (var layer in layerKeys.Select(key => int.TryParse(key, out var value) ? value : -1).Where(value => value >= 0).Order()) Layers.Add(layer);
        SelectedLayer = Layers.Contains(0) ? 0 : Layers.FirstOrDefault();
        SelectedLayout = Export.Layouts.ContainsKey("6x4") ? "6x4" : Layouts.FirstOrDefault();
        PreviewKeys = SelectedLayout is null ? [] : LayoutBuilder.Build(Export, SelectedLayout);
        PreviewButtons = SelectedLayout is null || !Export.Layouts[SelectedLayout].CustomButtons.TryGetValue("0", out var buttons) ? [] : buttons;
        Status = $"読み込み完了: {displayName} ({Layouts.Count} レイアウト)";
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(HasExport));
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(PreviewKeys));
        OnPropertyChanged(nameof(PreviewButtons));
    }

    public void SaveProject(string path)
    {
        var project = new PrintProject(sourceExportPath, SelectedLayout, SelectedPageSize, BackgroundImagePath,
            KeyFill, KeyBorder, TextColor, KeyBorderWidthMm, PageBackground, TrackpadXmm, TrackpadYmm,
            KeyCornerRadiusMm, KeyTextSizeMm, KeyFontFamily, BackgroundXmm, BackgroundYmm, BackgroundScale, BackgroundRotationDegrees, ShowBackgroundInKeymap, KeyFillOpacity,
            CustomPageWidthMm, CustomPageHeightMm, ShowCalibration, PngDpi, SelectedLayer, SelectedTheme, BackgroundOpacity);
        File.WriteAllText(path, JsonSerializer.Serialize(project, ProjectJsonOptions));
        Status = $"保存完了: {Path.GetFileName(path)}";
        OnPropertyChanged(nameof(Status));
    }

    public void LoadProject(string path)
    {
        var project = JsonSerializer.Deserialize<PrintProject>(File.ReadAllText(path))
            ?? throw new FormatException("Invalid .gtprint.json project.");
        if (string.IsNullOrWhiteSpace(project.SourceExportPath) || !File.Exists(project.SourceExportPath))
            throw new FileNotFoundException("The project's source export was not found.", project.SourceExportPath);
        LoadExport(project.SourceExportPath);
        SelectedLayout = project.Layout ?? SelectedLayout;
        SelectedLayer = project.Layer;
        SelectedPageSize = project.PageSize ?? "A4";
        KeyFill = project.KeyFill ?? KeyFill;
        KeyBorder = project.KeyBorder ?? KeyBorder;
        TextColor = project.TextColor ?? TextColor;
        KeyBorderWidthMm = project.KeyBorderWidthMm;
        PageBackground = project.PageBackground ?? PageBackground;
        TrackpadXmm = project.TrackpadXmm;
        TrackpadYmm = project.TrackpadYmm;
        KeyCornerRadiusMm = project.KeyCornerRadiusMm;
        KeyTextSizeMm = project.KeyTextSizeMm;
        KeyFontFamily = project.KeyFontFamily ?? KeyFontFamily;
        BackgroundXmm = project.BackgroundXmm;
        BackgroundYmm = project.BackgroundYmm;
        BackgroundScale = project.BackgroundScale;
        BackgroundRotationDegrees = project.BackgroundRotationDegrees;
        ShowBackgroundInKeymap = project.ShowBackgroundInKeymap;
        KeyFillOpacity = project.KeyFillOpacity;
        CustomPageWidthMm = project.CustomPageWidthMm;
        CustomPageHeightMm = project.CustomPageHeightMm;
        ShowCalibration = project.ShowCalibration;
        PngDpi = project.PngDpi;
        SelectedTheme = project.Theme ?? "標準";
        BackgroundOpacity = project.BackgroundOpacity;
        SetBackgroundImage(project.BackgroundImagePath is { } image && File.Exists(image) ? image : null);
        Status = $"読み込み完了: {Path.GetFileName(path)}";
        OnPropertyChanged(nameof(Status));
    }

    public void LoadRecent(string path) => LoadExport(path);

    private sealed record PrintProject(
        string? SourceExportPath,
        string? Layout,
        string? PageSize,
        string? BackgroundImagePath,
        string? KeyFill,
        string? KeyBorder,
        string? TextColor,
        double KeyBorderWidthMm,
        string? PageBackground,
        double TrackpadXmm = 25,
        double TrackpadYmm = 25,
        double KeyCornerRadiusMm = 1,
        double KeyTextSizeMm = 3.5,
        string? KeyFontFamily = "Arial",
        double BackgroundXmm = 0,
        double BackgroundYmm = 0,
        double BackgroundScale = 1,
        double BackgroundRotationDegrees = 0,
        bool ShowBackgroundInKeymap = true,
        double KeyFillOpacity = 0.85,
        double CustomPageWidthMm = 210,
        double CustomPageHeightMm = 297,
        bool ShowCalibration = false,
        int PngDpi = 300,
        int Layer = 0,
        string? Theme = "標準",
        double BackgroundOpacity = 1);
    public void SetBackgroundImage(string? path, int side = -1)
    {
        if (side == 0) LeftBackgroundImagePath = path;
        else if (side == 1) RightBackgroundImagePath = path;
        else BackgroundImagePath = path;
        Status = path is null ? "背景画像を解除しました" : $"背景画像: {Path.GetFileName(path)}";
        OnPropertyChanged(nameof(BackgroundImagePath));
        OnPropertyChanged(nameof(LeftBackgroundImagePath));
        OnPropertyChanged(nameof(RightBackgroundImagePath));
        OnPropertyChanged(nameof(Status));
    }

    public void ShowError(Exception exception)
    {
        Status = $"エラー: {exception.Message}";
        OnPropertyChanged(nameof(Status));
    }

#if !PRINT_STUDIO_BROWSER
    private SvgPrintOptions PrintOptions => ((SvgPrintOptions)(SelectedPageSize switch
    {
        "A3" => new(SelectedLayout!, BackgroundImagePath, 297, 420, TrackpadXmm, TrackpadYmm, KeyFill, KeyBorder, TextColor, KeyBorderWidthMm, PageBackground, KeyCornerRadiusMm, KeyTextSizeMm, KeyFontFamily, BackgroundXmm, BackgroundYmm, BackgroundScale, BackgroundRotationDegrees, ShowBackgroundInKeymap, KeyFillOpacity, ShowCalibration, SelectedLayer, BackgroundOpacity),
        "Letter" => new(SelectedLayout!, BackgroundImagePath, 215.9, 279.4, TrackpadXmm, TrackpadYmm, KeyFill, KeyBorder, TextColor, KeyBorderWidthMm, PageBackground, KeyCornerRadiusMm, KeyTextSizeMm, KeyFontFamily, BackgroundXmm, BackgroundYmm, BackgroundScale, BackgroundRotationDegrees, ShowBackgroundInKeymap, KeyFillOpacity, ShowCalibration, SelectedLayer, BackgroundOpacity),
        "Trackpad" => new(SelectedLayout!, BackgroundImagePath, 160, 114.9, 0, 0, KeyFill, KeyBorder, TextColor, KeyBorderWidthMm, PageBackground, KeyCornerRadiusMm, KeyTextSizeMm, KeyFontFamily, BackgroundXmm, BackgroundYmm, BackgroundScale, BackgroundRotationDegrees, ShowBackgroundInKeymap, KeyFillOpacity, ShowCalibration, SelectedLayer, BackgroundOpacity),
        "Custom" => new(SelectedLayout!, BackgroundImagePath, CustomPageWidthMm, CustomPageHeightMm, TrackpadXmm, TrackpadYmm, KeyFill, KeyBorder, TextColor, KeyBorderWidthMm, PageBackground, KeyCornerRadiusMm, KeyTextSizeMm, KeyFontFamily, BackgroundXmm, BackgroundYmm, BackgroundScale, BackgroundRotationDegrees, ShowBackgroundInKeymap, KeyFillOpacity, ShowCalibration, SelectedLayer, BackgroundOpacity),
        _ => new(SelectedLayout!, BackgroundImagePath, 210, 297, TrackpadXmm, TrackpadYmm, KeyFill, KeyBorder, TextColor, KeyBorderWidthMm, PageBackground, KeyCornerRadiusMm, KeyTextSizeMm, KeyFontFamily, BackgroundXmm, BackgroundYmm, BackgroundScale, BackgroundRotationDegrees, ShowBackgroundInKeymap, KeyFillOpacity, ShowCalibration, SelectedLayer, BackgroundOpacity),
    })) with { LeftBackgroundImagePath = LeftBackgroundImagePath, RightBackgroundImagePath = RightBackgroundImagePath, ShowOtherLayerLabels = ShowOtherLayerLabels };

    public void ExportSvg(string path)
    {
        if (Export is null || SelectedLayout is null) throw new InvalidOperationException("Load an export first.");
        File.WriteAllText(path, SvgPrintRenderer.Render(Export, PrintOptions));
        Status = $"書き出し完了: {Path.GetFileName(path)}";
        OnPropertyChanged(nameof(Status));
    }

    public void ExportPdf(string path)
    {
        if (Export is null || SelectedLayout is null) throw new InvalidOperationException("Load an export first.");
        RasterPrintRenderer.RenderPdf(Export, PrintOptions, path);
        Status = $"書き出し完了: {Path.GetFileName(path)}";
        OnPropertyChanged(nameof(Status));
    }

    public void ExportPng(string path)
    {
        if (Export is null || SelectedLayout is null) throw new InvalidOperationException("Load an export first.");
        var warning = RasterPrintRenderer.GetResolutionWarning(BackgroundImagePath, 160 * BackgroundScale, PngDpi);
        if (warning is not null) Status = $"警告: {warning}";
        RasterPrintRenderer.RenderPng(Export, PrintOptions, path, PngDpi);
        Status = warning is null ? $"書き出し完了: {Path.GetFileName(path)}" : $"書き出し完了（警告あり）: {Path.GetFileName(path)}";
        OnPropertyChanged(nameof(Status));
    }
#endif
}
