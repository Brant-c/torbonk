# B8 — Sorcerer shields, healing and Death Field

Launch the existing frontend with:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Survivors.ps1'
```

Select Sith Sorcerer and Korriban as before. `Open-Gameplay.ps1` opens gameplay
directly. WASD moves, abilities activate automatically, and R restarts the run.
The three B8 abilities unlock through level-up choices and populate the existing
single action bar. No source client or emulator needs to run.

| Ability | Initial Survivors behavior |
|---|---|
| Death Field | Nearby visible cluster; 35 Internal damage to each valid target within 0.34 source units; +3 HP per unique enemy whose HP actually decreased, capped at +15 per activation; 15 s cooldown; uses and triggers the existing GCD. |
| Static Barrier | While unshielded with a visible threat within 0.8 source units, immediately grants 30 absorption for up to 12 s; 18 s cooldown; off the offensive GCD. |
| Dark Heal | Immediately restores 25 missing HP below 50% of the player's configured maximum; 25 s cooldown; off the offensive GCD. |

Healing is applied and clamped through Health, never revives, and produces one
genuine healing event/flytext. Every damaged enemy retains its own damage event
and flytext. Fully shielded, dead, invalid or missed enemies do not count toward
Death Field sustain. The distinct-hit set resets each activation.

Shield strength and lifetime are separate from HP. Incoming damage resolves
absorption first, publishes an `Absorbed` event, and applies only the remainder
to HP. Absorption does not call the HP-damaged event or claim HP loss. The
temporary shield meter sits beneath the existing player health/readiness frame;
tooltips expose capacity, remaining time and trigger conditions. Individual
cooldown sweeps remain in the existing bar; the two off-GCD abilities are not
visually blocked by its shared sweep. Boss and hostile target UI remain intact.

## Progression and configuration

Ability resources are in `godot/Gameplay/Abilities/{deathfield,barrier,darkheal}.tres`.
They expose cooldown, healing, cap, threshold, shield capacity/duration, threat
range, radius, windup and gesture timing. No existing attack damage balance was
changed. The existing movement, environment, roster, waves, boss and frontend
composition are retained.

Three unlocks and three combined improvement cards are added. Improvements
become eligible only after unlock and have five ranks. This keeps the new pool
small while providing all requested ability-specific improvements:

* **Devouring Field / Artifact:** damage, radius and healing per target; the
  activation healing cap remains 15.
* **Enduring Barrier / Prototype:** shield strength, cooldown recovery and duration.
* **Dark Restoration / Premium:** healing, cooldown recovery and earlier activation,
  capped at 80% HP.

Per-rank components and rarity are data in `godot/Gameplay/Upgrades/*_power.tres`.
Existing Premium green, Prototype blue, Artifact purple and their selection
logic/multipliers are retained. `scripts/write_b8_tuning.py` records the initial
resource recipe. Restart resets ranks, unlocks, effective values, shields,
gestures, effects and cooldowns.

## Focused validation and stopping point

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Run-B8-Smoke.ps1'
```

The runner compiles C#, imports changed local resources, and runs the brief B8
Godot fixture. Receipt: `manifests/b8-smoke.json`; runtime log:
`logs/b8-runtime.log`. It covers real level choices, multi-target actual-hit
healing, absorption and overflow, low-health recovery, movement, independent
activations, rarity-scaled improvements, death and restart cleanup.

The existing NuGet vulnerability-service availability warning and
StormAreaIndicator nullable warning are unrelated to B8. No whole-archive
hashing, historical regression suite, bulk reconversion, extended benchmark or
rendered capture series is part of this milestone.

See [B8-PROVENANCE.md](B8-PROVENANCE.md) for resource paths and documented
approximations, especially the barrier break sound reuse and bounded FXSpec
adapter. Stop here for manual visual, audio and balance review. No additional
class, boss or map is included.
