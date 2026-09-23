# Shell

## Purpose
WinUI 3 application UI for browsing Omarchy theme backgrounds and applying a selection to desktop + lock screen.

## UX flow
1. On load (and Refresh): `CatalogService.RefreshAsync()` — scrape or last-good cache
2. Theme list → background grid
3. Selecting a background downloads via `FileBackgroundStore` and shows preview + attribution/source link
4. Apply → `IWallpaperApplier.ApplyAsync` (desktop + lock screen)

## API / wiring
- Composition root: `AppServices` constructs `ThemeScraper`, `CatalogCache`, `CatalogService`, `FileBackgroundStore`, `WallpaperApplier`
- First refresh uses `maxThemes: 15` to stay under unauthenticated GitHub API rate limits; cached catalog remains available afterward

## Dependencies
- Catalog, Scraper, Cache, BackgroundStore, Wallpaper
- Package capability: `internetClient`

## Project
`src/OmarchyBackgrounds.App`
