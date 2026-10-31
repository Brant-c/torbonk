# Plan: rework Sniper and Operative in-run talents

For an implementing agent. Work in D:\SWTORClassic\torbonk (pipeline/godot/Gameplay/Talents), on top of the uncommitted Jugg/Marauder, Assassin and Sorcerer work. Design source: `proposal.md` in this folder. Builds and tests run on Brant's PC; run Godot directly (the PowerShell runners break on Godot warnings; some smokes already fail for unrelated reasons). SWTOR assets only, no placeholders. Do not commit or open a PR unless Brant asks.

## Decisions (Brant, 2026-10-30)
- **Medicine is out of scope.** Brant cares about the DPS specs: Marksmanship, Engineering, Sniper Lethality, Concealment, Operative Lethality. Skip every Medicine card in the proposal (Field Surgeon, Kolto Burst, heal/cooldown/tick cards, Advantage Engine, Triage, Overdose, Prognosis, Emergency Response scaling). Do not touch Medicine talents or abilities.
- Unanswered questions use the proposal defaults: Corrosive Grenade and Cull may have cooldown cards (Interrogation Probe and Dart may not); Sever Tendon is control-like, no cooldown card; Echo and Legion reused for Dart, Corrosive Grenade and Interrogation Probe on both classes.
- Standing rules: one axis per card; `MaximumRank = 0` everywhere; every card scales with rarity; no cooldown on single-target DoTs or stuns (Dart, Interrogation Probe, Debilitate, Flashbang, Sever Tendon); ranged healing tiny; movement speed tiny; Debilitate stays out of Sniper.

## Steps
1. **Infrastructure** (`UpgradeDefinition.cs`, `RunProgression.cs`).
   - `Scalable()`: add `snm_` and `opm_` mechanic prefixes (MaximumRank 0, AmountPerRank > 0), as `jgm_`/`mrm_`.
   - `Interaction()` and `ClassPower()`: recognise the new prefixes.
   - Remove `MaximumRank=2` on Sniper Health/Speed; make Speed +1.5%, Health small, both uncapped. Operative also drops generic Range and Cooldown.
   - Add `OperativeUpgradeUsable` and call it from `IsEligible`. Keep Dart cards eligible when Venomous Blades replaces Dart (Laceration applies Dart).
2. **Sniper axis cards** (`SniperRunPowers.cs`). Replace the 14 bundles with separate lists in the style of `AssassinRunPowers`: damage (~+.9, Dart/Cull/Weakening ~+1.0), radius (+.12, grenade +.15), cooldown (+.18; not Dart, not Interrogation Probe; EMP floor 8 s), tick (`UpgradeEffect.TickRate` .15: Dart, Corrosive Grenade, Interrogation Probe), targets (ricochets for Ambush/Series/Followthrough replacing the flat +2; Corrosive Grenade; Cull splash; Weakening spread; Interrogation marks). Ids `sniper_power_*`, `sniper_radius_*`, `sniper_cooldown_*`, `sniper_tick_*`, `sniper_targets_*`. Set `Spec` explicitly per card.
3. **Sniper mechanics.** Convert the 12 existing mechanics to `snm_` scaled versions (numbers in proposal), and add: Firing Chain, Dead Eye, Echo Round, Hold the Line (Marksmanship); Probe Echo, Probe Legion (reuse `AssassinDotSlots` style helper), Relay Network, Cascade Detonation (6 s cap), Lingering Plasma (Engineering); Dart/Grenade Echo and Legion, Pandemic, Septic Detonation (6 s cap), Toxic Tempo (Lethality). Defence: Ballistic Dampers (1/(1+.01*sum) in `AgentIncoming`), Deployed Cover and Toxic Focus shield capacity (`ShieldPerRank`), Probe Shielding. Internal locks via `Ready(...)`. Gate in `SniperUpgradeUsable`.
4. **Operative axis cards** (`OperativeProgression.cs`). Same structure: remove Debilitate cooldown, Carbine card stays excluded under Concealment, cooldown cards for Backstab/Hidden Strike/Laceration/Shiv/Cull/Weakening/Grenade, target (cleave) cards for Backstab/Laceration, tick card for Laceration plus the poisons, radius cards. Ids `operative_*`.
5. **Operative mechanics.** Scale the 5 existing (`opm_`; re-point Exposed Nerves to Backstab and Sever Tendon). Add Opening Gambit, Hamstring Mark, Twin Slash, Counter Surge, Exploit the Stun (Concealment); shared Lethality Echo/Legion, Pandemic, Septic Detonation, Toxic Tempo, Venomous Echo. Defence: Shadow Veil, Close Quarters Recovery (+2 hp per rank per kill, 1 s lock), Toxic Resilience scaling.
6. **Offer logic** (`AgentOfferWeight`, `RunProgression.cs`). Weight by tree via `Spec`; never offer cards for unowned abilities; ensure each of the five DPS specs gets at least: one damage, one radius or target, one tick/cooldown, two mechanics, and a defence card in a seeded offer sample.
7. **Smoke tests.** Update `SniperRunPowersSmoke.cs`, `AgentSpecIdentitySmoke.cs`, `AgentProgressionSmoke.cs`, `AgentExpansionSmoke.cs`, `SniperLethalitySmoke.cs` for removed ids. Add checks: exactly one non-zero axis per axis card; no cooldown card on Dart, Interrogation Probe, Debilitate, Flashbang, Sever Tendon; `MaximumRank==0` on every new card; mechanics scale with rarity; per-spec minimum set offered over 12 seeds per spec.
8. Build with Godot .NET on the PC, run the smokes directly, report results to Brant.

## Do not
- Add rank caps, bundled cards, or cooldown on single-target DoTs or stuns.
- Touch Medicine, Bounty Hunter or other classes' cards.
- Use placeholder art, audio or fonts.
