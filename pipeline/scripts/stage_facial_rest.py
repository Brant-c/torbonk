"""Keep original facial bone positions instead of incompatible JBA translations."""
import argparse, hashlib, json, struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--backup-dir',type=Path,required=True)
backup=parser.parse_args().backup_dir;backup.mkdir(exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest()
records=[]
for path in sorted((ROOT/'godot/Assets').rglob('*.glb')):
    raw=path.read_bytes();length=struct.unpack_from('<I',raw,12)[0]
    doc=json.loads(raw[20:20+length]);removed=[]
    for animation in doc.get('animations',[]):
        keep=[]
        for channel in animation['channels']:
            target=channel['target'];node=doc['nodes'][target['node']]
            if target['path']=='translation' and node.get('name','').lower().startswith('fc_'):
                removed.append(dict(animation=animation.get('name',''),bone=node['name']))
            else:keep.append(channel)
        animation['channels']=keep
    if not removed:continue
    encoded=json.dumps(doc,separators=(',',':')).encode()
    assert len(encoded)<=length
    encoded+=b' '*(length-len(encoded))
    tail=raw[20+length:]
    result=struct.pack('<III',0x46546c67,2,20+len(encoded)+len(tail))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+tail
    assert result[20+len(encoded):]==tail, 'All geometry, weights, textures and keyframe buffers must be unchanged'
    saved=backup/(sha(raw)+'.glb')
    if not saved.exists():saved.write_bytes(raw)
    assert sha(saved.read_bytes())==sha(raw)
    # The JSON gets smaller. Updating the existing allocation avoids needing
    # a second full asset allocation on a nearly full drive.
    with path.open('r+b') as stream:
        stream.write(result)
    records.append(dict(path=str(path.relative_to(ROOT)),before=sha(raw),after=sha(result),removedChannels=removed,binaryPayloadUnchanged=True))
(ROOT/'manifests/facial-rest.json').write_text(json.dumps(dict(policy='Original fc_ rest translations; facial rotation and all non-facial animation channels retained',assets=records),indent=2))
print('FACIAL_REST_PASSED',len(records),'assets;',sum(len(r['removedChannels']) for r in records),'incompatible position tracks removed')
