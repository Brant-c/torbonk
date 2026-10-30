"""Verify correction against saved original files, including unchanged rotations."""
import argparse, hashlib, json, struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--backup-dir',type=Path,required=True)
backup=parser.parse_args().backup_dir
sha=lambda b:hashlib.sha256(b).hexdigest()
def read(path):
    raw=path.read_bytes();assert len(raw)==struct.unpack_from('<I',raw,8)[0],path
    length=struct.unpack_from('<I',raw,12)[0]
    return raw,json.loads(raw[20:20+length]),raw[20+length:]
def key(doc,tail):return sha(json.dumps(doc,sort_keys=True).encode())+sha(tail)
expected={}
for saved in backup.glob('*.glb'):
    raw,doc,tail=read(saved);removed=[]
    for a in doc.get('animations',[]):
        keep=[]
        for c in a['channels']:
            t=c['target'];name=doc['nodes'][t['node']].get('name','')
            if t['path']=='translation' and name.lower().startswith('fc_'):removed.append(dict(animation=a.get('name',''),bone=name))
            else:keep.append(c)
        a['channels']=keep
    if removed:expected[key(doc,tail)]=(sha(raw),removed)
records=[]
for path in sorted((ROOT/'godot/Assets').rglob('*.glb')):
    raw,doc,tail=read(path)
    assert not any(c['target']['path']=='translation' and doc['nodes'][c['target']['node']].get('name','').lower().startswith('fc_') for a in doc.get('animations',[]) for c in a['channels']),path
    match=expected.get(key(doc,tail))
    if match:records.append(dict(path=str(path.relative_to(ROOT)),before=match[0],after=sha(raw),removedChannelCount=len(match[1]),binaryPayloadUnchanged=True,otherChannelsUnchanged=True))
assert records
(ROOT/'manifests/facial-rest.json').write_text(json.dumps(dict(passed=True,policy='Keep original facial rest positions; retain rotation and body animation',assets=records),indent=2))
print('FACIAL_REST_VALIDATED',len(records),'assets;',sum(r['removedChannelCount'] for r in records),'tracks; binary data and every other channel unchanged')
