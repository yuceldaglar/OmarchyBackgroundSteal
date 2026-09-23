using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using OmarchyBackgrounds.Catalog;

namespace OmarchyBackgrounds_App;

public sealed partial class MainPage : Page
{
    private BackgroundImage? _selectedBackground;
    private string? _selectedLocalPath;

    public MainPage()
    {
        InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshCatalogAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshCatalogAsync();
    }

    private async Task RefreshCatalogAsync()
    {
        RefreshButton.IsEnabled = false;
        StatusText.Text = "Refreshing catalog…";
        try
        {
            var result = await AppServices.CatalogService.RefreshAsync();
            ThemeList.ItemsSource = result.Catalog.Themes.ToList();

            if (result.Catalog.Themes.Count == 0)
            {
                StatusText.Text = result.ErrorMessage is null
                    ? "No themes with backgrounds found."
                    : $"Catalog empty. {result.ErrorMessage}";
            }
            else if (result.UsedCacheFallback)
            {
                StatusText.Text =
                    $"Using last-good catalog ({result.Catalog.Themes.Count} themes). Refresh error: {result.ErrorMessage}";
            }
            else
            {
                StatusText.Text =
                    $"Loaded {result.Catalog.Themes.Count} themes (scraped {result.Catalog.ScrapedAt:u}).";
            }

            if (ThemeList.Items.Count > 0)
            {
                ThemeList.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to load catalog: {ex.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedBackground = null;
        _selectedLocalPath = null;
        ApplyButton.IsEnabled = false;
        PreviewImage.Source = null;

        if (ThemeList.SelectedItem is not Theme theme)
        {
            BackgroundGrid.ItemsSource = null;
            AttributionText.Text = "Select a theme";
            return;
        }

        BackgroundGrid.ItemsSource = theme.Backgrounds.ToList();
        AttributionText.Text = theme.RepoUrl;
        if (Uri.TryCreate(theme.RepoUrl, UriKind.Absolute, out var repoUri))
        {
            SourceLink.NavigateUri = repoUri;
        }

        if (theme.Backgrounds.Count > 0)
        {
            BackgroundGrid.SelectedIndex = 0;
        }
    }

    private async void BackgroundGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BackgroundGrid.SelectedItem is not BackgroundImage background)
        {
            return;
        }

        _selectedBackground = background;
        AttributionText.Text = background.Attribution;
        ApplyButton.IsEnabled = false;
        StatusText.Text = $"Downloading {background.FileName}…";

        try
        {
            _selectedLocalPath = await AppServices.BackgroundStore.GetLocalPathAsync(background);
            await SetPreviewFromPathAsync(_selectedLocalPath);
            ApplyButton.IsEnabled = true;
            StatusText.Text = $"Ready: {background.FileName}";
        }
        catch (Exception ex)
        {
            _selectedLocalPath = null;
            PreviewImage.Source = null;
            StatusText.Text = $"Download failed: {ex.Message}";
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedLocalPath is null || _selectedBackground is null)
        {
            return;
        }

        ApplyButton.IsEnabled = false;
        StatusText.Text = "Applying wallpaper…";
        try
        {
            await AppServices.WallpaperApplier.ApplyAsync(_selectedLocalPath);
            StatusText.Text = $"Applied desktop + lock screen: {_selectedBackground.FileName}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Apply failed: {ex.Message}";
        }
        finally
        {
            ApplyButton.IsEnabled = _selectedLocalPath is not null;
        }
    }

    private async Task SetPreviewFromPathAsync(string localPath)
    {
        // Avoid file:// URI issues with special characters (e.g. "@" in theme filenames).
        await using var stream = File.OpenRead(localPath);
        var ras = stream.AsRandomAccessStream();
        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(ras);
        PreviewImage.Source = bitmap;
    }
}
