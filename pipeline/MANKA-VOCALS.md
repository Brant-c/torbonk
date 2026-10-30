# EV opening ice cat vocal trial

`Planets/EternityVault/IceCat.tscn` and the Soa run’s `Planets/Soa/Manka.tscn` opt into vocal reactions. Four original April `voc_manka.bnk` variations are assigned to hurt and three to death. These assignments are authored for this trial: native event names/meanings have not been verified and no listening approval is claimed. Exact source offsets/hashes and selected output hashes are in `manifests/manka-vocals.json` and `manka-vocals-source.json`. `scripts/prepare_manka_vocals.py` reproduces the bounded selection from read-only April archives.

The first nonlethal hit requests a grunt. Subsequent eligible hits have a 30% chance, with a 1.35-second per-cat cooldown including failed rolls. A 35 ms delay lets same-frame lethal followups cancel hurt audio. This throttles periodic damage as well as direct attacks; this initial version does not classify periodic damage separately.

The scene audio manager owns three vocal players, at most two playing pain reactions. Death replaces that cat's pain audio and may displace another pain reaction. Hurt starts are separated by 240 ms globally; death starts by 140 ms. Simultaneous mass kills therefore produce a selected death call. No queue replays old screams later. Sounds vary slightly in pitch, avoid immediate per-cat repeats and use player-distance gain falloff over 1.5 world units. Default hurt/death gains are -18/-14 dB before the existing Combat bus and user volume. Edit the exported gains on the cat's Vocals node for listening adjustments.

Reset/player death clears these voices through AudioPresentation.Clear. Corpse removal leaves finite death tails playing. No health, damage, reward or animation rules were changed. Other enemies, including the matriarch, remain opt-out.

Listening adjustment, 2026-10-28: the shared NPC vocal mixer applies a further -6 dB to hurt and death calls. This also covers scene-specific gain overrides without changing player, music or other combat cue volumes.

Build normally, import the Godot project, then run `res://Gameplay/NpcVocalSmoke.tscn` with `--headless --max-fps 60`. Use real elapsed time: audio tails do not follow accelerated fixed-frame simulation. The receipt is `manifests/manka-vocals-smoke.json`. This tests actual scene damage/death hooks, rapid-hit suppression, same-frame death cancellation, the crowd cap, range rejection, finite tails and reset cleanup. Final clip suitability and mix still require listening in combat.
