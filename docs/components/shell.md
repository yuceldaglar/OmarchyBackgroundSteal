# Shell

## Purpose
WinUI 3 application UI for browsing Omarchy theme backgrounds and applying a selection to desktop + lock screen.

## UX flow
1. On load (and Refresh): `CatalogService.RefreshAsync()` — full theme list from omarchy.org (or last-good cache)
2. Theme list shows **all** scraped themes
3. Selecting a theme: `EnsureBackgroundsAsync` (GitHub Contents API once) → background grid + thumbnails
4. Selecting a background downloads via `FileBackgroundStore` and shows preview + attribution/source link
5. Apply → `IWallpaperApplier.ApplyAsync` (desktop + lock screen)

## API / wiring
- Composition root: `AppServices` constructs `ThemeScraper`, `CatalogCache`, `CatalogService`, `FileBackgroundStore`, `WallpaperApplier`
- Backgrounds are loaded lazily per theme to avoid GitHub rate limits on catalog refresh

## Dependencies
- Catalog, Scraper, Cache, BackgroundStore, Wallpaper
- Package capability: `internetClient`

## Project
`src/OmarchyBackgrounds.App`
