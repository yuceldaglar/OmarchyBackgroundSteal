using OmarchyBackgrounds.Catalog;

namespace OmarchyBackgrounds.BackgroundStore;

public interface IBackgroundStore
{
    Task<string> GetLocalPathAsync(BackgroundImage background, CancellationToken cancellationToken = default);
}
