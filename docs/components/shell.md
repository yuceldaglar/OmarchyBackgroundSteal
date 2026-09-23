# Shell

## Purpose
WinUI 3 application UI for browsing Omarchy theme backgrounds and applying a selection to desktop + lock screen.

## UX flow
1. On load (and Refresh): `CatalogService.RefreshAsync()` — full theme list from omarchy.org (or last-good cache)
2. Theme list shows **all** scraped themes
3. Selecting a theme: `EnsureBackgroundsAsync` (GitHub Contents API once) → background grid + thumbnails
4. Selecting a background downloads via `FileBackgroundStore` and shows preview + attribution/source link
5. Apply desktop, lock screen, or both via separate buttons → `IWallpaperApplier`

## API / wiring
- Composition root: `AppServices` constructs `ThemeScraper`, `CatalogCache`, `CatalogService`, `FileBackgroundStore`, `WallpaperApplier`
- Backgrounds are loaded lazily per theme via **jsDelivr** (avoids GitHub API rate limits); optional `OMARCHY_GITHUB_TOKEN` for API fallback

## Dependencies
- Catalog, Scraper, Cache, BackgroundStore, Wallpaper
- Package capability: `internetClient`

## Project
`src/OmarchyBackgrounds.App`
