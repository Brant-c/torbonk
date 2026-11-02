"""Trace Diversion's authored smoke grenade FX (sp_diversion_grenade_01) from April archives."""
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
FX=['abilities/spy/sp_diversion_grenade_01','abilities/spy/sp_diversion_impact_01','abilities/spy/sp_diversion_target_01']
for name in FX:visit(canonical(name,'fx'))
out=ROOT/'godot/Assets/Agent/Diversion';out.mkdir(parents=True,exist_ok=True)
for p,node in nodes.items():
 if p.endswith('.dds') and node['found']:Image.open(BytesIO(scan.read(idx,p))).save(out/(Path(p).stem+'.png'))
report=dict(evidence='Client-derived',roots=[canonical(n,'fx') for n in FX],nodes=nodes,edges=edges,graphs=graphs)
(ROOT/'manifests/diversion-fx-trace.json').write_text(json.dumps(report,indent=2))
print('MISSING',[p for p,v in nodes.items() if not v['found']])
for p,g in graphs.items():
 if p.endswith('.fxspec'):print(p,{k:len(v) for k,v in g.items() if isinstance(v,list)})
for e in edges:print(Path(e['source']).name,e['field'],e['target'])
def vec(v):return [float(x) for x in v.strip('()').split(',')]
groups={}
for name in FX:
 graph=graphs[canonical(name,'fx')];group=dict(placements=[dict(position=[0,0,0],rotation=[0,0,0])],cameraCentred=False,emitters=[],particles={},skipped=[])
 for entry in graph.get('_fxEmitterList',[]):
  ep=canonical(entry['_fxResourceName']);fields=graphs.get(ep,{})
  if fields.get('ParticleType')!='BASIC_EMITTER' or not fields.get('EmitFrequency') or not fields.get('EmitSpec'):group['skipped'].append(dict(path=ep,type=fields.get('ParticleType'),freq=fields.get('EmitFrequency')));continue
  leafpath=canonical(fields['EmitSpec']);leaf=graphs[leafpath];tex=canonical(leaf['TextureName'])
  group['emitters'].append(dict(entry=entry,fields=fields,particle=leafpath))
  group['particles'][leafpath]=dict(fields=leaf,texture='res://Assets/Agent/Diversion/'+Path(tex).stem+'.png')
 groups[name.split('/')[-1]]=group
[(out.parent/('diversion-fx-'+k+'.json')).write_text(json.dumps({k:v},indent=1)) for k,v in groups.items()]
for k,g in groups.items():print(k,len(g['emitters']),'emitters; skipped',g['skipped'])
# The renderer cannot honour per-emitter stop delays, so split each group by authored stop time:
# ONFXSTART emitters stop after _fxStopDelay; ONFXSTOP emitters run until the effect ends.
def stop_key(entry):
 t=entry.get('_fxStopDelay','0:00:00').split(':');secs=float(t[0])*3600+float(t[1])*60+float(t[2])
 return 'stop%03d'%round(secs*100) if entry.get('_fxWhenToStop')=='ONFXSTART' and secs>0 else 'run'
for k,g in groups.items():
 buckets={}
 for e in g['emitters']:buckets.setdefault(stop_key(e['entry']),[]).append(e)
 for tag,emitters in buckets.items():
  part=dict(g,emitters=emitters,particles={e['particle']:g['particles'][e['particle']] for e in emitters})
  (out.parent/f'diversion-fx-{k}-{tag}.json').write_text(json.dumps({k:part},indent=1))
  print('split',k,tag,[e['entry']['_fxName'] for e in emitters])
