# SWTOR Survivors — floating combat text

Flytext is integrated into `Gameplay/GameWorld.tscn`. Launch the existing game:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Gameplay.ps1'
```

WASD moves, R restarts, Esc closes. Lightning starts unlocked; Shock and Force
Storm still unlock through the existing upgrade choices. The original HUD and
single twelve-slot action bar remain in use.

Every positive applied damage/healing event immediately creates its own entry.
There is no simultaneous-entry limit, per-target cooldown, aggregation, eviction,
or clutter suppression. Storm produces a separate number for each damaged enemy
on every tick. Overkill displays only health actually removed. Invalid/zero
damage, healing at full health, and attempts to heal/damage a dead recipient do
not produce entries. Existing health upgrades now display the health they restore.
Restore/reset initialization does not fabricate healing.
Displayed amounts round to the nearest whole number (halves round up), with a
minimum of 1 for positive events. Applied damage/healing values remain unchanged.

Numbers project the recipient's world-space aim position through the existing
camera, with independent small screen-space offsets, upward movement and fading.
The anchor is a position snapshot at impact; text can finish after an actor dies
or despawns. Ordinary numbers use 23 design pixels, criticals 29 with a brief
pop; both last 1.5 seconds. Healing is green, outgoing damage yellow, incoming
damage red. Outline/shadow rendering keeps the text legible over Korriban.
These visual details still require manual review; provenance distinguishes
recovered values from reconstruction.

## Integration contract

`Health.Resolved` emits a `HealthChange` containing event kind, applied amount,
damage type and optional critical/text metadata. Existing `Damaged(float)` and
`Died` subscribers remain supported. `Combatant` adds recipient ID/key/team and
world position, then publishes a value snapshot to the scene's `CombatEvents`
stream. `FlytextService` subscribes to that stream; it never calls an ability,
calculates damage or looks at a visual effect.

`ApplyDamage(amount, damageType, isCritical: true)` and
`ApplyHealing(amount, isCritical: true)` accept authoritative critical metadata;
they do not roll criticals or change their magnitude. Current automatic abilities
do not produce criticals. Shields/resistance are not implemented by the combat
model. `CombatEventKind.Absorbed` / `Resisted` support future resolved outcomes
with recovered “Absorbed” / “Resist” labels. Future status events can supply
`HealthChange.Text`. Future DOTs can use the ordinary Health damage path.
No fabricated crit, shield or resistance events run during normal gameplay.

The service draws all entries on one input-transparent canvas using one shared
font. Entry objects are pooled without a cap. Expiration uses stable linear
compaction; presentation does not use gameplay RNG. No enemy node is retained
by an entry. Scene exit unsubscribes handlers and releases entries. R clears
transient entries; pausing for upgrade selection pauses their animation too.

## Verification and limits

`Run-Flytext-Smoke.ps1` compiles C# and runs the opt-in `--flytext-smoke`
fixture in the real gameplay scene. Receipt: `manifests/flytext-smoke.json`;
runtime log: `logs/flytext-smoke-runtime.log`.

The brief rendered smoke verified two actual Storm ticks on eight independent
Combatant/Health recipients: 8 entries, then 16 overlapping entries, all reaching
the existing camera canvas. It also checked fractional/critical metadata,
capped healing, zero/no-op events, overkill, death/despawn persistence, normal
expiration and restart clearing. No historical suites, archive hashing,
screenshots or benchmarks were run for this feature.

The first run completed all feature checks and exited normally, but its wrapper
flagged the existing unwritable `user://` shader-cache warning. The smoke launcher
now places only its process's cache under `pipeline/work/flytext-userdata` and
restores APPDATA afterward. The same brief fixture was repeated solely to
verify that repair: all feature checks and the wrapper passed, with no runtime
errors. Normal gameplay uses the existing launcher/settings.
NuGet vulnerability-feed access may emit NU1900 offline; compilation succeeded.

Known limitations: the exact native flytext font-face binding, renderer outline
filter and easing remain unresolved. The selected face is recovered original
art, with its flytext role reconstructed. Off-screen/behind-camera anchors have
normal camera visibility; their events are still created. Dense overlapping
events remain intentionally unrestricted. No extensive performance claim is
made. See `FLYTEXT-PROVENANCE.md` for the evidence and reconstructed choices.

The separate B5.1 animation smoke log observed during this task reported a Shock
Root-bone assertion. Flytext does not modify skeletons, animation tracks or the
cast controller. No live crash was found, and the user confirmed normal operation.

Stop here for manual visual review.
