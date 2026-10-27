# Sorcerer baseline — 2026-10-26

Current progression is documented in [SPEC-MILESTONES.md](SPEC-MILESTONES.md): Sorcerer starts with Force Lightning alone, without an automatic saber attack, and chooses Shock or Affliction at level five. Both Sorcerer and Assassin can learn Affliction, Creeping Terror and Death Field. Later Force Lightning upgrades are Madness choices. The original implementation/research notes below retain their historical context; the milestone document supersedes their earlier pool and ownership descriptions. Other classes retain their free basic weapon attack; see [BASIC-ATTACKS.md](BASIC-ATTACKS.md).

## Implemented

- Sorcerer's ordinary unlock and power pool excludes Death Field and Devouring Field. Assassin's kit includes both, loads the original B8 cast library, and uses the existing Death Field ground effect, impact audio and target feedback.
- Ritual Catalyst follows Death Field to Assassin. It remains unavailable before Death Field is learned. Sorcerer cannot receive it.
- Thundering Blast: locked offensive unlock, two-second cast, nine-second activation-to-activation cooldown, one internal-damage hit. Its recovery plays the recovered 1.6-second release clip. Moving out of range, losing line of sight, death or removal cancels the cast before damage.
- Affliction: separate offensive unlock; initial internal hit plus five three-second ticks over 15 seconds. Reapplication refreshes one instance per caster/ability/target. It survives range changes after application and expires normally.
- Blast checks the target's **caster-owned Affliction at impact**, applies a guaranteed critical and publishes the critical flag through Health/flytext. Another caster's Affliction and expired Affliction do not qualify. The blast does not consume Affliction.
- Retained four offensive / two utility slots. Lightning + Affliction + Blast leaves one offensive choice for Shock, Storm or Chain. The basic weapon attack uses no ability slot.
- Fixed two existing card inaccuracies: Chain Lightning says six seconds rather than eight; Devouring Field says the actual eight-point healing cap rather than 15.

### What is SWTOR-derived, and what is TorBonk tuning?

The April player ability is `abl.sith_inquisitor.skill.lightning.thundering_blast`, GOM `0xE000C26B65CE9541`, decimal `16141114831021184321`. Decoded fields show casting time 2, cooldown 9, Force cost 45, maximum range 3, icon `shadowtechnique`, and appearance `epp.sith_inquisitor.thunderclap.cast_loop`. Localized source text explicitly identifies internal damage and automatic critical hits against **your** Affliction. The effect graph checks the same effect tag present on Affliction, calls a hidden effect and modifies a meta-stat before the SpellDamage action. Historical field/enum names are labelled as such in the saved evidence.

Affliction is `abl.sith_inquisitor.affliction`, GOM `0xE000892C91A4F3EA`. Its periodic effect `/2/2`, `0xE00074724A77AF4E`, has duration `0x3A98` (15,000 ms), interval `0xBB8` (3,000 ms), stack limit one, by caster. The tooltip damage multiplier is six. The adapter expresses this as an initial hit and five subsequent ticks.

**Authored TorBonk numbers:** Blast 60 damage / 90 conditional critical; critical multiplier 1.5; Affliction 8 per hit, six-second reapplication cooldown; both use range 1.1. The game has no Force resource, native armor/resistance model, spell pushback, random crit stat model or original MMO damage scaling. These have not been restored or implied. The single-target, cast-time, internal-damage and owned-Affliction interaction are preserved within the existing automatic-combat system.

### Original presentation recovery

`manifests/sorcerer-source.json` records 51 selected resources with archive identity, offsets and decoded hashes. `sorcerer-conversion.json` records 16 textures. `sorcerer-audio.json` records all five selected Play/Stop events and 23 decoded media files; none of those events were missing. `sorcerer-geometry.json` records the actual `si_thundering_blast_force_energy.gr2` conversion. `Assets/Sorcerer/` contains the runtime library, textures, source parameters, FX graph, mesh and PCM.

The original appearance selects `AbilityChargeBack`, `AbilityCastInstant2Hand1`, the blast cast/projectile FX and `Play_ability_sithinquisitor_thunderclap_longbuildgrip`. The adapter selects matching original electrical JBA clips (`fp_1h_elect_charge_right_1`, `fp_2h_elect_instant_right_1`) through the established animation workflow. These are a semantic animation mapping, not proof of native weapon/state graph dispatch. Charge loops for the two-second windup; the release plays for its full converted duration.

The projectile uses its recovered GR2 and original material textures `force_energy_draw_in`, `caustics_tile_01`, `fx_thundering_blast_k`, source tints and UV rates. Shader composition, projectile travel of 0.25 seconds, effect sizes and impact timing are bounded Godot approximations. Damage resolves at cast completion independently of the travelling visual. Original `engine/zap.zzp` remains an unresolved engine primitive. Full native emitter execution, animation-note timing, Wwise mixer behavior and visual parity are not claimed. Affliction currently reuses the recovered Shock gesture and has no dedicated original Affliction VFX/audio adapter.

## Talent review: more interactions, fewer repeated percentages

