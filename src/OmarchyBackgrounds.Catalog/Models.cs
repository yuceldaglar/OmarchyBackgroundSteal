namespace OmarchyBackgrounds.Catalog;

public sealed class ThemeCatalog
{
    public DateTimeOffset ScrapedAt { get; set; }

    public string SourceUrl { get; set; } = "https://omarchy.org/themes/";

    public IList<Theme> Themes { get; set; } = new List<Theme>();
}

public sealed class Theme
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string RepoUrl { get; set; } = string.Empty;

    public IList<BackgroundImage> Backgrounds { get; set; } = new List<BackgroundImage>();
}

public sealed class BackgroundImage
{
    public string Id { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string Attribution { get; set; } = string.Empty;
}
