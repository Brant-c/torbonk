import sys,json,struct,xml.etree.ElementTree as ET,hashlib
from pathlib import Path
sys.path.insert(0,'pipeline/scripts');import map_scan as scan
from source_pipeline import SOURCE,ROOT
idx=scan.index();report=[]
def get(p):
 b=scan.read(idx,p);assert b,p
 f=SOURCE/p.lstrip('/');f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
 report.append(dict(path=p,sha256=hashlib.sha256(b).hexdigest()));return b
s=json.loads((ROOT/'work/maps/eternity_vault/selection.json').read_text())
p='/resources/art/static/area/bel_belsavis/sky/bel_raid_skyscene.gr2';get(p)
mp='/resources/art/shaders/materials/bel_raid_skyscene.mat';xml=ET.fromstring(get(mp));tex={}
for inp in xml.findall('.//input'):
 if inp.findtext('type')=='texture':
  v='/resources/'+inp.findtext('value').replace('\\','/').lower()+'.dds';get(v);tex[inp.findtext('semantic')]=v
s['materials'][mp]=dict(path=mp,derived='Skydome',alpha='None',twoSided='True',textures=tex,values={})
s['models']=[m for m in s['models'] if m['path']!=p]+[dict(path=p,materials=[mp])]
# Sky is camera-centred at runtime; the source sky-room placement is retained here for evidence.
s['skySource']=dict(room='skyscene_bel_raid',model=p,position=[0,-110.59980011,-100])
(ROOT/'work/maps/eternity_vault/selection.json').write_text(json.dumps(s))
from PIL import Image
from io import BytesIO
for name in ['eternity_vault_overlay','raid_eternity_overlay']:
 p='/resources/gfx/loadingscreen/'+name+'.dds';b=scan.read(idx,p)
 if b:
  get(p);Image.open(BytesIO(b)).save(ROOT/'godot/Assets/UI/loading/eternity_vault_overlay.png');print('LOADING',p);break
for p,out in [('/resources/art/planets/texture/planets_belsavis_d.dds','planets_belsavis_d.png')]:
 Image.open(BytesIO(get(p))).save(ROOT/'godot/Frontend/Assets/Recovered'/out)
(ROOT/'manifests/map-eternity_vault-refinement-source.json').write_text(json.dumps(report,indent=2))
print('STAGED SKY, ART')
