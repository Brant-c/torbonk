# Future integration — requires manual authorization

Do not follow these integration steps while B7 is active. C1 is complete as an isolated module; no merge or gameplay launch was performed.

## Copy boundary

1. After manual review and approval, copy **only `character-selection/Frontend/`** to `pipeline/godot/Frontend/`. Every runtime path is rooted at `res://Frontend/`; no rewrite is required.
2. Keep the gameplay project's existing `.csproj`, `project.godot`, launcher, scene configuration, input map and global settings. Do not copy this harness's project files, packages, `.local`, `.godot`, tools or research directories.
3. The gameplay project's normal C# source glob can compile `Frontend/Scripts/*.cs`. All classes live in `SwtorSurvivors.Frontend`; no autoload or gameplay assembly dependency is required. The compatible target is Godot.NET.Sdk 4.7.2 and net8.0.
4. Let Godot import the copied PNG, TTF, WAV and GLB resources during the authorized integration review. The model is self-contained with 24 embedded images. Character PNG copies are retained alongside it for source continuity but are not external GLB dependencies.
5. Include `Frontend/Data/*.json` as non-resource files in any export preset. Catalog paths are loaded dynamically, so include the entire `Frontend/Assets/` directory and the frontend scenes in packaged builds. None of this was export-tested in C1.

## Entry point and embedding

`Frontend/Scenes/MainMenu.tscn` is a reusable full-rect Control with `FrontendFlow.cs`. It can be instantiated under the game's own menu host without changing the game's entry scene. Its canvas fits itself to the supplied Control size. The harness points to it only for later manual review.

The six selection-page scenes provide named page surfaces owned by `FrontendFlow`; they are not independent games or standalone entry scenes. `CharacterPreview.tscn` is a separately reusable component. Call `ShowClass(artworkPath, advancedClassDefinition)` after it enters the scene tree; give it a size before adding it. A missing/empty preview path displays the supplied original class illustration. Add future model paths and idle clip names to `Frontend/Data/catalog.json`, rather than embedding model-selection branches in screens.

## Configuration contract

`SelectionConfiguration` and `AppearanceConfiguration` are ordinary C# models without a Godot dependency. The frontend configuration contains:

| Field | Meaning |
|---|---|
| `SchemaVersion` | Version 1 |
| `FactionId` | `empire` or `republic` |
| `BaseClassId` | Stable frontend class ID from catalog |
| `AdvancedClassId` | Stable advanced-class ID from catalog |
| `Appearance` | Preset and individual appearance IDs |
| `MapId` | Frontend map ID, initially `korriban` for supported content |
| `Playable` | Computed content-compatibility flag, not permission to launch |
| `PlayableStatus` | `content_available_integration_pending` or `preview_only` |

IDs are C1 integration keys, **not GOM IDs, native runtime selectors or claimed preexisting gameplay identifiers**. A future adapter must map them explicitly to the game's existing Sorcerer setup and Korriban scene. Appearance IDs describe the frozen source preset; they are not a working dynamic character generator.

After `_Ready`, `flow.CurrentConfiguration` returns an independent snapshot. `flow.RestoreConfiguration(config)` restores a supported catalog selection, validating faction/class/advanced-class/map relationships. Back/Continue require no disk persistence: `SelectionSession` owns the live choices and per-branch history. Configuration export preserves the active selection, not all branch history.

When the Ready screen is shown, `FrontendFlow.ConfigurationReady` emits a JSON snapshot. The Ready screen also provides a read-only configuration dialog. The signal is informational; it never starts gameplay. A consumer may subscribe with `flow.ConfigurationReady += json => ...` and deserialize with `System.Text.Json` or use `CurrentConfiguration`. Consumers must revalidate compatibility and must not trust a serialized `Playable` value; the model derives it each time. An altered/unsupported appearance makes the configuration unplayable.

## Future gameplay adapter boundary

An authorized later milestone should explicitly map only:

`empire / sith_inquisitor / sorcerer / korriban / sorcerer_april2012_bmn`

to the existing gameplay composition. Recheck the actual game's current architecture after B7, then supply its selection configuration, manage menu lifetime/input/audio, and connect the Play action. Until that adapter is reviewed, keep Play disabled. Class descriptions describe classic identities; none of their absent abilities is implemented by this module.

The current Exit button quits the review process. An embedded host should decide whether to retain it, route it to the host, or replace it with a return action. Settings are a placeholder plus a local sound mute; the frontend changes no global bus, input or settings configuration.

## Manual acceptance checks

Review menu/faction composition; browse both factions and all class branches; go backward and forward; confirm branch memories; inspect the Sorcerer's idle/framing/rotation/zoom; confirm every other class uses its own original artwork; inspect disabled appearance rows; select all six planets and their unavailable states; confirm only Sorcerer/Korriban reports content available; confirm Play stays disabled; inspect JSON summary. Review small/wide window fitting, keyboard focus, audio levels and the Settings popup. These checks were intentionally left for the user's manual launch.
