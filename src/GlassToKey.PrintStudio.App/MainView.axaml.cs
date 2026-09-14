using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using GlassToKey.PrintStudio.App.ViewModels;
using System.ComponentModel;
using System.IO;

namespace GlassToKey.PrintStudio.App;

public partial class MainView : UserControl
{
    private MainWindowViewModel? attachedViewModel;
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public MainView()
    {
        AvaloniaXamlLoader.Load(this);
        DataContextChanged += (_, _) => AttachViewModel();
        AttachedToVisualTree += (_, _) => AttachViewModel();
    }

    private void AttachViewModel()
    {
        if (DataContext is not MainWindowViewModel viewModel) return;
        var preview = this.FindControl<PreviewCanvas>("Preview");
        if (preview is null) return;
        if (ReferenceEquals(attachedViewModel, viewModel)) return;
        if (attachedViewModel is not null) attachedViewModel.PropertyChanged -= ViewModelPropertyChanged;
        attachedViewModel = viewModel;
        preview.SetKeys(viewModel.PreviewKeys);
        preview.SetButtons(viewModel.PreviewButtons);
        preview.SetStyle(viewModel.KeyFill, viewModel.KeyBorder, viewModel.TextColor, viewModel.KeyCornerRadiusMm, viewModel.KeyTextSizeMm, viewModel.KeyFontFamily, viewModel.KeyFillOpacity);
        preview.SetPageBackground(viewModel.PageBackground);
        preview.SetBackgroundPlacement(viewModel.BackgroundXmm, viewModel.BackgroundYmm, viewModel.BackgroundScale, viewModel.BackgroundRotationDegrees);
        preview.SetBackgroundVisibility(viewModel.ShowBackgroundInKeymap);
        preview.SetOtherLayerLabelsVisibility(viewModel.ShowOtherLayerLabels);
        preview.SetSideBackground(viewModel.LeftBackgroundImagePath, viewModel.RightBackgroundImagePath);
        preview.BackgroundPlacementChanged += (x, y, scale, rotation) =>
        {
            viewModel.BackgroundXmm = x;
            viewModel.BackgroundYmm = y;
            viewModel.BackgroundScale = scale;
            viewModel.BackgroundRotationDegrees = rotation;
        };
        viewModel.PropertyChanged += ViewModelPropertyChanged;
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainWindowViewModel viewModel) return;
        var preview = this.FindControl<PreviewCanvas>("Preview");
        if (preview is null) return;
        if (e.PropertyName == nameof(MainWindowViewModel.PreviewKeys)) preview.SetKeys(viewModel.PreviewKeys);
        if (e.PropertyName == nameof(MainWindowViewModel.PreviewButtons)) preview.SetButtons(viewModel.PreviewButtons);
        if (e.PropertyName == nameof(MainWindowViewModel.BackgroundImagePath)) preview.SetBackground(viewModel.BackgroundImagePath);
        if (e.PropertyName is nameof(MainWindowViewModel.LeftBackgroundImagePath) or nameof(MainWindowViewModel.RightBackgroundImagePath))
            preview.SetSideBackground(viewModel.LeftBackgroundImagePath, viewModel.RightBackgroundImagePath);
        if (e.PropertyName is nameof(MainWindowViewModel.BackgroundXmm) or nameof(MainWindowViewModel.BackgroundYmm)
            or nameof(MainWindowViewModel.BackgroundScale) or nameof(MainWindowViewModel.BackgroundRotationDegrees))
            preview.SetBackgroundPlacement(viewModel.BackgroundXmm, viewModel.BackgroundYmm, viewModel.BackgroundScale, viewModel.BackgroundRotationDegrees);
        if (e.PropertyName == nameof(MainWindowViewModel.ShowBackgroundInKeymap)) preview.SetBackgroundVisibility(viewModel.ShowBackgroundInKeymap);
        if (e.PropertyName == nameof(MainWindowViewModel.ShowOtherLayerLabels)) preview.SetOtherLayerLabelsVisibility(viewModel.ShowOtherLayerLabels);
        if (e.PropertyName is nameof(MainWindowViewModel.KeyFill) or nameof(MainWindowViewModel.KeyBorder) or nameof(MainWindowViewModel.TextColor)
            or nameof(MainWindowViewModel.KeyCornerRadiusMm) or nameof(MainWindowViewModel.KeyTextSizeMm) or nameof(MainWindowViewModel.KeyFontFamily)
            or nameof(MainWindowViewModel.KeyFillOpacity))
            preview.SetStyle(viewModel.KeyFill, viewModel.KeyBorder, viewModel.TextColor, viewModel.KeyCornerRadiusMm, viewModel.KeyTextSizeMm, viewModel.KeyFontFamily, viewModel.KeyFillOpacity);
        if (e.PropertyName == nameof(MainWindowViewModel.PageBackground)) preview.SetPageBackground(viewModel.PageBackground);
    }

    private TopLevel TopLevel => Avalonia.Controls.TopLevel.GetTopLevel(this)
        ?? throw new InvalidOperationException("The view is not attached to a top-level window.");

    private PreviewCanvas PreviewCanvasControl => this.FindControl<PreviewCanvas>("Preview")
        ?? throw new InvalidOperationException("Preview is not ready.");

    private void ShowCompatibility(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => PreviewCanvasControl.SetCompatibilityMode(true);
    private void ShowPrint(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => PreviewCanvasControl.SetCompatibilityMode(false);
    private void FitTrackpad(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => PreviewCanvasControl.FitTrackpad();
    private void Set100Percent(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => PreviewCanvasControl.Set100Percent();
    private void FitBackground(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => PreviewCanvasControl.FitBackground();
    private void ToggleDebugGrid(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox) PreviewCanvasControl.SetDebugGrid(checkBox.IsChecked == true);
    }

    private async void OpenExport(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var files = await TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = [new("JSON") { Patterns = ["*.json"] }] });
            if (files.Count == 1)
            {
                await using var stream = await files[0].OpenReadAsync();
                using var reader = new StreamReader(stream);
                ViewModel.LoadExportJson(await reader.ReadToEndAsync(), files[0].Name);
            }
        }
        catch (Exception exception) { ViewModel.ShowError(exception); }
    }

    private async void OpenProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var files = await TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
            if (files.Count == 1) ViewModel.LoadProject(files[0].Path.LocalPath);
        }
        catch (Exception exception) { ViewModel.ShowError(exception); }
    }

    private void ReloadSource(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try { ViewModel.ReloadSource(); } catch (Exception exception) { ViewModel.ShowError(exception); }
    }

    private async void SaveProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var file = await TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { SuggestedFileName = "glass-to-key-project.gtprint.json", DefaultExtension = "gtprint.json" });
            if (file is not null) ViewModel.SaveProject(file.Path.LocalPath);
        }
        catch (Exception exception) { ViewModel.ShowError(exception); }
    }

    private async void ChooseBackground(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var files = await TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
            if (files.Count == 1)
            {
#if PRINT_STUDIO_BROWSER
                await using var stream = await files[0].OpenReadAsync();
                var preview = this.FindControl<PreviewCanvas>("Preview")
                    ?? throw new InvalidOperationException("Preview is not ready.");
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                buffer.Position = 0;
                preview.SetBackground(buffer);
#else
                var side = (sender as Avalonia.Controls.Button)?.Tag is string tag && int.TryParse(tag, out var parsed) ? parsed : -1;
                ViewModel.SetBackgroundImage(files[0].Path.LocalPath, side);
#endif
            }
        }
        catch (Exception exception) { ViewModel.ShowError(exception); }
    }

    private async void ExportSvg(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
#if PRINT_STUDIO_BROWSER
        ViewModel.ShowError(new PlatformNotSupportedException("Export is available in the desktop app."));
        await Task.CompletedTask;
#else
        try
        {
            var file = await TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { SuggestedFileName = "glass-to-key-print.svg", DefaultExtension = "svg" });
            if (file is not null) ViewModel.ExportSvg(file.Path.LocalPath);
        }
        catch (Exception exception) { ViewModel.ShowError(exception); }
#endif
    }

    private async void ExportPdf(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
#if PRINT_STUDIO_BROWSER
        ViewModel.ShowError(new PlatformNotSupportedException("Export is available in the desktop app."));
        await Task.CompletedTask;
#else
        try
        {
            var file = await TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { SuggestedFileName = "glass-to-key-print.pdf", DefaultExtension = "pdf" });
            if (file is not null) ViewModel.ExportPdf(file.Path.LocalPath);
        }
        catch (Exception exception) { ViewModel.ShowError(exception); }
#endif
    }

    private async void ExportPng(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
#if PRINT_STUDIO_BROWSER
        ViewModel.ShowError(new PlatformNotSupportedException("Export is available in the desktop app."));
        await Task.CompletedTask;
#else
        try
        {
            var file = await TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { SuggestedFileName = "glass-to-key-print.png", DefaultExtension = "png" });
            if (file is not null) ViewModel.ExportPng(file.Path.LocalPath);
        }
        catch (Exception exception) { ViewModel.ShowError(exception); }
#endif
    }
}
