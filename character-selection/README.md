# SWTOR Survivors — C1 frontend module

C1 is staged exclusively in `D:/SWTORClassic/torbonk/character-selection` for the **same SWTOR Survivors game**. The project file is a temporary review harness. No gameplay project, launcher, shared tools, archives, or emulator files were edited. Neither Godot project was launched. Integration and the next milestone are intentionally pending manual review and authorization.

## What is included

- Minimal main menu with New Run, Character Selection, Settings, and Exit.
- Original Republic/Empire faction emblems and character-creation artwork.
- Eight classic base classes and sixteen advanced classes, with original icons and codex illustrations.
- Sorcerer-only interactive 3D preview: original converted character, original Idle clip, drag rotation, wheel zoom, double-click reset, independent world, camera and lighting.
- Appearance structure for species, body presentation, body type, face, hair, skin, complexion and equipment. The existing preset is the sole supported appearance; editing controls are disabled and explain why.
- Galaxy screen with original galaxy texture, original planet surfaces and illustrations, native map-panel art, six destinations and clear availability labels.
- Ready summary and disabled Play button. Only Empire / Sith Inquisitor / Sorcerer / Korriban / supported appearance reports content available.
- Back, Continue, Main Menu, Escape, keyboard button focus, selection highlights and authored fades. Choices survive page changes; each faction/base-class branch remembers its previous class choices. New Run deliberately starts fresh. Character Selection preserves the current session.
- Original interface-click, map-activation and map-close audio. Settings can mute the preview's interface sounds for the session.
- Portable typed configuration, a JSON example, catalog data, scene components, extraction evidence and integration instructions.

## Manual review entry point

After the other agent has finished, the user can open this isolated `project.godot` in **Godot 4.7.2 .NET**, allow the normal asset import, and review `Frontend/Scenes/MainMenu.tscn`. The existing Godot executable was not copied or started. No automatic launch script is included.

The menu uses a 1600 × 900 design canvas fitted and centered within the available window. The harness defaults to a 1280 × 720 window. Presentation is a Godot reconstruction using original assets, not an executed or pixel-identical Scaleform runtime.

## Verification performed

`tools/build.ps1`: isolated .NET compilation with Godot.NET.Sdk 4.7.2 / net8.0, **0 warnings / 0 errors**. NuGet packages, build output, process-local caches and logs reside here. It does not launch Godot.

`tools/validate_static.py`: static catalog and resource-reference validation, 2 factions / 8 classes / 16 advanced classes / 6 destinations / 8 scenes; exclusive Sorcerer model binding and exclusive Sorcerer/Korriban implementation flags. Results are in `manifests/static-validation.json`; compilation output is in `logs/build.log`.

**Not verified:** Godot import, live layout, shader compilation, rendered model framing, animation playback, sound playback, focus behavior and actual navigation. No runtime, screenshots, regression suite or gameplay tests were run. The source asset contact sheet under `work/` was used only to identify recovered original artwork.

## Read next

- [Integration instructions](INTEGRATION.md)
- [Original asset provenance and research](ASSET-PROVENANCE.md)
- [Limitations and review notes](LIMITATIONS.md)
- [Selection example](Frontend/Data/selection-example.json)

The transferable module is `Frontend/`. The harness project, local packages, caches, tools, source-assets, logs, and research manifests are development material and should not be merged into the gameplay project.
