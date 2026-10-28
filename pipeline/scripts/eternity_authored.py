"""Resolve the authored Default-room door and bounded surface weather graph."""
import json,re,struct,xml.etree.ElementTree as ET
from pathlib import Path
from io import BytesIO
from PIL import Image
from source_pipeline import ROOT,SOURCE
import map_scan as scan
from maps_common import records,sections
from lightning_source import decode_xml
from prepare_abilities import unpack
idx=scan.index();receipt=[]
import hashlib
def get(p):
    b=scan.read(idx,p);assert b is not None,p
    f=SOURCE/p.lstrip('/');f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
    receipt.append(dict(path=p,sha256=hashlib.sha256(b).hexdigest()));return b
def mat(path):
    x=ET.fromstring(get(path));t={};v={}
    for i in x.findall('input'):
        key=i.findtext('semantic');value=i.findtext('value')
        if i.findtext('type')=='texture':
            p='/resources/'+value.replace('\\','/').lower()+'.dds';get(p);t[key]=p
        else:v[key]=value
    return dict(path=path,derived=x.findtext('Derived'),alpha=x.findtext('AlphaMode'),twoSided=x.findtext('IsTwoSided'),textures=t,values=v)
s=json.loads((ROOT/'work/maps/eternity_vault/selection.json').read_text())
get('/resources/art/dynamic/spec/mag_doorrakatalg_doorlg.mag');get('/resources/art/dynamic/spec/doorrakatalg.dyc')
model='/resources/art/dynamic/mag/model/mag_doorrakatalg_doorlg.gr2';get(model)
for p in ['/resources/art/dynamic/spec/doorrakatalg_skeleton.gr2','/resources/anim/placeable/doorrakatalg/idle_close.jba']:get(p)
mp='/resources/art/shaders/materials/bel_arch_rakata_entrance_door_small_01.mat';s['materials'][mp]=mat(mp)
s['models']=[m for m in s['models'] if m['path']!=model]+[dict(path=model,materials=[mp],closedDoor=True,bindPose=True)]
area='4611686096259570491'
raw=get(f'/resources/world/areas/{area}/default.dat').decode();r=records(raw);door=r['4611686142220981203'];f=door['fields']
def vec(v):return [float(x) for x in v.strip('()').split(',')]
s['placements']=[p for p in s['placements'] if p['id']!=door['id']]+[dict(id=door['id'],asset=door['asset'],path=model,room='Default',sourceLine=door['line'],parent='0',position=vec(f['Position']),rotation=vec(f['Rotation']),scale=vec(f['Scale']),fields=f)]
door_placement=s['placements'][-1]
offset=json.loads((ROOT/'maps/eternity_vault.json').read_text()).get('doorAlignmentOffsetSourceXYZ',[0,0,0])
door_placement['authoredPosition']=vec(f['Position'])
door_placement['alignmentOffset']=offset
door_placement['position']=[a+b for a,b in zip(door_placement['authoredPosition'],offset)]
# The visible entrance continues into its authored transition room. Retain its
# original interior and fade portal rather than exposing the unloaded world.
header=sections(get(f'/resources/world/areas/{area}/area.dat').decode())
assets={l.strip().split('=',1)[0]:'/resources/'+l.strip().split('=',1)[1].replace('\\','/').lower().lstrip('/') for l in header['ASSETS'] if '=' in l}
transition=records(get(f'/resources/world/areas/{area}/01_surface_transition.dat').decode())
for rec in transition.values():
    path=assets.get(rec['asset'],'')
    if not (path.endswith('bel_arch_rakata_entrance_interior.gr2') or path.endswith('all_arch_fadeportal_tyt_rep_building_grey.gr2')):continue
    b=get(path);n=struct.unpack_from('<H',b,26)[0];offset=struct.unpack_from('<I',b,88)[0];names=[]
    for i in range(n):
        off=struct.unpack_from('<I',b,offset+i*4)[0];stem=b[off:b.index(b'\0',off)].decode();mp='/resources/art/shaders/materials/'+stem+'.mat'
        s['materials'][mp]=mat(mp);names.append(mp)
    s['models']=[m for m in s['models'] if m['path']!=path]+[dict(path=path,materials=names)]
    f=rec['fields'];s['placements']=[p for p in s['placements'] if p['id']!=rec['id']]+[dict(id=rec['id'],asset=rec['asset'],path=path,room='01_Surface_Transition',sourceLine=rec['line'],parent=f['ParentInstance'],position=vec(f['Position']),rotation=vec(f['Rotation']),scale=vec(f['Scale']),fields=f)]
(ROOT/'work/maps/eternity_vault/selection.json').write_text(json.dumps(s))
out=ROOT/'godot/Assets/Maps/eternity_vault';weather=dict(placements=[],emitters=[],particles={},unsupported=[])
raw=get(f'/resources/world/areas/{area}/01_surface.dat').decode()
for rec in records(raw).values():
    if rec['fields'].get('FxSpecName','').lower().removesuffix('.fxspec')=='fxtest/ed_env_raid_snow_test':
        weather['placements'].append(dict(id=rec['id'],position=vec(rec['fields']['Position']),rotation=vec(rec['fields']['Rotation'])))
fxpath='/resources/art/fx/fxspec/fxtest/ed_env_raid_snow_test.fxspec';fx=unpack(decode_xml(get(fxpath)).find('.//marshalData/node'))
for entry in fx['_fxEmitterList']:
    p='/resources/'+entry['_fxResourceName'].replace('\\','/').lower().lstrip('/')
    fields=dict(re.findall(r'^\s*\.(\w+)=(.*)$',get(p).decode().replace('\r',''),re.M))
    leaf='/resources/'+fields['EmitSpec'].replace('\\','/').lower().lstrip('/')
    data=dict(re.findall(r'^\s*\.(\w+)=(.*)$',get(leaf).decode().replace('\r',''),re.M))
    texture='/resources/'+data['TextureName'].replace('\\','/').lower().lstrip('/')
    name=Path(texture).stem+'.png';Image.open(BytesIO(get(texture))).save(out/name)
    weather['particles'][leaf]=dict(fields=data,texture='res://Assets/Maps/eternity_vault/'+name)
    if not fields['EmitFrequency']:
        weather['unsupported'].append(dict(path=p,reason='Empty EmitFrequency: native default not established; omitted rather than guessed'))
        continue
    weather['emitters'].append(dict(source=p,entry=entry,fields=fields,particle=leaf))
weather['limits']=['Bounded source billboard adapter; random distribution S/I/O spline semantics are not native verified.','Only explicit emission intervals are rendered; empty default intervals are retained as unsupported.','No generated snow art or global fog substitute.']
(out/'authored-weather.json').write_text(json.dumps(weather,indent=2))
(ROOT/'manifests/map-eternity_vault-authored-source.json').write_text(json.dumps(receipt,indent=2))
print('AUTHORED',len(weather['placements']),'placements;',len(weather['emitters']),'explicit emitters; closed Default-room door')
