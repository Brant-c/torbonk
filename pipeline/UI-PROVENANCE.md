# B4 original UI asset provenance

The latest fidelity pass uses the user's final 580x325 gameplay reference as its
layout guide. It removes all added opaque navy backing geometry. Native texture
alpha now controls the interiors and silhouettes. Original DDS/PNG pixels and
the existing asset conversion pipelines remain unchanged.

`bwaui_quickslots.gfx` imports `bwaui_slot.gfx`; this pass recovered that missing
3,776-byte dependency through the existing bounded read-only TOR reader.
Its archive hash is `0x602AE7CCDC3B86A3`, decoded SHA256
`af0afed49d6c399f4d4c6cb98d16836cfd9d934d8e7663d11059cc70fb143890`.
Export `ConversionSlotQuickslot` (156) places sprite 9, whose `defaultFrame`
places shape 2 using subimage 1. That subimage specifies atlas 1 at
`[660,126,716,182]` (XYXY). The twelve HUD slots use this original textured
56x56 artwork, uniformly scaled to 46x46 design pixels. End controls use
`bwaui_quickslots_1` and `_3`; page control uses `_6`. Four-slot companion
command modules inspected during research are **not used** in the HUD.

The joined portrait/level/unit-strip contour is a visual selection from atlas 15
at `[308,414,650,522]` (XYXY). Each 342x108 contour keeps its native aspect;
the player side is mirrored. This is actual original artwork, but its exact
portraitbar GFX symbol binding remains unresolved. No claim of a complete
native player-frame timeline reconstruction is made. Atlas 15's decoded source
hash is `42c2df9a14c0a63d42667262d7c76caea9892353019254ee081ccbba294872cd`
and TOR lookup hash is `0xBB0D52D9208BCADC`.

`Assets/UI/hud/*.tres` are reusable AtlasTexture resources over already imported
native PNG atlases; `scripts/recover_player_hud.py` regenerates these views and
`manifests/ui-player-hud.json` records their regions and evidence. No companion
bar crop, old atlas-14 toolbar crop, extra opaque plate, or skill-tree slot
is used by the bottom HUD. The old five `manualSlices` in `ui-recipe.json`
remain available as historical extraction evidence, not current HUD role bindings.

Original art: unit contours, player slots, end/page controls, bar fills, XP
frame, button/window/tooltip textures, Lightning icon, Eurostile outlines.
Adapted: placement and scale, mirrored player unit, clipped bar values, thin
connecting rules, top arena/counter grouping, Godot shadows and nine-slice
margins. The top status composition is custom, not an original SWTOR widget.
Portrait faces render the existing original `Assets/sorcerer.glb` once in
isolated Godot viewports; they are not recovered portrait paintings. Original
font kerning/hinting and native Scaleform layout behavior remain unresolved.

## B5.1 cooldown artwork

Client-derived evidence from the preserved `bwaui_slot.gfx`: sprite 155
(`ConversionIconButton`) places sprite 140 as both `mc_cooldownStates` and
`mc_gcdCooldownStates`. In sprite 140's `defaultFrame`, `mc_cooldown` is sprite
119, containing shape 118 and subimage 117. `mc_cooldownMask` is sprite 116,
containing shape 115 with a 50x50 rectangular boundary. The cooldown shape is
50x100, with an initial placement of (0,-50) inside that mask. Alternative style
labels and ready/frame animations also exist, but are not executed by this pass.

Subimage 117 binds image ID 7, `bwa_uitexture_10.dds`, rectangle
[929,308,979,408] (XYXY). `Assets/UI/hud/cooldown_wipe.tres` views that exact
50x100 region in the already converted native atlas. Source pixels and alpha are
unchanged. `Gameplay/UI/CooldownWipe.cs` clips and translates this original
blue-edged artwork top to bottom according to the actual remaining gameplay
cooldown. Every quickslot gets the shared GCD copy, including empty slots;
individual cooldown copies retain their own timers and seconds. This is a Godot
adaptation of the recovered geometry/artwork, not execution of native ActionScript
or a claim that every original timeline/style has been reproduced.

