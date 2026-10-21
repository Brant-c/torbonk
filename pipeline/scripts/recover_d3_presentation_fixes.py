"""Bounded source recovery for manually reported D3 presentation faults."""
import json,sys,struct
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE
character_source.MANIFEST=ROOT/'manifests/d3-presentation-source.json'
s=character_source.SelectedSource()
clips={'Smash':'cb_saber_ground_smash_01','Sniper':'cb_rifle_sniper_fire_big_1','SniperIdle':'cb_rifle_sniper_idle_1','Dart':'cb_rifle_bracer_fire_1'}
for name,stem in clips.items():
    p=f'/resources/anim/humanoid/bmnnew/{stem}.jba'
    print(name,s.has(p))
    if s.has(p):s.get(p,'D3 presentation correction',name)
p='/resources/anim/humanoid/am_humanoid_weapon_specific.xml'
if s.has(p):
    data=s.get(p,'D3 presentation correction','original weapon animation semantic binding')
    (ROOT/'work/d3/weapon-specific.xml').write_bytes(data)
for name in ['massivesmash','forceslam','smash','sweep']:
    p=f'/resources/gfx/icons/{name}.dds'
    if s.has(p):
        s.get(p,'D3 Warrior icon review','original April icon');Image.open(SOURCE/p.lstrip('/')).convert('RGBA').save(ROOT/f'work/d3/{name}.png')
(ROOT/'manifests/d3-presentation-conversion.json').write_text(json.dumps(dict(clips=clips),indent=2))
recipe=json.loads((ROOT/'manifests/d3-conversion.json').read_text());recipe['clips'].update(clips)
(ROOT/'manifests/d3-conversion.json').write_text(json.dumps(recipe,indent=2))
