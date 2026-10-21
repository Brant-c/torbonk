# Active pickup — D3 presentation corrections, 2026-10-21

User requested a handoff and STOP because only 1% usage remains. Resume only when they ask. Do not start Republic, planets, rotations, or unrelated work. Work under `D:/SWTORClassic/torbonk/pipeline`. Preserve the dirty working tree; it includes earlier milestones and D3. No commit/reset was requested.

## User's current corrections (authoritative)

* Powertech: Flame Sweep should visibly spin, emit transparent flames, and damage in a 360-degree area as the character spins. Existing billboard is black/off-center.
* Juggernaut: Smash currently looks like the Jedi Knight version, including blue icon. Use Sith Warrior version.
* Marauder: near correct, missing lightsaber sounds. Add lightsaber sounds across saber classes.
* Sniper: damage works but firing animation/sound/bolt are effectively missing.
* Operative: wrong animation and black-background effect.
* Assassin: spin/damage near correct, sound missing and huge red billboard effect is wrong.
* Further steering: orange/red generally denotes Empire/Sith; blue generally Jedi. Use original Imperial variants, not recoloured Jedi assets. Poison green and Sith purple can remain source-appropriate. Sorcerer's existing Force Lightning should be preserved.

## Baseline before this follow-up

D3 implemented all eight profiles, original modular starter-family armor, original main/off-hand weapons, automatic starters, shared DoTs/progression/HUD/previews. `D3-README.md` and `manifests/d3-smoke.json` describe the baseline; its 107 checks passed. Those passing checks PRECEDE the current incomplete corrections. Do not claim current working tree validated.

## Findings

1. `D3AbilityPresentation.End(true)` calls Release. A lethal single-target hit was invoking `Finish(true, "Target died / invalidated")`, deleting its new bolt/impact and stopping audio immediately. This explains disappearing Sniper presentation on kills.
2. Original flame/dart textures use additive blending. D3's ordinary Sprite3D renderer treated them as opaque, producing black rectangles. Need use correct additive material/shader, like existing Storm particles.
3. Lacerate's oversized red rectangle is `lightsaber02_trail_red_nomips` being shown as a large camera-facing impact. Remove that billboard; retain actual saber/pose and add saber swing audio.
4. Original Flame Sweep clip `cb_bounty_flame_sweep.jba` has major RootSpine rotation, but standard moving upper-body masks dilute it/remove lower-body pose. Full-body presentation is now configurable; physics must remain unrestricted. Root/world rotations sampled as identity; do not invent a world-motion decode. See `manifests/d3-spin-investigation.json` and `d3-pose-investigation.json`.
5. Sniper source fire appearance is `epp.agent.snipe.snipe_fire`: `RangeFireBig`, `sp_snipe_projectile_01`, standard blaster muzzle flash, and **Play_ability_spy_snipe_fire** (old binding used charge/activate).
6. Operative toxic dart source requests **RangeBracerFire** and left-elbow projectile, with **Play_ability_spy_poisondart_fire / impact**. Use original rifle-compatible bracer clip, not ordinary rifle fire.
7. Existing `smash.png` is visibly BLUE Jedi-style. Original April `forceslam.png` is PURPLE Sith fist/ground impact. `massivesmash` and `cb_saber_360_smash_01/02` are absent from April despite later filename hints. `cb_saber_ground_smash_01.jba` IS present and recovered as the Warrior replacement. Do not claim exact native semantic graph proof; document user-directed correction/source mapping.

## Already changed in this follow-up (UNBUILT/UNTESTED)

