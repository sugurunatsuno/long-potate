using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GlassToKey.PrintStudio.App.ViewModels;

namespace GlassToKey.PrintStudio.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new MainWindowViewModel();
    }
}
