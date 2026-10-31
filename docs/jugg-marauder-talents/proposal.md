# Juggernaut and Marauder in-run talents: overhaul proposal

Design only, no code changed. Read from D:\SWTORClassic\torbonk (branch claude/advanced-class-kits, with the uncommitted Assassin and Sorcerer work). Numbers are starting points for playtest. Rarity multiplier (x0.4 / 1 / 1.6 / 2) applies to every card.

## What exists today

**Juggernaut** (`JuggernautProgression.cs`, `JuggernautRuntime.cs`, `juggernaut.json`)
- 8 "run power" cards, each a bundle of damage + radius + recovery (e.g. Seismic Slam: +80% damage, +12% radius, +8% cooldown recovery). This is exactly what the Sorcerer split removed.
- 24 talent-tree mechanics double as drops, mostly one-time. The three area ones (Crushing Grip, Ravaging Sweep, Force Resonance) are capped at 3 ranks, which breaks the no-cap rule.
- Several are flat numbers (Soresu 8% mitigation, +8% saber damage, +10% Force cooldown speed). Few change *how* an ability plays. The interesting ones (Shockwave charges into Smash, Scream primes Smash, Pooled Hatred) are one-offs with no scaling.

**Marauder** (`MarauderProgression.cs`, `MarauderRuntime.cs`, `marauder.json`)
- **No run power cards at all.** There is no `MarauderRunPowers`. The only Marauder drops are the 9 Carnage tree cards, each max 1 rank. Annihilation and Marauder Rage tree talents never drop in a run.
- So a Marauder run has nothing that scales damage, radius, tick rate or cooldown per ability, and nothing for the bleed (Rupture / Deadly Saber / Force Crush) identity except one-off hooks.
- Healing exists as small flat hooks (Hungering Wounds, Saber Siphon).

**Reference (what "good" looks like now)**
- Assassin: single-axis damage / radius / cooldown / tick cards, no CDR on single-target DoTs, tick duplication ("Echo": +20% chance per rank to strike again), multi-slot ("Legion": +1 simultaneous instance per rank), and 9 mechanic cards that link abilities (Conduit, Forked Lightning, Harvest, Quickening Decay).
- Sorcerer: same split, plus range cards, an uncapped Contagion spread card, and cross-ability cooldown triggers.

## Design principles (all from Brant's rules)
1. One axis per card: damage, radius/range, tick rate, cooldown, targets, or healing.
2. No CDR on single-target DoTs. No rank caps anywhere; every card scales with the rarity roll.
3. Mechanic cards change how an ability works and tie abilities together, with an internal cooldown (`Ready(...)`) where they could chain.
4. Melee healing may exceed ranged healing, but stays a separate small card, never riding on damage cards. Movement speed stays tiny.
5. Defensives get much smaller per-rank numbers than damage.

## Juggernaut

**Identity to lean into:** a Smash-centred rage cycle (build charges, spend them on Smash), bleed/Crush DoTs, and a tank that converts damage taken into damage dealt.

### Single-axis power cards (replace the 8 bundles)
All uncapped, rarity-scaled. Old `juggernaut_power_*` ids are retired.

| Axis | Abilities | Per rank |
|---|---|---|
| Damage | Smash, Force Scream, Ravage, Impale, Crushing Blow, Sundering Assault, Force Choke, Force Crush, Intimidating Roar | ~+90-100% (Roar and Choke ~+60%) |
| Radius | Smash, Force Scream, Crushing Blow (once Shatter), Intimidating Roar, Ravage cone | ~+12% |
| Cooldown | Smash, Force Scream, Ravage, Impale, Crushing Blow, Sundering Assault, Intimidating Roar, Force Choke | ~+18% (diminishing via cooldown/(1+sum)) |
| Tick rate | Force Crush, Scream bleed, Impale bleed, Smash wound | +15% |
| Targets | Crushing Grip (+1 enemy), Ravaging Sweep (+1), Force Resonance (+2) | uncapped, replaces the rank-3 caps |

