# Powertech refinement — October 23, 2026

Powertech starts with Flame Sweep again, per gameplay review. Flame Burst is an optional unlock. Flame Sweep's original resource presentation, emitter, animation and audio are retained.

Shield Tech can equip Energy Shield, Carbonize and Oil Slick using a Powertech-only three-utility profile override. Offensive slots remain four. Powertech can choose Explosive Fuel; Rocket Punch is excluded from the drop pool; Pyrotech can choose Incendiary Missile, Flame Sweep and Flame Thrower. No specialization selection is required. Explosive Fuel retains its existing reactivearmor internal ID, resources and buff and is removed from Mercenary's kit/offer eligibility.

Four active run-local mechanic talents replace the old ambient/death-spread Powertech talents; Accelerant and Prototype Weaponry remain implemented but are excluded from offers with their removed abilities. Mechanic descriptions explicitly identify authored Survivors behavior. Prerequisites and rarity use existing progression rules. Combustible Spread snapshots the union of targets hit across a single Sweep's pulses, detects the caster's own missile status before hits, and applies at completion. It never performs a proximity scan. Reapplication uses PeriodicEffects refresh semantics. RunBuildEffects.SpreadLimit (4) and SpreadEffectiveness (1) are balancing controls. All targets get separate timers, events/flytext and target-attached visual lifetimes. Accelerant caps remaining burn time at eight seconds.

Carbonize blocks movement and attack processing, freezes presentation, restores it on expiry/death, and uses a recovered humanoid carbonize pose where the rig has enough matching bones. Nonmatching creature rigs freeze their current pose under original carbonite artwork. Elites receive 35% stun duration; major bosses are immune. Reinforced Shielding grants 5% reduction per successful stun, capped at 25%, for four seconds.

Energy Shield keeps its original April shield mesh, hexagon/scanline shader resources and independent 30-point absorption. Tint is corrected to blue. Pyro Shield triggers only while this Energy Shield is active, against attackers within .36 units, with a global one-second lock, eight internal/fire damage and original flame feedback. Retaliation never invokes another attacker hook. Oil Slick persists eight seconds; its slow and damage penalty expire .2 seconds after leaving. NPC melee, boss and tomb attacks use the same conditional defensive multipliers; defaults are neutral for all other runs.

## Original assets and provenance

- manifests/powertech-source.json: exact April archive entry receipts, decoded byte hashes and selected dependency paths.
- manifests/powertech-conversion.json and powertech-animation-conversion.json: recovered Rocket Punch, sustained flame start/loop/end and carbonize-loop JBA conversion, in a separate Powertech library. Existing character libraries are untouched.
- Assets/Powertech/source-parameters.json and fx.json: original FXSpec/particle/projector parameters for carbonite, missile burns, Oil Slick, Rocket Punch and Flame Burst.
- manifests/powertech-audio.json: original Wwise event/HIRC/DIDX media evidence for Carbonize, Oil Slick, Flame Burst and missile target fire. Carbonize/Oil Slick/Flame Burst cast PCM is bound. Existing missile, Energy Shield and Explosive Fuel sound bindings are retained.
- manifests/powertech-icon-evidence.json: April Incendiary Missile actually declares ablIconSpec=explosiveprobe; that original icon is deliberately retained.

Godot effects adapt original textures and clips; they do not execute native SWTOR particle simulation. The native bh_flamethrower FXSpec and LOD2 candidate were unavailable in the selected April archive; the cone uses recovered April fire artwork with original sustained flame animation. Exact Rocket Punch ability audio remains unbound. Missile target-fire loop PCM was recovered but is not played per NPC to avoid multiplying overlapping loops. These are remaining presentation limitations for manual review, not claims of exact native presentation parity.

## Validation and review boundary

