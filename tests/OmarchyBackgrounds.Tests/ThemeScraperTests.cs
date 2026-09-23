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
    public async Task ScrapeAsync_builds_catalog_from_html_and_github_contents()
    {
        var handler = new StubHandler(async (request, ct) =>
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("omarchy.org/themes", StringComparison.OrdinalIgnoreCase))
            {
                return Html("""
                    <a href="https://github.com/bjarneo/omarchy-aura-theme">Aura</a>
                    <a href="https://github.com/someone/empty-theme">Empty</a>
                    """);
            }

            if (url.Contains("repos/bjarneo/omarchy-aura-theme/contents/backgrounds", StringComparison.Ordinal))
            {
                return Json("""
                    [
                      {"name":"1.png","type":"file","download_url":"https://raw.githubusercontent.com/bjarneo/omarchy-aura-theme/main/backgrounds/1.png"},
                      {"name":"readme.txt","type":"file","download_url":"https://example.com/readme.txt"},
                      {"name":"nested","type":"dir","download_url":null}
                    ]
                    """);
            }

            if (url.Contains("repos/someone/empty-theme/contents/backgrounds", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var client = new HttpClient(handler);
        var scraper = new ThemeScraper(client, "https://omarchy.org/themes/");

        var catalog = await scraper.ScrapeAsync();

        Assert.Single(catalog.Themes);
        var theme = catalog.Themes[0];
        Assert.Equal("Aura", theme.Name);
        Assert.Equal("https://github.com/bjarneo/omarchy-aura-theme", theme.RepoUrl);
        Assert.Single(theme.Backgrounds);
        Assert.Equal("1.png", theme.Backgrounds[0].FileName);
        Assert.Contains("raw.githubusercontent.com", theme.Backgrounds[0].ImageUrl);
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
