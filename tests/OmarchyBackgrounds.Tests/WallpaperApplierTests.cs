using OmarchyBackgrounds.Wallpaper;

namespace OmarchyBackgrounds.Tests;

public class WallpaperApplierTests
{
    [Fact]
    public async Task SetDesktopAsync_throws_when_file_missing()
    {
        var applier = new WallpaperApplier();
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");

        await Assert.ThrowsAsync<FileNotFoundException>(() => applier.SetDesktopAsync(missing));
    }

    [Fact]
    public async Task SetLockScreenAsync_throws_when_file_missing()
    {
        var applier = new WallpaperApplier();
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");

        await Assert.ThrowsAsync<FileNotFoundException>(() => applier.SetLockScreenAsync(missing));
    }
}