* `Gameplay/AbilityDefinition.cs` adds `FullBodyGesture`, `ImpactTailsOnTargetDeath`, `WeaponAudio`, `OffhandAudio`, `PresentationKind`, `MuzzleBone`, `EffectTint`.
* `AutomaticAbility.cs`: passes full-body flag to gesture owner; opt-in instant attacks finish normally after lethal hits so presentation tails survive. Sorcerer defaults retain old behavior.
* `CharacterPresentation.cs`: optional fourth `TryBeginGesture` arg for full-body presentation, standing/full-body blend override, clears flag on reset/release, avoids travelling to same gesture clip again.
* New `Gameplay/D3Particle.gdshader`: additive original texture shader. **Not wired into D3 presentation yet.**
* `EquipmentDefinitions.cs` adds optional `HumAudio` to attachment record.
* `EquipmentAttachment.cs` plays low-volume original looping saber sample for red blades when a gameplay AudioPresentation exists (menu previews do not have one). Uses duplicated AudioStreamWav, Forward looping, B7Combat -32 dB. Needs build/runtime verification, loop-length/format check. No physics damage.
* `godot/Assets/D3/casts.glb` has been rebuilt successfully (Blender log `logs/d3-fixed-animations.log`, finished 14:45:39). Updated `manifests/d3-conversion.json` replaces:
  - Smash = `cb_saber_ground_smash_01`
  - Sniper = `cb_rifle_sniper_fire_big_1`
  - Dart = `cb_rifle_bracer_fire_1`
  - adds SniperIdle = `cb_rifle_sniper_idle_1`
  - other original clips retained.
* `scripts/finish_d3_presentation_fixes.py` ran successfully. It de-duplicates resource properties, sets Empire icon `forceslam`, new sound/gesture/presentation data, removes Lacerate/Rupture impact billboards, sets Sniper idle profile, assigns saber hum in equipment catalog.
* Flame Sweep now configured as `.25 s` windup + `1.2 s` moving activation, four `.3 s` radial ticks at `4.25 damage` (17 total), `.5 s` recovery, `2 s` gesture, full-body. **D3AbilityPresentation still old**, so rendering and audio new fields aren't consumed yet. Existing D3 smoke assumes immediate one-hit starters and must be adapted for intentional Flame pulses.
* `snipershot.tres` previously accumulated duplicate CastAudio entries each time finish_d3_content ran (it only split on ImpactTexture, which Sniper lacked). New fix script deduplicates entries; fix original staging script idempotence too.

## Recovery/scripts/evidence

Original TORs opened read-only via maintained `SelectedSource`, no original archive/emulator edits. SWTOR research skill already read: `C:/Users/brant/.codex/plugins/cache/created-by-me-remote/swtor-emulator-research/0.1.5/skills/swtor-emulator-research/SKILL.md`. MCP is read-only, status available. No agents spawned; don't delegate absent user/skill authorization.

New scripts:
* `recover_d3_presentation_fixes.py`: exact April clip/icon recovery; updates D3 clip recipe. Source receipt `manifests/d3-presentation-source.json`. Bounded failed candidates retained as evidence. Unavailable `am_humanoid_weapon_specific.xml` was not extracted.
* `recover_d3_weapon_audio.py`: original `weapons_data.bnk` HIRC + original melee swing FXSpec strings. XML at `work/d3/weapons.xml`.
* `recover_d3_fix_audio.py`: reused original Wwise HIRC/DIDX slice decoder. Fire media receipt `manifests/d3-fire-audio.json`; weapon media `d3-weapon-audio.json`. Output `Assets/D3/audio.json` merges old + new events.
* `finish_d3_presentation_fixes.py`: idempotent configuration pass (currently must be run after original finish script). Ran successfully. Should integrate into main finish/stage flow to prevent overwriting fixes.
* `probe_d3_pose.py`, `probe_d3_spin.py`: original JBA metadata/rotation samples only, not gameplay modifications.

Weapon event source strings: `play_weapon_melee_righthand_swing_normal`, `...lefthand...`, `play_weapon_lightsaber_awesomeness_right/left` from original `audio_weapon_melee_swingright_normal.fxspec` / left. Wwise generic swing has many weapon-switch branches. **Do NOT play arbitrary first branch from generic audio.json graph**. Fix script selects saber state **1359942228**, also present in saber-only awesomeness event, through `work/d3-weapon/events/weapons_data-3001-event [3705301445=1359942228]...txtp`. Right swing media: 173808209, 137923394, 725568289, 157735310, 230536224, 223861396, 409101069, 120336853. Left event deduplicated by wwiser; share selected right media and document adapter. `manifests/d3-saber-branch.json` records selection. Weapon decoder initially sliced 177 media across conditional event branches; only chosen saber branch needs staging/use. No additional broad recovery needed.

