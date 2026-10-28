"""Recover the two explicit April polearm run cycles without modifying archives."""
import json
from pathlib import Path
import character_source

ROOT = Path(__file__).resolve().parents[1]
character_source.MANIFEST = ROOT / 'manifests/assassin-locomotion-source.json'
source = character_source.SelectedSource()
source.manifest['maximumFiles'] = 2
source.manifest['maximumDecodedBytes'] = 8 * 1024 * 1024
clips = {'PoleForward': 'mv_pole_run_forward_01', 'PoleBack': 'mv_pole_run_back_01'}
for name, stem in clips.items():
    path = f'/resources/anim/humanoid/bmnnew/{stem}.jba'
    assert source.has(path), path
    source.get(path, name, 'Polearm directional locomotion; original bytes hashed')
(ROOT / 'manifests/assassin-locomotion-conversion.json').write_text(json.dumps({'clips': clips}, indent=2))
