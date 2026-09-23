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

    public ThemeScraper(HttpClient httpClient, string? themesUrl = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _themesUrl = string.IsNullOrWhiteSpace(themesUrl) ? DefaultThemesUrl : themesUrl;

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

        var catalog = new ThemeCatalog
        {
            ScrapedAt = DateTimeOffset.UtcNow,
            SourceUrl = _themesUrl,
        };

        foreach (var repo in repos)
        {
            var themeName = HumanizeThemeName(repo.Name);
            catalog.Themes.Add(new Theme
            {
                Id = $"{repo.Owner}/{repo.Name}".ToLowerInvariant(),
                Name = themeName,
                RepoUrl = repo.HtmlUrl,
            });
        }

        return catalog;
    }

    public async Task LoadBackgroundsAsync(Theme theme, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(theme);
        if (theme.Backgrounds.Count > 0)
        {
            return;
        }

        if (!TryParseRepo(theme.RepoUrl, out var owner, out var name))
        {
            return;
        }

        var apiUrl = $"https://api.github.com/repos/{owner}/{name}/contents/backgrounds";
        using var response = await _httpClient.GetAsync(apiUrl, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
        var entries = await response.Content
            .ReadFromJsonAsync<List<GitHubContentItem>>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (entries is null || entries.Count == 0)
        {
            return;
        }

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
                ?? $"https://raw.githubusercontent.com/{owner}/{name}/HEAD/backgrounds/{entry.Name}";

            theme.Backgrounds.Add(new BackgroundImage
            {
                Id = $"{theme.Id}:{entry.Name}",
                FileName = entry.Name,
                ImageUrl = imageUrl,
                Attribution = $"{theme.Name} — {theme.RepoUrl}",
            });
        }
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

            var key = $"{owner}/{repo}";
            if (IgnoredRepos.Contains(key) || key.Contains("/compare", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

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

    private static bool TryParseRepo(string repoUrl, out string owner, out string name)
    {
        owner = string.Empty;
        name = string.Empty;
        var match = GitHubRepoRegex.Match(repoUrl);
        if (!match.Success)
        {
            return false;
        }

        owner = match.Groups["owner"].Value;
        name = match.Groups["repo"].Value.TrimEnd('/');
        if (name.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        return owner.Length > 0 && name.Length > 0;
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
