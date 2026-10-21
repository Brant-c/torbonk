"""Extract only the original April single-saber jump clips for the current bmnnew rig."""
import json
import character_source
from source_pipeline import ROOT

CLIPS = {'Takeoff': 'mv_saber_jump_start', 'Airborne': 'mv_saber_jump_mid_idle',
         'Falling': 'mv_saber_fall', 'LandIdle': 'mv_saber_jump_end', 'LandRun': 'mv_saber_run_jump_end'}

character_source.MANIFEST = ROOT/'manifests/b9-jump-source.json'
if not character_source.MANIFEST.exists():
    character_source.MANIFEST.write_text(json.dumps(dict(profile='April2012 (operator labelled)',
        maximumFiles=5, maximumDecodedBytes=1024*1024, resources=[], dependencies=[], unresolved=[],
        selection='B9 current bmnnew single-saber jump locomotion, physics driven Godot adaptation')))
source = character_source.SelectedSource()
for name, stem in CLIPS.items():
    source.get(f'/resources/anim/humanoid/bmnnew/{stem}.jba', name, 'Original single-saber locomotion clip; April unique hash entry')
(ROOT/'manifests/b9-jump-conversion.json').write_text(json.dumps(dict(clips=CLIPS), indent=2))
(ROOT/'godot/Assets/Jump').mkdir(parents=True, exist_ok=True)
print('Extracted five original April single-saber jump clips; archives read-only.')
