"""Validate exported animation payload and catalog coverage without gameplay changes."""
import json,struct,math
from pathlib import Path
from source_pipeline import ROOT
p=ROOT/'godot/Assets/WarriorLibrary/casts.glb';data=p.read_bytes()
magic,version,length=struct.unpack_from('<III',data);assert magic==0x46546c67 and version==2 and length==len(data)
n,tag=struct.unpack_from('<II',data,12);assert tag==0x4e4f534a
d=json.loads(data[20:20+n]);bn,bt=struct.unpack_from('<II',data,20+n);assert bt==0x004e4942
payload=data[28+n:28+n+bn];assert len(payload)==bn
for view in d['bufferViews']:assert view.get('byteOffset',0)+view['byteLength']<=len(payload)
expected=json.loads((ROOT/'manifests/warrior-library-conversion.json').read_text())['clips']
edges=json.loads((ROOT/'manifests/warrior-library-conversion.json').read_text())['edges']
direct={Path(v).stem for k,vs in edges.items() if k.startswith('epp.') for v in vs if v.endswith('.jba')}
expected={k:v for k,v in expected.items() if v in direct or v in ('cb_saber_attack_right_1','cb_2saber_attack_right_1')}
names={a['name'] for a in d['animations']};assert set(expected)<=names,(set(expected)-names)
assert d['skins'] and any(n.get('name')=='bmnnew' for n in d['nodes'])
for a in d['animations']:
    for s in a['samplers']:
        acc=d['accessors'][s['input']];view=d['bufferViews'][acc['bufferView']]
        off=view.get('byteOffset',0)+acc.get('byteOffset',0)
        times=struct.unpack_from('<'+'f'*acc['count'],payload,off)
        assert all(math.isfinite(t) for t in times) and all(b>a for a,b in zip(times,times[1:]))
catalog=json.loads((ROOT/'godot/Assets/WarriorLibrary/catalog.json').read_text())
required=['ravage','vicious_throw','retaliation','enraged_defense','skill.rage.force_crush','skill.rage.obliterate','skill.immortal.crushing_blow','saber_throw','force_push','push','feint','obfuscate','predation','wounding_throw']
assert all(any(r['name']=='abl.sith_warrior.'+name and r['packages'] for r in catalog) for name in required)
result=dict(passed=True,animations=len(names),singleSaber=sum(n.startswith('cb_saber_') for n in names),dualSaber=sum(n.startswith('cb_2saber_') for n in names),requiredAbilityPackageMembership=len(required),bufferViews=len(d['bufferViews']),checks=['GLB header and bounds','all expected clips exported','bmnnew skin retained','strict finite animation time sequences','requested roster package membership'])
(ROOT/'manifests/warrior-library-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