No cooldown card for Force Crush (single-target DoT). Choke's cooldown card is the one judgment call, see questions.

### Mechanic cards (new, mostly uncapped or stacking)
1. **Fury Bank.** Every damaging hit from another ability banks 1 Fury (decays after 6 s). Smash spends all Fury for +4% damage per Fury, per rank of the card. Generalises Shockwave from "Crush ticks only, max 3" to the whole kit.
2. **Aftershock.** Smash repeats +1 time per rank, 1.2 s apart, each echo at 50% damage and 70% radius. The Juggernaut version of Legion: big payoff for stacking.
3. **Crush Legion / Crush Echo.** Direct ports of the Assassin cards: +1 simultaneous Force Crush instance per rank on each enemy, and a +20% per-rank chance for each Crush/bleed tick to strike again. Also covers Scream and Impale bleeds once owned.
4. **Reprisal.** Damage taken in the last 4 s is stored. Retaliation (and Saber Ward on cast) releases +25% per rank of that stored damage as a shockwave around you. Turns the tank half into damage. Internal 1 s lock.
5. **Rend Mark.** Force Scream marks every enemy it hits for 6 s. Impale, Ravage and Crushing Blow deal +15% per rank vs marked targets, and Crushing Blow's Shatter spreads the mark.
6. **Finishing Blow.** Crushing Blow gains +30% per rank vs enemies under 30% health; a kill with it refunds 2 s of its cooldown. Ties into the execute fantasy; Pooled Hatred already adds the "taking damage" half.
7. **Dread Presence.** Enemies hit by Intimidating Roar or Choke take +10% per rank from Smash and Force Scream for 5 s. Gives Roar a real job beyond its small hit.
8. **Blood Cadence.** Impale kills and Impale bleed ticks each shave 0.3 s off Ravage cooldown (internal 0.5 s). Extends Rampage into a rotation.
9. **Rolling Quake.** Each Smash that hits 3+ enemies recovers 1.5 s of Force Crush cooldown and refreshes Crush on a hit enemy. Synergy with Crushing Wave.

Keep the existing 24 talent drops, with the three area ones switched to the uncapped target cards above and Soresu/flat bonuses left as small cards.

### Defence and healing
- **Dark Blood heal size**: +15% per rank of the heal amount (the 2-3 hp hits). Melee, so stronger than Sorcerer's 5%.
- **Soresu**: +1% mitigation per rank, expressed as damage taken x 1/(1+0.01*sum) so it cannot hit 100%.
- **Saber Ward / Enraged Defense** cooldown: ~+8% per rank, nothing larger.

## Marauder

**Identity to lean into:** bleeds and spreading DoTs (Rupture, Deadly Saber, Force Crush spread by Sweeping Slash/Smash), the Gore window for Carnage cleave, and Ataru follow-ups.

### Bring all three trees into the run
Add drops for Annihilation and Marauder Rage tree talents, as Carnage already has. Today a Marauder who picks the DoT build never sees its own talents.

### New single-axis power cards (Marauder has none today)
`marauder_` ids so the class-preference logic offers them.

| Axis | Abilities | Per rank |
|---|---|---|
| Damage | Massacre, Ravage, Battering Assault, Annihilate, Gore, Sweeping Slash, Dual Saber Throw, Smash, Force Scream, Rupture, Deadly Saber, Force Crush | ~+90-120% (DoTs ~+90%) |
| Radius / cone | Massacre, Ravage, Sweeping Slash, Dual Saber Throw, Smash | ~+12% |
| Cooldown | Massacre, Ravage, Battering Assault, Annihilate, Sweeping Slash, Dual Saber Throw, Smash | ~+18% |
| Tick rate | Rupture, Deadly Saber, Force Crush | +15% (duration unchanged) |
| Targets | Massacre (+1), Ravage (+1), Dual Saber Throw (+1), Sweeping Slash (+1) | uncapped |

