# Recent Applied Themes

## Problem Statement
How might we let Windows users reopen Omarchy themes they’ve already applied — newest first — without searching the full catalog again?

## Recommended Direction
**New ApplyHistory component + Shell “Recent” list under Themes (dedupe-to-top).**

On any successful apply (desktop, lock screen, or both), record the **whole theme** (id, name, repo URL, timestamp). Persist forever in `%LocalAppData%\OmarchyBackgrounds\apply-history.json`. If the theme is already in history, **move it to the top** instead of duplicating. The Shell shows a Recent list below the Themes list; selecting an entry selects that theme in the main catalog list so the existing background grid / preview / apply flow runs unchanged.

Keep Wallpaper, Catalog, Scraper, and BackgroundStore APIs as they are. History is an observer of successful apply, not part of wallpaper plumbing.

## Key Assumptions to Validate
- [ ] “Theme of the background just applied” is the right history grain — apply 5 themes and see if Recent feels useful
- [ ] Dedupe-to-top matches expectation (not a raw event log) — re-apply an old recent and confirm it jumps to top
- [ ] After catalog refresh, recent themes still resolve by id — delete/rename edge: show a clear miss state
- [ ] Forever retention stays snappy with dozens/hundreds of entries — no virtualization until proven slow

## MVP Scope
**In**
- New project/component: `OmarchyBackgrounds.ApplyHistory` (`IApplyHistoryStore`, JSON persistence)
- Shell: Recent list under Themes (newest first); click → select theme in ThemeList
- Record after successful desktop / lock / both apply
- Dedupe by theme id → move to top
- `docs/components/apply-history.md` + index update

**Out**
- Per-image history, separate desktop/lock histories
- Favorites / pinning UI
- Auto-prune / max-N
- Changing Wallpaper or Scraper contracts

## Not Doing (and Why)
- **Storing history inside CatalogCache** — different lifecycle from scrape cache; keep components honest
- **Recording failed applies** — noise; only success
- **Deep-linking to a specific background file** — you asked for whole theme → grid
- **Cloud sync of history** — local-only product for now

## Open Questions
- Empty Recent label/copy (“No recent themes yet”)
- Whether Recent should show a small count badge or applied-at relative time (optional polish)
