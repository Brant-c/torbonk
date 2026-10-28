# Assassin endgame relics

Nine permanent Assassin-only relics are seeded into both new and existing valid inventory profiles. Any two distinct relics may be equipped across specializations. Existing ownership, atomic save, +0–+3 ranks and 500/1,500/3,000 Credit upgrade costs are reused. Tooltips select the equipped item's exact rank text. No loot tables or Sorcerer relic definitions were changed.

Combat is implemented in `Gameplay/AssassinRelics.cs`; ability hooks do not mutate shared resources. Secondary hits bypass attack proc dispatch. Parasite pulses have a death-proc guard; Wither secondary waves never enter the primary Wither hit path. Healing is capped per caster at 2/4/6/8 health per second, shared across direct, periodic and secondary damage. Lacerate uses the existing owned DoT source snapshot and only infects fresh targets, capped at 2/3/4/5 with Devouring Plague. Each newly infected target bursts once, even if multiple DoTs spread.

Death Field zones tick once per second for 4/6/8/10 seconds independently of ability casting and player movement. Rank three adds 25% saber damage inside the zone. Wither's rank bonuses are 75/125/175/250% damage and 25/40/60/80% radius; rank-three kills create half-radius waves for 175% base damage. Crushing Darkness marks last six seconds, connecting 2/3/4/5 neighbors. Defensive auras end with their defensive duration (or shield depletion), and do not stack duplicate copies of the same defensive ability. Hidden Predator grants 50% faster recovery and global cooldown progress for 2/3/4/5 seconds.

## Assets and limitations

Client-derived assets are reused in place; exact selected file hashes and original conversion receipt references are in `manifests/assassin-relic-assets.json`. Relic icons reuse the recovered dark-side relic icon family. Maul uses the original saber gesture and an additional original Maul impact; there is no separately recovered spectral saber actor or unique icon per relic. Shock uses its original arc presentation. Death Field uses the original B8 ground textures with the existing ground projection adapter. Wither, aura and parasite pulses use recovered April shockwave/wisp assets. These expanded effect sequences are custom runtime adaptations, not claims of original SWTOR gameplay or faithful EPP playback. No new unique relic audio was recovered; existing ability audio remains in use.

## Validation

Run `Run-Assassin-Relic-Smoke.ps1` for deterministic checks of all ordered distinct relic pairs, all ranks, duration/radius/damage, actual Wither and Death Field casts, healing limits, DoT deduplication, persistence, Credits and unequipped behavior. Results: `manifests/assassin-relic-smoke.json`. The existing Assassin refinement regression passed baseline (73), madness (74) and deception (74) checks. Validation is headless; final visual balance and feel still benefit from playtesting.
