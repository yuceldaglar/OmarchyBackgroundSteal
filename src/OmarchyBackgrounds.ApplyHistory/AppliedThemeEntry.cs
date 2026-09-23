namespace OmarchyBackgrounds.ApplyHistory;

public sealed class AppliedThemeEntry
{
    public string ThemeId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string RepoUrl { get; set; } = string.Empty;

    public DateTimeOffset AppliedAt { get; set; }
}
