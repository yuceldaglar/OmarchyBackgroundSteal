namespace OmarchyBackgrounds.Catalog;

public interface ICatalogCache
{
    Task<ThemeCatalog?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ThemeCatalog catalog, CancellationToken cancellationToken = default);
}
