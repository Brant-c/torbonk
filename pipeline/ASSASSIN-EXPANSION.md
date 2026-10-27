# Assassin expansion

The current combat, loadout and VFX changes are documented in [ASSASSIN-SYNERGY.md](ASSASSIN-SYNERGY.md), which supersedes the tuning below.

The follow-up in [ASSASSIN-REFINEMENT.md](ASSASSIN-REFINEMENT.md) supersedes the initial asset substitutions, Spike behavior, starting attacks and early progression described below.

Adds 24 class-specific pregame talents (8 each in Darkness, Deception and Madness), using the existing 12-point budget, validation, persistence and menu layout. Sorcerer catalog and tuning are retained. No specialization lock; old milestone choices remain compatible.

Abilities: retains Lacerate, Discharge, Death Field, Creeping Terror, Affliction and Force Shroud. Adds Voltaic Slash, Maul, Spike, Wither, Low Slash, Force Cloak and Deflection; reuses Shock, Crushing Darkness and Force Speed through class-local configuration. Four offensive/two utility slots and normal Banish/Reroll/Skip rules remain intact.

Voltaic Slash stores two 20% Shock charges. Deception can evolve Shock into a three-target chain, Maul into splash, Spike into area control and Lacerate into a wider sweep. Force Cloak pauses ordinary enemy pursuit for at most two seconds, provides 65% attack avoidance, and enables opening attacks for four seconds. Offensive abilities end disengagement. Maul gains 50% opening damage, increased by Duplicity. Bosses keep pursuing and resist stun. Deflection grants 50% attack avoidance for four seconds (five with Hollow); cooldown 24 seconds. Force Shroud remains a finite absorption shield. Low Slash/Spike have six-second per-enemy control locks and 12-second ability cooldowns.

Madness enables early Creeping Terror eligibility with fivefold offer preference, adds bounded life-drain, DoT damage, Lacerate spreading, Shock echoes and remaining-duration death transfers. Death Field always spreads all owned eligible DoTs, including Discharge, Terror, Affliction and Crushing Darkness; it heals 3 per target up to 10 per cast. Terror heals at most 2 HP/second across all Terror targets; Parasitism adds at most 1 HP/1.5 seconds across all DoTs. Darkness offers 8% mitigation, a 2 HP reactive heal every five seconds, shield/cooldown improvements and Wither support. Wither reduces enemy attack damage by 25% for four seconds.

## Asset limitations

All rendered assets are existing original SWTOR resources. Dedicated Maul, Voltaic Slash, Spike and Low Slash clips/icons/audio were not staged or verified in this pass: these use the existing original D3 double-saber Lacerate clip and appropriate existing saber icons/audio. Wither uses the staged original Discharge animation, electricity atlas and cast audio. Force Cloak/Deflection reuse the Shroud animation and icon; Force Cloak currently has no dedicated invisibility shader. Assassin talent icons use existing original generic saber/Force icons rather than verified individual talent art. These substitutions are intentional and are not represented as exact historical restorations. Dedicated assets may exist in the April archive; absence from this pass does not establish archive absence.

Existing original Death Field, Discharge, Lacerate, Shroud, Terror, Shock, Darkness and Force Speed resources remain in use. Character/weapon composition and double-bladed lightsaber remain unchanged. Avoidance is deterministic damage reduction, rather than random misses. Defensive reduction currently applies through the existing enemy attack path.

## Validation

Build and headless import pass. Assassin expansion smoke passes 27 checks with a saved hybrid profile, covering class isolation, persistence, slot limits, stealth gating, empowered Shock, bounded Terror healing, chain evolution, both spread paths, remaining-duration death transfer and defensive expiry. Existing Sorcerer baseline passes 29 checks; existing Assassin/Sorcerer compatibility passes 11 checks; Sorcerer refinement passes 151 checks. Existing map texture UID warnings and unavailable NuGet vulnerability feed warnings remain. No long-session balance or visual playtest was performed.