No cooldown card for Rupture, Deadly Saber or Force Crush (single-target DoTs, plus the code already floors Rupture/Crush at 8 s). Gore stays a mechanic card, not a power card, so its window cannot be spammed.

### Mechanic cards (new)
1. **Open Wounds** (Rupture, Deadly Saber, Force Crush): +20% per rank chance each tick strikes again. Assassin Echo port.
2. **Wound Legion**: +1 simultaneous instance per rank for each of the three bleeds. Ties to Sweeping Slash spread.
3. **Detonate.** Annihilate (and Gore at half) consumes bleeds on the target: instantly deals X% (35% per rank) of their remaining damage and leaves 2 s of the DoT. A burst button for the bleed build, gated by Rupture or Deadly Saber.
4. **Blood Tempo.** Each bleed tick on any enemy builds Tempo (internal 0.5 s). At 10 Tempo your next Massacre/Ravage costs no cooldown and strikes with +40% damage per rank. Makes bleeds drive the Carnage rotation.
5. **Gore Hunger.** Kills during Gore extend it by 0.5 s per rank (hard max: a single Gore cannot exceed 3x its base length, a time limit not a rank cap). Rewards cleave, makes Gore a snowball.
6. **Ataru Surge.** Ataru follow-ups strike +1 additional enemy per rank and shorten their internal cooldown by 0.1 s per rank (floor 0.6 s).
7. **Predator's Mark.** Predation leap marks the target; marked enemies take +10% per rank from your bleeds and Annihilate, and spread the mark to nearby enemies on death.
8. **Berserk Chain.** Kills during Berserk extend it by 0.4 s per rank. Plus Relentless Assault already shortens its cooldown.
9. **Siphon Pulse.** Dual Saber Throw hitting 3+ enemies heals a small amount per rank, once per 2 s. (Melee healing, still small per rank.)

### Defence and healing
- **Hungering Wounds** heal: +20% per rank of bleed-tick healing. Melee, so it can be stronger than Sorcerer.
- Defensive cooldown/mitigation: only tiny per-rank values, same as Juggernaut.

## Overlap between the two
Both classes use shared ids (`smash`, `forcecrush`, `forcescream`). Cards must carry the class prefix (`juggernaut_` / `marauder_`) and their usable checks must read the active class, as the Assassin and Sorcerer ones already do. Marauder's `MarauderStats` floors Smash cooldown at 10 s and Rupture/Crush at 8 s: a Smash cooldown card hits the floor quickly. I would drop the floor to ~6 s for Smash so the card matters, and keep the DoT floors.

## Risks
- Aftershock + Fury Bank + Smash CDR can compound on Juggernaut: add a Smash internal floor of ~5 s.
- Detonate with Legion and Echo could spike hard. Proposed: Detonate caps at 6 s of remaining DoT per bleed.
- Healing: Dark Blood and Hungering Wounds heal every hit, so uncapped ranks scale per hit count. Keep per-hit 1-second internal cooldowns (already present).

## Questions for Brant
1. Is it OK to add **Annihilation and Marauder Rage drops** (in addition to Carnage), so every Marauder spec has its own cards? I would default to yes.
2. **Healing size.** Melee heal cards at +15% (Dark Blood) and +20% (Hungering Wounds) per rank. Fine, or smaller?
3. **Force Choke cooldown card.** Choke is a channelled single target, not a DoT. Allowed, or treat it like single-target DoTs?
4. **Smash cooldown floor.** Lower Marauder's Smash floor from 10 s to ~6 s so CDR cards matter? Or keep Smash a deliberate burst button?
5. **Echo / Legion on bleeds.** Reuse Assassin's duplication and multi-slot cards as-is for Marauder bleeds and Juggernaut's Crush, or keep them Assassin-only?
