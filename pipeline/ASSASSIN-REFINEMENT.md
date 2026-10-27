# Assassin refinement

The current combat, loadout and VFX changes are documented in [ASSASSIN-SYNERGY.md](ASSASSIN-SYNERGY.md), which supersedes the tuning below.

Assassin starts with Lacerate alone. Its double-bladed lightsaber, original Lacerate animation and equipment remain. No basic filler attack is created or offered. Sorcerer tuning/catalog remain unchanged.

## Early progression and damage

Levels 2–6 offer two eligible core ability choices alongside normal training. Madness-invested builds prioritize Creeping Terror, then Death Field; Deception-invested builds prioritize Discharge, then Spike. A build with no talent investment initially sees Terror and Discharge. Other branches remain learnable; hybrid builds remain supported. Core abilities are no longer reserved until level 5/10/15 milestones. Banish, Reroll, Skip, replacement, offensive/utility limits, and eligibility filters still apply. Skipping a defining ability does not force it into the build.

Removed the duplicate Lacerate Mastery damage track, which was unintentionally converted into another global damage track alongside Combat Training. Assassin's remaining Combat Training gives 10% base damage per rank, at most five ranks (rarity scaling retained). Baseline Lacerate damage is 18 instead of 22, configured on Assassin's local ability instance. The same global training benefits learned DoTs and other attacks. Later milestone damage choices and artifacts remain available.

## Spike and stealth

Spike no longer requires Force Cloak. It fades the character into stealth for 0.85 seconds, pauses ordinary pursuit, then restores visibility and performs a 54-damage caster-centered AoE with 0.4 radius. It has a 12-second cooldown, brief stun, and six-second per-enemy control lock; bosses remain control-resistant. During the vanish, the controller holds other activations. Interrupting the vanish restores visibility immediately. Darkswell expands radius by 25% and damage by 20%. The opening also empowers Maul; Force Cloak remains a separate utility option.

The opacity of the character and equipped weapon meshes is saved and restored rather than permanently replacing their materials. Reset, cancellation and expiry clear the stealth state. No permanent invulnerability is introduced.

## Original assets

All 24 talent icons now use decoded April `ablIconSpec` mappings. Unsupported modern talent names were replaced in the UI with original April names (Electric Winds, Crackling Blasts, Force Horrors); saved talent IDs remain stable. The original names do not imply historical game mechanics: this is still a survivor adaptation.

Corrected new ability/offer icons: Voltaic Slash `vampiricslash`, Maul `sunderingassault`, Spike `spike`, Wither `crushingdarkness`, Low Slash `lowslash`, Force Cloak `force`, Deflection `lightsaberreflection`. Existing Lacerate, Discharge, Terror, Death Field, Shock, Crushing Darkness, Shroud and Speed icon mappings were checked against the April definitions.

Recovered a separate original JBA animation library. Maul, Voltaic Slash, Deflection and Wither follow April EPP clip references. Spike's EPP names the semantic `SaberSpecial3Attack1`; the adapter uses the original `cb_saber_sp3_attack_right_1` JBA. Low Slash's original EPP names `cb_consular_saber_strike2`, whose exact JBA was not located; the adapter uses the original pole `cb_pole_sp1_attack_right_5` clip. These two mappings remain semantic adaptations, not proven exact native action-provider resolutions.

Rebuilt new attack effects from their actual EPP-linked FXSpec/particle dependencies: Spike electrical rings/sparks, Maul impact, Voltaic impact, Low Slash shared Consular impact, Wither dark wisps/shockwave, Force Cloak shadow smoke and Deflection loop. Particle tint, blend mode and atlas artwork come from those selected resources. Emission counts, placement and timing are bounded Godot adaptations rather than a full SWTOR particle renderer. The large generic Lacerate effect/sound substitutions are removed from runtime use.

Recovered the original Imperial melee audio and the shared Republic melee audio referenced by Low Slash/Voltaic Slash. Stop events and an unused duplicate huge-hit alias do not produce standalone media graphs; the adapter uses resolved impact events and explicitly clears voices. Audio uses existing bus limits.

Evidence: `manifests/assassin-refinement-icon-mappings.json`, `assassin-refinement-extra-icons.json`, `assassin-refinement-appearance-evidence.json`, `assassin-refinement-source.json`, `assassin-refinement-conversion.json`, `assassin-refinement-animation-conversion.json`, and `assassin-refinement-audio.json`. Source TOR archives are read-only; each extracted original is hash-recorded.

## Validation

Run `Run-Assassin-Refinement-Smoke.ps1`; `-Review` also captures rendered combat screenshots. Tests cover baseline/Madness/Deception startup, all icon mappings, levels 2/3 offers, Banish/Reroll/Skip, own-stealth Spike, no premature damage, multi-target damage, fade/reveal/interruption cleanup, original effects/audio, animation availability, reset and bounded training. Existing Assassin expansion, Sorcerer baseline/refinement and starting-attack checks provide regression coverage. Rendered Spike and Wither output was inspected. Longer-session balance remains a playtest question.
