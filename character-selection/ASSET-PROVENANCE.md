# C1 original asset provenance and research

## Source and evidence boundary

Sources are the operator-labelled April 2012 archives under `D:/SWTORClassic/swtoremu/Assets2012April`. Historical filenames from the existing extracTOR `bin/Debug/net8.0-windows/filenames-all.txt` were discovery candidates, not proof of April presence. The read-only SWTOR Emulator Research skill/status was consulted; its indexed name search returned no character-creation/galaxy candidates, so the existing local name list and TOR reader were used.

`tools/recover.py` freezes only the pure `entries` and `pathhash` functions from the existing `pipeline/scripts/source_pipeline.py` into `tools/tor_reader.py`. It copies the existing data-only GFX tag reader into `tools/ui_formats.py`. Shared conversion modules are not imported or run. The isolated recovery opens archives in `rb`, reads entry tables, matches the lowercased canonical-path lookup3 hash, requires one entry, decodes only selected payloads, and hashes the decoded bytes. No bulk payload decoding or whole-archive integrity scan was done. Recovery is bounded to 180 MiB decoded data and 24 MiB per selected entry.

`manifests/recovery.json` is the deciding **Client-derived** evidence: canonical path, archive filename, lookup hash, data/entry offsets, compressed/decoded sizes, compression method, CRC index, decoded SHA256, preserved source output; plus GFX imports, exports, tag-1008 rectangles, source atlas and crop-output hashes. `manifests/candidates.json` records matching historical candidate paths, not runtime bindings. The successful recovery contains 104 selected resources from `swtor_main_gfx_1.tor` (78), `swtor_main_art_fx_1.tor` (20), and `swtor_main_art_misc_1.tor` (6), including 12 GFX movies. The separately copied original GUI bank is documented below.

DDS pixels were decoded by Pillow to native-size RGBA PNG without flips, resampling, gamma edits or recoloring. Crops are exact original GFX external-image/subimage rectangles. Runtime scale, shaders, placement, opacity, text and state behavior are authored in Godot. A recovered image does not establish original native widget semantics or exact screen placement. No native ActionScript, HSL, GFX or client runtime was executed.

## Character creation and main menu

Paths in this table are relative to `/resources/gfx/gfx_production/` unless stated otherwise.

| Need | Original resource and recovered use |
|---|---|
| Character-creation controller | `bwaui_character_create.gfx`: imports, strings, event names and external-image evidence preserved; not executed |
| Creation window | `bwaui_character_create_window.gfx`: original faction emblems, logo, allegiance artwork, frames and rotation-control artwork recovered |
| Creation panels | `bwaui_character_create_panels.gfx`: original paired and single blue panel art recovered |
| Faction-card frame | Same movie, subimage **9**, cleaner original single-panel frame; adopted during manual review to give faction emblems and copy a clear interior |
| Character selection | `bwaui_character_select.gfx`: definitions and shared event strings investigated; not executed |
| Empire emblem | `bwaui_character_create_window.gfx`, subimage **81**, active red emblem; subimage 88 inactive variant preserved |
| Republic emblem | Same movie, subimage **93**, active blue emblem; subimage 96 inactive variant preserved |
| Original logo | Same movie, subimage **101**; the added Survivors text overlay was removed during manual review |
| Allegiance title | Same movie, subimage **104**, English original artwork; 106/108 localized variants preserved but unused |
| Star background | `/resources/art/fx/texture/sky/char_select_stars.dds` |
| Menu globe surface | `/resources/art/fx/geometry/tython/texture/char_select_tython_high_res_d.dds` |
| Preview background evidence | `char_select_milky_way_p.prt` binds the original star texture; selected planet-glow/moon particle specifications are preserved under `source-assets/`, with readable isolated copies in `work/` |

Faction logos, title and frames were identified from original extracted pixels in an asset contact sheet. Tag-1008 IDs and rectangles are exact; the reconstructed Godot layout and selection-state mapping are adaptations, not a complete native timeline binding claim. Manual-review revisions removed added heading overlays, the Survivors text overlay, bold/gold text emphasis, drawn focus borders, selection glyphs and drawn galaxy guide lines. Functional labels use the recovered regular Eurostile font; button states and borders come from the preserved original texture slices.

