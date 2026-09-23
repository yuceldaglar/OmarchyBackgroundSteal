using System.Text.Json;
using OmarchyBackgrounds.Catalog;

namespace OmarchyBackgrounds.Cache;

public sealed class CatalogCache : ICatalogCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _catalogPath;

    public CatalogCache(string? rootDirectory = null)
    {
        var root = rootDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OmarchyBackgrounds");
        Directory.CreateDirectory(root);
        _catalogPath = Path.Combine(root, "catalog.json");
    }

    public string CatalogPath => _catalogPath;

    public async Task<ThemeCatalog?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_catalogPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(_catalogPath);
        return await JsonSerializer.DeserializeAsync<ThemeCatalog>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(ThemeCatalog catalog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var directory = Path.GetDirectoryName(_catalogPath)!;
        Directory.CreateDirectory(directory);

        var tempPath = _catalogPath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, catalog, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        // Atomic replace: never delete last-good until the new file is fully written.
        File.Copy(tempPath, _catalogPath, overwrite: true);
        File.Delete(tempPath);
    }
}
