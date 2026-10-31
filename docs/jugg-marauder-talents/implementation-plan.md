# Plan: rework Juggernaut and Marauder in-run talents

For an implementing agent. Work in D:\SWTORClassic\torbonk (pipeline/godot/Gameplay/Talents), on top of the uncommitted Assassin and Sorcerer work. Design source: `proposal.md` in this folder. Builds and tests run on Brant's PC. SWTOR assets only, no placeholders or stand-ins. Do not commit or open a PR unless Brant asks.

## Decisions (Brant, 2026-10-30)
- Reuse the Assassin mechanics (tick duplication "Echo", multi-slot "Legion") on Marauder bleeds and Juggernaut DoTs. Also borrow other classes' ideas (Sorcerer cross-ability cooldown triggers, Sorcerer Contagion spread) and invent new ones.
- Healing numbers in the proposal are fine for now (Dark Blood +15%/rank, Hungering Wounds +20%/rank). Tune later.
- Force Choke is a stun: NO cooldown card. Target-count and damage cards are good for it.
- Standardise: Annihilation, Carnage and Rage (Marauder) and Immortal, Vengeance and Rage (Juggernaut) must all reach the same card quality. Fill gaps per spec (step 5).
- Smash cooldown floor: not answered. Default: lower Marauder's Smash floor from 10 s to 6 s, keep the 8 s Rupture/Crush floor. Mention this to Brant in the final report.
- Standing rules: one axis per card; no rank caps (`MaximumRank = 0`); every card scales with rarity; no cooldown cards on single-target DoTs (Force Crush, Rupture, Deadly Saber, Scream/Impale bleeds); melee healing small per rank but may exceed ranged; movement speed tiny.

## Steps
1. **Juggernaut power cards** (`JuggernautProgression.cs`, replace the bundled `JuggernautRunPowers`). Generate separate single-axis lists in the style of `SorcererDamageCards` etc.
   - Damage: Smash, Force Scream, Ravage, Impale, Crushing Blow, Sundering Assault, Force Choke, Force Crush, Intimidating Roar (~+90-100%, Roar/Choke ~+60%).
   - Radius: Smash, Force Scream, Crushing Blow (requires Shatter), Intimidating Roar, Ravage cone (~+12%).
   - Cooldown (`CooldownPerRank` ~.18): Smash, Force Scream, Ravage, Impale, Crushing Blow, Sundering Assault, Intimidating Roar. NOT Force Choke, NOT Force Crush.
   - Tick rate (`UpgradeEffect.TickRate`, .15): Force Crush, Scream bleed, Impale bleed, Smash wound (gated on owning the bleed talent).
   - Healing: Dark Blood heal amount card.
   - Mitigation: Soresu card, +1%/rank, applied as 1/(1+.01*sum) inside `JuggernautIncoming`.
   - Ids `juggernaut_power_*`, `juggernaut_radius_*`, `juggernaut_cooldown_*`, `juggernaut_tick_*`. Keep old ids loading only if saves need it; otherwise delete.
2. **Juggernaut area talents uncapped.** `jg_grip`, `jg_sweep`, `jg_resonance` become repeatable target-count cards (`MaximumRank = 0`); `JuggernautAreaRank` loses its `cap` parameter (currently 3). Add a Force Choke damage card and a Choke target card.
3. **Juggernaut mechanic cards.** Add to `JuggernautRuntime.cs` with `Ready(...)` locks and gates in `JuggernautUpgradeUsable`: Fury Bank, Aftershock, Crush Echo and Crush Legion (reuse `AssassinMechanicPower` style helpers), Reprisal, Rend Mark, Finishing Blow, Dread Presence, Blood Cadence, Rolling Quake. Details are in the proposal. Add a Smash internal floor of 5 s.
4. **Marauder power cards.** Create `MarauderRunPowers()` in `MarauderProgression.cs`, wired in wherever `JuggernautRunPowers` is registered (search `RunPowers()` callers in `RunProgression.cs`). Axes and ability lists per the proposal. Cooldown cards NOT for Rupture, Deadly Saber, Force Crush, Force Choke. Tick cards for the three bleeds. Target cards: Massacre, Ravage, Dual Saber Throw, Sweeping Slash (replaces the `mr_blademaster` +3 cap, `Math.Min(3,cleave)` and `Math.Min(9,...)` in `MarauderStats`). Healing: Hungering Wounds, Saber Siphon.
5. **Marauder drops for all three specs.** `MarauderCarnageDrops` only reads Carnage. Add equivalent drops for Annihilation and Marauder Rage (`Spec` = tree id), and extend `MarauderUpgradeTree`/`MarauderUpgradeUsable` for their keys. Then audit gaps: each of the six specs (3 Jugg, 3 Marauder) must offer at least damage, radius or targets, tick/cooldown where allowed, 2-3 spec-specific mechanics, and one healing/defence card. Add mechanics for thin specs, for example:
   - Juggernaut Immortal: Reprisal, Dread Presence, a Retaliation/Saber Ward payoff.
   - Juggernaut Rage: Fury Bank, Aftershock, Rolling Quake.
   - Marauder Annihilation: Open Wounds, Wound Legion, Detonate, Blood Tempo.
   - Marauder Rage: Smash and Scream cards (Aftershock-style Smash echo, Scream spread), Predator's Mark.
6. **Marauder mechanic cards.** Open Wounds, Wound Legion, Detonate (cap 6 s of remaining DoT per bleed), Blood Tempo, Gore Hunger (limit one Gore to 3x base length), Ataru Surge, Predator's Mark, Berserk Chain, Siphon Pulse. Lower Smash floor to 6 s in `MarauderStats`.
7. **Offer logic** in `RunProgression.cs`: make new `juggernaut_`/`marauder_` families weighted by tree (like `JuggernautOfferWeight`), gate on owned abilities, never offer DoT cards for abilities not owned.
8. **Smoke tests.** Update `TalentsSmoke.cs` and any Juggernaut/Marauder smoke referencing removed ids. Add checks: every card has exactly one non-zero axis; no cooldown card on Force Crush, Rupture, Deadly Saber or Force Choke; `MaximumRank == 0` on every new card; each spec offers the minimum set from step 5.
9. Build with Godot .NET on the PC, run the smokes, report results to Brant.

## Do not
- Add rank caps, bundled cards, or CDR on single-target DoTs or Force Choke.
- Touch other classes' cards.
- Use placeholder art, audio, or fonts.
