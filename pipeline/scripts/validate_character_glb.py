"""Structural checks for the selected skinned character; stdlib only."""
import json,struct,math,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
data=(ROOT/'generated/sorcerer.glb').read_bytes()
magic,version,length=struct.unpack_from('<III',data);assert magic==0x46546c67 and version==2 and length==len(data)
jl,jt=struct.unpack_from('<II',data,12);assert jt==0x4e4f534a
doc=json.loads(data[20:20+jl]);bl,bt=struct.unpack_from('<II',data,20+jl);assert bt==0x004e4942
binary=data[28+jl:28+jl+bl]
formats={5120:('b',1),5121:('B',1),5122:('h',2),5123:('H',2),5125:('I',4),5126:('f',4)}
counts={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
def accessor(index):
    a=doc['accessors'][index];v=doc['bufferViews'][a['bufferView']];fmt,size=formats[a['componentType']];n=counts[a['type']]
    offset=v.get('byteOffset',0)+a.get('byteOffset',0);stride=v.get('byteStride',size*n)
    assert offset+(a['count']-1)*stride+size*n<=v.get('byteOffset',0)+v['byteLength']<=len(binary)
    result=[struct.unpack_from('<'+fmt*n,binary,offset+i*stride) for i in range(a['count'])]
    assert all(math.isfinite(x) for row in result for x in row)
    return result
for i in range(len(doc['accessors'])):accessor(i)
assert all('uri' not in b for b in doc['buffers'])
assert len(doc['skins'])==1
skin=doc['skins'][0];joints=skin['joints'];names=[doc['nodes'][i]['name'] for i in joints]
source=json.loads((ROOT/'manifests/character-conversion.json').read_text())
expected=source['skeleton']['bones']+source['clothBones']
assert set(names)=={b['name'] for b in expected};assert len(names)==len(expected)==211
parents={child:idx for idx,node in enumerate(doc['nodes']) for child in node.get('children',[])}
for b in expected:
    idx=joints[names.index(b['name'])]
    if b['parent']:assert doc['nodes'][parents[idx]]['name']==b['parent']
assert len(accessor(skin['inverseBindMatrices']))==211
triangles=vertices=0;worst_weight=0
for mesh in doc['meshes']:
    for p in mesh['primitives']:
        assert p.get('mode',4)==4;a=p['attributes']
        assert all(k in a for k in ['POSITION','NORMAL','TANGENT','TEXCOORD_0','JOINTS_0','WEIGHTS_0'])
        pos=accessor(a['POSITION']);ids=accessor(a['JOINTS_0']);weights=accessor(a['WEIGHTS_0'])
        assert all(0<=j<211 for row in ids for j in row)
        assert all(0<=w<=1 for row in weights for w in row)
        error=max(abs(sum(row)-1) for row in weights);assert error<1e-6;worst_weight=max(worst_weight,error)
        inds=[i[0] for i in accessor(p['indices'])];assert len(inds)%3==0 and max(inds)<len(pos)
        triangles+=len(inds)//3;vertices+=len(pos)
assert triangles==sum(m['triangles'] for m in source['meshes'])
animations=[]
for animation in doc['animations']:
    times=[x[0] for s in animation['samplers'] for x in accessor(s['input'])]
    duration=next(a['sourceDuration'] for a in source['animations'] if a['name']==animation['name'])
    assert abs(min(times))<1e-6 and abs(max(times)-duration)<1e-6
    animations.append({'name':animation['name'],'duration':max(times),'channels':len(animation['channels'])})
assert len(animations)==7
for material in doc['materials']:
    assert 'normalTexture' in material
    assert all(k in material['pbrMetallicRoughness'] for k in ['baseColorTexture','metallicRoughnessTexture'])
for im in doc['images']:
    assert im['mimeType']=='image/png' and 'uri' not in im
    v=doc['bufferViews'][im['bufferView']];assert binary[v.get('byteOffset',0):][:8]==b'\x89PNG\r\n\x1a\n'
result={'passed':True,'glbSha256':hashlib.sha256(data).hexdigest(),'bytes':len(data),'bones':len(joints),
        'meshes':len(doc['meshes']),'triangles':triangles,'vertices':vertices,'maxWeightSumError':worst_weight,
        'animations':animations,'materials':len(doc['materials']),'embeddedImages':len(doc['images']),
        'scope':'Project structural check; independent Blender roundtrip and Godot pose/render checks supplement it.'}
(ROOT/'manifests/character-glb-validation.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