The subsequent fit pass uses 24px regular class headings below the ornate panel cap, with 28px side insets. Faction emblems and copy were reduced to fit subimage 9. Rest/hover bindings were reversed at the user's request: standard-window slice 9 is the rest state, slice 24 is the ordinary hover state, and slice 18 is the selected hover/pressed state. Content padding stays the same between states to prevent text movement; these state assignments are presentation adaptations, not newly asserted native runtime bindings.

For ordinary buttons, focus/selection and panels, C1 reuses frozen copies of the existing B4 conversions: `bwaui_standard_window_{1,24,9,18}.png` and `bwaui_tooltip_1.png`. `manifests/prior/ui-source.json` and `ui-conversion.json` retain original paths, hashes, crop bindings and conversion policy. Fonts are copied `Eurostile.ttf` and `EurostileBold.ttf`, converted previously from `/resources/gfx/fonts/swtor_fonts.gfx` DefineFont3 outlines, font IDs 3 and 7. No replacement font was downloaded.

## Class icons and artwork

All artwork below was uniquely matched to the April GFX archive. Base illustrations are `/resources/gfx/codex/cdx.game_rules.classes.{key}.dds`:

| Displayed class | Codex key | Original icon key |
|---|---|---|
| Sith Warrior | `sith_warrior` | `sithwarrior` |
| Sith Inquisitor | `sith_inquisitor` | `sithsorcerer` |
| Imperial Agent | `agent` | `imperialagent` |
| Bounty Hunter | `bounty_hunter` | `bountyhunter` |
| Jedi Knight | `jedi_knight` | `jediknight` |
| Jedi Consular | `jedi_consular` | `jediconsular` |
| Smuggler | `smuggler` | `smuggler` |
| Trooper | `trooper` | `trooper` |

Icons use `/resources/gfx/icons/larger/{key}.dds` when present; Imperial Agent uses its recovered `/resources/gfx/icons/imperialagent.dds` because the larger candidate is absent. The original internal `sithsorcerer` icon name is retained as provenance; it is not treated as a base/advanced-class identity equivalence.

Advanced illustrations are `/resources/gfx/codex/cdx.advanced_classes.{key}.dds`, with these exact keys: `juggernaut`, `marauder`, `sith_sorcerer`, `sith_assassin`, `sniper`, `operative`, `mercenary`, `powertech`, `guardian`, `sentinel`, `jedi_sage`, `jedi_shadow`, `gunslinger`, `scoundrel`, `commando`, `vanguard`. Descriptions in the catalog are authored summaries, not recovered original localized text or implemented ability specifications.

## Existing Sorcerer and customization evidence

The staged model is an exact copy of the existing `pipeline/godot/Assets/sorcerer.glb`, SHA256 `c338202a83450f47d9976c113bb4e14a6071be6e102358e9eae1cac5b3b829ed`. Its seven existing animation clips include `Idle`; all 24 material images are embedded. No new character conversion was attempted.

The original assembly prescription and source/dependency hashes are frozen in `manifests/prior/character-recipe.json`, `character-source.json`, `character-conversion.json` and `character-artifacts.json`. `manifests/staged.json` maps each copied file to the prior converted output and its hash. Those prior documents reference historical pipeline locations; they were copied as evidence, not rewritten as new conversion results.

The existing recipe establishes Human / male / `bmn` body type, `/resources/art/dynamic/spec/bmnnew_skeleton.gr2`, head `head_human_bmn_caucasian_a01.gr2`, hair `hair_human_bmn_non_a01.gr2` with its original V01 material, and SS A04 C01 S01 robe assembly. Its source XML asset registrations include body-type filters, models, materials, palettes, skin indices and identity/tag information. The source manifests record dynamic `head/index.xml`, `chest/index.xml`, `leg/index.xml`, `hand/index.xml` and the remaining exact assembly dependencies.

This evidence supports the frozen preset only. It does not establish facial morphs, valid alternative species/body combinations or a working Godot material/mesh replacement system. Character-create GFX includes customization controls and slider imports, but their original runtime logic was not used. Consequently all appearance-editing controls are disabled.

## Galaxy and planets

