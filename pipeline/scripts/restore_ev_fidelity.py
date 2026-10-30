"""Recover exact April horizon resources and XRR-3 emissive mask; archives stay read-only."""
import hashlib,json,re,sys,struct,xml.etree.ElementTree as ET
from pathlib import Path
from io import BytesIO
from PIL import Image
import numpy as np
from source_pipeline import ROOT,SOURCE
import map_scan as scan
from maps_common import records,sections
idx=scan.index();out=ROOT/'godot/Assets/Maps/eternity_vault';receipt=[]
def get(path):
    data=scan.read(idx,path);assert data is not None,path
    receipt.append(dict(path=path,sha256=hashlib.sha256(data).hexdigest()))
    target=SOURCE/path.lstrip('/');target.parent.mkdir(parents=True,exist_ok=True)
    if target.exists():assert target.read_bytes()==data,path
    else:target.write_bytes(data)
    return data
aid='4611686096259570491'
area=get(f'/resources/world/areas/{aid}/area.dat').decode('utf-8-sig')
room=get(f'/resources/world/areas/{aid}/01_surface.dat').decode('utf-8-sig')
assets={l.strip().split('=',1)[0]:'/resources/'+l.strip().split('=',1)[1].replace('\\','/').lower().lstrip('/') for l in sections(area)['ASSETS'] if '=' in l}
model='/resources/art/static/area/all_all/arch/fadeportal/all_arch_fadeportal_bel_raid_snow_sky.gr2'
get(model)
mat='/resources/art/shaders/materials/all_arch_fadeportal_bel_raid_snow_sky.mat'
xml=ET.fromstring(get(mat));values={i.findtext('semantic'):i.findtext('value') for i in xml.findall('input')}
texture='/resources/'+values['DiffuseMap'].replace('\\','/').lower()+'.dds'
im=Image.open(BytesIO(get(texture))).convert('RGBA');im.save(out/'surface-horizon.png')
placement=next(r for r in records(room).values() if assets.get(r['asset'])==model)
vector=lambda value:[float(x) for x in value.strip('()').split(',')]
record=dict(id=placement['id'],sourceLine=placement['line'],sourceModel=model,scene='res://Assets/Maps/eternity_vault/surface-horizon.glb',
            position=vector(placement['fields']['Position']),rotation=vector(placement['fields']['Rotation']),scale=vector(placement['fields']['Scale']),
            material=values,textureSize=list(im.size))
boss=json.loads((ROOT/'eternity-enemies-recipe.json').read_text())['xrr3']['parts'][0]['material']
get(boss['path'])
diffuse=np.asarray(Image.open(BytesIO(get(boss['textures']['DiffuseMap']))).convert('RGBA'),dtype=np.float32)/255
rotation=np.asarray(Image.open(BytesIO(get(boss['textures']['RotationMap1']))).convert('RGBA'),dtype=np.float32)/255
# Established Creature/Uber adapter: RotationMap blue carries self-illumination.
linear=np.maximum(diffuse[:,:,:3],0)**2.1*rotation[:,:,2:3]
encoded=np.where(linear<=.0031308,linear*12.92,1.055*np.maximum(linear,0)**(1/2.4)-.055)
Image.fromarray(np.uint8(np.clip(encoded*255+.5,0,255))).save(out/'xrr3-source-emission.png')
# Append the texture to the existing boss GLB: all rig/mesh/animation buffers stay intact.
glb=out/'Enemies/xrr3.glb';raw=glb.read_bytes();size=struct.unpack_from('<I',raw,12)[0]
doc=json.loads(raw[20:20+size]);binary=raw[28+size:]
png=(out/'xrr3-source-emission.png').read_bytes()
image_name='xrr3_source_emission'
existing=next((i for i,x in enumerate(doc.get('images',[])) if x.get('name')==image_name),None)
if existing is None:
    saved=ROOT/'work/ev-fidelity-backup';saved.mkdir(exist_ok=True)
    (saved/(hashlib.sha256(raw).hexdigest()+'.glb')).write_bytes(raw)
    payload=binary+b'\0'*((-len(binary))%4);offset=len(payload);payload+=png
    view=len(doc['bufferViews']);doc['bufferViews'].append(dict(buffer=0,byteOffset=offset,byteLength=len(png)))
    image_index=len(doc['images']);doc['images'].append(dict(name=image_name,mimeType='image/png',bufferView=view))
    texture_index=len(doc['textures']);doc['textures'].append(dict(source=image_index))
    for material in doc['materials']:
        if material.get('name')=='battledroid_warbot_rakata_v01':
            material['emissiveTexture']=dict(index=texture_index);material['emissiveFactor']=[1,1,1]
    assert payload[:len(binary)]==binary
    doc['buffers'][0]['byteLength']=len(payload)
    payload+=b'\0'*((-len(payload))%4)
    encoded_json=json.dumps(doc,separators=(',',':')).encode();encoded_json+=b' '*((-len(encoded_json))%4)
    glb.write_bytes(struct.pack('<III',0x46546c67,2,28+len(encoded_json)+len(payload))+struct.pack('<II',len(encoded_json),0x4e4f534a)+encoded_json+struct.pack('<II',len(payload),0x004e4942)+payload)
record['boss']=dict(sourceMaterial=boss['path'],textureSize=list(diffuse.shape[1::-1]),emissionMaskRange=[float(rotation[:,:,2].min()),float(rotation[:,:,2].max())])
(out/'surface-fidelity.json').write_text(json.dumps(record,indent=2))
(ROOT/'manifests/ev-fidelity-source.json').write_text(json.dumps(dict(evidence='Client-derived exact archive resources',resources=receipt,horizon=record,limits=['OpacityFade is adapted using authored distances; native fade-space semantics remain unverified.','Packed Creature emissive response follows the existing source shader adapter; exact lighting parity is not asserted.']),indent=2))
print('EV_FIDELITY_SOURCE',record['id'],record['textureSize'],record['boss'])
