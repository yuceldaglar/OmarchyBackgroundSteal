# Shell

## Purpose
WinUI 3 application UI for browsing Omarchy theme backgrounds and applying a selection to desktop + lock screen.

## UX flow
1. On load (and Refresh): `CatalogService.RefreshAsync()` — full theme list from omarchy.org (or last-good cache)
2. Theme list shows **all** scraped themes; **Recent** under Themes shows applied themes (newest first)
3. Selecting a recent theme selects it in the Themes list (opens its background grid)
4. Selecting a theme: `EnsureBackgroundsAsync` → background grid + thumbnails
5. Selecting a background downloads via `FileBackgroundStore` and shows preview + attribution/source link
6. Apply desktop, lock screen, or both → on success, `IApplyHistoryStore.RecordAsync` (dedupe-to-top)

## API / wiring
- Composition root: `AppServices` constructs `ThemeScraper`, `CatalogCache`, `CatalogService`, `FileBackgroundStore`, `WallpaperApplier`, `FileApplyHistoryStore`
- Backgrounds are loaded lazily per theme via **jsDelivr** (avoids GitHub API rate limits); optional `OMARCHY_GITHUB_TOKEN` for API fallback

## Dependencies
- Catalog, Scraper, Cache, BackgroundStore, Wallpaper, ApplyHistory
- Package capability: `internetClient`

## Project
`src/OmarchyBackgrounds.App`
