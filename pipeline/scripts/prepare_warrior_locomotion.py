"""Recover six explicit April locomotion resources; archives remain read-only."""
import json
from pathlib import Path
import character_source

ROOT = Path(__file__).resolve().parents[1]
character_source.MANIFEST = ROOT / 'manifests/warrior-locomotion-source.json'
source = character_source.SelectedSource()
source.manifest['maximumFiles'] = 6
source.manifest['maximumDecodedBytes'] = 16 * 1024 * 1024
source.manifest['selection'] = 'single/dual saber forward/back and shared lateral running; original bmnnew rig'
clips = {
    'SaberForward': 'mv_saber_run_forward_01',
    'SaberBack': 'mv_saber_run_back_01',
    'DualForward': 'mv_2saber_run_forward_01',
    'DualBack': 'mv_2saber_run_back_01',
    'Left': 'mv_run_left',
    'Right': 'mv_run_right',
}
for name, stem in clips.items():
    path = f'/resources/anim/humanoid/bmnnew/{stem}.jba'
    assert source.has(path), path
    source.get(path, name, 'Explicit directional locomotion candidate; extracted bytes hashed')
(ROOT / 'manifests/warrior-locomotion-conversion.json').write_text(json.dumps({'clips': clips}, indent=2))
print('Recovered six directional locomotion clips.')
