# Carnage talent and run upgrade rework

Carnage follows a Massacre → Ataru cleave → Gore → Massacre/Ravage burst loop. Its eight talent IDs, positions, costs and point gates remain compatible with existing saved builds. Annihilation and Rage retain their trees; other classes are unchanged.

| Talent | Effect |
|---|---|
| Ataru Rhythm | Enables Ataru stance and grants 15% saber tick/cooldown haste. Does not accelerate bleeds. |
| Sweeping Blades | Full-damage Massacre cleave reaches five enemies; Ravage reaches six per strike. |
| Execute | Gore grants Massacre/Ravage another 20% direct damage. Ataru primes a guaranteed Force Scream critical. |
| Slaughter | Each Massacre activation recovers 0.75s of Gore cooldown, independent of enemies hit. |
| Ataru Mastery | Follow-ups gain 25% damage, reach at least four nearby targets, and have a 1.2s maximum internal cooldown. Ataru Frenzy's stronger target/frequency values still apply. |
| Gore Window | Adds two seconds to Gore. Combat scheduling prefers ready Massacre/Ravage strikes during Gore. |
| Relentless Assault | Saber hits recover 0.5s of Berserk cooldown, once per 0.5s. |
| Lasting Frenzy | Adds two seconds to Berserk. Gore-window saber hits recover 2s of Frenzy cooldown, once per second. Replaces the former DoT-spread trigger. |

The eight matching run drops are generated from the talent definitions, so names, icons and descriptions stay synchronized. Required abilities must be learned before dependent drops become eligible. A talent bought before the run suppresses its duplicate drop. All remain available to hybrid builds.

Carnage's early unlock order starts with Massacre, Gore and Ravage, then Battering Assault and offensive utility. A no-talent run that acquires Massacre also adopts this preference. Later Carnage offers reserve a usable interaction alongside damage mastery when available. Annihilation/Rage-focused builds keep their existing opening preferences and are not forced to take Carnage upgrades.

Blademaster's Reach replaces the generic six-rank cleave drop: three ranks each add one target and 8% radius to Massacre/Ravage. Their baseline full secondary damage remains intact. This fixes the previous upgrade being overwritten by their cleave settings. Battering Assault now empowers the entire next Massacre cleave or Ravage sequence rather than losing its benefit after the first victim.

Fixed fractional hook handling: run-earned Ataru Rhythm applies 15% haste rather than interpreting one learned rank as 100%. Lasting Frenzy's run drop matches the tree's two-second extension. Fractional pregame hooks now correctly suppress duplicate drops.

Presentation uses the same recovered Warrior animations, contacts and Ataru effects. No new animations, assets or relic loot tables were added. Ability tooltips and legacy drop descriptions describe the current mechanics.

Validation: build; dedicated Carnage smoke covering all eight drops, matching descriptions/icons, ability requirements, 15% haste, unchanged bleed intervals, full-damage target counts, capped reach, Ataru frequency/coverage/proc limits, once-per-activation Gore recovery, Gore/Berserk expiry, thirty seeded offer sets, saved IDs and duplicate suppression. Existing Marauder combat/presentation and all relic-rank/pairing checks pass. Runtime retains existing unrelated map-resource warnings.