Evidence: `manifests/sorcerer-talent-research.json` contains original identities, bucket hashes/offsets and localized descriptions retrieved with the read-only TOR research tool. `manifests/sorcerer-gom-evidence.json` preserves expandable ability/effect/appearance pages. Findings below distinguish original behavior from proposed Survivors adaptations. Percentages hidden behind source tooltip tokens are deliberately not invented.

| Current choice | Problem | Suggested improvement | Evidence / adaptation |
|---|---|---|---|
| Crackling Power | Unlimited damage ranks become the universal answer. | Keep as modest common training; stop using straight damage as the primary rare reward. | Design proposal; retain a baseline option for late runs. |
| Forked Lightning (`tick`) | Name suggests an extra arc, but only increases tick rate. | Rename this stat track; reserve Forked Lightning for an actual repeat-strike mechanic. | April Forked Lightning repeats Lightning Strike/Chain Lightning on the **same targets** at 30% damage. Current `fork` instead splashes another target. |
| Forked Current (`fork`) | Useful, but another always-on damage bonus with little timing. | Keep the cleave behavior under an honest name, or replace it with the researched repeat-arc choice. | Do not call current cleave an accurate restoration of Forked Lightning. |
| Lightning Barrage (missing) | Affliction currently has no talent bridge into the starter. | Add a next-channel proc: an Affliction tick primes a twice-as-fast Force Lightning channel with the same tick count and total damage. Use a ten-second lockout. | April: Affliction **critical** hits can grant a double-speed channel, ten-second internal cooldown. Deterministic every-N-tick triggering is a proposed adaptation until general periodic crits exist. |
| Lightning Storm (missing) | Storm and Chain do not cooperate. | Storm damage can reset Chain Lightning and prime its next instant activation, with a ten-second lockout. | April explicitly has Force Storm damage and Lightning Strike triggers, resets Chain, removes its activation time, ten-second internal cooldown. Trigger chance must be decoded or deliberately tuned. |
| Recklessness (`cooldown`) | A recognizable active SWTOR name currently means unlimited passive recovery. | Rename the passive track. Design a separate charged critical-window utility later. | Naming/design proposal; Recklessness's own effect graph has not been audited here. |
| Lightning Reach | Range expands without adding a new decision. | Cap low-rank reach; use rarer choices for targeting changes, such as Blast prioritizing your afflicted enemy. | Proposed auto-targeting adaptation; keep targeting deterministic and within line of sight. |
| Surging Shock | Ability-specific damage repeats the generic track. | Let Shock interact with a debuff/proc or make it the quick finisher route. | Proposal requiring a separate Shock talent source pass; no invented SWTOR authenticity claim. |
| Gathering Storm + Lingering Tempest | More radius and duration overlap; longer channels can also delay other casts. | Keep one area-growth choice. Use the other slot for Lightning Storm's Chain reset. | April proc evidence above. Duration is still a legitimate common option, not a defining artifact. |
| Clinging Storm | One of the few positioning effects. | Keep; make its purpose readable as area control. A second rank should change control behavior rather than only add damage. | Current TorBonk adaptation; original snare parity not established. |
| Barrier Reprisal | A depleted shield produces only 16 damage. | Add short nearby control when **your Static Barrier** ends; handle boss resistance and damage breaking control explicitly. | April Backlash blinds nearby enemies for three seconds when Static Barrier ends; damage ends the blind. Current depletion-only damage pulse differs. |
| Dark Escape | Any utility activation grants speed, including an automatic heal. | Tie it to a deliberate defensive condition, or make the mobility effect a separate Force Speed utility. | Proposal; do not label the current passive an imported Force Speed. |
| Desperate Power | Low-health damage is interesting, but automatic healing pulls against it. | Keep as a clear risky route; pair it with damage prevention and a visible health threshold, not further generic damage ranks. | Authored Survivors rule. |
| Enduring Barrier | Five ranks of three percentages are hard to read or feel. | Split into a common capacity choice and one behavior choice: expiry control or a shield-break proc. | Backlash provides the strongest recovered original mechanic. |
| Dark Restoration | Five ranks repeatedly raise heal, recovery and threshold together. | Offer one emergency-heal behavior choice and fewer explicit numeric ranks. Avoid making every run converge on the same sustain. | Proposed gameplay design. |
| Exsanguinate (missing) | Blast's new setup has no persistence investment. | A small Affliction-duration branch can support Blast uptime; show remaining duration and refresh behavior. | April Exsanguinate increases Affliction duration. Decode rank amounts before claiming an exact port. |
| Polarity Shift (missing) | No temporary caster tempo choice. | Add a utility with a short haste window that compresses cast and tick durations consistently while preserving channel tick counts. | April source says 20% alacrity; exact duration/rank tokens require decoding. Proposed integration must affect the **whole lifecycle**, not tick interval alone. |
| Wrath (future branch) | Potential interesting mechanic, easily misapplied to Blast. | Save for a future Crushing Darkness / Lightning Strike branch. | April Wrath makes those two abilities instant and +20% damage. It does **not** say Thundering Blast. |

