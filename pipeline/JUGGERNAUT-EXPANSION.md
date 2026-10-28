# Juggernaut expansion

## Opening and ability roster

Smash is now the free, replaceable starting class ability. Its existing original leap, slam timing, artwork and audio remain intact. Assault remains the slot-free weapon filler with its recovered Warrior Assault icon. Force Push and Vicious Slash are removed from Juggernaut's roster and offer pool; their source resources remain available to other classes.

Level 2 always offers Force Scream and Force Choke alongside a usable build talent. Unlearned core abilities retain priority through level 6. Levels 4–6 reserve one choice for an ability and up to two for eligible AoE talents, with low-rank talents preferred to keep new branches visible. Subsequent abilities follow pregame investment: Ravage/Impale for Vengeance, Sundering Assault/Saber Ward for Immortal, Force Crush/Obliterate for Rage. Rerolls refresh supporting choices while keeping early core abilities available; Banish and Skip remain available. Ability-dependent upgrades do not appear until their ability is owned.

The remaining kit includes Force Scream, Impale, three-hit Ravage, periodic/slowing Force Crush, Obliterate, Force Charge, Force Choke, Intimidating Roar, Saber Ward, Enraged Defense, Endure Pain, Vicious Throw, Retaliation, Sundering Assault, Crushing Blow and Backhand.

Normal offensive/utility limits remain in force; replacing free Smash transfers its free position. Learned counterattack, execution and defensive reactions remain slotless. Juggernaut retains 155 base health and 12% baseline incoming damage reduction. Its balance favors survival over Powertech's burning damage; longer encounter balance testing is still needed.

## Ranked AoE and build talents

Six run talents use normal progression, rank caps, eligibility, Banish and Reroll:

| Talent | Ability | Progression |
| --- | --- | --- |
| Crushing Grip | Force Choke | 1 → 2 → 3 → 4 nearby targets, full damage and existing control resistance |
| Resonant Voice | Force Scream | 1 → 3 → 5 → 7 nearby targets; secondary damage 65% |
| Ravaging Sweep | Ravage | 1 → 2 → 3 → 4 targets in front on each of its three hits; secondary damage 60% |
| Sundering Swings | Sundering Assault | 1 → 2 → 3 → 4 targets; secondary damage 60%, armor weakening on every victim |
| Rupturing Smash | Smash | Four-second Force wound on every enemy hit |
| Crushing Convergence | Force Choke + Force Crush | Choke applies learned Crush to its victims, feeding Smash charges and periodic synergies |

The original selected target retains priority and full damage. Additional targets are chosen deterministically by distance, then stable identity. Area size and target count are bounded. Unranked abilities remain single-target; run resets remove learned AoE changes. Multi-target Choke uses ordinary enemy stun resistance, including champion immunity. It does not add new native-client animation or particle claims.

## Pregame talents

27 talents, nine per tree, share the existing twelve-point hybrid budget. Crushing Grip (Immortal) and Ravaging Sweep (Vengeance) are new ranked entry talents. Force Resonance (Rage) is now a ranked entry talent with the same 3/5/7 target progression as its run counterpart. Rupturing Smash is an additional Rage talent. Pregame and run ranks combine up to the same target cap; upgrades stop appearing when that cap is reached. No exclusive build lock is introduced.

Immortal retains retaliation mitigation, defensive recovery and Scream shielding. Vengeance retains Impale/Scream empowerment, Ravage recovery, bleeds and Crushing Blow spread. Rage retains Crush-charged Smash, engagement burst, periodic spread and controlled sustain. New talents are survivors adaptations, not claims of native April 2012 talent names, ranks or formulas. All nine talents fit the existing two-column scrolling panels.

## Presentation and adaptations

`Gameplay/Talents/juggernaut-assets.json` records recovered presentation resources. Juggernaut Force Scream has a separate resource so Marauder retains its previous splash bleed, animation and tuning. Other new runtime hooks check Juggernaut identity; shared target limiting defaults to uncapped existing behavior for other classes.

Direct appearance-linked converted single-saber animations are used where resolved. Ravage, Force Charge, Sundering Assault and Enraged Defense still use the original basic single-saber clip as a fallback. Ravage has three gameplay beats; its native multi-hit animation remains unresolved. Native SWTOR FXSpec/material/particle playback and full Wwise variation fidelity remain unresolved. Recovered icons, PCM candidates and effect textures are reused. Endure Pain uses a temporary shield rather than increasing maximum health; Retaliation reacts to recent damage rather than an unavailable native parry event.

## Validation

Run `./Run-Juggernaut-Smoke.ps1`. It uses isolated saved profiles and writes `manifests/juggernaut-smoke.json`. Coverage includes opening offers across eight random seeds, acquisition, reroll/banish behavior, ranked target counts, armor weakening, periodic synergies, reset cleanup, durability, hybrid allocation, asset availability and full-bar replacement of the free starter. Build/runtime logs use `logs/juggernaut-*.log`.
