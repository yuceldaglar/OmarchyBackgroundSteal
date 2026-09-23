# Omarchy Backgrounds for Windows

## Problem Statement
How might we let Windows users browse Omarchy community theme backgrounds and apply a chosen image to desktop and lock screen — without installing Linux or the full Omarchy stack?

## Recommended Direction
**Catalog Gallery + WinUI 3 (Windows App SDK).**

Ship a native Windows app that scrapes [omarchy.org/themes](https://omarchy.org/themes/), resolves each theme’s background assets, shows a browseable gallery, and applies the selected image to **desktop wallpaper** and **lock screen**. Persist the last successful scrape so a failed update still leaves a usable catalog.

WinUI 3 keeps the product feeling like a real Windows app and leaves room for Fluent UI polish. Architecture stays componentized from day one, with each component documented under `docs/`, so later “theme colors and other properties” plug in without rewriting browse/apply.

## Key Assumptions to Validate
- [x] Theme pages/repos expose `backgrounds/` (or equivalent) in a stable, scrapable way — scrape ~10 themes and open every image URL
- [ ] Wallpaper licenses allow in-app preview/cache with attribution (prefer cache over redistributing packs) — spot-check 5 theme READMEs
- [ ] Lock screen can be set for a normal user on Win11 without elevation — prototype one apply path end-to-end (code path exists; confirm in packaged app run)
- [x] omarchy.org markup changes are survivable via last-scrape JSON/cache — first-run must succeed once; later runs degrade gracefully
- [ ] “Browse + apply wallpaper” is enough delight without colors in v1 — use it yourself for a week

## MVP Scope
**In**
- WinUI 3 shell: theme list → background grid → preview → Apply
- Scraper + durable last-good catalog cache
- On-demand image download/cache for preview and apply
- Apply to desktop wallpaper and lock screen
- Basic error/empty states (offline, scrape fail → use cache)
- `docs/` one page per component (purpose, API boundary, deps)

**Out**
- Full Omarchy theme install / `colors.toml` → Windows theming
- Electron / web wrapper
- Self-hosted wallpaper CDN
- Multi-monitor per-display art, slideshow, Store polish
- Sparse git clone of full theme repos (keep as future option if scrape is too fragile)

## Not Doing (and Why)
- **Full theme colors / terminal / accent sync** — different product; deferred by design so browse+apply ships
- **Sparse-clone of every theme repo** — heavier disk/network; revisit only if scrape can’t reach assets reliably
- **Mirroring all wallpapers to your own host** — ops + licensing cost before product value is proven
- **Avalonia / WPF / Electron** — WinUI 3 chosen for native Windows fit; avoid dual stacks
- **Admin-required installers or services** — keep apply path user-scoped where possible

## Open Questions
- Exact scrape strategy: HTML of themes index → per-theme git URL → raw `backgrounds/` paths — what’s the most stable link graph?
- Cache format and location (e.g. `%LocalAppData%\OmarchyBackgrounds\catalog.json` + image folder)
- Attribution UX in the gallery (theme name + source link required?)
- Package as unpackaged WinUI vs MSIX for first builds
