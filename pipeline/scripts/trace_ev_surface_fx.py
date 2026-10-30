"""Trace every authored EV surface/sky FX dependency from April archives."""
import sys,json,re,hashlib,struct
from pathlib import Path
from io import BytesIO
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parent))
import map_scan as scan
from source_pipeline import ROOT,SOURCE
from maps_common import records,sections
from lightning_source import decode_xml
from prepare_abilities import unpack
idx=scan.index();nodes={};edges=[];placements=[];graphs={}
def canonical(raw,kind=''):
 p=raw.replace('\\','/').lower().strip().lstrip('/')
 if p.startswith('resources/'):p=p[10:]
 if '/' not in p and p.endswith('.prt'):p='art/fx/particles/'+p
 if kind=='fx' and not p.startswith('art/'):p='art/fx/fxspec/'+p
 if kind=='fx' and not p.endswith('.fxspec'):p+='.fxspec'
 return '/resources/'+p
def visit(p):
 if p in nodes:return
 b=scan.read(idx,p); nodes[p]=dict(found=b is not None)
 if b is None:return
 nodes[p].update(sha256=hashlib.sha256(b).hexdigest(),bytes=len(b))
 f=SOURCE/p.lstrip('/');f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
 refs=[]
 if p.endswith('.fxspec'):
  obj=unpack(decode_xml(b).find('.//marshalData/node'));graphs[p]=obj
  def walk(v):
   if isinstance(v,dict):
    for k,x in v.items():
     if isinstance(x,str) and (k=='_fxResourceName' or 'Texture' in k) and x and re.search(r'\.(prt|dds|gr2|fxspec|ext)$',x,re.I):refs.append((k,canonical(x)))
     elif isinstance(x,(list,dict)):walk(x)
   elif isinstance(v,list):
    for x in v:walk(x)
  walk(obj)
 elif p.endswith('.prt'):
  fields=dict(re.findall(r'^\s*\.(\w+)=(.*)$',b.decode().replace('\r',''),re.M));graphs[p]=fields
  for k in ('EmitSpec','EmitAtDeathSpec','TextureName'):
   if fields.get(k):refs.append((k,canonical(fields[k])))
 elif p.endswith('.gr2'):
  n=struct.unpack_from('<H',b,26)[0];off=struct.unpack_from('<I',b,88)[0]
  for i in range(n):
   o=struct.unpack_from('<I',b,off+i*4)[0];name=b[o:b.index(b'\0',o)].decode();refs.append(('material',canonical('art/shaders/materials/'+name+'.mat')))
 elif p.endswith('.mat'):
  import xml.etree.ElementTree as ET
  for x in ET.fromstring(b).findall('input'):
   if x.findtext('type')=='texture':refs.append((x.findtext('semantic'),canonical(x.findtext('value')+'.dds')))
 for label,target in refs:edges.append(dict(source=p,field=label,target=target));visit(target)
area='/resources/world/areas/4611686096259570491/'
for room in ('01_surface','skyscene_bel_raid'):
 p=area+room+'.dat';visit(p)
 for r in records(scan.read(idx,p).decode()).values():
  fx=r['fields'].get('FxSpecName')
  if fx:
   target=canonical(fx,'fx');placements.append(dict(room=room,id=r['id'],fields=r['fields'],fx=target));visit(target)
# The authored static turbulent dome and fade portal are separate from the FX graph.
for p in ('art/fx/geometry/skies/fx_turbulantskydome_01.gr2','art/static/area/all_all/arch/fadeportal/all_arch_fadeportal_bel_raid_snow_sky.gr2'):visit(canonical(p))
for p in ('art/lights/ilm_exterior_lightramp.dds','art/environmentmaps/hot_main_1/hoth_exterior01.dds'):visit(canonical(p))
out=ROOT/'godot/Assets/Maps/eternity_vault'
for p,node in nodes.items():
 if p.endswith('.dds') and node['found']:
  Image.open(BytesIO(scan.read(idx,p))).save(out/(Path(p).stem+'.png'))
report=dict(evidence='Client-derived',placements=placements,nodes=nodes,edges=edges,graphs=graphs,
 engineTypes=['/resources/engine/projector.ext'],
 unresolved=['Engine projector implementation, sound-event bank resolution, source distribution/spline semantics and empty snow emission defaults.'])
(ROOT/'manifests/ev-surface-fx-trace.json').write_text(json.dumps(report,indent=2))
groups=[]
for placement in placements:
 if 'ed_env_raid_snow_test' in placement['fx']:continue
 graph=graphs[placement['fx']];group=dict(placements=[],emitters=[],particles={})
 def vec(v):return [float(x) for x in v.strip('()').split(',')]
 pos=vec(placement['fields']['Position'])
 sky=placement['room']=='skyscene_bel_raid'
 if sky:pos=[pos[0],pos[1]+100,pos[2]+100]
 group['placements']=[dict(position=pos,rotation=vec(placement['fields']['Rotation']))]
 group['cameraCentred']=sky
 for entry in graph.get('_fxEmitterList',[]):
  ep=canonical(entry['_fxResourceName']);fields=graphs[ep]
  if fields.get('ParticleType')!='BASIC_EMITTER' or not fields.get('EmitFrequency'):continue
  leafpath=canonical(fields['EmitSpec']);leaf=graphs[leafpath]
  tex=canonical(leaf['TextureName'])
  group['emitters'].append(dict(entry=entry,fields=fields,particle=leafpath))
  group['particles'][leafpath]=dict(fields=leaf,texture='res://Assets/Maps/eternity_vault/'+Path(tex).stem+'.png')
 groups.append(group)
(out/'authored-local-effects.json').write_text(json.dumps(groups,indent=2))
print('FX_TRACE',len(placements),'placements',len(nodes),'resources',len(edges),'edges')
print('MISSING',[p for p,v in nodes.items() if not v['found']])
for p,g in graphs.items():
 if p.endswith('.fxspec'):print(Path(p).name,{k:len(v) for k,v in g.items() if isinstance(v,list)})