C# build succeeded. A brief headless smoke fixture passed 28 checks (see manifests/powertech-smoke.json), covering startup, eligibility, single-target missile, attached burn status, activation-bounded spread, foreign-caster rejection, independent ticks/expiry, shield absorption, retaliation lock, stun stop/recovery/resistance, Oil Slick expiry and frontal cone repeated/rear target behavior. An initial smoke found an incorrect icon assumption; April GOM evidence corrected it. Final runtime log had no errors or warnings. Build reports existing nullable warnings and an offline NuGet audit warning.

Run Run-Powertech-Smoke.ps1 for the bounded fixture after importing new assets. Stop here for manual gameplay and visual review. Review flame alignment while moving, carbonite poses on creature/humanoid rigs, blue shield scale, oil ground alignment and the three build directions. No full regressions, archive hashing passes, screenshots or extended gameplay runs were performed.


## Visual correction after gameplay review

The texture-card presentation was incorrect for both original projector maps. Carbonize now applies an instance-local triplanar material overlay to the enemy's actual skinned mesh surfaces using proj_carbonite_01; it preserves previous overlays and restores them when the stun ends, the enemy dies or the effect exits. Refresh reuses the coating instead of stacking cards. Burn sprites retain their existing independent effect path.

Oil Slick uses StormAreaIndicator's existing terrain-only ray sampling, slope-following mesh, hole/cliff guards and surface lift. Original proj_oilpuddle_border luminance provides an irregular transparent mask and proj_oilpuddle_wrap provides detail; neither is displayed as an opaque quad. The ground projection is created after the slick's activation position has been assigned. Slow, damage reduction and duration are unchanged.

Flame Sweep is the sole starter again. Flame Burst replaces it in the locked kit and unlock-offer list; talent prerequisites remain consistent. Flame Thrower, its unlock and Engulfing Flames now use bountyhunter artwork, as declared by April abl.bounty_hunter.flame_thrower ablIconSpec (see manifests/powertech-flamethrower-icon.json). This historical icon name is intentional.

C# compilation passed. A brief rendered smoke passed 30 checks, including the restored starter, mesh coatings without carbonite billboards, actual terrain projection, burn spread, defensive cleanup and cone targeting. One rendered frame was inspected at manifests/powertech-projection-review.png: no rectangular oil card or carbonite billboards remain. Rendering reported a sandbox user shader-cache directory warning; no shader compilation errors. Stop for manual gameplay review.


## Pool, burn-seed and Carbonize caster correction

Flame Burst and Rocket Punch are excluded from the Powertech kit and unlock pool; their dependent Accelerant and Prototype Weaponry talents are also excluded. Assets/code remain for reference.

Incendiary Missile now deals 3 upfront damage (previously 22), with the existing 3 damage per periodic tick. Its burn lasts six seconds (previously four), allowing time for the next Flame Sweep to propagate it. It selects highest **current absolute health** within its range and line of sight, with deterministic distance/identity ties. The new targeting rule is appended to the enum and used only by IM; existing enum values and other abilities' selection behavior are preserved. Combustible Spread remains the prerequisite talent for propagation.

April appearance epp.bounty_hunter.carbonize.carbonize_cast explicitly uses cb_bounty_flame_sweep. Carbonize now binds the existing recovered D3/Flame spin as a full-body gesture; it waits for another gesture to end and keeps its own spin through recovery. It has a .25-second startup and a two-second presentation; the stun still applies once. Its separately recovered original chargeup event is now bound at activation and the original spray sound at the stun impact, with a class-local gain adjustment. The caster emits original carbonite_spray_02 particles from the left wrist during the spin using the source image's actual alpha channel (measured 0..253), through the alpha material. Opaque black-background/projector texture cards are not used for this spray. Enemy mesh coating and Oil Slick terrain rendering remain unchanged.

Source: manifests/powertech-carbonize-cast-evidence.json and powertech-audio.json. C# build passed. Brief rendered smoke passed 36 checks including Rocket Punch exclusion, highest-health IM targeting, 3-point upfront damage, Carbonize gesture ownership, original sound playback and emitter lifecycle. Existing sandbox shader-cache warning remains; no shader compilation errors. Stop for manual gameplay/sound review.
