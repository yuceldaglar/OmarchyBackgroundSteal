using System.Net;
using System.Text;
using OmarchyBackgrounds.Cache;
using OmarchyBackgrounds.Catalog;
using OmarchyBackgrounds.Scraper;

namespace OmarchyBackgrounds.Tests;

public class ThemeScraperTests
{
    [Fact]
    public void ExtractRepos_parses_unique_theme_links_and_skips_site_repos()
    {
        const string html = """
            <a href="https://github.com/omacom/omarchy">site</a>
            <a href="https://github.com/omacom/omarchy-site/compare">compare</a>
            <a href="https://github.com/bjarneo/omarchy-aura-theme">Aura</a>
            <a href="https://github.com/bjarneo/omarchy-aura-theme">Aura again</a>
            <a href="https://github.com/JJDizz1L/aetheria">Aetheria</a>
            """;

        var repos = ThemeScraper.ExtractRepos(html);

        Assert.Equal(2, repos.Count);
        Assert.Contains(repos, r => r.Owner == "bjarneo" && r.Name == "omarchy-aura-theme");
        Assert.Contains(repos, r => r.Owner == "JJDizz1L" && r.Name == "aetheria");
    }

    [Fact]
    public async Task ScrapeAsync_lists_all_themes_without_github_contents_calls()
    {
        var githubCalls = 0;
        var handler = new StubHandler((request, _) =>
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("omarchy.org/themes", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Html("""
                    <a href="https://github.com/bjarneo/omarchy-aura-theme">Aura</a>
                    <a href="https://github.com/someone/empty-theme">Empty</a>
                    """));
            }

            if (url.Contains("api.github.com", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref githubCalls);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var client = new HttpClient(handler);
        var scraper = new ThemeScraper(client, "https://omarchy.org/themes/");

        var catalog = await scraper.ScrapeAsync();

        Assert.Equal(2, catalog.Themes.Count);
        Assert.All(catalog.Themes, t => Assert.Empty(t.Backgrounds));
        Assert.Equal(0, githubCalls);
    }

    [Fact]
    public async Task LoadBackgroundsAsync_prefers_jsdelivr_and_skips_github_api()
    {
        var githubApiCalls = 0;
        var handler = new StubHandler((request, _) =>
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("data.jsdelivr.com/v1/packages/gh/bjarneo/omarchy-aura-theme@main", StringComparison.Ordinal))
            {
                return Task.FromResult(Json("""
                    {
                      "files": [
                        {
                          "type": "directory",
                          "name": "backgrounds",
                          "files": [
                            { "type": "file", "name": "1.png", "size": 10 },
                            { "type": "file", "name": "readme.txt", "size": 1 }
                          ]
                        }
                      ]
                    }
                    """));
            }

            if (url.Contains("api.github.com", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref githubApiCalls);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var client = new HttpClient(handler);
        var scraper = new ThemeScraper(client);
        var theme = new Theme
        {
            Id = "bjarneo/omarchy-aura-theme",
            Name = "Aura",
            RepoUrl = "https://github.com/bjarneo/omarchy-aura-theme",
        };

        await scraper.LoadBackgroundsAsync(theme);

        Assert.Single(theme.Backgrounds);
        Assert.Equal("1.png", theme.Backgrounds[0].FileName);
        Assert.Contains("cdn.jsdelivr.net", theme.Backgrounds[0].ImageUrl);
        Assert.Equal(0, githubApiCalls);
    }

    [Fact]
    public async Task CatalogService_falls_back_to_cache_when_scrape_fails()
    {
        var dir = Path.Combine(Path.GetTempPath(), "OmarchyBackgroundsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var cache = new CatalogCache(dir);
            await cache.SaveAsync(new ThemeCatalog
            {
                ScrapedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                Themes =
                {
                    new Theme { Id = "cached", Name = "Cached Theme", RepoUrl = "https://github.com/x/y" },
                },
            });

            var service = new CatalogService(new FailingScraper(), cache);
            var result = await service.RefreshAsync();

            Assert.True(result.UsedCacheFallback);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal("Cached Theme", result.Catalog.Themes[0].Name);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static HttpResponseMessage Html(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "text/html") };

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class FailingScraper : IThemeScraper
    {
        public Task<ThemeCatalog> ScrapeAsync(CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("network down");

        public Task LoadBackgroundsAsync(Theme theme, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request, cancellationToken);
    }
}
