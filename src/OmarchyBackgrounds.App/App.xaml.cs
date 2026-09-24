using Microsoft.UI.Xaml;

namespace OmarchyBackgrounds_App;

public partial class App : Application
{
    public static Window MainWindowInstance { get; private set; } = null!;

    public App()
    {
        // Required for PublishSingleFile + Windows App SDK SxS / resources.pri lookup.
        Environment.SetEnvironmentVariable(
            "MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY",
            AppContext.BaseDirectory);
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        MainWindowInstance = new MainWindow();
        MainWindowInstance.Activate();
    }
}
