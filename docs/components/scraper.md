# Theme Scraper

## Purpose
Discover Omarchy community themes and their wallpaper files by scraping [omarchy.org/themes](https://omarchy.org/themes/) and resolving each theme’s `backgrounds/` folder **on demand** without burning GitHub API quota.

## API boundary
- `IThemeScraper.ScrapeAsync()` → `ThemeCatalog`
  - Parses unique `github.com/{owner}/{repo}` links from the themes HTML
  - Returns **all** themes with empty `Backgrounds`
- `IThemeScraper.LoadBackgroundsAsync(Theme)` tries, in order:
  1. **jsDelivr** `data.jsdelivr.com` package listing + `cdn.jsdelivr.net` image URLs (default; no GitHub REST quota)
  2. GitHub HTML `/tree/{branch}/backgrounds` scrape
  3. GitHub Contents API (last resort). Optional token via `OMARCHY_GITHUB_TOKEN` or `GITHUB_TOKEN` raises the limit (~5000/hr authenticated)

## CatalogService
- `RefreshAsync()` — scrape full theme list, merge any previously cached backgrounds, save cache
- `EnsureBackgroundsAsync(Theme)` — load backgrounds once per theme, then persist catalog cache
- On scrape failure: return last-good cache with `UsedCacheFallback = true`

## Dependencies
- `OmarchyBackgrounds.Catalog` models + `IThemeScraper`
- `HttpClient` (caller supplies; must send a User-Agent)

## Project
`src/OmarchyBackgrounds.Scraper` (+ `CatalogService` in `src/OmarchyBackgrounds.Catalog`)
