using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmarchyBackgrounds.Wallpaper;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace OmarchyBackgrounds_App;

public sealed partial class MainPage : Page
{
    private readonly IWallpaperApplier _wallpaperApplier = new WallpaperApplier();

    public MainPage()
    {
        InitializeComponent();
    }

    private async void ApplyLocalImage_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Choosing image…";

        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindowInstance));
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".webp");
        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            StatusText.Text = "Cancelled.";
            return;
        }

        try
        {
            ApplyButton.IsEnabled = false;
            StatusText.Text = "Applying…";
            await _wallpaperApplier.ApplyAsync(file.Path);
            StatusText.Text = $"Applied desktop + lock screen: {file.Name}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Apply failed: {ex.Message}";
        }
        finally
        {
            ApplyButton.IsEnabled = true;
        }
    }
}
