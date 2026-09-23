# Catalog Cache

## Purpose
Persist the last successfully scraped theme catalog so the app still works when a refresh fails or the machine is offline.

## API boundary
- `ICatalogCache` (defined in Catalog)
  - `LoadAsync()` → `ThemeCatalog?` (`null` when no cache file exists)
  - `SaveAsync(ThemeCatalog)` — writes `catalog.json`
- Default location: `%LocalAppData%\OmarchyBackgrounds\catalog.json`
- Constructor accepts an optional root directory (tests / custom installs)

## Behavior
- Missing file → `null` (not an empty catalog object)
- Save writes via a temp file then replaces the destination so a crash mid-write does not wipe last-good
- JSON uses camelCase property names

## Dependencies
- `OmarchyBackgrounds.Catalog` models
- System.Text.Json

## Project
`src/OmarchyBackgrounds.Cache`
