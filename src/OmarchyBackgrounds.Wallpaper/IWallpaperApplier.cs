namespace OmarchyBackgrounds.Wallpaper;

public interface IWallpaperApplier
{
    Task SetDesktopAsync(string imagePath, CancellationToken cancellationToken = default);

    Task SetLockScreenAsync(string imagePath, CancellationToken cancellationToken = default);

    Task ApplyAsync(string imagePath, CancellationToken cancellationToken = default);
}
