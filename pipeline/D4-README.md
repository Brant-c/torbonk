# D4 — Korriban: Trials of the Sith

Korriban now has a level 1–30 core run with six serialized trials, six ordinary enemy types, four optional caches and a larger original terrain assembly. Launch normally with `Open-Survivors.ps1`, select any Imperial advanced class and Korriban. `Open-Gameplay.ps1` also opens the expanded Korriban composition. Other planets keep their existing scenes and configuration.

## Implemented progression

Regions are an **open-air Survivors adaptation** on contiguous original Valley terrain. Their arrangement is authored; they are not a claim that the MMO's Lower Wilds or four named tomb interiors have been reproduced. Exploration is optional: trials find a safe nearby spawn around the player, so staying in the opening region does not block the core run. The burial courts provide optional encounter backdrops.

| Levels | Progression region | Active enemies | Milestone | Reward |
|---|---|---|---|---|
| 1–5 | Valley of the Dark Lords | Broodlings, Shyrack Foragers | 5: K'lor'slug Brood Mother, brood call with adds | Three elite choices; one XP cache |
| 6–10 | Tomb Precinct | Broodlings, Shyracks, Crypt Looters | 10: **Broodmatron**, locked burrow eruption | Three major choices, including an eligible defining relic |
| 11–15 | Tomb Precinct | Looters, ancient droids, Shyracks, broodlings | 15: Shyrack Screecher, directional call and reinforcements | Three elite choices; guarded relic cache |
| 16–20 | Lower Wilds | Spitting Tuk'ata, Shyracks, Terentatek Maulers, ancient droids | 20: **Ancient Droid Protector**, locked crossing firing lanes | Three major choices; healing cache |
| 21–25 | Lower Wilds | Tuk'ata, maulers, droids, looters | 25: elite Terentatek Mauler, heavy frontal claws | Three elite choices; guarded tomb cache |
| 26–30 | Forbidden Tombs | Maulers, droids, Tuk'ata, Shyracks | 30: **Ancient Beast**, frontal claws / ground eruptions; three warnings below half health | Three major choices; core-run completion |

The existing 30-second wave cadence, population caps, steering and shared automatic abilities remain. The planet configuration replaces Korriban's old wave-5-only boss and every-twentieth-spawn elites. Levels reached during a fight remain due; the single existing encounter controller finishes its current actor, waits for rewards/level choices and a five-second gameplay recovery, then starts the next due milestone. No second encounter director is introduced.

The Korriban-only XP curve starts at 16 and grows by 1.105 per level, with a level-30 cap (about 2,600 total XP to reach 30). Run duration and class balance still require manual play. Final victory stops regular spawning and surviving attackers; R restarts, or return to the menu.

## Environment and roster

- Original A4 scene retained as the assembly foundation, including its authored spawn and two sectors. Four adjacent source sectors add three times the ground coverage: XZ `[20.2,58.0,6.2,31.4]`, 37.8 × 25.2 source units. Source heights, holes, texture blends and full parent transforms are retained.
- 424 source placement records; 418 rendered source prop instances after merging the retained 107 with 311 additions. 77 converted environment model resources, 46 beyond the original 31. These include additional ruined architecture, pillars, cliffs, broken statues, pottery, burial objects and ancient containers. Seven extra authored burial-court props and four cache objects use original models. Two unsupported camp monitor overlays are hidden and recorded, with transform parents preserved.
- Ordinary enemies now include the original three plus Spitting Tuk'ata (fast hybrid spit/melee), Ancient Droid Protector (slow ranged firing lane), and Terentatek Mauler (slow heavy frontal attack).
- Brood Mother uses the recovered **queen** mesh/material, distinct from the existing Broodmatron. Screecher uses its verified original Shyrack appearance. All new rig families have original idle, run, attack, hit and death clips. New models are not unrelated resized substitutes.
- Imported scenery has derived collision; wide courts have no roofs. Spawn placement retains full-body clearance, terrain support and reachable-corridor checks. A bounded route check connects the opening spawn to all four cache sites in order. The forbidden cache was moved from an isolated upper ledge to the connected original valley floor.

