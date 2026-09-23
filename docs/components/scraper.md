# Theme Scraper

## Purpose
Discover Omarchy community themes and their wallpaper files by scraping [omarchy.org/themes](https://omarchy.org/themes/) and resolving each theme’s GitHub `backgrounds/` folder.

## API boundary
- `IThemeScraper.ScrapeAsync()` → `ThemeCatalog`
- Implementation: `ThemeScraper`
  - Parses unique `github.com/{owner}/{repo}` links from the themes HTML
  - Skips site repos (`omacom/omarchy`, compare links, etc.)
  - Calls GitHub Contents API: `/repos/{owner}/{repo}/contents/backgrounds`
  - Keeps image files only (`.png`, `.jpg`, `.jpeg`, `.webp`, `.bmp`, `.gif`)
  - Uses `download_url` from GitHub when present
- Optional `maxThemes` constructor arg for smoke tests / rate-limit control

## CatalogService
- `CatalogService.RefreshAsync()` (in Catalog project)
  - On success: scrape → save cache → return catalog
  - On failure: return last-good cache with `UsedCacheFallback = true` and `ErrorMessage`
  - If no cache: empty catalog + error message

## Dependencies
- `OmarchyBackgrounds.Catalog` models + `IThemeScraper`
- `HttpClient` (caller supplies; must send a User-Agent for GitHub)

## Project
`src/OmarchyBackgrounds.Scraper` (+ `CatalogService` in `src/OmarchyBackgrounds.Catalog`)
