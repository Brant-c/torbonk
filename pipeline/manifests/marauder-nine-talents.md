# Nine talents per Marauder tree

Marauder now has 27 one-point talents: nine each in Annihilation, Carnage and Rage. The shared budget remains 12, allowing a full 9-point tree plus three points elsewhere, or a mixed allocation. Existing saved talent IDs, ranks and tier gates remain valid. Other classes retain their layouts and catalogs.

New bottom-right talents, each requiring four points in earlier tiers:

- **Deep Wounds (Annihilation):** Rupture and Deadly Saber gain 20% damage, including their periodic damage through the existing ability scaling system.
- **Overwhelm (Carnage):** activating Ravage during Gore recovers 1.5 seconds of Massacre cooldown, once per activation. Cleave victims and subsequent strikes cannot multiply the refund. Also included in Carnage's matching run-drop pool with ability requirements and pregame duplicate suppression.
- **Unleashed Rage (Rage):** Force Crush and Smash gain 20% damage through the existing scaling system.

The existing frames now contain a complete 3×3 grid. Row spacing increases from 103 to 145 design pixels, filling the previously empty lower section. Names receive 42 pixels of vertical space for two-line labels, with a gap before the following row. All 27 buttons and labels fit inside their frames. The per-tree counter updates automatically to `/ 9`.

Existing recovered Rupture, Ravage and Force Crush icons and effects are reused. No icon imports, new character animations or new combat effects are required.

Validation: build; dedicated Carnage smoke covering nine-point trees, 9/3 and 4/4/4 allocations, rejection of two full trees, all 27 frame bounds, both new damage talents, Overwhelm's bounded recovery, nine matching Carnage drops and existing combat/offer checks. Rendered preview: `marauder-nine-talents.png`.
