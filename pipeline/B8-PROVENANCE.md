# B8 — April source provenance and adaptations

This milestone uses the operator-labelled April 2012 archives read-only. No
emulator or source archive was edited. Historical field/enum labels from the
research service remain labelled interpretations; IDs retain their namespaces.
This is a Survivors implementation, not restored native MMO/server behavior.

## Definition evidence (Client-derived)

`manifests/b8-definitions.json` preserves the selected definitions, source
bucket paths, offsets and selected-resource hashes. `b8-gom-evidence.json`
preserves expandable appearance schedules and parameters, including attachment
names and animation-note triggers.

| Gameplay name | April GOM definition | Icon specification | Appearance / original clips |
|---|---|---|---|
| Death Field | `abl.sith_inquisitor.skill.madness.death_field`, `0xE000B6A548225F23` | `deathfield` | `epp.sith_inquisitor.death_field.cast`; `cb_buff_consumption_exit` |
| Static Barrier | `abl.sith_inquisitor.backlash`, `0xE000D2FE01262A97` | `electricshield` | `epp.sith_inquisitor.static_barrier.cast_instant` / `.buff`; `cb_buff_unleash` |
| Dark Heal | `abl.sith_inquisitor.dark_heal`, `0xE000FBA79666D299` | `darkGift` | `epp.sith_inquisitor.dark_heal.cast_loop` / `.cast_end`; `cb_buff_consumption_enter`, `_loop`, `_exit`, `fp_1h_elect_instant_right_4` |

Static Barrier's `backlash` identity is established by the definition's icon,
appearance resources and the Static Barrier EPP chain, not a cross-namespace
numeric equality. April metadata records Death Field's 15 s cooldown, Barrier's
4.5 s ability cooldown and Dark Heal's 1.5 s casting time. B8 deliberately uses
its own defensive cooldowns and brief, movement-compatible self-cast gestures.

## Selected assets (Client-derived)

`manifests/b8-source.json` records 91 exact resources (about 2.55 MiB decoded),
each with canonical path, unique lookup3 TOR entry, archive offset, decoded-byte
hash and dependency edge. Existing archive inventory metadata is reused; no
whole-archive hashing was performed. `prepare_b8.py` reuses `SelectedSource`,
the existing binary XML decoder and DDS conversion. Original extracts are
verified before reuse, never overwritten. The geometry material inputs are
followed explicitly, including diffuse, normal and shared default textures.

* Death Field: `abilities/sith_inquisitor/si_death_field_hand_cast`, `_aoe`,
  `_cast`, `_impact_target` FXSpecs; original `fx_si_death_field.gr2` geometry,
  its `Uber` material and `si_death_field_d` texture. After manual review, the
  runtime projects the original lightning texture and expanding ring onto the
  terrain using the existing Force Storm projection mesh, rather than displaying
  the opaque texture on the recovered raised GR2 shell. The converted GR2 is
  retained as source evidence. Smoke, flash and target-impact textures remain original. Hand effects follow
  `vfx_hand_left`; the source target attachment is `vfx_aim01`. The source cast
  has an additional 0.5 s delayed layer; the burst adapter uses a 0.3 s windup.
* Barrier: `si_backlash_caster_shield` / `si_backlash_caster_tick` FXSpecs;
  original shield-accents `fx_si_backlash_partialshieldcentered.gr2`, its diffuse
  material, `sphere_displaced_01` and `2x2_shield_orbs` artwork. The source
  particle scales the small cap 6–12 times and references `FxRoot` / `bip01`.
  Runtime plates and billboard layers follow `Chest` to center the adapted
  shield around the character instead of its low root. Activation, hit and
  break feedback share this attachment. The opaque-black glow textures and
  shield plates use additive blending, so black contributes no light and does
  not obscure the scene. Original smoke textures retain their source alpha.
* Dark Heal: `si_darkgift_caster_01` / `si_darkgift_target_01` FXSpecs and their
  original lightning-core, electrical flash and flare textures. Hand effects
  follow `LeftWrist`; body effects follow `Chest`. Original finger/hand tracks
  remain in the additive animation library.

`b8-conversion.json`, `b8-geometry.json` and `b8-animation-conversion.json`
record output hashes and conversion metadata. Only the selected two GR2 meshes
and five JBA clips were converted. The shared conversion script accepts `--b8`;
the B5 GLB remains unchanged. Source JBA files were not modified. Original clip
lengths are 1.0 s (Death Field), 1.6 s (Barrier), 0.333 s (Dark Heal entry),
1.067 s (healing loop), 1.2 s (alternate healing end). Runtime gestures are
shortened through the existing blending/lease system, without rewriting clips.

## Audio (Client-derived media; authored playback)

`manifests/b8-audio.json` retains original event IDs, typed Wwise HIRC graphs,
exact DIDX offsets, media hashes and PCM conversions. The existing audio
extraction pipeline accepts `--b8`; it recovers the eight selected playable
event graphs and their 38 referenced media files. Runtime selects these voices:

| Use | Original event | Media ID | Source media bank |
|---|---|---|---|
| Death Field impact | `Play_ability_sithinquisitor_deathfield_impact` | `26930158` | `abilities_creatures_media.bnk` |
| Barrier activation | `Play_ability_sithinquisitor_backlash_chargeup` | `168062615` | `abilities_general_media.bnk` |
| Dark Heal | `Play_ability_sithinquisitor_darkgift_cast` | `142371144` | `abilities_imperialmelee_media.bnk` |
| Barrier break approximation | second original chargeup variation, reused | `388541304` | `abilities_general_media.bnk` |

There is no recovered independent barrier-break event binding. Break feedback
reuses the second chargeup variation quietly; it is not asserted to be native
break audio. The general Force buildup event was not found in the selected
bank; B8 does not substitute a fabricated event. Persistent source hum/smoke
graphs are retained as evidence but not played continuously, to avoid overlap.
All runtime voices register with the existing combat mixer, voice budget and
repeat limiter. Audio limits never suppress combat events or flytext. Death,
restart and scene teardown stop every voice, including one-shot tails.

## Known presentation limits

The Godot adapter reproduces a bounded subset of FXSpec/PRT behavior using
original meshes and pixels. Native zap/light primitives, complete particle
emitters, animated material graphs, projector composition, Wwise layering and
animation-note dispatch are approximated. Mesh placement, tint, opacity,
billboard atlas selection, size, fading and brief self gestures are authored.
Shield plates use the original cap geometry in a six-sided arrangement;
the source's complete rotating-emitter hierarchy is not reimplemented.
Persistent barriers observe Health and never retain the gesture controller.
Visual/audio failure cannot prevent the ability's gameplay effect.

Manual review is required for appearance fidelity, effect size, shield
readability, sound balance, animation blending and survival balance. No exact
MMO timings, combat rules or native client acceptance are claimed.
