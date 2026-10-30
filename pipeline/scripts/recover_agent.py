import json,sys
from pathlib import Path
sys.path.insert(0,'D:/SWTORClassic/swtoremu/Diagnostics/python-deps')
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE
character_source.MANIFEST=ROOT/'manifests/agent-source.json'
s=character_source.SelectedSource()
clips={'Shiv':'cb_spy_shiv_1','Backstab':'cb_agent_backstab','Laceration':'cb_spy_shiv_1'}
for key,stem in clips.items():s.get(f'/resources/anim/humanoid/bmnnew/{stem}.jba',key,'original Agent knife gesture')
for icon in ['criticalblade','shiv','laceration','koltoinjection','surgicalprobe','orbitalstrike','suppressivefire']:
 p=f'/resources/gfx/icons/{icon}.dds'
 if s.has(p):
  s.get(p,icon,'selected Agent icon');Image.open(SOURCE/p.lstrip('/')).convert('RGBA').save(ROOT/f'godot/Assets/UI/icons/{icon}.png')
(ROOT/'manifests/agent-conversion.json').write_text(json.dumps(dict(clips=clips),indent=2))
(ROOT/'godot/Assets/Agent').mkdir(exist_ok=True)
code=(ROOT/'scripts/convert_ability_animations.py').read_text().replace("profile='d2' if", "profile='agent' if '--agent' in sys.argv else 'd2' if").replace("ROOT/('godot/Assets/D2/casts.glb'", "ROOT/('godot/Assets/Agent/casts.glb' if profile=='agent' else 'godot/Assets/D2/casts.glb'")
(ROOT/'scripts/convert_agent_animations.py').write_text(code)
