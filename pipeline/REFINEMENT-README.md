# SWTOR Survivors — B5.1

B5.1 stops at locomotion/casting compatibility, independent automatic abilities,
and action-bar feedback. No new abilities, enemies, terrain, or bars are added.
Original GLB/JBA, textures, effects data and PCM audio are unchanged.

Launch for manual review:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Gameplay.ps1'
```

Add `-Editor` for the Godot editor. WASD moves; abilities activate automatically;
R restarts; 1/2/3 or a click selects an upgrade; F3 toggles diagnostics; Esc exits.
Lightning starts unlocked. Shock and Storm still unlock through level-up choices.
There is one existing twelve-slot SWTOR action bar, without a cast bar.

## Animation

The recovered Sorcerer has 211 animated nodes/bones and 633 transform tracks per
clip. Its `Root` splits into `Pelvis` (legs/skirt) and `RootSpine -> LowerBack ->
Chest -> Chest1 -> Chest2` (collars, arms, head, fingers and upper VFX sockets).
CharacterPresentation builds an instance-local Godot AnimationTree over the
original animation library. Run/Idle keep advancing during gestures. While
running, the pelvis, hips, knees, feet and leg-attached skirt tracks come only
from locomotion. Upper casting uses original SWTOR tracks, including all finger
and socket tracks under Chest1. The lower-back and chest masks use configurable
0.30/0.65 weights; Chest1 and its descendants use full casting weight. At rest,
the full original casting pose fades in over locomotion. Transitions into/out of
movement and between gesture clips use a configurable 0.16-second blend.

The four gesture branches have synchronized state-machine replicas so each
blend-tree output has a single consumer. Sync is enabled even at zero blend
weight, keeping standing and moving copies at the same animation time. Only
CharacterPresentation writes the live skeleton. AnimationPlayer retains the
source library but no longer competes with the AnimationTree for playback.
Hit and Death form the final full-body override; Hit expires, Death holds until
reset. Source libraries are duplicated in memory before loop configuration.

AutomaticAbility requests one gesture owner, switches its own clip by phase,
and releases it after GestureSeconds. An occupied/fading gesture is skipped;
there is no stale gesture queue and no second dominant upper-body gesture.
Reactions can release a gesture while its ability continues. An ability ending
cannot release another ability's gesture. VFX always read the evaluated original
skeleton sockets; skipped gestures do not suppress damage, VFX or audio.

## Ability execution and tuning

AbilityController advances every ability before considering a new activation.
Round-robin admission remains fair among ready, targetable abilities. The GCD
regulates new activations rather than channel/effect lifetimes. Each ability has
its own target, phase clock, cooldown, presentation and damage ticks. Cooldowns
start on activation and retain their captured duration even if upgraded later.
Each ability can have one active instance; a very short configured cooldown does
not create overlapping copies of that same ability.

| Ability | Survivors tuning | Role |
|---|---|---|
| Lightning | 12 Energy x 3 ticks; 0.4 s windup, 0.9 s burst, 0.3 s ticks, 0.16 s recovery; 1.8 s cooldown | Starting focused single-target burst, interrupts on invalid target |
| Shock | 20 Energy once; 0.12 s windup, 0.28 s recovery; 3.8 s cooldown | Rapid supplementary hit, useful against weak/briefly exposed targets |
| Storm | 5 Energy per target x 5 ticks; 0.3 s activation, 4 s area, 0.8 s ticks; 8 s cooldown | Cluster clearing; fixed 0.32-radius area, 0.8 s gesture |

These are configurable Survivors values, not restored MMO balance. All three
currently obey and trigger the shared 1.5-second GCD. Shock remains an ordinary
optional unlock: its value is rapid delivery without a sustained channel; it
still spends a GCD and is not free extra damage every frame. Its original assets
and ability-specific upgrade remain available. This role/pool choice is subject
to manual balance review alongside Lightning's larger single-target burst.

Storm validates acquisition and activation range, then persists independently
of its original seed target, the caster's movement/range, other casts and GCD.
Every area tick checks current hostile membership, health, radius and line of
sight from the area center rather than from the moving caster. Death, restart
and scene exit clean up every effect. Existing Health, targeting acquisition,
enemy AI, XP, leveling, rarity, terrain and navigation remain in use.

The Lightning effect adapter has an externally driven mode for gameplay phases;
its original standalone inspection behavior remains the default. Presentation
adapters cannot own playback or apply damage. Storm's caster arc ends with its
gesture while its original-textured ground strikes/audio continue independently.

## GCD and the existing bar

`AbilityController.GlobalCooldownSeconds` defaults to 1.5. AbilityDefinition has
`UsesGlobalCooldown` and `TriggersGlobalCooldown` switches for future exceptions.
All activation attempts, including exceptions, share a configurable 0.18-second
minimum spacing. A GCD-triggering exception cannot restart an existing GCD.

The existing icon artwork and slot layout are retained. Every quickslot, including
empty slots and slots for GCD-exempt abilities, displays the same top-to-bottom
GCD wipe using original SWTOR cooldown artwork. Populated slots separately show
an individual cooldown wipe with numeric seconds. Each uses its actual gameplay
timer and captured duration, updated every displayed frame.
A clear icon with no wipe is ready; GCD marks a temporary GCD-only block;
ACTIVE marks an ongoing effect if its individual cooldown has already expired.
The original readiness strip also follows the GCD. One ability's activation
cannot reset another's individual cooldown or sweep. Pausing freezes clocks and
wipes together. `CooldownWipe` translates the original 50x100 blue-edged texture
inside a rectangular slot mask; it does not recolor the source or rotate a radial
overlay. Source `bwaui_slot.gfx` sprite 155 instantiates sprite 140 separately as
`mc_cooldownStates` and `mc_gcdCooldownStates`. Its default style binds subimage
117 from atlas 10 at [929,308,979,408] (XYXY), through shape 118 and sprite 119,
clipped by shape 115's 50x50 mask. The Godot adapter drives top-to-bottom movement
from the gameplay clock; original ActionScript timing is not executed. See
`manifests/ui-player-hud.json` and `UI-PROVENANCE.md`.

## Future additions — design only

Keep separate AbilityDefinition resources for targeting, timing, GCD policy,
gesture clips and presentation event keys. Extend the shared lifecycle with an
explicit gameplay-effect strategy selected by data rather than hard-coded ID
branches. Target acquisition may need a Self rule and ground selection needs an
explicit policy. The present Energy-damage executor is not yet a generic buff,
shield or status engine; those extensions are intentionally deferred.

| Future ability | Gameplay-effect extension | GCD/gesture policy to configure |
|---|---|---|
| Death Field | One burst on an area query, then status instances keyed by target and source; durations/stacking configured separately | Brief dominant gesture; burst/debuff lifetime independent of it |
| Static Barrier | Self/ally targeting plus an absorption component before Health damage; capacity, expiry and replacement/refresh policy in data | Defensive GCD exception if desired; optional short gesture |
| Force Slow | Targeted timed movement modifier consumed by EnemyAgent/navigation; expiry, strength, stacking and immunity in data | Short gesture; control duration independent of animation |

Do not overload DamageType or fake shield/control effects as damage ticks.
Future effect instances should have explicit cancellation on death/reset and
independent lifetime/stacking rules. Reuse UpgradeDefinition.AbilityId to scope
rarity-scaled damage, capacity, debuff or duration modifiers; add explicit upgrade
effect kinds where needed. Preserve Premium green (#55d884), Prototype blue
(#60c2ed) and Artifact purple (#be8bf1), and existing 1.0/1.25/1.5 multipliers.
No future ability has been implemented or put in the unlock pool.

## Review limits

The moving mask preserves the original local hand/finger animation, but torso
blending changes world-space reach and cannot reproduce the stationary MMO pose
exactly. Run speed is not stride-warped; strafing uses the existing movement-facing
behavior. Original start/end gestures are deliberately shortened at authored
phase boundaries. Shock may lose its gesture when another has priority; its hit
still occurs. Audio overlaps are allowed per active effect, with the existing
bounded source-audio adapters. Existing reconstructed FX, cloth, source scale,
camera and collision limitations remain; see ABILITIES-README.md for provenance.

Only a C# build and the brief B5.1 runtime smoke are intended for this milestone.
The older B2/B5 validation scripts contain full-body/exclusive-cast assumptions
and are historical fixtures, not acceptance checks for this architecture.
Use Run-Refinement-Smoke.ps1 to repeat the bounded B5.1 check. Manual review should
cover moving Lightning, start/stop transitions, waist/shoulder motion, Shock's
short gesture, Storm + Lightning overlap, icon sweeps, and paused/death/reset UI.
Stop at B5.1 until that review is complete.

## Validation result

C# compilation passed with zero errors. The existing NU1900 warning concerns
unavailable NuGet vulnerability-feed metadata; restored dependencies compiled.
The completed bounded rendered smoke passed: all three cast poses retained exact
Run leg poses, upper poses and sockets remained distinct, all damage paths ran,
GCD admission restricted new activations, Storm ticked during GCD after releasing
its gesture and survived leaving caster range, individual cooldowns stayed
independent, and pause/death/restart completed. No runtime errors remained.

Early smoke attempts exposed a fixture timing issue: AnimationTree synchronizes
new AnimationPlayer libraries through deferred signals, so the comparison must
wait for a process frame after loading them. The final fixture does so. A small
ChannelClock tolerance also preserves final ticks for float-authored 0.9/0.3
and 4.0/0.8 timings. Runtime verification stores its temporary application data
under `pipeline/work/refinement-user` to keep sandbox cache writes local.

Evidence: `manifests/refinement-smoke.json`, `logs/refinement-smoke-runtime.log`
and `logs/refinement-check.log`. No historical regression suites, source hashing,
captures or benchmarks were run. Stop here for manual gameplay/visual review.

The initial quickbar refinement extended the shared GCD sweep to all twelve
slots, including empty slots. Individual cooldown feedback stays ability-local.
This UI-only follow-up was compiled; its appearance is left for manual review.

The earlier radial GCD correction removed the native Range control's default step of 1:
on a normalized 0–1 cooldown it rounded fractional time to full/empty, hiding the
radial animation. Both individual and shared sweeps now use Step=0. The remaining
sector shrinks counterclockwise so the clearing edge travels clockwise across
each slot. The shared gameplay GCD remains 1.5 seconds. A focused rendered UI
probe confirmed 91 distinct sweep positions across all twelve slots (eleven
empty), while the individual cooldown retained its separate value. Compilation
passed; see `logs/gcd-sweep-build.log` and `logs/gcd-sweep-runtime.log`.

The final vertical GCD correction supersedes that radial presentation. It uses
the recovered original SWTOR slot cooldown texture and rectangular mask for both
individual and shared cooldowns. Compilation and a focused rendered check passed:
91 distinct downward edge positions on all twelve slots (eleven empty), correct
original atlas region, independent individual timing, and overlay cleanup at
readiness. One UI-only frame was inspected. Evidence:
`logs/gcd-vertical-build.log`, `logs/gcd-vertical-runtime.log` and
`work/ui/gcd-vertical-runtime.png`. Manual in-game review remains the next step.
