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
- Lock screen (tried in order):
  1. Copy to `%LocalAppData%\OmarchyBackgrounds\applied\lockscreen_*` (unique name)
  2. WinRT `UserProfilePersonalizationSettings.TrySetLockScreenImageAsync` / `LockScreen.SetImageFileAsync` (best with package identity)
  3. `PersonalizationCSP` registry (`LockScreenImagePath` / `Url` / `Status`) under HKCU, then HKLM if writable
- Unpackaged debug builds often fail WinRT lock-screen APIs; the registry fallback covers many Home/Pro setups without admin.

## Dependencies
- Windows desktop (user32)
- Windows Runtime user profile APIs
- Microsoft.Win32 registry
- No dependency on Catalog, Scraper, or BackgroundStore

## Project
`src/OmarchyBackgrounds.Wallpaper`