Exact new GOM appearance evidence was retrieved through tree API during this chat but **not yet saved to file**. If needed, retrieve `epp.agent.snipe.snipe_fire` and `epp.agent.toxic_dart.toxic_dart_real` tree recursively under effAppearanceSchedule; original earlier evidence already includes toxic dart. Tool persistent JS store may contain `d3fixevidence` but don't rely on it across agents. Source provenance includes bucket hashes via MCP.

## NEXT: finish renderer, then focused validation

1. Rewrite/extend `Gameplay/D3AbilityPresentation.cs` (it is still OLD).
   - Consume PresentationKind, WeaponAudio/OffhandAudio, MuzzleBone, EffectTint.
   - Correct additive transparent sprites using D3Particle shader; don't recolour blue Jedi source assets.
   - Flame: start finite emission on Begin/Channel, spawn small source flame atlas particles from animated LEFT WRIST/bracer through the spin, following moving actor. No huge central billboard, no black panels. Four shared gameplay radial ticks already configured; rendering has no damage authority. End emissions on completion/interruption, allow finite tails on normal completion.
   - Lacerate/Rupture: no large impact billboard, retain genuine moving red sabers. Play selected original saber swing; Marauder off-hand second swing can be delayed slightly during animation.
   - Sniper: original fire sound (new binding), visible warm Empire muzzle flash and thicker brief red/orange bolt/ribbon. Instant damage is independent. Lethal shots must retain effects/audio. Use corrected sniper-specific clip.
   - Operative: original bracer animation/sound, small appropriate projectile/impact; remove old glob rectangle.
   - Smash: Sith ground slam, purple Sith icon/orange-red source impact. Full-body flag set. Preserve source artwork with additive blending.
   - Keep voice pool bounded; old renderer already allocates 3 voices per instance. May need 5 for ability + main/off-hand audio. Do not register/finalize new voices per attack (retains stale registry handles).
   - For muzzle positioning use actual EquipmentAttachment world transform with axis correction when RightWeapon/rifle; otherwise source skeleton LeftWrist/bracer. Guard preview/test instances and absent attachments.
2. Original finish_d3_content.py overwrites corrections and has duplicate sound-field bug. Make its output idempotent and invoke new correction pass last (or consolidate). `stage_d3.py` can similarly recreate old clip resources if rerun; document final corrective sequence.
3. Add/extend focused runtime checks for lethal-tail survival, actual active gesture, saber sound bound/played, additive shader bound, no Lacerate huge billboard, moving Flame full-body pose + four radial ticks covering front/back/left/right with no unintended duplicates. Existing `D3Smoke.cs` assumes 3 instant Flame hits and must account for intended four pulses. Avoid repeating long baseline suites. Build, import, one brief rendered runtime, then stop for manual review. User will judge presentation.
4. Update `D3-README.md` current source mappings/limits and `record_d3_delivery.py` receipt list to include new source/audio receipts; regenerate selected output hashes. Current receipts/readme reflect prior D3 baseline and are stale for these corrections.

Tools/runtimes:

* Python with Pillow: `C:/Users/brant/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`
* Blender: `C:/Program Files/Blender Foundation/Blender 5.2/blender.exe`
* Blender Python: `.../Blender 5.2/5.2/python/bin/python.exe` (no Pillow)
* Build: `dotnet build pipeline/godot/SwtorAssetInspection.csproj --no-restore --nologo -p:UseSharedCompilation=false`
* Godot: `pipeline/tools/godot/Godot_console.exe`
* Existing focused launcher: `pipeline/Run-D3-Smoke.ps1` (needs assertions updated first).

No compile, import, or runtime test has run since the current correction code edits. Animation conversion finished successfully; no other task process is expected to be running. User asked to stop here and leave handoff only.
