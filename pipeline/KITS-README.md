# Advanced-class ability kits — design plan and milestone log

Branch `claude/advanced-class-kits`. Baseline: D3 (eight Imperial classes, one starter each) plus the PR #1 balance direction
(heals nerfed, Overload as utility, relics rare and strong). Sorcerer's kit is unchanged.

## Rules for every kit

* **Assets are original SWTOR only.** New abilities reuse already-recovered clips (`Assets/D3/casts.glb`), PCM (`Assets/D3/Audio`),
  textures and icons. Anything not yet recovered is listed under *Needs MCP recovery* and left on a reused placeholder, never invented.
  The SWTOR research MCP was not connected in the session that wrote this plan.
* Orange/red = Empire/Sith, Sith purple and poison green allowed. No recoloured Jedi (blue) art.
* Same slot rules as Sorcerer: 4 offensive, 2 utility. Each class gets **2 offensive + 1 utility unlock** and **2 mechanic talents**.
  Unlocks are level-up offers (weight 3 like Sorcerer's), so a run grows into its kit instead of starting with it.
* No heals. Utility = shield, stagger, or a short self-buff. Numbers are Survivors tuning, deliberately modest versus the starter.
* Rendering has no damage authority; every mechanic lives in `RunBuildEffects` / `PeriodicEffects` and is covered by the smoke test.

## Shared engine work

1. `classes.json` gets `Kit` (ability ids). `GameWorld` builds each as a locked `AutomaticAbility` with `D3AbilityPresentation`.
2. `UpgradeEffect.UnlockAbility` pools for each class; Sorcerer pools untouched.
3. **DoT spread framework** (`PeriodicEffects.On(target)` + hooks in `RunBuildEffects`): *Contagion* (every Nth tick the DoT jumps to the nearest
   un-afflicted enemy) and *Last Gasp* (an afflicted enemy that dies passes its DoT on). One refreshable instance per caster/ability/target is preserved.
4. `AbilityEffect.Buff` for short self-buffs (damage or haste), expressed as data.
5. Fix list for existing starters (from `D3-HANDOFF.md`): lethal hits keep their presentation, additive FX shader, no black/huge billboards,
   Empire-coloured FX, saber/rifle/bracer audio bound, Flame Sweep spin + four radial pulses.

## Kits

| Class | Identity | Starter | Unlock A (off.) | Unlock B (off.) | Unlock (util.) | Mechanic talents |
|---|---|---|---|---|---|---|
| Marauder | Bleeding frenzy | Rupture (bleed) | Annihilate: heavy single hit, +50% vs bleeding | Force Scream: splash that bleeds secondaries at half strength | Berserk: +25% damage 6 s, 20 s cd | **Last Gasp** (bleed passes on death), **Blood Rhythm** (every 4th bleed tick refunds 0.5 s of Rupture cd) |
| Operative | Poison stacker | Corrosive Dart | Corrosive Grenade: splash that poisons everything it touches | Weakening Blast: small hit, target takes +20% damage 5 s | Adrenal Stim: +20% speed 5 s | **Contagion** (poison jumps every 3rd tick), **Lethal Dose** (each refresh +10% poison, max 3) |
| Assassin | Force + poison | Lacerate (radial) | Creeping Terror: single DoT, slows | Discharge: radial burst, short DoT on all | Force Shroud: shield 20/6 s | **Contagion**, **Static Blades** (Lacerate refreshes DoTs on hit enemies) |
| Juggernaut | Rage tank | Smash (radial) | Vicious Slash: fast single hit | Ravage-style Crushing Blow: heavy single hit | Intimidating Roar: radial slow + stagger | **Rage** (hits build 5 stacks, +5% damage each), **Shockwave** (Smash slows 2 s) |
| Powertech | Burn + armor | Flame Sweep (radial) | Flame Burst: single hit + burn | Incendiary Missile: splash + burn | Ion Shield: shield 30/8 s, flames when broken | **Wildfire** (burn passes on death), **Searing Sweep** (Sweep ticks refresh burns) |
| Sniper | Precision | Sniper Shot | Ambush: 80 dmg, 8 s cd | Series of Shots: 3-tick channel | Shield Probe: shield 20/6 s | **Target Lock** (+10% per consecutive hit on same target, max 4), **Followthrough** (kill with Ambush resets Sniper Shot) |
| Mercenary | Arsenal / marked | Tracer Missile | Heatseeker Missiles: heavy splash | Unload: 4-tick channel | Reactive Armor: shield 20/6 s | **Tracer Lock** (Tracer-marked enemies take +25% from other abilities 6 s), **Chain Reaction** (marked enemy death explodes) |

## Needs MCP recovery (placeholders until the research MCP is available)

Distinct clips for Annihilate, Vicious Slash, Crushing Blow, Force Scream, Discharge, Flame Burst, Ambush/Series, Heatseeker, Unload, buff/shield
casts; per-ability icons beyond the already-recovered set; ability-specific audio events. Each ability currently reuses its class's recovered
clip, icon family and PCM.

## Milestones

* **K0** — plan (this file), branch.
* **K1 (done)** — shared framework, fixes to existing starters, Marauder + Operative kits, smoke.
  * `Kit` in `classes.json`; kit abilities are wired locked and unlocked by level-up offers; one shared damage track for the whole kit.
  * `PeriodicEffects`: `Ticked`, `Has/On/RefreshAll`, Lethal Dose stacks. `RunBuildEffects`: Contagion, Last Gasp (`PassOn`), Blood Rhythm, Static Blades hook, short buffs (`AbilityEffect.Buff`).
  * `D3AbilityPresentation` rewritten: additive `D3Particle.gdshader` (camera-facing in-shader), per-kind effects (slam ring, flame emitter from the wrist,
    saber audio, rifle bolt + muzzle flash, bracer projectile), bounded effect/voice pools, no central billboards, tails survive lethal hits.
  * Marauder: Annihilate, Force Scream, Berserk, Last Gasp, Blood Rhythm. Operative: Corrosive Grenade, Weakening Blast, Adrenal Stim, Contagion, Lethal Dose.
  * Validation: `Run-Kits-Smoke.ps1` (per-class receipts `manifests/kit-smoke-<class>.json`: marauder 31, operative 33, powertech 7, sniper 4 checks), plus the existing
    D3 (107) and D2 smokes still pass. Flame Sweep is now four pulses over the spin; D3Smoke was adjusted for that intentional change.
  * Not verified: how the new effects *look* and *sound* in the rendered game. Wrist/muzzle alignment, flame spin and saber audio levels need a manual pass.
* **K2** — Assassin + Juggernaut + Powertech.
* **K3** — Sniper + Mercenary.
* **K4** — balance pass against PR #1 direction, README/receipt updates.
