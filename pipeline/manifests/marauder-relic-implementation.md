# Marauder endgame relics

Eleven permanent Marauder accessories use the existing two slots, class-family inventory visibility, +0–+3 tooltips, save migration and Credit upgrade costs. All 110 ordered distinct pairs pass equipment checks. No loot tables were added.

| Relic | +0 → +3 |
|---|---|
| Accelerated Agony | Annihilate accelerates all owned Rupture/Deadly Saber bleeds by 50% for 3s → 100% for 5s; no duration loss. |
| Exsanguination | Annihilate releases 20% → 50% remaining scheduled bleed damage; 2s internal cooldown; no consumption. |
| Crimson Executioner | Bleeding kills store 25% → 70% remaining damage, capped at 150 → 600; next Annihilate/Vicious Throw consumes storage. |
| Gore Master | Massacre and each Ravage strike add a 25% → 70% existing contact hit during Gore; window gains 0.5 → 1.25s. |
| Ataru Frenzy | Massacre empowers follow-ups for 6s: 25% → 70% bonus damage; internal cooldown 0.9 → 0.45s; 3 → 6 targets. |
| Predatory Carnage | Predation grants 20% → 50% attack/recovery acceleration and lasts 7 → 10s; Massacre recovers 1 → 2.5s of Gore cooldown. |
| Crushing Fury | Smash releases 20% → 50% remaining Force Crush damage, preserving the effect. |
| Shockwave | Up to 3 Force Crush charges; each grants 10% → 25% Smash damage and 8% → 20% radius. +3 repeats a 60% impact after 0.45s. |
| Obliteration | Engagement empowers the next Smash: 30% → 75% damage and 20% → 50% radius, resolving around its target. See fallback below. |
| Endless Berserk | Kills add 0.5 → 2s, with total activation duration capped at 10 → 22s. Existing aura/audio expiry remains authoritative. |
| Twin Blades | Saber Throw applies owned Deadly Saber to 2 → 5 hit enemies without overwriting existing bleeds. +1/+2/+3 add one 25%/50%/75% contact hit per cast. |

Carnage without relics: Massacre has full-damage frontal cleave for 3 enemies (5 with Sweeping Ravage), with half its former cooldown. Ravage's existing strikes hit 4 enemies (6 with the talent) at full damage. Ataru follow-ups hit 3 nearby enemies at 40% of the triggering hit. Existing Gore's 60% weapon damage window empowers these cleaves. Sweeping Ravage's tooltip describes the strengthened interactions.

Secondary damage bypasses attack hooks. Storage, charges, targets and extensions are bounded. Bleed keys retain one caster/ability/target owner. Reset clears relic state and pending impacts. Unequipped relics grant no bonuses.

Asset provenance: `marauder-relic-assets.json` maps every item to its existing Warrior icon, source effect and evidence. Existing animations, Warrior contacts, Ataru contacts, Smash effect/audio and Berserk lifecycle are reused. No new character animation or generic combat effect was authored.

Limitation: this kit has no playable Obliterate ability. Obliteration therefore arms on existing Force Charge engagement; its tooltip explicitly states the substitution. Recovered Saber Throw start/end clips already form the existing `DualThrow` alias; Twin Blades does not introduce a Dual Saber Throw animation.

Validation: build passes; `marauder-relic-smoke.json` passes all four ranks, 110 ordered equipment pairs, restrictions, persistence, non-consuming bursts, acceleration duration, bounded/consumed storage, duplicate bleed prevention, delayed impact, and unequipped baseline checks. Existing Marauder combat/presentation smoke passes with the requested six-target talented Ravage expectation. Juggernaut smoke stops at `seed 1: early build talent is usable`; no Juggernaut implementation was modified. Existing map import warnings remain in runtime logs.
