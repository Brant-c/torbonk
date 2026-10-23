# D4 source provenance and fidelity limits

Evidence is **Client-derived** from the operator-labelled April 2012 archives, with exact path/hash, archive offset and decoded SHA256 receipts. Runtime checks are **Behavior-verified in this Survivors adapter**. No original archive or emulator file is edited. No whole-archive hashing or bulk payload scan is part of D4.

## Terrain and scenery

Area `/resources/world/areas/4611686022825732973/area.dat`, room `baseroom.dat`. Original sectors `4611686022895042660`, `4611686022895042664`; added sectors `4611686022895042668`, `4611686022895042675`, `4611686022895042676`, `4611686051466075186`. `d4-environment-source` dependencies are in `manifests/d4-source.json`; selected terrain and placements in `godot/Assets/Korriban/D4/environment.json`; converted model bounds, source paths and complete world matrices in `d4-environment-model-conversion.json`; height/weight/topology notes in `d4-environment-terrain-conversion.json`.

The A4 `Assets/Korriban/KorribanEnvironment.tscn` is retained and instantiated as the new scene's foundation by `BuildD4.gd`. Matching placement/terrain identities are reused rather than doubled. Parent records remain even for hidden unsupported monitor overlays. Newly arranged burial courts have explicit authored-placement metadata. Additional original resources include:

- `/resources/art/static/area/kor_korriban/item/kor_tomb_item_sarcophagus.gr2`
- `/resources/art/static/area/kor_korriban/item/kor_tomb_item_sarcophagus_ruin.gr2`
- `/resources/art/static/area/kor_korriban/item/kor_tomb_item_brazier_ancient.gr2`
- `/resources/art/static/area/kor_korriban/item/kor_tomb_item_statue_03.gr2`
- `/resources/art/static/item/container/item_con_crate_ancient_01.gr2`

The original ASSETS/ROOMS contain numbered Sith tomb chamber, hallway, floor, entrance and Academy sets. Seven bounded room definitions were inspected, with their exact receipts retained. The original ambience prototype also names Ajunta, Marka, Tulak and Naga regions, including relic/machine vaults and creature chambers (`d4-ambience-definition.json`). This establishes source content availability; it does **not** equate a numbered room or a sound-table key with a named map room. No unverified named interior is claimed as playable.

Native physics is not recovered: terrain uses derived triangles; solid clutter/props use existing triangle/convex policies. Original source holes and seams remain. No fabricated terrain, repair floor or invisible substitute topography is added. Material PBR translation and terrain UV phase retain the earlier pipeline's documented approximation. Source scheme values remain in `area.dat`; stage ambient colors are an authored adaptation.

## Characters, models and clips

`d4-definitions.json` and `d4-additional-definitions.json` retain the deciding NPC → appearance → original registry fields, bucket path/offset and SHA256 evidence. IDs remain strings. Historical decoder field labels are distinguished from April bucket/prototype names.

| Role | April NPC identity | Appearance / registry model-material |
|---|---|---|
| Spitting Tuk'ata | `npc.location.korriban.mob.tukata.spitting_tukata`, `0xE00005AF8FAD37B2` | `npp.creature.dog.tukata`; `1000123 / 1001442` |
| Ancient Droid Protector | `npc.location.korriban.mob.droid.ancient_droid_protector`, `0xE000497ED8A04217` | `npp.droid.assassin_droid.ancient_guardian_a01`; `1002841 / 1002844` |
| Ancient Beast | `npc.location.korriban.class.sith_warrior.slaying_the_beast.ancient_beast`, `0xE00057884D53EE58` | `npp.creature.terentatek.terentatek`; `1000158 / 1001462` |
| Terentatek Mauler | `npc.location.korriban.mob.terentatek.terentatek_mauler`, `0xE00013C1E80729C3` | Same original terentatek appearance family |
| Brood Mother | `npc.location.korriban.mob.klorslug.klorslug_brood_mother`, `0xE000115E172F0922` | `npp.creature.klorslug.queen`; `1006617 / 1006618` |
| Shyrack Screecher | `npc.location.korriban.mob.shyrack.shyrack_screecher`, `0xE000384BC19FF5C7` | `npp.creature.dragonbat.shyrack`; `1003627 / 1003628` |

New models are `/resources/art/dynamic/creature/model/dog_tukata_a01.gr2`, `assassin_ancientguardian_a01.gr2`, `terentatek_terentatek_a01.gr2`, and the queen model resolved through creature index `1006617`. The full exact paths/materials/textures are in `d4-enemies-recipe.json`, with decoded source receipts in `d4-source.json`.

