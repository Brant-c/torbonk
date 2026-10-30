# Juggernaut talent and icon overhaul

Juggernaut now follows the Warrior 3×3 layout: nine one-point talents per tree and twelve shared points. Middle-row talents require two earlier points; bottom-row talents require five. Saved talent IDs remain intact. Former three-rank area talents now grant their complete target coverage for one pregame point; matching in-run target upgrades retain their three-rank progression and cannot stack beyond their caps. Saved allocations are revalidated, making invalid or surplus ranks available to spend again.

| Tree | General top row | Ability-focused middle row | Stronger bottom row |
|---|---|---|---|
| Immortal | Soresu Mastery, Dark Blood, Revenge | Blade Barricade, Unleashed, Crushing Grip | Sonic Barrier, Invincible, Undying |
| Vengeance | Single Saber Mastery, Pooled Hatred, Unstoppable | Draining Scream, Eviscerate, Ravaging Sweep | Savagery, Rampage, Shatter |
| Rage | Force Alacrity, Force Mastery, Undying Rage | Force Resonance, Decimate, Dominate | Shockwave, Crushing Wave, Rupturing Smash |

Top rows provide cross-tree defense, healing, saber bonuses and Force bonuses. Dark Blood supports learned damaging abilities rather than only Sundering Assault. Pooled Hatred empowers the next learned saber attack. Unstoppable provides brief, bounded protection from learned saber activations rather than unavailable leaps. Force Alacrity grants Force cooldown recovery and Force Mastery grants Force damage. Force Scream now primes Dominate's next Smash bonus, avoiding disabled Charge/Obliterate dependencies.

All 27 talent drops derive from the tree data and use the corrected icons. Required abilities must be learned. Bottom-row drops start at run level eight. Pregame talents suppress equivalent drops, including fractional mitigation and full area ranks. The legacy Smash slow card is named Crippling Smash and retains a distinct identity from the Force Crush charge talent Shockwave. Existing Sundering Assault cleave and Choke/Crush Convergence upgrades remain available.

Repeated placeholder artwork is replaced by recovered Warrior talent textures. `juggernaut-talent-icons.json` records each source record, original icon name, source metadata, UI path and SHA-256. Original art is copied without alteration; related recovered icons are used for adapted Survivors talents rather than claiming these mechanics are exact 2012 talents. The two legacy Rage/Smash-slow cards also use recovered artwork. Godot imports are present. Existing combat animations and effects are preserved.

Validation completed: build, full Juggernaut combat and progression regression, nine-point trees and 9/3 budget, five-point gates, icon loading, unique drop identities, early offers/rerolls/banishes, area caps, periodic cleanup, generic bonuses, burst consumption, duplicate suppression, replacement slots, rendered layout and Marauder regression. Existing unrelated map import warnings remain. Tests preserve the Juggernaut talent save.

Rendered preview: `juggernaut-nine-talents.png`.
