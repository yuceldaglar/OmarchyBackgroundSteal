using OmarchyBackgrounds.ApplyHistory;
using OmarchyBackgrounds.BackgroundStore;
using OmarchyBackgrounds.Cache;
using OmarchyBackgrounds.Catalog;
using OmarchyBackgrounds.Scraper;
using OmarchyBackgrounds.Wallpaper;

namespace OmarchyBackgrounds_App;

internal static class AppServices
{
    public static CatalogService CatalogService { get; }
    public static IBackgroundStore BackgroundStore { get; }
    public static IWallpaperApplier WallpaperApplier { get; }
    public static IApplyHistoryStore ApplyHistory { get; }

    static AppServices()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("OmarchyBackgrounds/1.0");

        CatalogService = new CatalogService(new ThemeScraper(http), new CatalogCache());
        BackgroundStore = new FileBackgroundStore(http);
        WallpaperApplier = new WallpaperApplier();
        ApplyHistory = new FileApplyHistoryStore();
    }
}
