namespace OmarchyBackgrounds.Catalog;

public interface IThemeScraper
{
    /// <summary>
    /// Lists all themes from the Omarchy themes page (no per-repo GitHub API calls).
    /// Backgrounds may be empty until <see cref="LoadBackgroundsAsync"/> runs.
    /// </summary>
    Task<ThemeCatalog> ScrapeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fills <paramref name="theme"/>.Backgrounds via the GitHub Contents API when empty.
    /// </summary>
    Task LoadBackgroundsAsync(Theme theme, CancellationToken cancellationToken = default);
}
