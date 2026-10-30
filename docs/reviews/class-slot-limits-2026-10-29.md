# Shared class ability limits — 2026-10-29

Normalized Sorcerer, Assassin, Juggernaut, Marauder, Operative and Sniper around the Sorcerer benchmark. Mercenary and Powertech are excluded at the user's request; Powertech retains its existing three utility slots and both Bounty Hunter classes retain their existing ability exemptions.

| Class | Offensive slots | Utility slots | Exempt abilities |
| --- | ---: | ---: | --- |
| Sorcerer | 4 | 2 | Existing Affliction and talent-granted Polarity Shift |
| Assassin | 4 | 2 | Free replaceable Lacerate starter |
| Juggernaut | 4 | 2 | Free replaceable Smash starter; basic Vicious Slash fallback |
| Marauder | 4 | 2 | Free replaceable Vicious Slash starter; basic Assault fallback |
| Operative | 4 | 2 | Free replaceable Shiv starter |
| Sniper | 4 | 2 | Free replaceable Snipe starter; basic Rifle Shot fallback |

All other abilities count according to their runtime category. This includes Warrior defenses, reactions, forms and buffs, Assassin utility/control, Deception Electrocute and Operative Blow for Blow. The change does not reclassify offensive control attacks such as Debilitate; it makes their existing category consume a real slot.

The common policy is applied to the completed class roster in GameWorld before progression initializes. Serialized legacy Slotless flags no longer create extra exemptions for the six covered classes. Profile overrides for Operative and Sniper now read 4/2. Assassin no longer receives a special six-offensive limit.

## Replacement and talent grants

- Full offensive and utility categories offer same-category replacements through the existing level-up flow.
- Replacing a free starter transfers its free position to the incoming attack. The former starter loses its own exemption, preventing reacquisition from creating a second free position. The credit can transfer again through ordinary replacement; restart restores the original starter and clears inherited credits.
- Talent-granted Electrocute and Blow for Blow are initially admitted only through normal slot admission. Once replaced, stat recalculation no longer silently re-enables them alongside a full utility loadout.
- Both have talent-gated acquisition cards, so a replaced signature ability can be selected again through normal category replacement. A fresh run resets talent admission and restores the initial talent grant when space exists.
- Existing Sorcerer Affliction/Polarity behavior is retained as the benchmark.
- Quickbar help and affected ability descriptions reflect actual costs. The six covered classes fit their legal loadouts within the existing twelve-icon bar.

## Verification

`pipeline/Run-Class-Slots-Smoke.ps1 -SkipImport` exercises all six classes plus Deception and Counterstrike talent variants: **292 checks** across eight isolated runs. It covers startup exemptions/counts, ordinary admission, rejection at full categories, replacement offers, category isolation, transfer/reacquisition of the free starter position, recalculation after replacement, talent-ability reacquisition, and restart.

The same runner accepts `-Classes` and `-Variants` for bounded reruns. The last rerun rechecks Sorcerer, Assassin, Operative and both talent variants after excluding Bounty Hunters.

Additional focused checks: Inquisitor revision suite **25 Sorcerer / 24 Assassin**, Sorcerer refinement **154**. These preserve the recent Deception gameplay, presentation repairs, Sorcerer stationary starts and Assassin Death Field exemption under the new budgets. The previous test that force-enabled thirteen Assassin icons now checks that a legal capped loadout displays every learned ability.

Build succeeds with existing nullable/obsolete warnings and unavailable NuGet vulnerability metadata. Validation is focused on slot policy and the affected Inquisitor behavior, not a claim that every legacy class balance fixture has been updated to the smaller budgets.
