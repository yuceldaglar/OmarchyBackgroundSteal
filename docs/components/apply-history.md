# Apply History

## Purpose
Remember which Omarchy **themes** the user successfully applied, newest first, so they can reopen a theme’s background grid without searching the full catalog.

## API boundary
- `IApplyHistoryStore`
  - `RecordAsync(AppliedThemeEntry)` — insert/move theme to top (dedupe by `ThemeId`)
  - `ListAsync()` — newest-first list (empty when no file)
- `AppliedThemeEntry`: `ThemeId`, `Name`, `RepoUrl`, `AppliedAt`
- Default path: `%LocalAppData%\OmarchyBackgrounds\apply-history.json`
- Optional root directory constructor arg for tests

## Behavior
- Forever retention (no prune)
- Re-recording an existing theme moves it to the top with a fresh `AppliedAt`
- Atomic write via temp file then replace
- Does not depend on Catalog, Scraper, or Wallpaper

## Dependencies
- System.Text.Json only

## Project
`src/OmarchyBackgrounds.ApplyHistory`
