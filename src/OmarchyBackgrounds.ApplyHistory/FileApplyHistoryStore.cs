using System.Text.Json;

namespace OmarchyBackgrounds.ApplyHistory;

public sealed class FileApplyHistoryStore : IApplyHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _historyPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileApplyHistoryStore(string? rootDirectory = null)
    {
        var root = rootDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OmarchyBackgrounds");
        Directory.CreateDirectory(root);
        _historyPath = Path.Combine(root, "apply-history.json");
    }

    public string HistoryPath => _historyPath;

    public async Task RecordAsync(AppliedThemeEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.ThemeId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var items = (await LoadEntriesAsync(cancellationToken).ConfigureAwait(false)).ToList();
            items.RemoveAll(i => string.Equals(i.ThemeId, entry.ThemeId, StringComparison.OrdinalIgnoreCase));

            var recorded = new AppliedThemeEntry
            {
                ThemeId = entry.ThemeId,
                Name = entry.Name,
                RepoUrl = entry.RepoUrl,
                AppliedAt = entry.AppliedAt == default ? DateTimeOffset.UtcNow : entry.AppliedAt,
            };
            items.Insert(0, recorded);
            await SaveEntriesAsync(items, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<AppliedThemeEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadEntriesAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<AppliedThemeEntry>> LoadEntriesAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_historyPath))
        {
            return Array.Empty<AppliedThemeEntry>();
        }

        await using var stream = File.OpenRead(_historyPath);
        var items = await JsonSerializer.DeserializeAsync<List<AppliedThemeEntry>>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return items ?? new List<AppliedThemeEntry>();
    }

    private async Task SaveEntriesAsync(IReadOnlyList<AppliedThemeEntry> items, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_historyPath)!;
        Directory.CreateDirectory(directory);
        var tempPath = _historyPath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, items, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Copy(tempPath, _historyPath, overwrite: true);
        File.Delete(tempPath);
    }
}
