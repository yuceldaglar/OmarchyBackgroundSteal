# Wallpaper

## Purpose
Apply a local image file as the Windows **desktop wallpaper** and **lock screen** image.

## API boundary
- `IWallpaperApplier`
  - `SetDesktopAsync(string imagePath)`
  - `SetLockScreenAsync(string imagePath)`
  - `ApplyAsync(string imagePath)` — desktop then lock screen
- Input is an absolute or resolvable **local file path**. Remote URLs are out of scope (use BackgroundStore first).

## Implementation notes
- Desktop: Win32 `SystemParametersInfoW` with `SPI_SETDESKWALLPAPER` and `SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE`.
- Lock screen: WinRT `UserProfilePersonalizationSettings.TrySetLockScreenImageAsync` (requires package identity / supported SKU).
- Missing files and unsupported lock-screen personalization throw; callers surface errors to the UI.

## Dependencies
- Windows desktop (user32)
- Windows Runtime user profile APIs
- No dependency on Catalog, Scraper, or BackgroundStore

## Project
`src/OmarchyBackgrounds.Wallpaper`
