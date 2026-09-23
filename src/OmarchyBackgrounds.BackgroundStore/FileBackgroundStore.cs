using System.Security.Cryptography;
using System.Text;
using OmarchyBackgrounds.Catalog;

namespace OmarchyBackgrounds.BackgroundStore;

public sealed class FileBackgroundStore : IBackgroundStore
{
    private readonly HttpClient _httpClient;
    private readonly string _imagesDirectory;

    public FileBackgroundStore(HttpClient httpClient, string? rootDirectory = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        var root = rootDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OmarchyBackgrounds");
        _imagesDirectory = Path.Combine(root, "images");
        Directory.CreateDirectory(_imagesDirectory);
    }

    public string ImagesDirectory => _imagesDirectory;

    public async Task<string> GetLocalPathAsync(
        BackgroundImage background,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(background);
        ArgumentException.ThrowIfNullOrWhiteSpace(background.ImageUrl);

        var fileName = BuildSafeFileName(background);
        var localPath = Path.Combine(_imagesDirectory, fileName);
        if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
        {
            return localPath;
        }

        var bytes = await _httpClient.GetByteArrayAsync(new Uri(background.ImageUrl), cancellationToken)
            .ConfigureAwait(false);
        var tempPath = localPath + ".tmp";
        await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken).ConfigureAwait(false);
        File.Copy(tempPath, localPath, overwrite: true);
        File.Delete(tempPath);
        return localPath;
    }

    private static string BuildSafeFileName(BackgroundImage background)
    {
        var extension = Path.GetExtension(background.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = Path.GetExtension(new Uri(background.ImageUrl).AbsolutePath);
        }

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".img";
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(background.Id))).ToLowerInvariant();
        var shortHash = hash[..16];
        var baseName = Path.GetFileNameWithoutExtension(background.FileName);
        baseName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        baseName = baseName.Replace('@', '_').Replace('#', '_');
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "background";
        }

        if (baseName.Length > 40)
        {
            baseName = baseName[..40];
        }

        return $"{baseName}_{shortHash}{extension.ToLowerInvariant()}";
    }
}
