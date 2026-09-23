using OmarchyBackgrounds.BackgroundStore;
using OmarchyBackgrounds.Cache;
using OmarchyBackgrounds.Catalog;
using OmarchyBackgrounds.Scraper;
using OmarchyBackgrounds.Wallpaper;

namespace OmarchyBackgrounds_App;

internal static class AppServices
{
    // Cap GitHub Contents API calls on first refresh to stay under unauthenticated rate limits.
    private const int MaxThemesPerRefresh = 15;

    public static CatalogService CatalogService { get; }
    public static IBackgroundStore BackgroundStore { get; }
    public static IWallpaperApplier WallpaperApplier { get; }

    static AppServices()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("OmarchyBackgrounds/1.0");

        CatalogService = new CatalogService(
            new ThemeScraper(http, maxThemes: MaxThemesPerRefresh),
            new CatalogCache());
        BackgroundStore = new FileBackgroundStore(http);
        WallpaperApplier = new WallpaperApplier();
    }
}
