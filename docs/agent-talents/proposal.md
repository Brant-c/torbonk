# Sniper and Operative in-run talents: overhaul proposal

Design only, no code changed. Read from D:\SWTORClassic\torbonk (branch claude/advanced-class-kits, with the uncommitted Jugg/Marauder, Assassin and Sorcerer work). Numbers are starting points for playtest. The rarity multiplier (x0.4 / 1 / 1.6 / 2) applies to every card.

## What exists today

**Sniper** (`SniperRunPowers.cs`, `AgentRuntime.cs`, `agent.json`)
- 14 "power" cards, each a bundle of damage + radius + recovery (e.g. Heavy Ordnance: +80% damage, +12% radius, +15% recovery). This is the bundling the Sorcerer split removed.
- 12 mechanic cards plus 2 in `OperativeProgression.cs` (Clean Kill, Shrapnel Feed). All are `MaximumRank=1`, so they never scale with rarity and cannot be stacked. Their numbers (refund 2 s, +60% vs probed, 50% radius) are fixed.
- Spec coverage is uneven. Marksmanship has 3 mechanics, Engineering 5, Lethality 4. Nothing for tick rate, no tick duplication or multi-slot for Dart, Corrosive Grenade or Interrogation Probe, no target-count card (ricochets are a flat +2), and no healing card. Defence is only the generic Health card.
- Generic Health and Speed cards are capped at 2 ranks (`MaximumRank=2` in `RunProgression.cs`), which breaks the no-cap rule.

**Operative** (`OperativeProgression.cs`, `AgentRuntime.cs`)
- 12 bundled power cards. Only 5 mechanics (`op_flank`, `op_bleed_echo`, `op_cull_reset`, `op_weak_reset`, `op_kill_reset`), all one-rank, and 4 of 5 are Concealment-flavoured.
- **Medicine has nothing.** No card touches Kolto Injection, Kolto Probe, Surgical Probe, Stim Boost, Adrenaline Probe, Shield Probe or Tactical Advantage. A Medicine Operative sees the same cards as a Concealment one.
- Operative has no `OperativeUpgradeUsable` check (Sniper and Assassin do), so mechanic cards can be offered for abilities you do not own.
- Operative keeps the generic damage-less pool (Range, Cooldown, Health, Speed), so all-ability bundles still appear. Crippling Strike gives cooldown to Debilitate, which is a stun.
- Lethality is a copy of Sniper's, with no Operative-specific identity (poison plus Venomous Blades, Cull in melee range).

**Reference.** Assassin and Sorcerer single-axis cards, Echo (`as_dup_`) and Legion (`as_multi_`) for DoTs, and the Jugg/Marauder proposal's mechanic cards (`jgm_`, `mrm_` families, which are the only mechanics `Scalable()` rolls rarity for).

## Design principles (Brant's rules)
1. One axis per card: damage, radius/range, tick rate, cooldown, target count, or healing. No bundles.
2. No cooldown card on single-target DoTs or stuns. No rank caps (`MaximumRank=0`). Every card scales with rarity, including the mechanic numbers (new `snm_` / `opm_` prefixes, added to `Scalable()`).
3. Every spec offers damage, a radius or target card, a tick or recovery card, two or more mechanics, and a healing or defence card.
4. Ranged healing stays much lower than melee. Movement speed stays tiny. Debilitate stays out of Sniper's pool.
5. Mechanics tie abilities together and carry an internal cooldown (`Ready(...)`) where they could chain.

## Shared axis cards (class-prefixed `sniper_` / `operative_`)

Same lists feed both classes where the ability exists in the kit. Per rank, rarity-scaled.

