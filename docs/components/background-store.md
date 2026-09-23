# Background Store

## Purpose
Download theme wallpaper images on demand and keep a durable local cache for preview and apply.

## API boundary
- `IBackgroundStore.GetLocalPathAsync(BackgroundImage)` → absolute local file path
- Implementation: `BackgroundStore`
  - Root: `%LocalAppData%\OmarchyBackgrounds\images\` (overridable)
  - Safe file names derived from original name + short hash of background id
  - Existing non-empty files are reused (no re-download)
  - Writes via temp file then replace

## Dependencies
- `OmarchyBackgrounds.Catalog` (`BackgroundImage`)
- `HttpClient`

## Project
`src/OmarchyBackgrounds.BackgroundStore`
