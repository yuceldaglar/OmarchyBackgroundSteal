# Theme Scraper

## Purpose
Discover Omarchy community themes and their wallpaper files by scraping [omarchy.org/themes](https://omarchy.org/themes/) and resolving each theme’s GitHub `backgrounds/` folder **on demand**.

## API boundary
- `IThemeScraper.ScrapeAsync()` → `ThemeCatalog`
  - Parses unique `github.com/{owner}/{repo}` links from the themes HTML
  - Skips site repos (`omacom/omarchy`, compare links, etc.)
  - Returns **all** themes with empty `Backgrounds` (no GitHub API calls)
- `IThemeScraper.LoadBackgroundsAsync(Theme)`
  - Calls GitHub Contents API: `/repos/{owner}/{repo}/contents/backgrounds`
  - Keeps image files only (`.png`, `.jpg`, `.jpeg`, `.webp`, `.bmp`, `.gif`)
  - Uses `download_url` from GitHub when present

## CatalogService
- `RefreshAsync()` — scrape full theme list, merge any previously cached backgrounds, save cache
- `EnsureBackgroundsAsync(Theme)` — load backgrounds once per theme, then persist catalog cache
- On scrape failure: return last-good cache with `UsedCacheFallback = true`

## Dependencies
- `OmarchyBackgrounds.Catalog` models + `IThemeScraper`
- `HttpClient` (caller supplies; must send a User-Agent for GitHub)

## Project
`src/OmarchyBackgrounds.Scraper` (+ `CatalogService` in `src/OmarchyBackgrounds.Catalog`)
