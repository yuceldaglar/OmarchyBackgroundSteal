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
    private ThemeCatalog? _current;

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
            var previous = await _cache.LoadAsync(cancellationToken).ConfigureAwait(false)
                ?? _current;
            MergeCachedBackgrounds(catalog, previous);
            await _cache.SaveAsync(catalog, cancellationToken).ConfigureAwait(false);
            _current = catalog;
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
                _current = cached;
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

    public async Task EnsureBackgroundsAsync(Theme theme, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(theme);
        if (theme.Backgrounds.Count > 0)
        {
            return;
        }

        await _scraper.LoadBackgroundsAsync(theme, cancellationToken).ConfigureAwait(false);

        if (_current is not null)
        {
            await _cache.SaveAsync(_current, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void MergeCachedBackgrounds(ThemeCatalog fresh, ThemeCatalog? previous)
    {
        if (previous is null || previous.Themes.Count == 0)
        {
            return;
        }

        var byId = previous.Themes
            .Where(t => t.Backgrounds.Count > 0)
            .ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var theme in fresh.Themes)
        {
            if (theme.Backgrounds.Count == 0 && byId.TryGetValue(theme.Id, out var cached))
            {
                foreach (var background in cached.Backgrounds)
                {
                    theme.Backgrounds.Add(background);
                }
            }
        }
    }
}
