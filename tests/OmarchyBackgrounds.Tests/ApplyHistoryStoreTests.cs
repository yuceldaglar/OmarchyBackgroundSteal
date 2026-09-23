using OmarchyBackgrounds.ApplyHistory;

namespace OmarchyBackgrounds.Tests;

public class ApplyHistoryStoreTests
{
    [Fact]
    public async Task ListAsync_returns_empty_when_file_missing()
    {
        var dir = CreateTempDir();
        try
        {
            var store = new FileApplyHistoryStore(dir);
            var items = await store.ListAsync();
            Assert.Empty(items);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task RecordAsync_dedupes_to_top_newest_first()
    {
        var dir = CreateTempDir();
        try
        {
            var store = new FileApplyHistoryStore(dir);

            await store.RecordAsync(new AppliedThemeEntry
            {
                ThemeId = "a/theme-one",
                Name = "One",
                RepoUrl = "https://github.com/a/theme-one",
                AppliedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            });
            await store.RecordAsync(new AppliedThemeEntry
            {
                ThemeId = "b/theme-two",
                Name = "Two",
                RepoUrl = "https://github.com/b/theme-two",
                AppliedAt = DateTimeOffset.Parse("2026-01-02T00:00:00Z"),
            });
            await store.RecordAsync(new AppliedThemeEntry
            {
                ThemeId = "a/theme-one",
                Name = "One",
                RepoUrl = "https://github.com/a/theme-one",
                AppliedAt = DateTimeOffset.Parse("2026-01-03T00:00:00Z"),
            });

            var items = await store.ListAsync();
            Assert.Equal(2, items.Count);
            Assert.Equal("a/theme-one", items[0].ThemeId);
            Assert.Equal(DateTimeOffset.Parse("2026-01-03T00:00:00Z"), items[0].AppliedAt);
            Assert.Equal("b/theme-two", items[1].ThemeId);
            Assert.True(File.Exists(store.HistoryPath));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "OmarchyBackgroundsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
