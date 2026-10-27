# Windows portable release

Run from the repository root:

```powershell
.\pipeline\packaging\Build-Windows.ps1
```

Requires the existing local Godot 4.7.2 .NET engine and .NET SDK. The first run
also needs the official `Godot_v4.7.2-stable_mono_export_templates.tpz` in
`pipeline/tools/downloads/`. Download it from:

https://github.com/godotengine/godot-builds/releases/tag/4.7.2-stable

The script checks its SHA-256 against the official release asset digest before
extracting the Windows x64 release template into the project-local engine's
template directory. The .NET runtime package may require NuGet access on the
first export. Players do not need the SDK or runtime installed separately.

Outputs:

- `pipeline/generated/releases/TorBonk-<timestamp>-Windows-x64.zip`: player download.
- The matching directory contains `TorBonk/` and a SHA-256 file manifest.
- `pipeline/work/releases/<timestamp>/`: isolated project and build/import/export logs.

Each invocation uses new directories. It copies current on-disk game content,
including files not committed to Git, and reuses the imported asset cache.
It never invokes Git, deletes old releases, or changes the source project.
Do not edit assets while the initial copy is running if a consistent snapshot
is required. The export preset is copied into the isolated project only.
The original application name and save-folder identity are retained.
The copied menu also gets a `--package-check --map=<id>` startup hook for
release verification. This hook is absent from the source game project and
does not affect a normal double-click launch.

The preset exports all Godot resources and explicitly includes JSON, since
the game constructs resource paths from code and catalog entries. The copied
C# project requests a self-contained release with the .NET runtime included.
Use `-SkipZip` to retain the exported folder without making a ZIP.

Before sharing a new release, test the exported executable from another folder
with a fresh user profile, then test it on another Windows x64 computer without
the development tools. Check all four maps, each playable class, audio, upgrades,
Collections, fullscreen, restart, and settings/save persistence. Existing source
smoke tests that write receipts relative to `res://../` are not portable release
tests and should not be used unchanged against the packaged game.

Automated package checks:

```powershell
.\pipeline\packaging\Test-Windows.ps1 -GameFolder <exported-TorBonk-folder> -StagedProject <isolated-project-folder>
```

This inventories the exported pack with the editor, then launches the actual
release executable headlessly for the menu, all maps, and every class on
Korriban, with test saves isolated under the build's `release-checks/appdata`.
The editor-only pack inspection can report C# autoload instantiation errors
because it is not running the published runtime; its resource inventory is
separate from the actual executable's startup checks, which reject errors.

Keep the entire extracted TorBonk folder together. Updates can be extracted
into a new folder, preserving application-data saves. The main repository's
SWTOR asset permission notice still applies; packaging grants no new rights.
