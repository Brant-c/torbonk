# SWTOR HUD fidelity refinement — manual review pending

This pass follows the user's final reference: player and target unit frames
above the ends of exactly one centred horizontal player quickbar. The earlier
custom UI reference and the four-slot companion command module are not the
layout or quickbar artwork used for this pass.

## Audit and corrections

- The previous opaque navy polygons were added by AddSolidBacking throughout
  HUD frames, buttons, windows and tooltips. Removed that extra layer; native
  texture alpha supplies the background.
- The previous health frame was a 506x127 atlas-14 crop squeezed into 287x32,
  and a 504x128 crop was used as both a 604x56 quickbar and a 500x44 top toolbar.
  Their corner proportions and large dark interiors did not match unit bars.
  Replaced the bottom framing with joined native unit art at its original
  aspect. The top no longer uses quickbar framing.
- The previous action slots used a skill-tree frame. Traced the actual player
  quickbar's ImportAssets2 dependency to bwaui_slot.gfx and recovered its default
  textured slot. All twelve slots now use that artwork.
- Removed the disconnected central portrait disc and the separate SURVIVOR
  caption below the portrait. Names, level sockets, health/readiness strips,
  portraits and the quickbar now share the unit-frame geometry.
- Preserved the original Force Lightning icon and its cooldown presentation.
  Empty slots are reserved player slots, not additional abilities or action bars.
- Kept original quickbar end/page artwork. The four-slot companion command
  module is excluded, including its circular command-mode control.
- Replaced the top rectangle with a cyan arena heading, a fine source-art rule
  and separate wave/time/defeated counters. Restart is a small upper-right control.
  R still resets and Esc still quits; no gameplay pause system was added.
- Kept Eurostile, replaced global text outlines with a one-pixel shadow, and
  corrected window/tooltip nine-slice margins. No extra opaque backing remains.
- Unit contour and slot artwork scale uniformly. Only plain connecting rules,
  bar fills and nine-sliced panels extend along their flexible dimensions.

## Actual art and adaptations

See UI-PROVENANCE.md and manifests/ui-player-hud.json for deciding evidence.
Native art includes player slots, quickbar end/page controls, joined unit
contours, health/readiness/XP fills, icon, button/panel/tooltip textures and fonts.
The slot default state is traced through the player quickbar's dependency.
The unit contour is selected visually from original atlas pixels; exact GFX
symbol binding is unresolved. Layout, mirrored player contour, counter grouping,
portrait model crops, shadows and nine-slice settings are Godot adaptations.
This does not reconstruct the complete native Scaleform interface.

The bottom design spans 800x148 units, reaches 896 pixels wide at 1920x1080,
and centres on the viewport with a six-pixel bottom inset. It scales down with
available width and height; the top heading scales independently. Smaller
window rendering remains for manual review.

Movement, camera, combat, enemy AI/spawning, waves, health, progression,
character/environment assets and existing conversion scripts were not changed.
The additive recovery helper creates UI resource views over existing atlases.
Only the missing player-slot GFX definition was recovered from the archives.

## Lightweight verification

- C# build: passed, zero errors. NU1900 warned that NuGet vulnerability data
  could not be fetched from api.nuget.org; compilation completed successfully.
- One brief Godot 4.7.2 headless startup: exit 0, no reported runtime errors.
  Gameplay scene, 1920x1080 argument, 180 process frames.
  Log: logs/hud-fidelity-startup.log.
- No A1–A4 regressions, archive hashing, gameplay capture runs or repeated
  rendered validation cycles were performed.

The headless startup check establishes startup compatibility only. It does not
establish visual fidelity. Stop here for the user's manual visual review.

## Launch

From D:/SWTORClassic/torbonk in PowerShell:

    .\pipeline\Open-Gameplay.ps1

For an explicit 1920x1080 review window after the successful build:

    & .\pipeline\tools\godot\Godot.exe --path .\pipeline\godot res://Gameplay/GameWorld.tscn --resolution 1920x1080

WASD move; Lightning remains automatic; R or the small Restart control begins
a new run; Esc quits. Resize for a smaller-window anchoring review.

