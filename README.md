# Omarchy Backgrounds for Windows

WinUI 3 app to browse Omarchy theme backgrounds and apply them to desktop + lock screen.

## Run

Open **`OmarchyBackgroundSteal.sln`** in Visual Studio (**not** `.slnx` — that file was removed because VS kept selecting Any CPU, which WinUI does not support).

1. Toolbar: **Debug** + **x64**
2. Right-click **OmarchyBackgrounds.App** → **Set as Startup Project**
3. Press **F5**

If an old error dialog still appears: close Visual Studio completely, confirm the repo has no `.vs` folder, then open `OmarchyBackgroundSteal.sln` again.

Or from the repo root:

```powershell
.\run.ps1
```

```powershell
dotnet run --project .\src\OmarchyBackgrounds.App\OmarchyBackgrounds.App.csproj -c Debug -p:Platform=x64
```

The app runs **unpackaged** so Visual Studio can debug it without the Single-project MSIX Packaging Tools extension. Desktop wallpaper works; lock screen may require package identity later.

## Build & test

```powershell
dotnet build .\OmarchyBackgroundSteal.sln -c Debug
dotnet test .\OmarchyBackgroundSteal.sln -c Debug
```

## Docs

- Product idea: [docs/ideas/omarchy-backgrounds-windows.md](docs/ideas/omarchy-backgrounds-windows.md)
- Components: [docs/components/README.md](docs/components/README.md)