Recovered native movies: `ui_hud_galaxymap.gfx`, `ui_galaxymap_infowindow.gfx`, `ui_galaxymap_panel.gfx`, `ui_galaxymap_planet_info.gfx`, `ui_galaxymap_scanlines.gfx`, `ui_galaxymap_centerbutton.gfx`, `ui_galaxymap_leftbutton.gfx`, `ui_galaxymap_closebttn.gfx`. Imports and external-atlas definitions remain in the manifest. The info panel uses native `ui_galaxymap_panel` subimage **3**; other faction, scan-line and pointer artwork was recovered for future use.

The galaxy background is `/resources/art/fx/texture/galaxy_map/gal_map_galaxy_core.dds`. Related native region, core-gradient and indicator textures are also preserved. Their Godot chart composition is simplified and schematic. Manual-review corrections preserve the original image pixels: the original transparent starfield now sits over opaque black across the window, removing the gray clear-color wash; 2:1 planet surface maps feed square shader quads, keeping projected globes round; destination globes and their buttons share centered, nonoverlapping groups. Schematic destination positions were adjusted for spacing and are not native galaxy coordinates.

| Destination | Codex illustration under `/resources/gfx/codex/` | Surface under `/resources/art/planets/texture/` |
|---|---|---|
| Korriban | `cdx.planets.korriban.dds` | `planets_korriban_d.dds` |
| Tython | `cdx.planets.tython.dds` | `planets_tython_d.dds` |
| Dromund Kaas | `cdx.planets.dromund_kaas.dds` | `planets_dromund_kass_d.dds` |
| Coruscant | `cdx.planets.coruscant.dds` | `planets_coruscant_d.dds` |
| Hutta | `cdx.planets.hutta.dds` | `planets_hutta_d.dds` |
| Ord Mantell | `cdx.planets.ordmantell.dds` | `planets_ord_mantell_d.dds` |

The original spelling `kass` is preserved in the surface filename; the display name is Dromund Kaas. The Korriban planet FXSpec was inspected as data and references `/art/planets/korriban.gr2`; no planet mesh or additional gameplay environment was converted. Surface spheres in the chart are shader projections of original textures, not the original planet meshes.

## Audio

The already identified original bank was copied into C1 as `source-assets/gui.bnk`. Exact April entry: `swtor_main_bnk_audiodata_1.tor`, lookup hash `0xD634B5F5260713CD`, decoded SHA256 `5f2ffe5d7d70698c66c97a34c602acc3b0d932c2886cb7849a1e23aededde7ac`. Its prior evidence labels the canonical bank `/resources/bnk2/swtor_startup.bnk`; that label is retained as prior evidence. The existing wwiser XML and vgmstream executable/dependencies were frozen inside C1 before recovery.

`tools/recover_audio.py` collects original GFX event-name strings, computes Wwise FNV-1 event IDs, requires typed `CAkEvent` matches, follows parsed action/target/child fields, and extracts only selected embedded DIDX/DATA media. `manifests/frontend-audio-discovery.json` preserves event candidates and graph edges; `frontend-audio.json` records selected variants, event IDs, decoded-bank media offsets, WEM/output hashes, durations and usage. WAV decoding runs only the isolated copy of vgmstream.

- `play_gui_interface_buttonclick` → `Audio/select.wav`.
- `Play_gui_galaxymap_mapitem_activate` → `Audio/map.wav`.
- `Play_gui_galaxymap_mapclose` → `Audio/back.wav`.

One traced variation is used per event. Back uses the native map-close sound as an authored navigation reuse. Native randomization and mixing are not reproduced. No character-creation music or class narration was adopted.

## Missing candidates and deliberate limits

Fourteen historical candidates lacked a unique April match, including newer `bwaui_character_create_faction_select`, `..._customize`, `..._breadcrumbbar`, `bwaui_advancedclass_selection_window`, newer `bwaui_galaxymap` / `..._shared`, `ui_character_create_classpanel`, two guessed GUI XML paths, newer galaxy geometry texture locations, and the larger Imperial Agent icon. Exact misses are in `recovery.json`. Original older `ui_*` galaxy files and `bwaui_character_create_window` were recovered instead. No modern assets were silently substituted.

No full native character-preview environment, full customization engine, Scaleform runtime, native galaxy navigation, narration or music pipeline was implemented. Original artwork was prioritized; Godot Control reconstruction supplies the usable C1 selection flow.
