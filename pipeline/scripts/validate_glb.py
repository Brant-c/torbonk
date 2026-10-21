"""Validate a small self-contained static GLB without third-party packages."""
import hashlib, json, math, struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
data=(ROOT/'generated/sith_academy_door.glb').read_bytes()
magic,version,length=struct.unpack_from('<III',data)
assert magic==0x46546c67 and version==2 and length==len(data)
jl,jt=struct.unpack_from('<II',data,12); assert jt==0x4e4f534a
doc=json.loads(data[20:20+jl]); bl,bt=struct.unpack_from('<II',data,20+jl)
assert bt==0x004e4942; binary=data[28+jl:28+jl+bl]
assert all('uri' not in b for b in doc['buffers'])
assert 'animations' not in doc and 'skins' not in doc
assert len(doc['meshes'])==1 and len(doc['materials'])==1
formats={5120:('b',1),5121:('B',1),5122:('h',2),5123:('H',2),5125:('I',4),5126:('f',4)}
counts={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
def accessor(i):
    a=doc['accessors'][i]; view=doc['bufferViews'][a['bufferView']]
    fmt,size=formats[a['componentType']]; n=counts[a['type']]
    start=view.get('byteOffset',0)+a.get('byteOffset',0); stride=view.get('byteStride',size*n)
    assert start+(a['count']-1)*stride+size*n<=view.get('byteOffset',0)+view['byteLength']<=len(binary)
    values=[struct.unpack_from('<'+fmt*n,binary,start+j*stride) for j in range(a['count'])]
    assert all(math.isfinite(x) for v in values for x in v)
    return values
for i in range(len(doc['accessors'])): accessor(i)
primitives=[]
for mesh in doc['meshes']:
    for p in mesh['primitives']:
        assert p.get('mode',4)==4
        a=p['attributes']; assert all(k in a for k in ['POSITION','NORMAL','TANGENT','TEXCOORD_0'])
        positions=accessor(a['POSITION']); normals=accessor(a['NORMAL']); tangents=accessor(a['TANGENT'])
        indices=[v[0] for v in accessor(p['indices'])]; assert len(indices)%3==0 and max(indices)<len(positions)
        normal_error=max(abs(math.sqrt(sum(x*x for x in n))-1) for n in normals)
        assert normal_error<1e-5
        assert all(abs(abs(t[3])-1)<1e-6 for t in tangents)
        minimum=[min(v[c] for v in positions) for c in range(3)]
        maximum=[max(v[c] for v in positions) for c in range(3)]
        primitives.append({'vertices':len(positions),'triangles':len(indices)//3,'min':minimum,'max':maximum,'normalMaxLengthError':normal_error})
images=[]
for im in doc['images']:
    assert im['mimeType']=='image/png' and 'uri' not in im
    v=doc['bufferViews'][im['bufferView']]; b=binary[v.get('byteOffset',0):v.get('byteOffset',0)+v['byteLength']]
    assert b.startswith(b'\x89PNG\r\n\x1a\n')
    w,h=struct.unpack_from('>II',b,16)
    images.append({'name':im.get('name'),'dimensions':[w,h],'sha256':hashlib.sha256(b).hexdigest()})
mat=doc['materials'][0]
assert all(k in mat for k in ['normalTexture','emissiveTexture'])
assert all(k in mat['pbrMetallicRoughness'] for k in ['baseColorTexture','metallicRoughnessTexture'])
result={'passed':True,'glbSha256':hashlib.sha256(data).hexdigest(),'bytes':len(data),
        'selfContained':True,'primitives':primitives,'embeddedImages':images,
        'extensionsUsed':doc.get('extensionsUsed',[]),'material':mat,
        'scope':'Structural/accessor/finite-value checks; not the full Khronos conformance validator'}
(ROOT/'manifests/glb-validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
