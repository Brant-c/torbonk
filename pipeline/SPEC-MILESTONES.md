# Spec milestones — Sorcerer and Assassin

Sorcerer starts with Force Lightning alone, without an automatic saber attack. Every fifth level presents explicit spec talent choices. Ordinary levels retain three randomized power offers. Choices remain independent: Lightning, Madness and hybrid runs are all supported, with no permanent spec declaration or purity gate.

| Level | Lightning | Madness |
|---|---|---|
| 5 | Unlock Shock | Unlock Affliction |
| 10 | Unlock Force Storm | Unlock Death Field |
| 15 | Unlock Chain Lightning | Unlock Creeping Terror |
| 20 | Unlock Thundering Blast | Upgrade Force Lightning: +40% base damage |
| 25 | Unlock Shock, or +50% base damage if owned | Unlock Affliction, or +50% base damage if owned |
| 30 | Unlock Thundering Blast, or +40% base damage if owned | Unlock Force Lightning, or +40% base damage if owned |

An unowned ability is unlocked; an owned ability is strengthened. Milestone bonuses use their displayed fixed amount, without a hidden rarity multiplier. Milestone unlocks are withheld from ordinary randomized offers, so the named choices cannot be preempted by an earlier random drop. Channel training is offered only after a channel ability is owned, and its card is named Channel Mastery. Existing developer grants remain available for fixtures.

Force Lightning is the common starting ability; its later upgrades belong to the Madness milestone column. Lightning's column focuses on Shock, Storm, Chain Lightning and Thundering Blast. Mixing remains useful: Affliction can still support Thundering Blast's guaranteed critical interaction.

Force Storm and Death Field are mutually exclusive at the ability-controller level, including direct unlock attempts. This is an equipped-ability restriction, not a broad spec lock. The four offensive / two utility slot rules remain. When a milestone unlock would exceed its category limit, selecting it opens a replacement screen showing currently equipped abilities. Nothing is removed until an ability is chosen there. Back returns to the original milestone without spending the choice. Basic attacks never occupy a slot or appear as replacement candidates.

Assassin uses the same levels with three named columns: Darkness (tank), Deception (DPS), and Hatred (DPS). The first milestone offers Force Shroud / Discharge / Affliction. Hatred offers Death Field at 10 and Creeping Terror at 15; subsequent choices strengthen shield recovery/capacity, melee/Discharge damage, or DoT damage. No permanent spec lock is imposed. Other classes retain their existing randomized progression pending their own kit design.

## Shared DoTs and Death Field

Sorcerer and Assassin both have Affliction, Creeping Terror and Death Field. A Death Field impact snapshots every distinct caster-owned DoT present on an affected enemy **before** applying burst damage. Each is reapplied at full configured duration to every surviving affected enemy, with no artificial target limit. Range, radius, hostility and line of sight still define which enemies are hit. A killed seed still contributes its DoTs. Foreign-caster effects are excluded.

Creeping Terror now deals 12 internal damage immediately and 12 each second for six seconds (84 base total). Its damaging periodic ticks heal the caster for one HP, at most once per second across all targets carrying that caster's Creeping Terror. Spreading to a large pack increases damage coverage while retaining modest healing. Its slow and original presentation remain. These numbers and spread/healing rules are authored TorBonk tuning, not a claim of exact MMO balance restoration.

## UI and original SWTOR artwork

Milestone cards lead with the spec name and its actual SWTOR icon, with the original role symbol alongside it. Ability art and its effect are the main content; actions say Upgrade, Unlock or Replace. No spec-explanation footer, rarity-math footnote or permanent-specialization warning is needed. The replacement view shows current damage totals/cooldowns and a Back button. Keyboard 1–4 and normal focus navigation remain supported.

Nine icons were recovered read-only from the installed SWTOR client's `swtor_main_gfx_assets_1.tor`: Lightning, Madness, Corruption, Darkness, Deception, Hatred, DPS, heal and tank. `manifests/spec-ui-installed-source.json` records source paths, exact TOR path hashes/offsets, Zstandard decoding lengths, DDS hashes, PNG hashes and dimensions. This installed-client UI art is explicitly separate from the April-2012 gameplay asset provenance. It is native-resolution DDS → RGBA PNG conversion, with no redraw or recolor. No generated replacement artwork is used. The existing SWTOR controls, chrome and font are reused.

Both current Sorcerer paths use the DPS symbol; Creeping Terror's self-heal does not turn Madness into a healer spec. Darkness uses the tank symbol. The recovered heal/Corruption artwork is ready for future healing-path choices rather than mislabelling a damage choice.

`Recover-Spec-UI.ps1` repeats the bounded nine-icon recovery using the existing local ZstdSharp decoder. The archive is opened read-only. `Run-Spec-Milestones-Smoke.ps1 -SkipImport` checks progression and combat; add `-Review` for rendered UI captures. Captures cover Sorcerer levels 5/10/15/20 and replacement, plus Assassin's three-way level-five choice.

Validation covers milestone timing, independent mixed paths, Shock/Affliction unlocks and the Force Lightning-only start, both directions of Storm/Death Field exclusion, spread across 24 enemies including a lethal seed, exclusion of foreign DoTs, Creeping Terror healing limits, Force Lightning's Madness cast, full-slot replacement/back, ordinary choices between milestones, later milestones and restart. Visual review verifies the actual icon/role artwork and card layouts. Full encounter balance remains a gameplay review.

The milestone and startup smoke runners cover the current Force Lightning-only start, level-five unlocks and absence of a Sorcerer saber attack. Rendered milestone checks pass without runtime/shader errors. Build has the existing nullable warnings and offline advisory-feed warning. No native SWTOR gameplay parity or final balance acceptance is implied by these fixtures.
