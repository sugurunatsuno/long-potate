using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using PrintStudioApp = GlassToKey.PrintStudio.App.App;

[assembly: SupportedOSPlatform("browser")]

namespace GlassToKey.PrintStudio.Browser;

internal sealed partial class Program
{
    private static Task Main(string[] args) =>
        BuildAvaloniaApp().StartBrowserAppAsync("out", new BrowserPlatformOptions
        {
            PreferFileDialogPolyfill = true,
            RegisterAvaloniaServiceWorker = true,
        });

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<PrintStudioApp>();
}
