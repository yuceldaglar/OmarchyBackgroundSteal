using Microsoft.UI.Xaml;

namespace OmarchyBackgrounds_App;

public partial class App : Application
{
    public static Window MainWindowInstance { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        MainWindowInstance = new MainWindow();
        MainWindowInstance.Activate();
    }
}
