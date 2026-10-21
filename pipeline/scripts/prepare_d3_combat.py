import sys
sys.path.insert(0,'D:/SWTORClassic/swtoremu/Diagnostics/python-deps')
import json,xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE
character_source.MANIFEST=ROOT/'manifests/d3-source.json'
s=character_source.SelectedSource()
names=Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').read_text().splitlines()
clips=dict(Smash='cb_saber_sp2_attack_right_1',Rupture='cb_2saber_sp2_attack_right_13',Lacerate='cb_pole_blade_dance_end',Dart='cb_rifle_fire_1',Sniper='cb_rifle_fire_big_1',Missile='cb_2pistol_bracer_fire_big_1',Flame='cb_bounty_flame_sweep',DualIdle='cb_2saber_idle_right_1',DualRun='mv_2saber_run_forward_01',PoleIdle='cb_pole_idle_right_1',PoleRun='mv_pole_run_forward_01',RifleIdle='cb_rifle_idle_1',RifleRun='mv_rifle_run_forward',PistolsIdle='cb_2pistol_idle_1',PistolsRun='mv_2pistol_run_forward',PistolIdle='cb_pistol_idle_1',PistolRun='mv_pistol_run_forward')
out=ROOT/'godot/Assets/D3';out.mkdir(exist_ok=True)
for key,stem in clips.items():
    p=f'/resources/anim/humanoid/bmnnew/{stem}.jba'
    print(key,s.has(p))
    if s.has(p):s.get(p,key,'original BMN compatible gesture/locomotion')
for icon in ['smash','rupture','spinningstrike','toxicdart','snipe','unexplodedmortarshell','flamingsweep']:
    p=f'/resources/gfx/icons/{icon}.dds';s.get(p,icon,'verified ablIconSpec');Image.open(SOURCE/p.lstrip('/')).convert('RGBA').save(ROOT/f'godot/Assets/UI/icons/{icon}.png')
(ROOT/'manifests/d3-conversion.json').write_text(json.dumps(dict(clips=clips),indent=2))
# Discover only April-verified low-level weapon definitions. Names are candidates until hash lookup.
for slot in ['weapon']:
    p=f'/resources/art/dynamic/{slot}/index.xml'
    print('REGISTRY',p,s.has(p))
    if s.has(p):
        r=ET.fromstring(s.get(p))
        for a in r:
            if any(t in (a.findtext('ArtName') or '').lower() for t in ['rifle_low','sniper_low','blaster_low','saber_pole','saberstaff','doublebladed']):print(ET.tostring(a,encoding='unicode')[:900])
