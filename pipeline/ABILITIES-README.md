# SWTOR Survivors — B5

The original Force Storm lightning-layer and sustained moving-cast restoration
is documented in [STORM-SOURCE-README.md](STORM-SOURCE-README.md). It supersedes
the simplified Storm VFX and short Storm gesture described below.

Force Storm's subsequent green ground-indicator enhancement is documented in
[STORM-RETICLE-README.md](STORM-RETICLE-README.md), including original green
projector provenance, live-radius scaling and focused verification.

**B5.1 supersedes the gameplay, animation-lease and cooldown behavior below.**
See [REFINEMENT-README.md](REFINEMENT-README.md) for current tuning, architecture,
launch instructions, future ability design and review limitations. The B5 asset
provenance below remains applicable.

B5 is ready for manual review. Force Lightning starts unlocked. Shock and Force
Storm unlock through level-up choices and then attack automatically. Death Field is deferred.
The existing arena, movement, enemies, waves, leveling, portraits and single
twelve-slot action bar remain in use.

Launch:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Gameplay.ps1'
```

Add `-Editor` to open the gameplay scene in Godot 4.7.2 .NET. The launcher
builds C# first and selects `Gameplay/GameWorld.tscn`; the project's main scene
remains the original inspection scene. WASD moves, R restarts, F3 toggles
diagnostics, Esc closes. Click an upgrade or press 1 / 2 / 3 during selection.
There is no additional action bar or manual ability-key activation.

## Combat and presentation

| Ability | B5 damage | Timing | Targeting |
|---|---|---|---|
| Force Lightning | 8 Energy per tick | Original 1.067 s start, 3 s channel, 0.5 s ticks, 0.5 s recovery, 1 s cooldown | Nearest visible hostile within 0.85 source units; invalidation ends the cast |
| Shock | 22 Energy burst | 0.22 s impact cue, remainder of original 1.067 s clip, 6 s cooldown | Nearest visible hostile within 1.0 source units |
| Force Storm | 7 Energy per target per tick | Original 0.867 s start, 6 s channel, 1 s ticks, original 0.4 s end, 8 s cooldown | Hostile-centered cluster within 1.1 source units, fixed 0.32-radius sphere |

These are configurable **Survivors tuning values**, not restored MMO damage
formulas. Physical source units remain uncalibrated. Storm's eight-second
cooldown is authored for this game; April's selected ability has no explicit
ability cooldown. Its six-second channel is April-derived. Shock's selected
April cooldown is six seconds. No Force costs, resistance/armor calculation,
stuns, crits or talent rules are restored.

`AbilityDefinition` resources under `godot/Gameplay/Abilities/` contain damage
type, damage, cooldown, target rule, area mode, range, radius, windup, channel,
tick and recovery timing, original animation keys and presentation event keys.
`AutomaticAbility` owns one reusable cast lifecycle and applies damage through
the existing `Health` component. Damage type is recorded by Health; mitigation
is not implemented. The existing integer-index `ChannelClock` supplies ticks.
`AutomaticLightning` is a small compatibility adapter for older fixtures.

`AbilityController` gives one ability the full-body animation lease, including
start/channel/end. Ready abilities take turns in round-robin order, beginning
with Lightning. A newly ready ability waits rather than restarting an existing
cast. Cooldown clocks advance independently during other casts; each recovery
cooldown begins after its own cast ends. A shared 1.5 s global cooldown begins
at each trigger and is visible as a radial sweep on every populated slot.
The bar also shows independent cooldown seconds, CAST and upgrade rank/color.
Hover a slot for current damage, cadence, range, radius, cooldown and upgrades.
The blue readiness strip shows global cooldown readiness.

Storm locks the selected cluster center at cast start and continues if its
seed enemy dies. Every tick rechecks living hostile membership, range and
environment line of sight. New enemies can enter the area, and enemies can
leave it. Leaving cast range cancels Storm. Single-target casts cancel on
target death, removal, lost range or blocked line of sight.

The unchanged A3 Force Lightning effect remains behind `LightningPresentation`.
`OriginalAbilityPresentation` implements the same presentation interface for
the new abilities. It owns animation, source-textured ribbons/flashes and
decoded audio only. It cannot apply damage. A reported presentation-start
failure does not suppress gameplay damage. Death, restart and scene exit
release effects, sounds and animation ownership.

## Upgrades and rarity

Shock and Force Storm enter the ordinary randomized three-choice level-up pool
as **NEW ABILITY / UNLOCK** cards. Each can be selected once per run, in either
order; an unlock is not guaranteed on every level. Locked abilities cannot
cast and do not populate action-bar slots. Their specific stat upgrades become
eligible only after unlocking, and an unlocked ability's unlock card leaves
the pool. General ability bonuses already trained apply to newly unlocked
abilities too. Restart returns to Force Lightning alone.

Ability resources configure this with `StartsUnlocked`; one-rank upgrade
resources use `UnlockAbility` and `AbilityId`. Unlock rarity retains the
existing card color but does not multiply the ability's base stats or count
as a stat-upgrade rank.

The exact existing rarity names and colors are preserved:

| Rarity | Existing color | Default effect multiplier |
|---|---|---|
| Premium | Green — #55d884 | 1.0 |
| Prototype | Blue — #60c2ed | 1.25 |
| Artifact | Purple — #be8bf1 | 1.5 |

`Gameplay/Upgrades/rarity.tres` configures the multipliers. Each upgrade's
`AmountPerRank` is multiplied by its rarity strength; a custom `RarityRules`
resource can override the shared rules. Rarity does not weight offer selection.
Cards show effective amounts. Existing damage/range/recovery upgrades affect
all abilities; tick-frequency upgrades affect channels. Surging Shock increases
Shock damage, and Gathering Storm increases Storm radius. Health and movement
upgrades retain their existing roles. Bonuses add relative to base values;
recovery/tick rates divide the base interval by 1 + total bonus.

`AbilityId` scopes an upgrade to an ability; an empty value applies to all
appropriate abilities. Tooltips show effective stats and the highest trained
rarity affecting that slot. The small +N badge counts affecting trained ranks.
Tick cadence is captured at channel start; a cadence upgrade affects the next
channel. Damage/range and future cooldowns use the updated stats. Existing
cooldown time already in progress is not retroactively shortened. Restart
clears all ranks, restores the captured base values and resets ability unlocks.

## Original asset provenance

Evidence is **Client-derived**, from operator-labelled April 2012 archives.
GOM names come from April buckets; some decoder field/enum labels come from
the established historical name map. This is not native-client parity.
Original archives and the emulator were not changed.

| Original definition | GOM ID | Deciding evidence |
|---|---|---|
| `abl.sith_inquisitor.shock` | 0xE0000E5C9702DB07 / 16140916855329184519 | Bucket 19, offset 14364; icon shock, cooldown 6, appearance cast_instant |
| `abl.sith_inquisitor.force_storm` | 0xE000A6C0C1C1F0C2 / 16141084411310502082 | Bucket 82, offset 203908; icon forceStorm, channel 6, ground target, channel_loop |

The saved expandable GOM pages include appearance schedules for Shock
`cast_instant` and Storm `channel_loop`, `channel_end` and `aoe`.
Shock's PlayAnim is `fp_1h_elect_instant_right_1`; Storm uses
`fp_lightning_storm_start`, `fp_lightning_storm_loop` and
`fp_lightning_storm_end`. All four selected `bmnnew` JBA files were found
in the April TORs and converted with the existing GR2/JBA Blender importer
and A2 sampling policy. `Assets/Abilities/casts.glb` supplies an additive B5
animation library, redirected by exact bone names to the live original rig.
The established `sorcerer.glb` is unchanged. Non-bone/world transform tracks
are omitted from the additive library; movement remains physics-driven.

The appearance graph selects:

- `si_shock_01.fxspec` and `si_shock_target_01.fxspec`.
- `abilities/sith_inquisitor/si_forcestorm_cast.fxspec`,
  `si_forcestorm_targetarea.fxspec` and particle-emitted
  `si_forcestorm_lightningstrike.fxspec` /
  `si_forcestorm_caster_groundbolts_01.fxspec`.
- `abilities/all/all_imp_target_reticle_gtaoe_persist.fxspec`.

Dependency traversal follows particle/material/FXSpec links to 67 selected
resources, including four JBA clips, two DDS icons and seventeen FX textures.
It records exact TOR entry hashes, archive/data offsets, source hashes,
dependency fields and unresolved engine primitives. DDS conversion uses the
existing Pillow path without resampling. The adapter uses Shock's original
512_SingleArcB_bw and electric_orb textures and Storm's original
4x1_tiling_electrical_bw and glow_basic_alpha textures. Other recovered textures
remain available for later fidelity work.

Shock's original FX attaches arcs at `vfx_hand_left` / `vfx_elbow_left`,
and target effects reference `vfx_aim01` and limb sockets. Storm's source
defines a ground target, particle-emitted strikes, caster elbow/weapon/root
attachments, a 0.55 s target-area emitter delay and the original texture and
lightning width/envelope fields. The bounded adapter uses the exact existing
vfx sockets where practical, a fixed gameplay center and source-textured
ground strikes. Complete raw parameters are retained separately.

Original Wwise names are recovered from appearance dynamic @audio values and
FXSpec sound fields, joined by lowercase FNV-1 to typed HIRC Event objects,
then followed with the existing wwiser pipeline to Sound source IDs.
The selected media are read from their actual bank's DIDX/DATA ranges; HIRC
offset and size are checked against DIDX. Forty linked Sound references resolve
to 37 distinct media files, decoded with the existing vgmstream to unnormalized PCM WAV.

| Original event | Wwise Event ID |
|---|---|
| Play_ability_sithinquisitor_shock_buildup | 4218212909 |
| Play_ability_sithinquisitor_shock_cast | 494826667 |
| Play_ability_sithinquisitor_shock_explode | 643435821 |
| Play_ability_sithinquisitor_forcestorm_cast_caster | 1810829058 |
| Play_ability_sithinquisitor_forcestorm_channelloop | 3961591709 |
| Play_ability_sithinquisitor_forcestorm_aoe_bolt | 2849835463 |
| Play_ability_sithinquisitor_forcestorm_explode | 3269593009 |
| Play_ability_sithinquisitor_forcestorm_fieldstaticloop_caster | 1213601329 |
| Stop_ability_sithinquisitor_forcestorm_channelloop | 3122828659 |
| Stop_ability_sithinquisitor_forcestorm_fieldstaticloop_caster | 425345835 |

The new runtime plays buildup/cast/explode for Shock and cast/channel/bolt
for Storm, selecting one bounded media variation per event. Other recovered
events and full graph text remain available in the manifest. Godot loop/fade
cleanup approximates original Stop actions; this is not the Wwise mixer.

Jedipedia references checked:
[Shock](https://swtor.jedipedia.net/en/abl/shock-3) and
[Force Storm](https://swtor.jedipedia.net/en/abl/force-storm-17).
The public pages identify the same ability GOM IDs and names but show later
rules, including a shorter Storm channel. April archive evidence takes
precedence for historical claims.

| File | Contents |
|---|---|
| `manifests/abilities-gom-evidence.json` | Original identities, bucket hashes/offsets and expanded schedule pages |
| `manifests/abilities-source.json` | Bounded extraction receipts and dependency graph |
| `manifests/abilities-conversion.json` | Texture receipts, clips and decoded literal parameters |
| `manifests/abilities-animation-conversion.json` | Four JBA identities, durations, sampling and additive GLB |
| `manifests/abilities-audio.json` | Original event IDs, wwiser graphs and exact bank/media offsets and hashes |
| `godot/Assets/Abilities/fx.json` | Nested original FXSpec parameters consumed by the adapter |
| `godot/Assets/Abilities/source-parameters.json` | Literal dependency fields, including particle resources and sockets |
| `scripts/prepare_abilities.py` | Existing SelectedSource reader and Pillow dependency conversion |
| `scripts/convert_ability_animations.py` | Existing Blender/JBA import policy, additive export |
| `scripts/prepare_ability_audio.py` | Existing wwiser/vgmstream chain, exact linked media selection |

## Known limitations

- Original `engine/zap.zzp` and `engine/light.lit` are engine primitives,
  unavailable as independent April archive resources in the selected lookup.
  Godot ribbon geometry/light behavior is reconstructed; no substitute art was
  introduced. Source curves, spline semantics, particle emitter counts,
  projection, noise and full attachment-alias behavior are not reproduced.
- The original Storm reticle FX and textures are recovered. The subsequent
  green indicator adapter projects original targeting art onto terrain; see
  STORM-RETICLE-README.md. Full native projector execution is not implemented.
  Its generic orbital-bombardment reticle audio events were not found in the
  selected Imperial-melee data bank; no claim of global absence is made.
- Shock impact at 0.22 s is an authored cue. Original appearance note keys
  were recovered, but exact note timestamps/native note dispatch remain
  unresolved. The original clip plays for its full duration.
- Wwise layers, bus routing, random history and spatial mix are approximated
  by bounded PCM playback. Full original graph evidence is retained.
- Storm particles and strikes approximate the source FX without recreating
  the native emitter executor. Texture atlases, smoke, full caster-hand sparks,
  full native projected-reticle animation and all limb arcs are not executed.
- Existing source scale, cropped terrain, cloth, camera and collision limits
  remain. This milestone does not add enemies, bosses, planets or terrain.

## Lightweight validation

C# build passed with zero errors. NuGet vulnerability-feed metadata was
unavailable (NU1900); compilation used the already restored dependencies.
New assets were imported in Godot; one rendered B5 runtime smoke passed.
It checked the Lightning-only start, locked cast/upgrade exclusion, both actual
level-up unlock selections, duplicate-unlock exclusion, dynamic bar updates,
unlock reset, all three damage paths, multi-target Storm, independent cooldowns,
one simultaneous full-body lease, original clip availability, one action bar,
rarity-scaled area upgrade, pause, death and restart cleanup.

Evidence: `manifests/abilities-smoke.json`,
`logs/abilities-build.log` and `logs/abilities-smoke-runtime.log`.
No historical asset regression suite, archive rehash, performance benchmark or
visual acceptance pass was run. Visual/gameplay review is left to the user.
Optional repeat: `pipeline/Run-Abilities-Smoke.ps1`. Stop after B5.
