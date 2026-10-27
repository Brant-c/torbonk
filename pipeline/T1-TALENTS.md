# T1 — Pregame talent loadouts

Main Menu → Talents opens a class selector and three original-style Sorcerer trees. Allocate up to 12 points across any trees; no specialization declaration is required. Click an icon to invest, right-click to refund a point when no dependent investment would become invalid, or Reset / Respec to clear the profile. Changes save immediately; Save Build also retries a failed save. Starting a new run never requires visiting this screen.

Only Sorcerer has an allocation profile. All existing advanced-class IDs remain, and the class selector labels other profiles unavailable. Collections remains cosmetic only.

## Implementation

- `godot/Gameplay/Talents/sorcerer.json`: configurable budget, 24 stable IDs, class eligibility, four compact tiers per tree, ranks, costs, prerequisite ranks, required earlier-tier investment, descriptions, original icon paths, source GOM identities and effect descriptors.
- `TalentCatalog`: generic eligibility, allocation validation and deterministic replay of saved ranks. Unknown IDs, negative/excess ranks, unsupported classes, excess budget and unmet dependencies are discarded. Earlier tiers fund later ones; a tier cannot fund itself.
- `TalentProfiles`: independent `user://talent-profiles.json`, schema version 1 and per-class dictionaries. Writes use a temporary file followed by replacement. An unreadable or unknown-version save yields an empty gameplay profile and a warning; writing refuses to overwrite it and displays an error. No XP unlocks or run grind.
- `TalentRuntime`: a validated snapshot per new run. Recalculation derives modifiers from the existing run baselines. Restart clears proc timers, counters and temporary effects while retaining the same build snapshot. Editing a profile cannot mutate a running build.
- `TalentsScreen`: reusable data-driven columns, actual client headers/backgrounds/connectors/slot frames/rank plates, original icons, class artwork, rank states, dependency tooltips, class selection, save/reset and keyboard Back navigation.

Sorcerer's scripted five-level Lightning/Madness choices are removed. Its existing unlock resources are now eligible in ordinary run offers. Relevant talented abilities receive a modest offer-weight preference; off-tree abilities retain eligibility. Selected talents also add optional damage-training offers after the associated ability is learned. Talent investment never grants active abilities. Force Lightning remains the sole starting ability, the four offensive/two utility limits remain, and full-slot offers use the existing replacement screen. Sorcerer alone bypasses Storm/Death Field exclusivity. Assassin's milestone choices and exclusivity are unchanged.

No ability animation, sound, VFX, GCD, targeting, base cast, or base cooldown resource was reconstructed. No planet, enemy difficulty, equipment, camera/cursor, Collections, shared HUD or artifact persistence rules were changed.

## Curated effects

| Corruption | Lightning | Madness |
|---|---|---|
| Dark Mending: Dark Heal recovery | Convection: Shock/Blast damage | Calcify: Lightning/Affliction damage |
| Empty Body: Dark Heal output | Lightning Spire: range/Storm radius | Torment: Shock extends Affliction |
| Dark Resilience: low-health protection | Conduction: electrical hits recover Chain | Force Horrors: longer DoTs |
| Efficacious Currents: barrier capacity | Forked Lightning: existing fork mechanic | Parasitism: limited low-health periodic healing |
| Resurgence: low-health barrier healing | Lightning Barrage: Affliction boosts Lightning tick rate | Wrath: Lightning recovers DoT cooldowns |
| Force Bending: barrier recovers Dark Heal | Lightning Storm: Shock/Storm recover Chain | Deathmark: Field marks amplify periodic damage |
| Reconstruct: barrier-break recovery/retaliation | Chain Lightning: extra jump/training | Lingering Nightmares: faster DoT ticks/training |
| Force Surge: emergency recovery | Thundering Blast: afflicted-target synergy/training | Creeping Death: existing death-triggered DoT transfer |

Corruption's new proc healing is flat, health-conditional or rate-limited: Resurgence 12 seconds, Reconstruct 20 seconds, Force Surge 30 seconds. Parasitism heals at most 0.5/1 HP every two seconds below half health. Emergency recovery cannot revive a lethal hit. Protection from Dark Resilience is capped. The preexisting Creeping Terror and Death Field healing behavior remains.

Periodic talents modify the existing PeriodicEffects application/tick path. Death Field still snapshots caster-owned DoTs before burst damage and reapplies them through the existing area spread. Creeping Death uses RunBuildEffects' existing last_gasp/Spread path; it does not implement another spread engine. Forked Lightning likewise reuses the existing fork hook.

## Original-client provenance and adaptations

Evidence is **Client-derived**, not proof of native SWTOR execution or an exact restoration of MMO balance.

