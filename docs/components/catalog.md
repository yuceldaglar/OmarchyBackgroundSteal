# Catalog

## Purpose
Shared theme/background models, cache/scraper contracts, and refresh orchestration.

## API boundary
- Models: `ThemeCatalog`, `Theme`, `BackgroundImage`
- `ICatalogCache`, `IThemeScraper`
- `CatalogService.RefreshAsync()` → `CatalogRefreshResult` (fresh scrape or last-good fallback)

## Dependencies
- Implementations live in Cache / Scraper projects; App wires them through `AppServices`

## Project
`src/OmarchyBackgrounds.Catalog`