## Rewards and presentation

Scheduled elites always provide three choices; bosses provide three stronger choices and guarantee an eligible major-only relic when available. Normal run-artifact rules still apply: three slots, unique equipment, explicit replacement, no duplicate death payment, no persistent artifact ownership. Exhausted relic pools retain useful instant-power choices.

Korriban's catalog adds **Pilgrim's Seal** (+12% movement), **Predator's Relic** (2 HP per ability hit, at most once/second) and **Tomb Executioner's Sigil** (+40% damage below 35% enemy HP). These are authored Survivors items using recovered SWTOR icons, available to every implemented class. Existing ability-dependent relic prerequisites remain. Run rewards never enter cosmetic Collections. Existing cosmetic boss milestones now require a major classification, preventing the new scheduled elites from being mistaken for bosses.

Four intentional caches: Valley XP (28), guarded precinct relic choice, Wilds healing (40 HP), guarded forbidden relic choice. Collection is automatic within 0.24 source units after any guardian dies; no precision clicking. Caches unlock at levels 1/8/15/23 and reset with the run.

The existing SWTOR boss frame, announcement composition, Eurostile fonts, item art and reward/replacement controls are reused. Spatial region titles and a compact next-trial label are added. Warnings use distinct circles, lanes and frontal sectors. Persistent terrain-conforming warning meshes keep boundaries visible on slopes; each strike damages once even where lanes overlap. Each applied hit/tick retains its own combat event and floating text.

Original Valley and Naga Sadow ambience samples play through the existing audio system's World bus, with a single bounded ambience voice. Three recovered creature/droid vocal samples are used as limited attack cues. Original Korriban torch flame and thin-smoke artwork animates around ritual braziers. Korriban source lighting-scheme colors inform the gentle stage tint. Native Wwise layering, environmental lighting and particle curves are approximated, not reproduced wholesale.

## Configuration, recovery and evidence

- `godot/Gameplay/Planets/Korriban/run.json`: levels, stage rosters, XP, caches, artifact/audio catalogs and encounter schedule.
- The same directory contains enemy scenes, six encounter resources, the Korriban artifact catalog and audio tuning. HP, movement, ranges, warnings and damage are editable scene properties.
- `Run-D4-Pipeline.ps1`: bounded exact-path recovery/conversion; `Run-D4-Smoke.ps1`: C# build and one brief rendered integration session with isolated user data. `-SkipImport` reuses imports. `--d4-routes` is an optional bounded headless route check, not a gameplay simulation.
- `D4-INVENTORY.md` records the pre-change inventory; `D4-PROVENANCE.md` records source identities and adapter limits; `manifests/d4-delivery.json` indexes final verification and preserved files.

## Verification and remaining review

C# compilation and focused Godot checks pass. Final rendered smoke: 84 checks, no runtime errors. The checks cover six unique scheduled actors under a level jump to 30, all model/clip loads, eligible rewards, replacement, duplicate payout prevention, final phase warnings, per-hit floating text, caches and death/restart cleanup. Headless route checks pass for all four sites. Fifteen preserved configuration/environment files match their pre-D4 hashes, including Hutta, Dromund Kaas, Balmorra, class registry, shared enemy roster and shared artifact catalog. Build warnings remain the unavailable NuGet vulnerability feed and two pre-existing nullable warnings in `AutomaticAbility.cs`; the smoke harness also has a nullable-analysis warning.

**Stop at D4 for manual review.** Walk the added terrain and burial courts, listen to both ambience beds, inspect original materials/animations and the ground warnings at normal camera distance. Play melee, ranged, Force and tech builds through the six trials, assess XP pace, elite/boss pressure, rewards and camera obstruction. Automated smoke success does not establish gameplay balance or visual/audio acceptance.

Future extensions, **not implemented**: full Ajunta Pall / Marka Ragnos / Tulak Hord / Naga Sadow interiors, Academy exterior reconstruction, hostile acolyte outfits, additional native attack clips/voice variations, complete native environmental lighting/VFX graphs and a global navigation graph. No D5 or other planet expansion is included.