`scripts/recover_player_hud.py` reproduces the AtlasTexture view;
`manifests/ui-player-hud.json` records the source binding. The focused rendered
check is recorded in `logs/gcd-vertical-runtime.log`.

All extracted resources come from the operator-labelled April 2012 archive
set under `D:/SWTORClassic/swtoremu/Assets2012April`. The selected UI entries are
in `swtor_main_gfx_1.tor`. Archives are opened read-only. No emulator files or
archives are changed, and no original UI code is executed.

`manifests/ui-source.json` records every extracted resource's exact canonical
path, 64-bit archive lookup hash, archive filename, entry/data offsets, sizes,
compression method, decoded SHA256 and preserved source path. Its whole-archive
SHA256 values are carried from the existing baseline; B4 only checks archive
size/mtime against it and hashes selected decoded resources. No bulk archive
hashing was repeated. Historical filename lists supplied candidates; presence
was checked by unique April archive lookup and selected bytes were decoded.

Originals remain under `source-assets/april2012/resources/gfx/`. Generated
PNG/TTF files are separate under `godot/Assets/UI/`.

| Need | April resource path, relative to `/resources/gfx/` |
|---|---|
| Health, resource styles, XP fills | `gfx_production/bwaui_bars.gfx` |
| Player/target framing and portrait artwork | `gfx_production/bwaui_portraitbar.gfx` |
| Quickbar/slots | `gfx_production/bwaui_quickslots.gfx` |
| Experience frames/fills | `gfx_production/bwaui_experiencebarswindow.gfx` |
| Panel borders/backgrounds | `gfx_production/bwaui_standard_window.gfx` |
| Buttons and highlight artwork | `gfx_production/bwaui_buttons.gfx` |
| Tooltip borders | `gfx_production/bwaui_tooltip.gfx` |
| Skill choices and ability selection | `gfx_production/bwaui_skilltree.gfx`, `bwaui_abilities_window.gfx` |
| Original fonts | `fonts/swtor_fonts.gfx`, `fonts/fonts_en-us.swf` |
| Ability icons | `icons/{sithsorcerer,markofpower,forcespeed,chargedgauntlets,forkedlightning,chainlightning}.dds` |

The reader parses GFX/SWF headers, inflates CFX/CWS payloads and reads tags as
data. GFX tag 1009 identifies external `bwa_uitexture_1.dds` through
`bwa_uitexture_21.dds` in the same `gfx_production` archive directory. Tag 1008
supplies each exact atlas rectangle. All 21 atlases are preserved and converted
at native dimensions. The nine selected definitions yield 260 subimages.

`manifests/ui-conversion.json` joins every generated file to the source texture,
GFX definition, subimage/image IDs, crop coordinates, dimensions, conversion
settings and output hash. It also records original exported symbol names/IDs
and ImportAssets2 linkage names with the corresponding `.gfx` archive identity
where uniquely found. These linkage dependencies are recorded without running
or reconstructing their ActionScript. Examples: skilltree imports the standard
window, buttons, textbox and slot; abilities imports those plus scrollbar and
treeview. All original texture dependencies needed for the selected slices are
recovered. `ui-recipe.json` records the chosen gameplay roles separately from
original symbol identities: button state mappings and nine-slice margins are
adaptations, not native-state equivalence claims.

DDS conversion uses Pillow RGBA → lossless PNG, with no resizing, Y flip,
recoloring or explicit gamma transform. Godot uses lossless texture imports,
no mipmaps or premultiplication, native sizes, and its default alpha-border
fix in the import cache. The preserved DDS and generated PNG remain unchanged
by that cache processing.

