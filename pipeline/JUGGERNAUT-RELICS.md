# Juggernaut endgame relics

Six permanent accessories, granted at +0 to new and existing valid equipment profiles. Available in the Sith Warrior bag, compatible only with Juggernaut; any two different relics fit the existing slots. No specialization requirement. Existing Credit costs and atomic persistence apply.

| Relic | +0 / +1 / +2 / +3 |
|---|---|
| Worldbreaker | 2 / 3 / 3 / 4 full Smash impacts, 0.35 / 0.30 / 0.25 / 0.20 seconds between echoes |
| Endless Fury | 2 / 3 / 4 / 5 Force Crush targets within 0.4 world units of the primary |
| Ravager's Wrath | 2 / 3 / 4 / 5 additional nearby Ravage targets, full damage each strike |
| Impaling Fury | 2 / 3 / 3 / 4 Impale hits, 0.250 / 0.225 / 0.200 / 0.175 seconds between echoes |
| Vengeful Retribution | +25 / 50 / 75 / 100% Retaliation range; +25 / 40 / 55 / 70% cooldown recovery |
| Undying Wrath | Damage-triggered healing every 0.50 / 0.33 / 0.25 / 0.20 seconds; +20 / 30 / 40 / 50% damage; 32 / 40 / 48 / 56 healing cap per activation |

Abilities must be unlocked/equipped normally. Retaliation uses the ordinary four-second incoming-damage reaction, consumes it through normal activation, and retains normal animation and cooldown. Undying Wrath retains Enraged Defense's six-second duration and damage-trigger requirement. Its cap counts healing attempts conservatively; overhealing cannot create extra budget. Health still caps at maximum.

Smash echoes retain the original radius and impact/audio presentation at the landing location without restarting the leap. Impale repeats existing target impact effects. Echoes bypass cast/proc/periodic application hooks and check caster life, combat enablement, unlocked ability and current relic equipment before dispatch. Ravage/Force Crush expand a single target list before ordinary dispatch, preventing overlap with talent cleaves; Force Crush retains normal periodic effects, slow and talent interactions. Unequipped relics do not alter base stats.

Assets: reuse the already recovered April darksiderelic1/2/3 DDS artwork. See manifests/assassin-relic-assets.json for hashes and April extraction receipts and manifests/d2-conversion.json for original resource paths. These six custom names have no verified unique historical item-to-icon mapping; three recovered icons are shared by six relics. All presentation uses the existing Warrior library, animations, target effects and audio. No new animations or generic effects were created.

Validation: Run-Juggernaut-Relic-Smoke.ps1 builds and runs isolated equipment/combat tests. Receipt: manifests/juggernaut-relic-smoke.json. Existing Juggernaut baseline and Marauder relic suites also run for regression coverage.
