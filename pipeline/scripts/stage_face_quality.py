"""Replace only rebaked face images; preserve every mesh, rig and animation byte."""
import argparse, hashlib, json, struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sha=lambda b:hashlib.sha256(b).hexdigest()
source=ROOT/'generated/character'
names=['head_human_caucasian_a01c01_m_basecolor','eye_human_non_a01_c01_basecolor','hair_human_non_a01_v01_m_basecolor']
images={name:(source/(name+'.png')).read_bytes() for name in names}
parser=argparse.ArgumentParser()
parser.add_argument('--backup-dir',type=Path,default=ROOT/'work/face-quality-backup')
backup=parser.parse_args().backup_dir;backup.mkdir(exist_ok=True)
records=[]
for path in sorted((ROOT/'godot/Assets').rglob('*.glb')):
    raw=path.read_bytes()
    length,kind=struct.unpack_from('<II',raw,12);assert kind==0x4e4f534a
    doc=json.loads(raw[20:20+length]);start=20+length
    if 'images' not in doc:continue
    replacements={im['bufferView']:images[im['name']] for im in doc['images'] if im.get('name') in images and 'bufferView' in im}
    if not replacements:continue
    size,kind=struct.unpack_from('<II',raw,start);assert kind==0x004e4942
    old=raw[start+8:start+8+size];binary=bytearray()
    previous=json.loads(json.dumps(doc))
    changed=[]
    for index,view in enumerate(doc['bufferViews']):
        assert view.get('buffer',0)==0
        offset=view.get('byteOffset',0);data=old[offset:offset+view['byteLength']]
        new=replacements.get(index,data)
        if new!=data:changed.append(index)
        binary.extend(b'\0'*((-len(binary))%4))
        view['byteOffset']=len(binary);view['byteLength']=len(new);binary.extend(new)
    if not changed:
        records.append(dict(path=str(path.relative_to(ROOT)),after=sha(raw),alreadyCurrent=True))
        continue
    # Check every untouched buffer view, including indices, weights and keyframes.
    for i,v in enumerate(previous['bufferViews']):
        if i in replacements:continue
        n=doc['bufferViews'][i]
        assert old[v.get('byteOffset',0):v.get('byteOffset',0)+v['byteLength']]==binary[n['byteOffset']:n['byteOffset']+n['byteLength']]
    doc['buffers'][0]['byteLength']=len(binary)
    encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4)
    binary.extend(b'\0'*((-len(binary))%4))
    result=struct.pack('<III',0x46546c67,2,28+len(encoded)+len(binary))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(binary),0x004e4942)+binary
    saved=backup/(sha(raw)+'.glb')
    if not saved.exists():saved.write_bytes(raw)
    path.write_bytes(result)
    records.append(dict(path=str(path.relative_to(ROOT)),before=sha(raw),after=sha(result),replacedImageViews=changed,otherBufferViewsUnchanged=True))
(ROOT/'manifests/face-quality.json').write_text(json.dumps(dict(cause='sRGB bake target followed by a second sRGB encoding',images={k:sha(v) for k,v in images.items()},assets=records),indent=2))
print('Updated face images in',len(records),'assets; all other buffer views unchanged.')