| Axis | Abilities | Per rank |
|---|---|---|
| Damage | every owned offensive ability (Dart and Interrogation Probe include periodic damage) | ~+90% (Dart, Cull, Weakening ~+100%) |
| Radius / cone | Suppressive Fire, Orbital Strike, Plasma Probe, Explosive Probe, EMP Discharge, Fragmentation, Corrosive Grenade, Cull splash, Backstab / Laceration / Hidden Strike cleave | +12% (grenade +15%) |
| Cooldown | all of the above **except** Dart, Interrogation Probe, Debilitate, Flashbang | +18%, diminishing via cooldown/(1+sum) |
| Tick rate | Dart, Corrosive Grenade, Interrogation Probe, Laceration, Kolto Probe | +15%, duration unchanged |
| Targets | Ricochets (Ambush, Series of Shots, Followthrough), Corrosive Grenade, Cull splash, Weakening Blast spread, Interrogation Probe marks, Backstab / Laceration cleave | +1 per rank, uncapped |

## Sniper

### Marksmanship: covered precision fire, ricochets, the Ambush > Series > Followthrough chain
- Axis cards for Ambush, Series of Shots, Followthrough, Suppressive Fire. Ricochet target card replaces the flat +2 of Penetrating Rounds.
- Mechanics (existing, now scaling): **Crossfire Discipline** (refund 1.5 s Followthrough / 2 s Ambush per rank), **Entrenched Salvo** (+25% damage in Entrench per rank, 2 s refund per rank), **Penetrating Rounds** becomes the ricochet damage retention card (+5% retained per jump per rank).
- Mechanics (new):
  1. **Firing Chain.** Each shot of the Ambush > Series > Followthrough sequence inside the 6 s window adds a link: +6% damage per link per rank to the next shot; resets when the chain breaks. Rewards the existing `_followup` window.
  2. **Dead Eye.** Ambush deals +25% per rank vs enemies above 70% health; Followthrough deals +25% per rank vs enemies below 30%. Opening and finishing roles.
  3. **Echo Round.** Followthrough fires again 1 s later at 50% damage, +1 repeat per rank (Aftershock port). Internal 2 s lock.
  4. **Hold the Line.** While in cover, cooldowns on Marksmanship shots recover +4% faster per rank per second stood still (resets on moving). Ties into `AgentPositioned`.
- Defence: **Ballistic Dampers** card, +1% reduction per rank, applied as damage x 1/(1+.01*sum). **Deployed Cover** shield capacity +5% per rank. No healing.

### Engineering: probe marks, detonation, persistent area, EMP recovery
- Axis cards for Explosive Probe, Orbital Strike, Plasma Probe, Interrogation Probe (damage and tick rate only, no cooldown), EMP Discharge (cooldown has a 8 s floor), Fragmentation.
- Mechanics (existing, now scaling): **Probe Overload** (detonation also deals +25% of remaining per rank), **Targeted Suppression** and **Probe Targeting** (+bonus vs probed per rank), **Coordinated Bombardment**, **Overclocked EMP** (refund 3 s per rank).
- Mechanics (new):
  1. **Probe Echo / Probe Legion.** Port of Assassin Echo (+20% chance per rank that each probe tick strikes again) and Legion (+1 simultaneous probe per enemy per rank).
  2. **Relay Network.** Orbital Strike and Plasma Probe hits on a probed enemy spread the probe to +1 nearby enemy per rank (Contagion port).
  3. **Cascade Detonation.** Explosive Probe detonates probes on every enemy in its radius at +30% per rank of remaining damage, capped at 6 s of remaining per probe (avoids Legion spikes). Replaces the single-victim limit of Probe Overload.
  4. **Lingering Plasma.** Plasma Probe fields last +1 s per rank.
- Defence: **Probe Shielding**, probe hits grant a small shield, +3 per rank, once per 6 s. EMP heal (12 hp) scales +4% per rank.

