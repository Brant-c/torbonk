import json,re
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE
character_source.MANIFEST=ROOT/'manifests/agent-source.json';src=character_source.SelectedSource()
mapping={'shiv':0,'backstab':1,'hiddenstrike':2,'laceration':3,'agentcull':4,'koltoprobe':5,'surgicalprobe':6,'followthrough':7,'interrogationprobe':8,'plasmaprobe':9,'empdischarge':10,'weakeningblast':11}
rows=json.loads((ROOT/'manifests/agent-ability-definitions.json').read_text())
for id,i in mapping.items():
 d=json.loads(rows[i]['result']['content'][0]['text']);icon=next(x['value'] for x in d['children'] if x['key']=='ablIconSpec').lower()
 path=f'/resources/gfx/icons/{icon}.dds'
 if not src.has(path):continue
 src.get(path,d['identity']['name'],'April ablIconSpec');Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(ROOT/f'godot/Assets/UI/icons/{icon}.png')
 for p in [ROOT/f'godot/Gameplay/Abilities/{id}.tres',ROOT/f'godot/Gameplay/Upgrades/unlock_{id}.tres']:
  if p.exists():p.write_text(re.sub(r'^Icon = .*$',f'Icon = "{icon}"',p.read_text(),flags=re.M))