### Drop review

| Current drop | Assessment | Suggested change |
|---|---|---|
| Ancient Sith Holocron: +1 Chain bounce | Changes coverage, but duplicates Lightning Relay's two ranks. | Keep one as progression. Give the relic a distinct proc or repeat-arc behavior, with a visible cooldown. |
| Storm Focus: +2 seconds | Exactly duplicates Lingering Tempest. | Replace with a Storm → Chain interaction; a boss relic should alter the rotation. Do not add another duration-only relic. |
| Tomb Guardian Relic: shield after elite/boss kill | Reward often activates after the danger has ended; an existing barrier can suppress it. | Consider banking the ward for the next encounter or rewarding a successful defensive event. Explicitly define interaction with Static Barrier. |
| Forbidden Codex: +30% damage / -20% health | Real tradeoff, but generic across classes and likely the default boss reward. | Retain as a universal risky option; add class-specific alternatives before making it the Sorcerer baseline. |
| Ritual Catalyst: Death Field vulnerability | Correctly belongs with Assassin after this change. Current effect buffs all ability damage without charge consumption. | For a later Assassin pass, use Deathmark: a bounded number of periodic hits receive the bonus. April Deathmark names the next **ten periodic** damaging abilities, not four seconds of all damage. Amount and duration remain to decode. |
| Dark Restoration Relic | Another heal multiplier on top of the same talent track. | Change to a distinct emergency-heal behavior or a defensive proc. Do not add a third flat heal scaling source. |
| Overcharged Conduit | One of the strongest existing designs: cross-ability cooldown interaction. | Keep the concept. Require an equipped qualifying source and another usable cooldown recipient; consider including Blast deliberately. Current source list is Lightning/Chain. |
| Restorative Echo / Tomb Ward / Force Renewal | Useful immediate fallbacks, weak boss-defining choices; heal/shield can have no immediate benefit. | Preserve three-choice fallback guarantees; distinguish emergency consumables from relic rewards and avoid spending a major reward on a no-op. |

## Recommended order for the next design pass

1. **Caster combo:** Affliction → Thundering Blast, then Lightning Barrage. This gives Sorcerer a clear setup/payoff identity using the imported ability.
2. **Storm conductor:** Force Storm → Chain Lightning reset, then a distinct repeat-arc relic. This competes for offensive slots with the Blast route.
3. **Barrier control:** replace the small reprisal damage pulse with sourced control rules, after defining boss resistance and which shield owns the effect.
4. Separate ordinary stat progression, mechanic talents, relics and instant recovery rewards. Avoid duplicate duration/bounce/heal multipliers across talent and relic slots.
5. Improve offer prerequisites before adding more cards: require both sides of a combo, do not offer source-less procs, account for full slots, and decide whether a run can replace an earlier offensive choice. Current unlocks are permanent for the run; filling all four offensive slots can lock out Blast/Affliction.

All proc chances, stack caps, internal cooldowns, ownership checks and consumed charges should be data with targeted behavior checks. Rendering must continue to have no damage authority. Preserve rarity colors, run reset and slot rules as the shared baseline.

## Validation

`Run-Sorcerer-Smoke.ps1 -SkipImport` builds the project and runs separate isolated Sorcerer/Assassin scenes. Focused checks cover class ownership, locked relic exclusion, unlock duplication, two-second delayed hit, internal damage, guaranteed critical/flytext flag, foreign and expired Affliction exclusion, expiry cadence, cancellation, original asset availability, audio/effect cleanup, four-slot enforcement and run reset.

Validation passed 27 Sorcerer and 10 Assassin checks, plus all 32 checks in the existing Assassin kit smoke (including Contagion and Static Blades). A rendered Sorcerer run also passed its 27 checks without runtime/shader errors; `manifests/sorcerer-charge.png` and `manifests/sorcerer-blast.png` were visually inspected. These are fixture captures, not a full encounter playtest. Build has existing nullable warnings and the offline NuGet advisory-feed warning. The original source archives and existing unrelated difficulty work were preserved. Old B8/D2 fixtures that explicitly assumed Sorcerer Death Field remain historical; this new class-aware runner is the regression for the changed ownership.

Manual review still matters: verify the new ability cards and action bar, audible buildup/release, readable projectile size, cast pacing while moving, crit feedback with Affliction, and Assassin's Death Field in a real encounter. Passing fixtures do not establish native SWTOR visual parity or final balance.

### Thundering Blast visibility correction

The held charge now follows both original hand sockets after animation updates, using the recovered `bw_flare_gray` wrist-glow texture and animated `electric_rings_bw_02` 2×2 atlas. The previous adapter displayed the complete ring sheet as one faint sprite. A dedicated additive billboard shader preserves source colors, per-frame UVs, scale and fade. The recovered projectile mesh/material textures and impact textures have stronger, bounded brightness and slightly larger impact layers. Charge audio/effects stop on release or cancellation. Damage, critical rules and cast/cooldown timing are unchanged. No replacement artwork was generated. Rendered charge/impact captures and the focused Sorcerer smoke verify the correction.
