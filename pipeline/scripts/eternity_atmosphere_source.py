"""Recover the EV surface environment and cloud projector inputs, preserving source hashes."""
import sys,json,hashlib,re
from pathlib import Path
from io import BytesIO
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parent))
import map_scan as scan
from source_pipeline import ROOT
from lightning_source import decode_xml
from prepare_abilities import unpack
from maps_common import records,sections
idx=scan.index(); out=ROOT/'godot/Assets/Maps/eternity_vault'; receipt=[]
def get(p):
 b=scan.read(idx,p);assert b is not None,p
 receipt.append(dict(path=p,sha256=hashlib.sha256(b).hexdigest()));return b
area=get('/resources/world/areas/4611686096259570491/area.dat').decode('utf-8-sig')
room=get('/resources/world/areas/4611686096259570491/01_surface.dat').decode('utf-8-sig')
scheme=re.search(r'EnviroScheme=(.*)',room)[1].strip()
assets={l.strip().split('=',1)[0]:'/resources/'+l.strip().split('=',1)[1].replace('\\','/').lower().lstrip('/') for l in sections(area)['ASSETS'] if '=' in l}
conversion=json.loads((ROOT/'manifests/map-eternity_vault-model-conversion.json').read_text())
outputs={m['path']:m['output'] for m in conversion['models']}
selected=json.loads((ROOT/'work/maps/eternity_vault/selection.json').read_text())
ids={p['id'] for p in selected['placements']}
scenery=[]
for k,r in records(room).items():
 path=assets.get(r['asset'],''); f=r['fields']
 if k in ids or path not in outputs or f.get('Hidden')=='true' or '/art/static/area/' not in path:continue
 if any(x in path for x in ('dbo_','collision','blocker','trigger','fadeportal','occluder')):continue
 assert f.get('ParentInstance','0')=='0',k
 def vec(key):return [float(x) for x in f[key].strip('()').split(',')]
 scenery.append(dict(id=k,model=outputs[path],position=vec('Position'),rotation=vec('Rotation'),scale=vec('Scale')))
(out/'surface-distant-scenery.json').write_text(json.dumps(scenery,indent=2))
line=next(l for l in area.splitlines() if l.startswith('"'+scheme+'"'))
fx=unpack(decode_xml(get('/resources/art/fx/fxspec/environments/skies/sky_raids_overhead_clouds_01.fxspec')).find('.//marshalData/node'))
p=fx['_fxProjectorList'][0]
for key,name in [('_fxProjectionTexture','raid-clouds.png'),('_fxProjectionTexture_layer1','raid-cloud-mask.png')]:
 path='/resources/'+p[key].replace('\\','/').lower().lstrip('/')
 Image.open(BytesIO(get(path))).save(out/name)
(out/'surface-atmosphere-source.json').write_text(json.dumps(dict(scheme=scheme,schemeRecord=line,projector=p,models=fx['_fxModelList'],emitters=fx['_fxEmitterList'],resources=receipt,limits=['Clouds use converted source geometry and projector texture alpha, tint, tiling, scrolling and angular falloff; native dual-planar projection remains approximate.','Lightning and local particles use bounded source-field adapters; spline/distribution semantics, sound events and empty emission defaults remain unresolved.','Godot exponential fog and ambient light approximate the original fog ramp and illumination lookup.']),indent=2))
print('Recovered',scheme,'cloud layers',len(receipt),'resources')
