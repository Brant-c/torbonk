# B7 asset provenance and licensing

Evidence is **Client-derived** from the operator-labelled April 2012 archives at
`D:/SWTORClassic/swtoremu/Assets2012April`, using the established read-only TOR
reader, GFX parser, Pillow, wwiser and vgmstream. This label does not authenticate
a retail patch manifest. Historical filenames were candidates until checked by
unique lookup hash and decoded bytes. Archives were not modified or rehashed.

## Recovered original assets

| Use | Original identity / binding |
|---|---|
| Level banner | `/resources/gfx/gfx_production/bwaui_levelup_popup_window.gfx`; subimages 1, 4 and 7, and `mc_levelTextbox` / `LevelUpPopupWindow` strings |
| Gold frame | Subimage 1 → image 18, `bwa_uitexture_14.dds`, XYXY `[615,790,1012,895]` |
| Gold gleam | Subimage 4 → image 18, same atlas, `[310,904,752,1008]` |
| Arrow | Subimage 7 → image 10, `bwa_uitexture_20.dds`, `[982,962,1022,1009]` |
| Boss chrome/fill/font | Existing B4 original `bwaui_standard_window_1`, `bwaui_tooltip_1`, `bwaui_bars_157`, `bwaui_bars_21`, Eurostile / Eurostile Bold; see `UI-PROVENANCE.md` and `manifests/ui-conversion.json` |
| Pickup glow | `items/loot_common.fxspec` → `/resources/art/fx/particles/loot_core_glow.prt` → `loot_core_glow_p.prt` → `/resources/art/fx/texture/glow/bw_flare_gray.dds` |
| Reward ring | `loot_common.fxspec` `_fxProjectionTexture` → `/resources/art/fx/texture/symbol/tri_ring_blurred.dds` |
| Level-up audio | `abilities/all/all_level_up_red.fxspec` references `Play_gui_state_levelup_core`, Imperial probes in/out and probeswoosh events |
| Startup bank | `/resources/bnk2/swtor_startup.bnk`, TOR hash `0xD634B5F5260713CD`, BKHD bank ID `3980808364`, Wwise version 62 |
| Recovered inventory/UI audio | Candidate name `Play_gui_inventory_pickup_default`, FNV-1 ID `1944075995`, verified as a typed startup HIRC Event → Action → container → Sound → media `163129423`; retained for UI, superseded for XP collection |
| Eruption audio | `creatures/generic_impacts/cr_generic_aoe_ground_pound.fxspec` references `Play_ability_creature_generic_spawn_rockfountain` and `Play_ability_creature_generic_spawnlarge_groundland`; typed `abilities_creatures_data.bnk` event graphs resolve selected media `24757886` and `113287997` in their exact HIRC/DIDX media bank locations |

Paths abbreviated in the table under `items/`, `abilities/` and `creatures/` are
relative to `/resources/art/fx/fxspec/`. The level-popup movie declares 24 fps but
its root and all five recovered sprites contain only one frame. That establishes
static artwork availability, not an original fade/hold animation duration.

Startup event/media mappings retained for reproducibility:

| Event | Typed event ID | Selected media |
|---|---:|---:|
| Imperial probes in | 651965756 | 130814347 |
| Imperial probes out | 1335401467 | 576226339 |
| Level-up core | 1949208809 | 787228180 |
| Probeswoosh | 3053292103 | 96462397 |

The core, probes and pickup PCM are original recovered media. Only seven PCM
files were originally staged for B7 (about 3.7 MB including the presentation artwork). The
probeswoosh is retained as recovered evidence but is not currently assigned to
a runtime cue. No looping level-up sound is staged or started.

`manifests/b7-source.json` preserves exact archive names, entry/data offsets,
compression, path hashes, decoded SHA-256 and baseline archive identities for
selected source files. `b7-gui-bank.json`, `b7-audio.json` and
`b7-impact-audio.json` preserve typed events, wwiser graphs, media offsets,
source/output hashes and durations. Impact extraction validates the declared
HIRC bank ID, memory size and decoded offset against that bank's DIDX/DATA;
missing-bank warnings from the initial wwiser graph are resolved by this exact
selected-media check. `b7-visuals.json` records source crops and converted hashes;
`b7-research.json` records bounded discovery and unresolved candidates.

DDS → PNG conversion is native-size RGBA without resampling or recoloring.
WEM → WAV uses the existing vgmstream decoder without normalization. Original
source bytes remain separately under `source-assets/april2012/`; generated game
assets are under `godot/Assets/Presentation/`. Original fonts use the existing
B4 conversion, with its documented kerning/hinting limits.

