# T2.2 — Sorcerer refinement

Sorcerers now start with slotless Affliction alongside Force Lightning. Offensive and utility limits, the existing talent trees, and Banish/Reroll/Skip remain in place.

- Mixed level-up offers contain at most one new ability. Acquisition chance starts at 18%, rises by 9.5 percentage points per level without acquisition, and caps at 65%. Rerolls and banishes retain the level's chance roll. Talent investment raises related offer weights without excluding other trees. Full categories use replacement choices; Affliction can be traded when capped, carrying its free position to the replacement; other slotless effects cannot be replaced.
- Sorcerer Affliction/Creeping Terror/Crushing Darkness recast cooldowns are 12/14/18 seconds. They select enemies without their own matching DoT and defer when every available enemy already has it. Force Lightning fills gaps after ready attacks.
- Death transfers each player-owned DoT to one nearby uninfected enemy, preserving remaining duration, tick phase, damage and stacks. Creeping Death increases transfer reach. Death Field remains the full area spread and supports Crushing Darkness.
- Lightning Strike starts as a fast single-target attack, with cleave and Forked Current evolution. Shock starts at 22 damage to one target and can learn cleave. Crushing Darkness adds a strong single-target internal DoT.
- Polarity Shift grants 25% faster ticks and ability recovery for six seconds, with a 30-second cooldown. Lightning Barrage or run training enables it; it uses no slot. Force Speed uses a utility slot and grants two seconds of double movement speed when enemies approach within 0.35. Player movement remains input-controlled.
- Tick-frequency training now affects periodic damage while preserving durations. In-flight channels and active DoTs respond to frequency changes without replaying ticks. Existing talents include Lightning Strike, Crushing Darkness and Death Field recovery/radius interactions.
- Force Storm releases its character gesture after 0.8 seconds. Its original ground lightning, area indicator, audio and damage persist for the effect duration, allowing attacks after the GCD.
- Overload uses the enemy body's collision sweep for physical displacement, with existing terrain checks and knockback resistance.

## Original asset recovery

`scripts/prepare_sorcerer_refinement.py` uses the existing bounded April TOR reader and dependency traversal. `manifests/t22-source.json` records exact archive entries and decoded hashes; `t22-conversion.json`, `t22-animation-conversion.json` and `t22-audio.json` record conversions. Existing Sorcerer assets are retained; the additions live in `godot/Assets/SorcererRefinement`.

Client-derived ability icons: Lightning Strike `lightningStrike`, Crushing Darkness `dark_embrace`, Polarity Shift `polarityCharge`, Force Speed `forceSpeed`. Lightning Strike uses its explicit `fp_1h_elect_instant_right_4` release. Crushing Darkness uses the recovered one-hand instant semantic adapter; Polarity uses `cb_buff_consumption_exit`. Force Speed's appearance schedule supplies VFX/audio without a casting gesture, so it preserves the player's existing locomotion.

Presentation uses bounded Godot adapters for original textures, electrical ribbons, gestures and decoded Wwise media. This is not a native FXSpec/Wwise engine. The optional shared `Play_ability_general_forcedark_boomshort` event remains unavailable in the selected Imperial bank; the ability-specific Crushing Darkness events are recovered and used.

## Validation

Run `./Run-Sorcerer-Refinement-Smoke.ps1` (or `-SkipImport` with imported assets). `-Review` also captures Storm/Lightning Strike overlap and Crushing Darkness.

- Build succeeds.
- T2.2: 136 checks pass, covering level 2–30 offers, controls, limits, DoT targeting/timing/propagation, actual enemy death, utility expiry, evolving attacks, Storm overlap and collision-safe knockback.
- Existing Sorcerer and Assassin baseline checks pass.
- Talent selection, hybrid allocation, reset and cross-process persistence checks pass. The existing talent test reports ObjectDB instances at engine shutdown; its assertions pass.
- Rendered Storm overlap and new effect captures reviewed. Existing terrain, character models and talent-tree layout are preserved.

Stopped after this Sorcerer pass. The player's existing progression/HUD work was preserved.


## Presentation follow-up

Polarity Shift is now actually unlocked by Lightning Barrage or run training rather than being universally unlocked and merely activation-gated. Untalented Sorcerers start with only Force Lightning and Affliction on the action bar; a saved build granting Polarity correctly adds it at startup. Four additional startup/reset checks cover the saved talent build.

Polarity and Force Speed now draw the recovered black-backed spark/glow layers with the additive shader required by those textures. Rendered captures confirm transparent backgrounds. Dark Escape uses the recovered Force Speed artwork to indicate movement utility.

Affliction now plays its original `si_consumingdarkness_cast` sound and flare, and attaches the recovered `si_darkaffliction_target` glow, smoke and audio to its victim for the DoT lifetime. Its glow uses additive composition; smoke uses the original alpha-blend convention. Ambient Affliction loops are capped at four simultaneous voices, and the audio registry prunes freed status voices. Cast sounds remain audible independently. The source traversal and audio manifest include these added April dependencies.

Follow-up validation: build succeeds; 136 focused checks and four saved-talent startup checks pass. Sorcerer/Assassin baseline and talent selection/persistence checks remain passing. Captures: `t22-affliction.png`, `t22-polarity.png`, `t22-forcespeed.png`.

## Affliction replacement

When offensive abilities are capped, Affliction now appears in the existing replacement picker. The incoming ability inherits its free position for that run, so the normal offensive slot count stays unchanged. Further replacements carry that same free position rather than creating another. Trading baseline Affliction consumes its one acquisition, matching trained-ability replacement behavior; its owned DoTs are removed. Restart restores baseline Affliction and clears the inherited exemption. Utility categories, Polarity Shift, basic attacks and normal replacement handling retain their existing rules.

Validation: build and 151 focused checks pass, including normal replacement, capped Affliction replacement, a subsequent replacement of that free position, DoT cleanup, slot counts and restart.