- `manifests/t1-talent-client-evidence.json`: April GOM identity, bucket path, offset, resource SHA256 and decoded fields for every selected original talent. The exact `ablIconSpec` values drive icon recovery. Historical decoder field labels remain explicitly labelled in the evidence.
- `manifests/t1-talent-art-source.json` and `t1-talent-art-conversion.json`: 24 unique April TOR icon lookups, archive/entry identities, decoded original hashes and output hashes. Native DDS → RGBA PNG; no redraw, recoloring or resampling. Reproduce with `scripts/prepare_talents.py` using the existing Pillow runtime.
- `manifests/t1-talent-ui-provenance.json`: subset of the existing April UI recovery with source identities, original GFX tag-1008 rectangles, exports/imports and font conversion records. Tree panel: `bwaui_skilltree_30`; header: `42`; icon frame: `9`; connector states: `1/5`; rank plates: `13/16`. Tooltip frame: `bwaui_tooltip_1`. Eurostile regular/bold uses the previously recovered embedded client outlines.
- `manifests/t1-talent-localization-evidence.json`: bounded original English ability-string results for Dark Heal, Static Barrier, Affliction, Force Lightning and Death Field. Source `/resources/en-us/str/abl.stb`, TOR path hash `0x8154956D54305B3B`, decoded SHA256 `6363a31bf926f22393518562e2a02b5b8dfb33ed1b9b26418160ec7d482c40ab`. Existing `sorcerer-talent-research.json` retains original Lightning Barrage, Forked Lightning and related descriptions.

The April Corruption ability package was also inspected: `pkg.abilities.sith_inquisitor.sorcerer.corruption`, GOM `0xE00003EC09ECA79C`, bucket 196, offset 3716, SHA256 `fafde943c4cd60f18ba83530c271f09d46cbf26fcbf56501d4c274aa808f8b0c`. It exposes 19 ability entries. Its Dark Mending `ablAbilityDataRanks` list contains a single zero entry, which is insufficient evidence for the original skill-tree maximum rank. No original prerequisite/tier layout was established from that package.

**Adapted for Survivors:** all descriptions displayed in the menu, gameplay effects and numbers, rank maxima, prerequisites, 0/3/6/9 earlier-tier thresholds, four-tier layout, and 12-point budget. Reconstruct and Force Surge become defensive hooks rather than new MMO healing abilities. Creeping Death uses a death-transfer effect rather than claiming its historical MMO effect. Original names/icons are used even when the effect is adapted. The existing class artwork is reused; no modern installed-client specialization icon is presented as April talent art.

The user-mentioned screenshot was not attached to this chat. Visual fidelity is therefore based on the recovered April artwork and original three-column arrangement; exact screenshot matching remains for manual review.

## Lightweight verification and limits

`Run-Talents-Smoke.ps1` compiles C#, imports assets once when needed, launches one brief rendered acceptance fixture and performs a second headless save-load check. Its APPDATA is isolated under `work/t1-talents-userdata`, so it does not change the player's profile. Use `-SkipImport` after assets are imported. Outputs: `manifests/t1-talents-smoke.json`, `t1-talents-persistence.json`, `t1-talents.png` and `logs/t1-talents-*.log`.

The fixture covers menu access, rank/budget/prerequisite validation, free respec, class isolation, 12-point hybrids, existing frontend handoff, starting ability, single application/recalculation, Storm + Death Field, slot limits/replacement eligibility, ordinary offers through level five, shared DoT registration/spread configuration, restart and saved-profile isolation from active runs. It does not replace manual combat balance, mouse/keyboard tooltip inspection or exact historical screenshot review. Legacy Sorcerer milestone smoke fixtures document the superseded milestone behavior and are not T1 acceptance tests.

Final verification: C# build succeeded with zero errors (the existing five nullable warnings and an offline NuGet advisory-feed warning). The final rendered fixture passed 50 checks; a separate process passed the persistence check. Both runtime logs contain no ERROR/SCRIPT ERROR entries. Godot reports 420 ObjectDB instances remaining at rendered-fixture shutdown; that shutdown warning has not been investigated as part of this limited milestone. The final capture was visually inspected after correcting icon/frame draw order and texture minimum-size ordering. Reimports' unrelated tracked import-setting changes were restored to their pre-task state.

The inspected Sorcerer roster is Force Lightning, Shock, Force Storm, Thundering Blast, Affliction, Creeping Terror, Death Field, Static Barrier, Dark Heal, Chain Lightning and Overload, from the current GameWorld composition; all remain present.

T1 stops here: no further class trees, unrelated content or global balance changes.
