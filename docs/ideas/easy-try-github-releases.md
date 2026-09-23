# Easy Try via GitHub Releases

## Problem Statement
How might we let any Windows user download and run Omarchy Backgrounds in minutes, without Visual Studio or building from source?

## Recommended Direction
Local `dotnet publish` (self-contained win-x64) → zip → GitHub Release. A small `release.ps1` on the laptop creates the artifact; upload via the GitHub Releases UI (optional: `gh release create` later). README leads with Download, not clone.

Stay unpackaged (`WindowsPackageType=None`) for MVP. Signing and Store are deferred — signing is a paid/certificate workflow, not a one-click step. SmartScreen warnings are mitigated with clear “More info → Run anyway” docs until trust upgrades matter.

## Key Assumptions to Validate
- [x] Self-contained zip runs on a PC without the .NET SDK *(publish + zip verified locally; confirm on a second machine)*
- [ ] SmartScreen “Run anyway” is acceptable with clear README copy
- [x] Release steps fit in one script you will actually run before each tag *(`release.ps1`)*

## MVP Scope
**In**
- `release.ps1` (or equivalent): publish self-contained `win-x64`, zip output
- README “Download” section pointing at latest GitHub Release
- First portable Release artifact

**Out**
- Code signing / Authenticode
- MSIX packaging
- Microsoft Store
- winget
- CI publish pipeline (until the local path is boring)

## Not Doing (and Why)
- **Store submission** — unknown effort; not needed for “try from Releases”
- **Code signing for v1** — procedure is non-trivial; revisit if downloads grow
- **Full CI pipeline first** — laptop-local is the constraint; automate after it works twice
- **Changing Wallpaper/lock-screen packaging for the release** — separate problem from distribution

## Open Questions
- Zip the whole publish folder vs. how we name the entry `.exe` in README
- Whether to also publish `win-arm64` from day one or x64 only
