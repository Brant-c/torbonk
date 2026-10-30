# Sith Warrior focused implementation review — 2026-10-29

Reviewed the current working tree without changing gameplay or presentation code. Scope: the configured Marauder and Juggernaut kits, resource references, presentation dispatch, throw lifecycle, defensive cues, and the existing presentation smoke tests. This is implementation evidence, not verification of exact April-client audiovisual fidelity.

## Findings

1. **P2 — Juggernaut Vicious Throw has no flying weapon.** `WarriorAbilityEffects.cs:116` creates thrown-saber objects only when `MarauderVisual` is true. Juggernaut's `viciousthrow.tres` requests a throwing clip and projectile SourceFx, but receives only cast/contact effects and audio. The equipped saber is not leased into the flight path. Share the single-saber throw adapter with Juggernaut and align its 0.2-second damage windup with visible arrival. The smoke test checks damage/contact coverage for this ability but tests actual weapon flight only in the Marauder-only block.

2. **P2 — Juggernaut Endure Pain still displays a bounty-hunter energy shield.** `D3AbilityPresentation.ShowShield` falls through to `sphere_hexagon_shield.glb`, hexagon texture and scanlines. `D3PresentationRepairs.ShowDistinctShield` handles Warrior Saber Ward but not Endure Pain. Endure Pain therefore has a sustained visual, but uses the wrong visual identity. Add a Warrior-specific defensive treatment and verify its expiry with the actual shield.

3. **P2 — Defensive impact sounds are configured but unreachable.** Endure Pain configures cast `10681804.wav` and impact `256217830.wav`; Enraged Defense configures cast `61363040.wav` and impact `857325983.wav`. `D3AbilityPresentation.Impact` returns immediately for non-damage effects, and `BeginWarrior` plays their impact sound only when CastAudio is empty. Both have CastAudio, so neither configured impact sound plays through these paths. They are audible at activation; this is missing lifecycle coverage, not total silence. Decide the intended activation/proc timing and explicitly dispatch the selected cue. Enraged Defense's healing callback currently applies healing without a dedicated presentation callback.

4. **P3 — Enraged Defense retains a basic attack gesture and generic sustained cue.** Its StartClip and LoopClip are `Basic/Saber`; its active six-second window is recognized, but its ID has no dedicated texture selection in `AdvanceWarriorEffects` and falls back to `glow/bw_flare_gray.png` at chest/wrists. It works mechanically and has visible feedback, but still reads as a placeholder. Select a defensible recovered gesture and a distinct defensive/healing treatment. Native asset selection remains to be verified.

## Verification

- Enumerated 31 configured Marauder abilities and 16 Juggernaut abilities, including starters and basic attacks. All literal res:// references in those ability resources exist on disk. File existence does not prove correct asset identity.
- Fresh `Run-Warrior-Presentation-Smoke.ps1 -SkipImport` run: Marauder **242 checks passed**, Juggernaut **117 checks passed**. Build succeeded. The runner also checks runtime logs for ERROR/SCRIPT ERROR entries.
- Build reported NU1900 because online NuGet vulnerability metadata was unavailable.
- Tests confirm loaded clip names, gestures, damage/contact dispatch, Smash landing, and several Marauder utility/throw cases. Their passing result does not establish the missing Juggernaut projectile, correct Endure Pain artwork, or the two defensive impact sounds.
- No fresh rendered capture or listening pass was performed. Do not interpret the resource check as a claim that every sound or animation is artistically correct.

## Current context

Yesterday's presentation fixes are present: shared Smash, per-victim contacts, distinct Carnage clips, camouflage fading, overlapping Marauder auras, and timed Marauder saber flights. The older report's claim that Marauder Force Charge is excluded/disabled is stale: it is now in the configured kit and has a distance-based activation rule. Treat current source and fresh tests as the review baseline.

Suggested order: Juggernaut throw, Endure Pain identity, defensive sound lifecycle, then Enraged Defense gesture/proc feedback. Extend the presentation fixtures to cover these specific behaviors before another visual/listening pass.