### Lethality: owned poisons, Cull echoes, Weakening Blast extension
- Axis cards (Dart: damage, tick, no cooldown). **Echo / Legion for Dart and Corrosive Grenade** (ports).
- Mechanics (existing, now scaling): **Concentrated Volley** (+50% Cull radius per rank; Weakening Blast empower +50% per rank), **Zeroing In** (+20% tick speed in cover per rank), **Virulent Weakness** (+1 echoed tick per rank), **Contaminated Rounds** (+25% echo per rank).
- Mechanics (new):
  1. **Pandemic.** A poison that expires or whose host dies jumps to the nearest un-poisoned enemy, +1 jump per rank (2 s lock).
  2. **Septic Detonation.** Cull consumes poison on its target for +35% per rank of remaining damage and leaves 2 s (Detonate port, 6 s cap).
  3. **Toxic Tempo.** Poison ticks recover 0.15 s per rank of Cull and Weakening Blast cooldown (internal 0.5 s, shared with existing Lethal Alignment).
- Defence: **Toxic Focus** shield +3 per rank; poison-tick healing is a tiny separate card (+1 hp per rank, 1 s lock).

## Operative

### Concealment: Backstab / Hidden Strike / Laceration melee, openings, Counterstrike
- Axis cards for Backstab, Hidden Strike, Laceration, Sever Tendon, Shiv. Cooldown cards for Backstab, Hidden Strike, Laceration, Shiv; not Debilitate. Cleave target cards for Backstab and Laceration.
- Mechanics (existing, now scaling): **Flanking Rhythm** (refund 1.5 s per rank), **Venom Carrier** (+25% echo per rank), **Opportunist**, **Exposed Nerves** (re-pointed to Backstab and Sever Tendon, not Debilitate), **Predator's Momentum**.
- Mechanics (new):
  1. **Opening Gambit.** Leaving stealth with Hidden Strike opens a 4 s window: all Concealment attacks +20% per rank.
  2. **Hamstring Mark.** Sever Tendon marks for 6 s: Backstab and Laceration deal +15% per rank vs marked enemies; mark spreads on death.
  3. **Twin Slash.** Laceration's second swipe repeats +1 time per rank at 50% (Aftershock port).
  4. **Counter Surge.** Blow for Blow reflects +15% per rank more and lasts +0.5 s per rank.
  5. **Exploit the Stun.** Stunned or Debilitated enemies take +15% per rank from all Concealment attacks.
- Defence and healing: **Shadow Veil**, damage taken -1% per rank while stealth window is open. **Close Quarters Recovery**, kills heal +2 per rank (melee, allowed more than Sniper, still small).

### Medicine: Kolto, Tactical Advantage economy, sustain that powers damage
- Healing and defence axis cards, all separate and small per rank: Kolto Injection healing +12%, Kolto Probe healing +12%, Surgical Probe healing +12% (melee).
- Cooldown: Kolto Injection, Surgical Probe, Stim Boost, Shield Probe at +18%. Kolto Probe and Adrenaline Probe: see questions.
- Tick: Kolto Probe tick rate +15%.
- Damage and radius (so Medicine is not support-only): **Field Surgeon**, while Kolto Probe is active Concealment and Lethality direct hits deal +20% per rank. **Kolto Burst**, Kolto Injection releases a pulse for 40% per rank of the heal amount as damage to enemies in a small radius; separate radius card (+12%) and target card (+1 enemy per rank).
- Mechanics (new, plus scaled Medical Screen, Emergency Response, Toxin Therapy from Enduring Kolto):
  1. **Advantage Engine.** Tactical Advantage max +1 per rank and lasts +2 s per rank.
  2. **Triage.** Surgical Probe heals +20% per rank per Advantage spent, and refunds 1 s of Kolto Injection cooldown.
  3. **Overdose.** Stim Boost and Adrenaline Probe grant +15% per rank damage to Medicine-linked abilities for 6 s.
  4. **Prognosis.** Kolto Injection overheal becomes a shield (50% per rank of the overheal).
  5. **Emergency Response** scaled: threshold +1.5 points per rank, cooldown -1 s per rank (floor 12 s).
