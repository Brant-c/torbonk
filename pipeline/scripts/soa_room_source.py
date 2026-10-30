"""Bounded April environmental dependencies for the complete Infernal Prison shell."""
import json, re, struct, xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image
from io import BytesIO
from maps_common import source, work, asset_dir, records
from lightning_source import decode_xml
from prepare_abilities import unpack

src=source(); out=asset_dir(); selection=json.loads((work()/'selection.json').read_text()); missing=[]
room=records(src.get('/resources/world/areas/4611686096259570491/05_infernal_prison.dat').decode())
def canon(p): return '/resources/'+p.replace('\\','/').lower().lstrip('/').removeprefix('resources/')
def get(p): return src.get(canon(p),'Infernal Prison environmental graph','explicit source dependency')
def vector(s): return [float(v) for v in s.strip('()').split(',')]
def placement(r,path):
    f=r['fields']
    return dict(id=r['id'],asset=r['asset'],path=path,room=selection['room'],sourceLine=r['line'],parent=f['ParentInstance'],position=vector(f['Position']),rotation=vector(f['Rotation']),scale=vector(f['Scale']),fields=f)
def model(path):
    b=get(path); count=struct.unpack_from('<H',b,26)[0]; offset=struct.unpack_from('<I',b,88)[0]; mats=[]
    for i in range(count):
        p=struct.unpack_from('<I',b,offset+4*i)[0]; name=b[p:b.index(b'\0',p)].decode(); mp='/resources/art/shaders/materials/'+name+'.mat'
        xml=ET.fromstring(get(mp)); textures={};values={}
        for inp in xml.findall('input'):
            key=inp.findtext('semantic'); val=inp.findtext('value')
            if inp.findtext('type')=='texture':
                tex=canon(val)+'.dds'
                if not src.has(tex):
                    record=dict(material=mp,path=tex)
                    if record not in missing: missing.append(record)
                    continue
                get(tex);textures[key]=tex
            else:values[key]=val
        selection['materials'][mp]=dict(path=mp,derived=xml.findtext('Derived'),alpha=xml.findtext('AlphaMode'),twoSided=xml.findtext('IsTwoSided'),textures=textures,values=values);mats.append(mp)
    selection['models'].append(dict(path=path,materials=mats))
    return mats

# The MAG explicitly binds this model and two material slots. Keep its source bind pose.
get('/resources/art/dynamic/spec/mag_envrakatapiston_01.mag')
get('/resources/art/dynamic/spec/envrakatapiston.dyc')
get('/resources/art/dynamic/spec/envrakatapiston_skeleton.gr2')
piston='/resources/art/dynamic/mag/model/mag_envrakatapiston_01.gr2';model(piston)
selection['placements'].append(placement(room['4611686160115790248'],piston))

fx=unpack(decode_xml(get('/resources/art/fx/fxspec/environments/raids/env_raids_lava_pool.fxspec')).find('.//marshalData/node'))
entry=fx['_fxModelList'][0]; lava=canon(entry['_fxResourceName']); mats=model(lava)
p=placement(room['4611686142014550059'],lava)
p['scale']=[a*b for a,b in zip(p['scale'],vector(entry['_fxScale']))]
p['position']=[a+b for a,b in zip(p['position'],vector(entry['_fxAttachPosition']))]
p['id']+='_lava';p['parent']='0'
if not missing: selection['placements'].append(p)
else: selection['models']=[m for m in selection['models'] if m['path']!=lava]
lava_material=selection['materials'][mats[0]]
textures={}
for key,path in lava_material['textures'].items():
    name=Path(path).stem+'.png'; Image.open(BytesIO(get(path))).save(out/name);textures[key]='res://Assets/Maps/soa_p3/'+name
if missing:
    for mp in mats: selection['materials'].pop(mp)

lights=[dict(id=r['id'],fields=r['fields']) for r in room.values() if 'LightType' in r['fields']]
fx_records=[dict(id=r['id'],fields=r['fields']) for r in room.values() if 'FxSpecName' in r['fields']]
emberfx=unpack(decode_xml(get('/resources/art/fx/fxspec/env_ord_rising_embers.fxspec')).find('.//marshalData/node'))
emitter=emberfx['_fxEmitterList'][0]
def prt(p):return dict(re.findall(r'^\s*\.(\w+)=(.*)$',get(p).decode().replace('\r',''),re.M))
ef=prt('/resources/art/fx/particles/'+emitter['_fxResourceName'])
pf=prt('/resources/art/fx/particles/'+ef['EmitSpec'])
Image.open(BytesIO(get(pf['TextureName']))).save(out/'fx_embers_02.png')
environment=dict(lights=lights,fxPlacements=fx_records,lava=dict(available=not missing,instance=p['id'],material=lava_material,textures=textures),missing=missing,
    embers=dict(emitter=ef,particle=pf,texture='res://Assets/Maps/soa_p3/fx_embers_02.png'),
    limits=['Piston uses source bind geometry; animation state unresolved.',
    'Lava AnimatedUV graph retained; absent source textures prevent playback. Static source decal retained.',
    'Light ellipsoids map to spherical Godot ranges; source light textures are not reproduced.',
    'Ember trajectory and hue/size endpoints retained; native random/spline, drag and buoyancy semantics unresolved.',
    'Directional projector forward-axis conversion and indoor ambient fill are renderer adaptations, not native lighting parity.',
    'Lava burst FX and environmental sound remain recorded but not reproduced.'])
(out/'room-environment.json').write_text(json.dumps(environment,indent=2))
(work()/'selection.json').write_text(json.dumps(selection))
print('SOA_ROOM_SOURCE',len(selection['placements']),'placements',len(selection['models']),'models',len(lights),'lights')