The recovered pickup PCM is quiet (about −34 dBFS RMS); its original wwiser
graph reports +12 dB gain. Runtime cue gains account for that, and the quiet
rock warning receives gain plus combat ducking. Prototype confirmation includes
the probe cue's audible swell rather than cutting only its quiet lead-in.
These gain/duration adjustments followed the smoke pass and require listening
review; they do not change audio paths or playback/cleanup logic.

## Authored adaptations and unresolved bindings

The boss health layout is a new composition from recovered SWTOR components;
an original dedicated boss-widget binding was not recovered. Arrival/defeat
text and the reward-ring timing are authored. The level banner's placement,
dynamic level text, fades and sequencing approximate the original presentation;
this does not execute the native Scaleform or original probe choreography.

The XP mote geometry, teal tint, size, float/pulse and collection trail are
authored for Survivors. Its glow pixels come from an original world-loot effect,
not a UI icon. The amber boss warning/countdown and impact shader are authored.
They do not reuse or change the green Force Storm indicator.

Original hover, upgrade-selection, boss-arrival and boss-victory audio bindings
were not recovered. Hover/click use short, quiet portions of recovered pickup
PCM. Rarity confirmation and encounter cues adapt the recovered pickup/probe
media with configured gain, pitch and duration. Level-up uses the recovered core.
Burrow warning/impact reuse original generic rock/ground sounds; these are not
asserted to be native Broodmatron bindings. Truncation/fades, priorities, voice
limits, gain staging and ducking are authored; native Wwise routing, containers
and random histories are not reproduced completely.

The historical achievement-popup and banner-library GFX candidates did not have
unique April matches. The recovered message/system movies were inspected as
static notification candidates. These bounded results are not proof of absence
from unnamed archive entries. No full UI subsystem reverse engineering was done.

## Rights and external sources

No external audio, soundtrack recordings or new third-party audio dependency
was downloaded. The artwork, fonts and audio originate in the user's supplied
SWTOR archives and remain proprietary material of their respective rights
holders (SWTOR / BioWare / Electronic Arts / Lucasfilm as applicable). No open or
royalty-free license is claimed for those assets, and extraction does not grant
redistribution rights. There is therefore no new external creator credit,
source URL, attribution requirement or open-audio license to track for B7.

## Listening-review follow-up: XP, Shock and Force Storm

The XP inventory click has been replaced with `xp_soft_chime.wav`, a new authored
0.28-second tone made entirely from sine oscillators, with a 12 ms rounded
attack and tapered release. `scripts/make_xp_chime.py` is the reproducible recipe;
`manifests/xp-chime.json` records output hash, frequency components, sample rate
and level measurements. This is authored project audio, not original SWTOR
media. It contains no external samples or third-party attribution dependency.

Shock and Storm still use the same original recovered PCM. Their native parallel
action/container groups now guide runtime layer selection rather than choosing
one component from a flattened graph. `manifests/audio-polish.json` joins the
configured groups to the existing `abilities-audio.json` event graphs; every
configured media ID was verified against its recovered event graph and local
PCM file. Individual gains remain authored and centrally tunable. Storm's
second looping layer uses a bounded variant, rather than reproducing Wwise's
complete random sequence/history. No new archive extraction was needed.

Finite audio tails now survive normal visual completion; abort/death/restart
still clear them. The focused rendered check passed all 13 checks in
`manifests/audio-polish-smoke.json`. Sound preference and final relative volume
remain subject to listening review.

### Supplied XP sound replaces the authored chime

The current XP cue uses the user's `C:/Users/brant/Downloads/17892505.wem`.
Both supplied WEM and OGG files are preserved unchanged under
`source-assets/user-supplied/xp-pickup-17892505/`. The WEM was decoded with the
existing vgmstream tool to a 44.1 kHz stereo PCM16 WAV (0.623 seconds), then
reduced by 12 dB without normalization, pitch change, trimming or looping.
`xp_17892505_quiet.wav` is the current pickup asset, with neutral cue gain and
the existing Reward bus gain/retrigger limit. Original Downloads files were
not modified. The authored chime remains unused.

`manifests/xp-pickup-user-source.json` records source/output hashes and conversion
settings; `scripts/stage_user_xp_audio.py` reproduces the conversion. The
supplied filename alone does not establish archive origin, creator or license;
this file is not asserted to have been recovered from the April corpus.
