namespace OmarchyBackgrounds.ApplyHistory;

public interface IApplyHistoryStore
{
    Task RecordAsync(AppliedThemeEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppliedThemeEntry>> ListAsync(CancellationToken cancellationToken = default);
}
