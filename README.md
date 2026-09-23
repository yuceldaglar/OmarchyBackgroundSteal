# Omarchy Backgrounds for Windows

WinUI 3 app to browse Omarchy theme backgrounds and apply them to desktop + lock screen.

## Run

From the repo root, use **one** of these (do not run bare `dotnet run` — there is no single project at the root):

```powershell
.\run.ps1
```

```powershell
dotnet run --project .\src\OmarchyBackgrounds.App\OmarchyBackgrounds.App.csproj -c Debug -p:Platform=x64
```

In Cursor / VS Code: open **Run and Debug** → **Omarchy Backgrounds**.

Startup project: `src/OmarchyBackgrounds.App` (solution: `OmarchyBackgroundSteal.sln`).

## Build & test

```powershell
dotnet build .\OmarchyBackgroundSteal.sln -c Debug
dotnet test .\OmarchyBackgroundSteal.sln -c Debug
```

## Docs

- Product idea: [docs/ideas/omarchy-backgrounds-windows.md](docs/ideas/omarchy-backgrounds-windows.md)
- Components: [docs/components/README.md](docs/components/README.md)