DefineFont3 (tag 75) IDs 3 and 7 in `swtor_fonts.gfx` contain Eurostile and
Eurostile Bold. `ui_formats.py` converts quadratic outlines/advance widths from
20480-unit coordinates to a 1024-UPM TrueType font and inverts Y. Original
glyph codes and left bearings are preserved; kerning/hint instructions are
not. Generated font timestamps are fixed for repeatable output. Godot renders
them as dynamic fonts with grayscale antialiasing. The SWF source also contains
R6D8 Mod and Aurek-Besh; these were inspected but not converted for this HUD.

`manifests/ui-gom-evidence.json` retains read-only research results with GOM
IDs, bucket paths/offsets/hashes and the `ablIconSpec` values. Class illustrations
were identified by archive lookup at `icons/characterdetails/sithsorcerer_*.dds`
and `icons/larger/sithsorcerer{,_desat}.dds`; see
`work/ui/class-icon-candidates.json`. These are candidates for later character
menus, not decoded or asserted to be a separate class crest in the B4 HUD.

Reference: [Jedipedia file reader](https://swtor.jedipedia.net/reader). Its reader is a
JavaScript archive/file browser. The research MCP had no verified saved reader
view for `bwaui_skilltree.gfx` (hash `0xA50A1A1BDCBD319C`); local preserved bytes
and the bounded parser supplied the deciding evidence. No live/PTS resource
identity was substituted for the April archives.

## B10.1 side run-status panel

The arena/wave/timer/defeated grouping is now a compact 180×156 upper-left tracker using the same recovered `bwaui_standard_window_1` artwork as the menu. Its NinePatchRect renders at uniform 0.5 scale with native source margins left 128, right 184, top 72, bottom 64. The title sits inside the original cap and three label/value rows sit below it. Narrow windows place it below the existing boss dock if their horizontal bounds overlap. This is a Survivors layout of original menu chrome, not a native SWTOR score widget. Boss phase/warning text now uses the shared pale text color; the existing blue boss name, health frame and bar behavior remain unchanged. No source pixels or gameplay counter logic changed.

PvP scorecard/scoreboard candidates were inspected through exact April resource lookups and bounded data-only GFX parsing. Selected bytes and atlas rectangles are recorded in `manifests/b101-pvp-source.json`. These remain research inputs; no PvP or character-portrait frame is used by the side tracker.

## B10.1 level-up refinement

The original level-up popup subimages 1 (397×105 gold frame), 4 (442×104 flare) and 7 (40×47 level icon) remain unchanged at their native aspect. They are recovered from `bwaui_levelup_popup_window.gfx`; bindings and source rectangles remain in `manifests/b7-research.json` and `b7-visuals.json`. The final composition follows the supplied SWTOR reference: original frame at (62,20), original flare above it at (-104,9), original icon at (98.35,52.85), and left-aligned recovered Eurostile Bold text at 18px/20px in muted cream. The flare/icon placement follows the recovered GFX sprite offsets; text-field sizing/color and the existing SURVIVOR display name are Godot adaptations to the reference, not newly proven native text-field settings. Banner wording is 'SURVIVOR Has Reached:' / 'Level N'.

The selection pane retains standard-window slice 1, skill-tree slices 30/32, original ability icons, original rarity icon frames and recovered regular Eurostile. Its three-card composition is unchanged. Removed explanatory subtext and its Restart action, reserved space for wrapping titles and reduced unused panel height. No new artwork, external fonts or drawn card borders were introduced. Confirmation/pause/choice behavior and source artwork are unchanged.
# D1.1 Collections reuse

D1.1 reuses the existing converted SWTOR windows, standard buttons/highlights,
character-creation preview frame, square `bwaui_buttons_381` icon frame, D1 item
icons, class artwork, Eurostile and tooltip frame. No source archives changed.
These are Survivors layout/state assignments of original artwork; they do not
claim the exact native inventory layout. Missing alternate/saber item icons use
the recovered empty icon frame with explicit names. See
[D11-README.md](D11-README.md) for exact resources, scope and validation.

