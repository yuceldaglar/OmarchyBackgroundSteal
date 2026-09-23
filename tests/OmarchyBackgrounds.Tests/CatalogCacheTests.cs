using OmarchyBackgrounds.Cache;
using OmarchyBackgrounds.Catalog;

namespace OmarchyBackgrounds.Tests;

public class CatalogCacheTests
{
    [Fact]
    public async Task LoadAsync_returns_null_when_file_missing()
    {
        var dir = CreateTempDir();
        try
        {
            var cache = new CatalogCache(dir);
            var loaded = await cache.LoadAsync();
            Assert.Null(loaded);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_then_LoadAsync_round_trips_catalog()
    {
        var dir = CreateTempDir();
        try
        {
            var cache = new CatalogCache(dir);
            var catalog = new ThemeCatalog
            {
                ScrapedAt = DateTimeOffset.Parse("2026-09-23T12:00:00Z"),
                Themes =
                {
                    new Theme
                    {
                        Id = "aura",
                        Name = "Aura",
                        RepoUrl = "https://github.com/bjarneo/omarchy-aura-theme",
                        Backgrounds =
                        {
                            new BackgroundImage
                            {
                                Id = "aura:1.png",
                                FileName = "1.png",
                                ImageUrl = "https://raw.githubusercontent.com/bjarneo/omarchy-aura-theme/main/backgrounds/1.png",
                                Attribution = "Aura — https://github.com/bjarneo/omarchy-aura-theme",
                            },
                        },
                    },
                },
            };

            await cache.SaveAsync(catalog);
            var loaded = await cache.LoadAsync();

            Assert.NotNull(loaded);
            Assert.Equal(catalog.ScrapedAt, loaded!.ScrapedAt);
            Assert.Single(loaded.Themes);
            Assert.Equal("Aura", loaded.Themes[0].Name);
            Assert.Single(loaded.Themes[0].Backgrounds);
            Assert.Equal("1.png", loaded.Themes[0].Backgrounds[0].FileName);
            Assert.True(File.Exists(cache.CatalogPath));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "OmarchyBackgroundsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
