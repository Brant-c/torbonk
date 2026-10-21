# B8 results — 2026-10-21

Implemented Death Field, Static Barrier and Dark Heal using the existing
automatic ability lifecycle, cooldown/GCD scheduler, Health, combat events,
flytext, progression, rarity, animation layers, single action bar and audio mixer.
Prior uncommitted B7/frontend work was retained.

The final C# build succeeded with zero errors. Two existing warnings remain:
NuGet vulnerability-service reachability (`NU1900`) and the nullable conversion
in `StormAreaIndicator.cs` (`CS8600`).

The focused rendered Godot session passed all 42 checks after the visual correction. Evidence:
`manifests/b8-smoke.json`, `logs/b8-runtime.log`. It confirmed:

* Six distinct targets lost 35 HP each; Death Field healed 15 HP total. A fully
  shielded enemy, dead enemy and out-of-area enemy produced no sustain.
* A second activation hitting two targets healed 6 HP. Moving every target out
  during windup produced no healing. Missing presentation did not block damage.
* Every actual damage, healing and absorption event reached flytext separately.
* A 30-point barrier absorbed 12 without HP loss, then absorbed 18 of the next
  25 damage and passed exactly 7 to HP. No stacking; expiry and no-threat gating
  also passed.
* Dark Heal respected full HP, the strict below-50% threshold and configured
  150 maximum HP, restored 25, and respected its own cooldown.
* Self abilities activated during offensive animation/GCD; moving while healing
  changed the player's world position.
* Real level choices unlocked all three abilities in the existing action bar.
  Each combined improvement applied its rarity-scaled configuration.
* Death cleared shield state, gestures, all three presentations and audio;
  restart restored HP, locks, cooldowns, ranks and baseline ability values.

Manual review follow-up: opaque-black glow textures now use additive blending.
Death Field's original lightning/ring artwork uses the existing terrain-only
Force Storm projection instead of the raised textured shell, and Static Barrier
follows the chest for activation, persistence, hit and break feedback. The brief
smoke added projection, chest-position and blending checks. Its single review
frame, `generated/b8/effects-review.png`, was inspected: Death Field follows the
ground without a black rectangle and the transparent barrier surrounds the
character at chest height. Source art and combat values are unchanged.

An initial brief launch stopped at the socket-presence assertion: source
FXSpecs name `bip01`, while the exported original skeleton names it `Bip01`.
The explicit case alias was corrected before the final passing session. Local
animation conversion also required the established workspace temporary folder
and returning the imported rig to Object mode before export. No source JBA was
edited, and no unrelated animation/mesh was reconverted.

No archive rehash, historical suite, extended benchmark or capture series ran.
The normal frontend composition was preserved and compiled; a separate full
character/map-selection walkthrough was outside the requested brief B8 smoke.

Stop after B8 for manual visual/audio/balance review. Original pixels, selected
geometry, clips and PCM are recovered; native particle/material/Wwise behavior
is adapted. Barrier break audio is a documented quiet reuse of an original
chargeup variation, not a recovered independent break event. See
[B8-PROVENANCE.md](B8-PROVENANCE.md) and [B8-README.md](B8-README.md).
