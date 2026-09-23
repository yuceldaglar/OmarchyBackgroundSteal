using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

    private static readonly Regex GitHubBlobPathRegex = new(
        @"/blob/(?<branch>[^/]+)/backgrounds/(?<file>[^""?\s]+)",
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

    private static readonly string[] PreferredRefs = ["main", "master"];

    private readonly HttpClient _httpClient;
    private readonly string _themesUrl;
    private readonly string? _githubToken;

    public ThemeScraper(HttpClient httpClient, string? themesUrl = null, string? githubToken = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _themesUrl = string.IsNullOrWhiteSpace(themesUrl) ? DefaultThemesUrl : themesUrl;
        _githubToken = string.IsNullOrWhiteSpace(githubToken)
            ? Environment.GetEnvironmentVariable("OMARCHY_GITHUB_TOKEN")
              ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            : githubToken;

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

        // Prefer jsDelivr — does not consume GitHub REST API quota.
        if (await TryLoadFromJsDelivrAsync(theme, owner, name, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        // Fallback: scrape GitHub HTML directory pages.
        if (await TryLoadFromGitHubHtmlAsync(theme, owner, name, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        // Last resort: GitHub Contents API (optional token raises limit from ~60/hr to 5000/hr).
        await TryLoadFromGitHubApiAsync(theme, owner, name, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> TryLoadFromJsDelivrAsync(
        Theme theme,
        string owner,
        string name,
        CancellationToken cancellationToken)
    {
        foreach (var gitRef in PreferredRefs)
        {
            var url = $"https://data.jsdelivr.com/v1/packages/gh/{owner}/{name}@{gitRef}";
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var files = new List<(string FileName, string ImageUrl)>();
            CollectBackgroundFilesFromJsDelivr(
                document.RootElement,
                owner,
                name,
                gitRef,
                currentDir: null,
                files);

            if (files.Count == 0)
            {
                continue;
            }

            foreach (var file in files.DistinctBy(f => f.FileName, StringComparer.OrdinalIgnoreCase))
            {
                theme.Backgrounds.Add(new BackgroundImage
                {
                    Id = $"{theme.Id}:{file.FileName}",
                    FileName = file.FileName,
                    ImageUrl = file.ImageUrl,
                    Attribution = $"{theme.Name} — {theme.RepoUrl}",
                });
            }

            return theme.Backgrounds.Count > 0;
        }

        return false;
    }

    private static void CollectBackgroundFilesFromJsDelivr(
        JsonElement node,
        string owner,
        string name,
        string gitRef,
        string? currentDir,
        List<(string FileName, string ImageUrl)> files)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            var type = node.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
            var entryName = node.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

            if (string.Equals(type, "file", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(entryName)
                && string.Equals(currentDir, "backgrounds", StringComparison.OrdinalIgnoreCase))
            {
                var extension = Path.GetExtension(entryName);
                if (ImageExtensions.Contains(extension))
                {
                    var imageUrl =
                        $"https://cdn.jsdelivr.net/gh/{owner}/{name}@{gitRef}/backgrounds/{entryName}";
                    files.Add((entryName, imageUrl));
                }
            }

            if (node.TryGetProperty("files", out var children)
                && children.ValueKind == JsonValueKind.Array)
            {
                var nextDir = currentDir;
                if (string.Equals(type, "directory", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(entryName))
                {
                    nextDir = currentDir is null ? entryName : $"{currentDir}/{entryName}";
                }

                foreach (var child in children.EnumerateArray())
                {
                    CollectBackgroundFilesFromJsDelivr(child, owner, name, gitRef, nextDir, files);
                }
            }

            return;
        }

        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in node.EnumerateArray())
            {
                CollectBackgroundFilesFromJsDelivr(child, owner, name, gitRef, currentDir, files);
            }
        }
    }

    private async Task<bool> TryLoadFromGitHubHtmlAsync(
        Theme theme,
        string owner,
        string name,
        CancellationToken cancellationToken)
    {
        foreach (var branch in PreferredRefs)
        {
            var url = $"https://github.com/{owner}/{name}/tree/{branch}/backgrounds";
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in GitHubBlobPathRegex.Matches(html))
            {
                var fileName = Uri.UnescapeDataString(match.Groups["file"].Value);
                if (fileName.Contains('/', StringComparison.Ordinal))
                {
                    continue;
                }

                var extension = Path.GetExtension(fileName);
                if (!ImageExtensions.Contains(extension) || !seen.Add(fileName))
                {
                    continue;
                }

                var matchedBranch = match.Groups["branch"].Value;
                theme.Backgrounds.Add(new BackgroundImage
                {
                    Id = $"{theme.Id}:{fileName}",
                    FileName = fileName,
                    ImageUrl = $"https://raw.githubusercontent.com/{owner}/{name}/{matchedBranch}/backgrounds/{fileName}",
                    Attribution = $"{theme.Name} — {theme.RepoUrl}",
                });
            }

            if (theme.Backgrounds.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private async Task TryLoadFromGitHubApiAsync(
        Theme theme,
        string owner,
        string name,
        CancellationToken cancellationToken)
    {
        var apiUrl = $"https://api.github.com/repos/{owner}/{name}/contents/backgrounds";
        using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        if (!string.IsNullOrWhiteSpace(_githubToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _githubToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        if ((int)response.StatusCode == 403 || (int)response.StatusCode == 429)
        {
            throw new InvalidOperationException(
                "GitHub API rate limit exceeded. Background listing now prefers jsDelivr; " +
                "if this persists, set OMARCHY_GITHUB_TOKEN (or GITHUB_TOKEN) to a personal access token.");
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
