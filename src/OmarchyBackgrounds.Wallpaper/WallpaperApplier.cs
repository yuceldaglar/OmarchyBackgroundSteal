using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.Storage;
using Windows.System.UserProfile;

namespace OmarchyBackgrounds.Wallpaper;

public sealed class WallpaperApplier : IWallpaperApplier
{
    private const uint SpiSetDeskWallpaper = 0x0014;
    private const uint SpifUpdateIniFile = 0x01;
    private const uint SpifSendWinIniChange = 0x02;

    private static readonly string AppliedDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OmarchyBackgrounds",
        "applied");

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

        // Stable copy with a unique name — Windows often ignores lock-screen updates to the same path.
        var appliedPath = CopyForLockScreen(fullPath);
        var errors = new List<string>();

        if (await TrySetLockScreenWinRtAsync(appliedPath, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        errors.Add("WinRT personalization API unavailable or returned false (common for unpackaged apps).");

        if (TrySetLockScreenViaPersonalizationCsp(appliedPath))
        {
            return;
        }

        errors.Add("PersonalizationCSP registry write failed or was denied.");

        throw new InvalidOperationException(
            "Could not set lock screen. " + string.Join(" ", errors) +
            " On unpackaged builds, Windows may require Settings to allow custom lock screens.");
    }

    public async Task ApplyAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        await SetDesktopAsync(imagePath, cancellationToken).ConfigureAwait(false);

        try
        {
            await SetLockScreenAsync(imagePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Desktop wallpaper was applied, but lock screen failed: {ex.Message}", ex);
        }
    }

    private static string CopyForLockScreen(string sourcePath)
    {
        Directory.CreateDirectory(AppliedDirectory);
        foreach (var old in Directory.EnumerateFiles(AppliedDirectory, "lockscreen_*"))
        {
            try
            {
                File.Delete(old);
            }
            catch
            {
                // Best-effort cleanup of previous lock-screen copies.
            }
        }

        var extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var destPath = Path.Combine(AppliedDirectory, $"lockscreen_{Guid.NewGuid():N}{extension}");
        File.Copy(sourcePath, destPath, overwrite: true);
        return destPath;
    }

    private static async Task<bool> TrySetLockScreenWinRtAsync(string imagePath, CancellationToken cancellationToken)
    {
        try
        {
            if (!UserProfilePersonalizationSettings.IsSupported())
            {
                return false;
            }

            var file = await StorageFile.GetFileFromPathAsync(imagePath).AsTask(cancellationToken)
                .ConfigureAwait(false);
            var settings = UserProfilePersonalizationSettings.Current;
            if (await settings.TrySetLockScreenImageAsync(file).AsTask(cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            // Alternate WinRT entry point used by some desktop apps.
            await LockScreen.SetImageFileAsync(file).AsTask(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TrySetLockScreenViaPersonalizationCsp(string imagePath)
    {
        // Prefer HKCU so we do not require elevation. Falls back to HKLM if writable (admin).
        if (TryWritePersonalizationCsp(Registry.CurrentUser, imagePath))
        {
            return true;
        }

        try
        {
            return TryWritePersonalizationCsp(Registry.LocalMachine, imagePath);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryWritePersonalizationCsp(RegistryKey root, string imagePath)
    {
        try
        {
            using var key = root.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP", writable: true);
            if (key is null)
            {
                return false;
            }

            key.SetValue("LockScreenImagePath", imagePath, RegistryValueKind.String);
            key.SetValue("LockScreenImageUrl", imagePath, RegistryValueKind.String);
            key.SetValue("LockScreenImageStatus", 1, RegistryValueKind.DWord);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfoW(uint uiAction, uint uiParam, string pvParam, uint fWinIni);
}