- Medicine does not use the generic Health card.

### Operative Lethality: Cull in melee, poison plus Venomous Blades
Same axis list as Sniper Lethality and **Echo / Legion**, with Operative identity:
- **Venomous Echo.** With Venomous Blades, Laceration poison ticks count for Dart Echo and Legion (Laceration-applied Dart is the same effect).
- **Septic Detonation**, **Pandemic**, **Toxic Tempo** shared.
- **Toxic Resilience** tiny heal scales +1 hp per rank.
- Existing `op_cull_reset` and `op_weak_reset` scaled, with Weakening Blast now recovering Cull / Backstab, so Lethality's own mechanics do not point at Concealment abilities.

## Generic cards, offers, infrastructure
- Sniper and Operative both drop the generic Damage, Range and Cooldown cards. Health stays (small, uncapped, +4 per rank); Speed stays at +1.5% per rank, uncapped. Remove `MaximumRank=2`.
- `Scalable()` gains `snm_` and `opm_` mechanics. `Interaction()` in `RunProgression.cs` learns the new prefixes.
- `OperativeUpgradeUsable` added (Sniper has one). Dart cards stay offered when Venomous Blades replaces Dart, because Laceration applies Dart.
- Offers weight by tree (`AgentOfferWeight`): Concealment, Medicine and both Lethality trees get explicit `Spec` ids so each spec sees its own cards.

## Six-spec coverage check

| Spec | Damage | Radius / target | Tick / cooldown | Mechanics | Heal / defence |
|---|---|---|---|---|---|
| Marksmanship | 4 | ricochets, Suppressive | Rapid/Recoil via Cooldown | 7 | Dampers, Cover shield |
| Engineering | 6 | radii, Relay, Probe marks | probe tick, cooldowns | 9 | Probe Shielding, EMP heal |
| Lethality (Sniper) | 4 | Grenade, Cull splash | Dart/Grenade tick, cooldowns | 7 | Toxic Focus |
| Concealment | 5 | cleave targets | cooldowns, Laceration tick | 10 | Shadow Veil, recovery |
| Medicine | Field Surgeon, Kolto Burst | Burst radius/targets | Kolto tick, cooldowns | 5 | 3 heal cards, shield |
| Lethality (Operative) | 4 | Grenade, Cull splash | tick, cooldowns | 6 | tiny heal |

## Risks
- Cascade Detonation + Probe Legion + Echo can spike: 6 s cap per probe.
- EMP cooldown recovery feeds other cooldowns: 8 s floor on EMP.
- Medicine heal plus Kolto Burst damage could become a self-sustaining melee loop: Burst has a 1 s lock and heals scale small.
- Existing saves or tests that reference old `sniper_power_*` and `operative_power_*` ids need updates in `SniperRunPowersSmoke.cs`, `AgentSpecIdentitySmoke.cs`, `AgentProgressionSmoke.cs`.

## Questions for Brant
1. **Medicine damage.** Is it OK to invent Field Surgeon and Kolto Burst (reusing the existing Kolto and Cover Pulse SWTOR effects) so Medicine has damage and radius cards? Default yes.
2. **Medicine heal size.** +12% per rank for Operative heals (melee) versus Sniper stays near +4%. Fine, or smaller?
3. **Corrosive Grenade and Cull cooldown.** Grenade is an area DoT, not single target: allow a cooldown card (like Death Field)? Default yes. Interrogation Probe stays without one.
4. **Sever Tendon.** Treat as a stun-like control (no cooldown card) or allow? Default: no cooldown card.
5. **Kolto Probe / Adrenaline Probe cooldown.** Adrenaline Probe is a global cooldown trigger, so default no card for it; Kolto Probe allowed. OK?
6. **Echo / Legion.** Reuse for Dart, Corrosive Grenade and Interrogation Probe on both classes? Default yes.