Original `/resources/art/dynamic/spec/assassin.dyc` explicitly declares `Skeleton=bmnnew_skeleton.gr2`; absent historical assassin skeleton filenames are not used. The guardian uses compatible original BMN pistol idle/run/fire/hit/death. Its original NPC ranged weapon is `itm.npc.blaster.low01.a01_v01`, already converted in `Assets/D3/pistol.glb`, on the original `RightWeapon` socket. The original electroblade weapon relationship is also recorded but not used for this ranged adaptation.

Tuk'ata uses the `dog` rig, queen the `klorslug` rig with `slug` clips, terentatek its own rig/clips. Conversion preserves original bone weights and 60 Hz original JBA sampling. Receipt `d4-enemies-conversion.json`: 57 / 134 / 93 / 101 bones respectively, five clips each, no unmatched animation bones. Screecher reuses the already verified original Shyrack model/material and uses the source 0.75 scale. Tuk'ata/droid scales follow 0.9/0.95 source values. Beast/mauler/queen encounter sizes, collision profiles, attack patterns, health, XP and timings are Survivors tuning; native NPC scale values are preserved as evidence, not silently claimed as exact gameplay parity.

The three major identities are supported by original NPC definitions. No historical Sith Lord model or original MMO boss scripting is claimed. Broodmatron's original earlier conversion/provenance is retained. The droid crossing lanes, queen call, Screecher call, Mauler frontal and Beast two-phase trial are authored gameplay on authentic source appearances.

## Atmosphere, effects and rewards

`sndAmbienceRegionsTablePrototype`, `0xE000F65814430FED`, April bucket `417.bkt`, contains the original Valley/tomb events. `d4-ambience-definition.json` preserves selected values and exact map paths. The table was read in bounded pages; its keys are not treated as GOM IDs.

Original `play_amb_korriban_valley_of_the_sith_ext` (`3049950536`) and `Play_amb_korriban_tomb_naga_main` (`4180000628`) resolve as typed CAkEvent records in `/resources/bnk2/location_korriban_data.bnk`. Only two unique long media slices were decoded, `5536795` (10.90 s) and `1898149` (10.13 s), from the actual referenced `/resources/bnk2/cinematics_media.bnk`. Event names, complete wwiser graphs, DIDX offsets, RIFF identity, lengths and SHA256 receipts are in `d4-audio.json`. Duplicate references in the native graph share the same decoded output. HIRC's declared byte offset differs from DIDX's decoded-bank offset by 16 bytes; both are retained, while extraction uses the actual DIDX entry with matching media ID/size and RIFF header. The report does not claim these two offset conventions are identical.

Original vocal media are bounded slices from `voc_tukatasmall.bnk`, `voc_terentatek.bnk`, `voc_assassindroidancientguardian.bnk`, linked to NPP sound-package evidence. The first selected variation is used as an authored attack warning; its precise native attack/death event meaning is unverified. The single looping bed and limited cues approximate native Wwise layering, random history and spatialization. Reverb and the complete native ambience graph are not reproduced.

`/resources/art/fx/fxspec/env_kor_torch.fxspec` resolves its `env_kor_torch_01(.prt/_p.prt)` and `kor_torch_smoke_02(.prt/_p.prt)` dependencies. Source flame uses `/resources/art/fx/texture/fire/5x5_flame_lick_04_bw.dds`, a 5×5 atlas with 0.1 animation speed; smoke uses `/resources/art/fx/texture/smoke/smoke_thin.dds`. Original parameter text is retained in `d4-environment-fx.json`. Godot uses two persistent billboards per brazier, with authored hue/motion/attachment. Warnings are reconstructed geometry in the established boss style and conform to the original terrain; rendering has no damage authority.

Run item names/rules are authored, not recovered canonical SWTOR items. Icons reuse the original `darksiderelic1`, `darksiderelic3`, `holocron` artwork and existing D2 receipts. The existing SWTOR UI art, fonts, reward audio and replacement lifecycle remain. No item is saved to cosmetic Collections.

## Scope and verification

Shared interfaces are optional and default-compatible: planet run data, anchored guard spawning, guaranteed encounter rewards, original weapon-scene attachment, cached Korriban animation libraries, one World ambience voice, and terrain-conforming warning surfaces. Crowd cell lists are reused to reduce allocation without reducing damage/flytext events. Other maps retain blank planet configuration and their original wave/roster/reward behavior.

`d4-delivery.json` records focused verification, original asset counts and the unchanged pre-D4 hashes of 15 preserved files. These checks do not establish native client equivalence, exhaustive navigation safety or manual visual/audio/balance acceptance.
