using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GlassToKey.PrintStudio.App;

public partial class MainView : UserControl
{
    public MainView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
