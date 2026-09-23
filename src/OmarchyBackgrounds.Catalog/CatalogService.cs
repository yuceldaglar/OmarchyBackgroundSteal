namespace OmarchyBackgrounds.Catalog;

public sealed class CatalogRefreshResult
{
    public required ThemeCatalog Catalog { get; init; }

    public bool UsedCacheFallback { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed class CatalogService
{
    private readonly IThemeScraper _scraper;
    private readonly ICatalogCache _cache;

    public CatalogService(IThemeScraper scraper, ICatalogCache cache)
    {
        _scraper = scraper ?? throw new ArgumentNullException(nameof(scraper));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<CatalogRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var catalog = await _scraper.ScrapeAsync(cancellationToken).ConfigureAwait(false);
            await _cache.SaveAsync(catalog, cancellationToken).ConfigureAwait(false);
            return new CatalogRefreshResult
            {
                Catalog = catalog,
                UsedCacheFallback = false,
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var cached = await _cache.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return new CatalogRefreshResult
                {
                    Catalog = cached,
                    UsedCacheFallback = true,
                    ErrorMessage = ex.Message,
                };
            }

            return new CatalogRefreshResult
            {
                Catalog = new ThemeCatalog(),
                UsedCacheFallback = true,
                ErrorMessage = ex.Message,
            };
        }
    }
}
