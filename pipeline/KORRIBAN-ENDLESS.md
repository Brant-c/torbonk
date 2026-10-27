# Korriban continuous survival

Korriban starts at the Imperial camp on the entrance approach in front of the original scaffold (XZ 39.51257, 22.627346). Stages change enemy compositions at levels 1, 6, 11, 16, 21, 26, 31, 36, 41 and 46. Ten sequential encounters unlock every five levels through 50. Encounters and stage changes never disable ordinary reinforcements or require travel. Completing the last encounter leaves survival running.

Enemy effective level is the greater of player level and 1 + elapsed active seconds / 24. Newly spawned health and damage keep growing after the level cap; speed and attack frequency have bounded bonuses. Existing ground attacks shorten their cooldown and windup, retaining a minimum half-second warning. Population starts at 12, grows by one per level and is capped at 36 active enemies including bosses and summons. Ordinary reinforcements reserve one slot for an encounter. Every second defeated enemy drops XP at the original pickup value; the counter resets with the run. Paused upgrade screens do not advance pressure. Long-run balance still needs playtesting.

Chests offer relic choices, including stacking +50% XP attraction/pickup range and +15% cooldown recovery lasting for the run without consuming equipment slots. XP rewards disappear at the cap. Reaching 50 clears loose XP, ignores further XP awards and prevents new enemy XP pickups. Restart resets bonuses and pressure.

Chest icons sit above the measured model bounds. The frontier extends the outer source border samples by eight source units, retaining terrain textures and interior holes. Movement bounds extend slightly onto this authored apron. This is surrounding terrain coverage, not extraction of additional original MMO sectors. In-game visual review is still needed.

Spawn selection checks full body clearance and at least seven clear 0.8-unit walking exits before accepting a point, including on Korriban. Korriban candidates are constrained to the entrance approach (X 38–42, Z 21–26) so the fallback cannot move the player behind the base into the mountain. The spawn regression verifies this against the live terrain and prop collision.

Validation: build succeeded; KorribanEndlessSmoke passed every level-up choice from 2 to 50, cap cleanup/reward filtering, camp spawn, frontier loading, pressure beyond 50 and restart reset. Population checks also passed: the cap holds at levels 26, 50 and 500; both ordinary and limit-bypassing boss spawn requests are rejected at the hard cap; six actual enemy deaths produce three XP pickups. Runtime log contains no errors. Existing asset UID warnings remain.

Run the focused check after building:

```powershell
& pipeline/tools/godot/Godot_console.exe --headless --path pipeline/godot res://Gameplay/GameWorld.tscn --log-file D:/SWTORClassic/torbonk/pipeline/logs/korriban-endless-runtime.log --quit-after 600 -- --korriban-endless-smoke --advanced-class=juggernaut --map=korriban
```



