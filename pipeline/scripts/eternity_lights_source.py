"""Recover the authored EV 01_Surface light.lit placements verbatim from the April archives."""
import sys,json,hashlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import map_scan as scan
from source_pipeline import ROOT
from maps_common import records,sections
idx=scan.index();A='/resources/world/areas/4611686096259570491/';BS=chr(92)
area=scan.read(idx,A+'area.dat').decode('utf-8-sig')
room_bytes=scan.read(idx,A+'01_surface.dat');room=room_bytes.decode('utf-8-sig')
assets={l.strip().split('=',1)[0]:l.strip().split('=',1)[1].replace(BS,'/') for l in sections(area)['ASSETS'] if '=' in l}
keep=('Position','Rotation','Scale','LightType','Intensity','Range','InnerAngle','OuterAngle','Color','Falloff','ShadowAngle','Hidden','DoCharacters','DoHeightmaps','CubeShadows')
lights=[dict(id=k,**{x:r['fields'][x] for x in keep if x in r['fields']}) for k,r in records(room).items() if assets.get(r['asset'],'').endswith('.lit')]
out=ROOT/'godot/Assets/Maps/eternity_vault/surface-lights.json'
out.write_text(json.dumps(dict(source=A+'01_surface.dat',sha256=hashlib.sha256(room_bytes).hexdigest(),lights=lights),indent=1))
print('lights',len(lights))
