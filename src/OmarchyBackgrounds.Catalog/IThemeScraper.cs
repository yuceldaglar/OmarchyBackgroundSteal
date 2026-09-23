namespace OmarchyBackgrounds.Catalog;

public interface IThemeScraper
{
    Task<ThemeCatalog> ScrapeAsync(CancellationToken cancellationToken = default);
}
