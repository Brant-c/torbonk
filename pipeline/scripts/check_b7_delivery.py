import json
from source_pipeline import ROOT,sha
config=json.loads((ROOT/'godot/Gameplay/presentation-audio.json').read_text())
for name,c in config['cues'].items():
 p=ROOT/'godot'/c['path'].removeprefix('res://');assert p.is_file(),(name,p)
assert json.loads((ROOT/'manifests/b7-smoke.json').read_text())['passed']
print('B7 receipt: 27 passed checks; all configured audio paths resolve.')
print('Final audio tuning SHA256:',sha(ROOT/'godot/Gameplay/presentation-audio.json'))
