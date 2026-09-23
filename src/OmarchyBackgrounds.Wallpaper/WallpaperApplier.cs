using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.System.UserProfile;

namespace OmarchyBackgrounds.Wallpaper;

public sealed class WallpaperApplier : IWallpaperApplier
{
    private const uint SpiSetDeskWallpaper = 0x0014;
    private const uint SpifUpdateIniFile = 0x01;
    private const uint SpifSendWinIniChange = 0x02;

    public Task SetDesktopAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.GetFullPath(imagePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Wallpaper image was not found.", fullPath);
        }

        if (!SystemParametersInfoW(SpiSetDeskWallpaper, 0, fullPath, SpifUpdateIniFile | SpifSendWinIniChange))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set desktop wallpaper.");
        }

        return Task.CompletedTask;
    }

    public async Task SetLockScreenAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.GetFullPath(imagePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Lock screen image was not found.", fullPath);
        }

        if (!UserProfilePersonalizationSettings.IsSupported())
        {
            throw new NotSupportedException("Lock screen personalization is not supported on this system.");
        }

        var file = await StorageFile.GetFileFromPathAsync(fullPath).AsTask(cancellationToken).ConfigureAwait(false);
        var settings = UserProfilePersonalizationSettings.Current;
        var success = await settings.TrySetLockScreenImageAsync(file).AsTask(cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            throw new InvalidOperationException("Failed to set lock screen image.");
        }
    }

    public async Task ApplyAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        await SetDesktopAsync(imagePath, cancellationToken).ConfigureAwait(false);
        await SetLockScreenAsync(imagePath, cancellationToken).ConfigureAwait(false);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfoW(uint uiAction, uint uiParam, string pvParam, uint fWinIni);
}
