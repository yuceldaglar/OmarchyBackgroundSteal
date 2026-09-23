using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using OmarchyBackgrounds.Catalog;

namespace OmarchyBackgrounds.Scraper;

public sealed class ThemeScraper : IThemeScraper
{
    public const string DefaultThemesUrl = "https://omarchy.org/themes/";

    private static readonly Regex GitHubRepoRegex = new(
        @"https?://github\.com/(?<owner>[A-Za-z0-9_.-]+)/(?<repo>[A-Za-z0-9_.-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".gif",
    };

    private static readonly HashSet<string> IgnoredRepos = new(StringComparer.OrdinalIgnoreCase)
    {
        "omacom/omarchy",
        "omacom/omarchy-site",
    };

    private readonly HttpClient _httpClient;
    private readonly string _themesUrl;
    private readonly int? _maxThemes;

    public ThemeScraper(HttpClient httpClient, string? themesUrl = null, int? maxThemes = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _themesUrl = string.IsNullOrWhiteSpace(themesUrl) ? DefaultThemesUrl : themesUrl;
        _maxThemes = maxThemes;

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("OmarchyBackgrounds", "1.0"));
        }
    }

    public async Task<ThemeCatalog> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        var html = await _httpClient.GetStringAsync(_themesUrl, cancellationToken).ConfigureAwait(false);
        var repos = ExtractRepos(html);
        if (_maxThemes is int limit)
        {
            repos = repos.Take(limit).ToList();
        }

        var catalog = new ThemeCatalog
        {
            ScrapedAt = DateTimeOffset.UtcNow,
            SourceUrl = _themesUrl,
        };

        foreach (var repo in repos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var theme = await TryBuildThemeAsync(repo, cancellationToken).ConfigureAwait(false);
            if (theme is not null && theme.Backgrounds.Count > 0)
            {
                catalog.Themes.Add(theme);
            }
        }

        return catalog;
    }

    internal static IReadOnlyList<GitHubRepo> ExtractRepos(string html)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var repos = new List<GitHubRepo>();

        foreach (Match match in GitHubRepoRegex.Matches(html))
        {
            var owner = match.Groups["owner"].Value;
            var repo = match.Groups["repo"].Value.TrimEnd('/');
            if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                repo = repo[..^4];
            }

            // Skip compare/blob/tree path tails accidentally captured as repo names.
            if (repo.Contains('.', StringComparison.Ordinal) && !repo.Contains('-', StringComparison.Ordinal)
                && repo is not ("omarchy" or "omarchy-site"))
            {
                // still allow normal repo names with dots
            }

            var key = $"{owner}/{repo}";
            if (IgnoredRepos.Contains(key) || key.Contains("/compare", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Drop path segments that aren't repos (e.g. omarchy-site/compare).
            if (repo.Equals("compare", StringComparison.OrdinalIgnoreCase)
                || repo.Equals("blob", StringComparison.OrdinalIgnoreCase)
                || repo.Equals("tree", StringComparison.OrdinalIgnoreCase)
                || repo.Equals("issues", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!seen.Add(key))
            {
                continue;
            }

            repos.Add(new GitHubRepo(owner, repo, $"https://github.com/{owner}/{repo}"));
        }

        return repos;
    }

    private async Task<Theme?> TryBuildThemeAsync(GitHubRepo repo, CancellationToken cancellationToken)
    {
        var apiUrl = $"https://api.github.com/repos/{repo.Owner}/{repo.Name}/contents/backgrounds";
        using var response = await _httpClient.GetAsync(apiUrl, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var entries = await response.Content
            .ReadFromJsonAsync<List<GitHubContentItem>>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (entries is null || entries.Count == 0)
        {
            return null;
        }

        var themeName = HumanizeThemeName(repo.Name);
        var theme = new Theme
        {
            Id = $"{repo.Owner}/{repo.Name}".ToLowerInvariant(),
            Name = themeName,
            RepoUrl = repo.HtmlUrl,
        };

        foreach (var entry in entries)
        {
            if (!string.Equals(entry.Type, "file", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var extension = Path.GetExtension(entry.Name);
            if (!ImageExtensions.Contains(extension))
            {
                continue;
            }

            var imageUrl = entry.DownloadUrl
                ?? $"https://raw.githubusercontent.com/{repo.Owner}/{repo.Name}/HEAD/backgrounds/{entry.Name}";

            theme.Backgrounds.Add(new BackgroundImage
            {
                Id = $"{theme.Id}:{entry.Name}",
                FileName = entry.Name,
                ImageUrl = imageUrl,
                Attribution = $"{themeName} — {repo.HtmlUrl}",
            });
        }

        return theme.Backgrounds.Count == 0 ? null : theme;
    }

    private static string HumanizeThemeName(string repoName)
    {
        var name = repoName;
        if (name.StartsWith("omarchy-", StringComparison.OrdinalIgnoreCase))
        {
            name = name["omarchy-".Length..];
        }

        if (name.EndsWith("-theme", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^"-theme".Length];
        }

        name = name.Replace('-', ' ').Replace('_', ' ').Trim();
        if (name.Length == 0)
        {
            return repoName;
        }

        return string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    internal sealed record GitHubRepo(string Owner, string Name, string HtmlUrl);

    private sealed class GitHubContentItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("download_url")]
        public string? DownloadUrl { get; set; }
    }
}
