import json, hashlib, zlib, struct
from source_pipeline import ROOT, SOURCE, sha, pathhash
from probe_lightning_audio import fnv

discovery=json.loads((ROOT/'manifests/lightning-audio-discovery.json').read_text())
match=discovery['matches'][0]; e=match['entry']
names=__import__('pathlib').Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt')
candidates=[]
with names.open(encoding='utf-8-sig') as f:
    for line in f:
        p=line.strip().lower()
        if p.endswith('.bnk') and f'0x{pathhash(p):016X}'==e['archiveHash']: candidates.append(p)
with open(e['archive'],'rb') as f:
    f.seek(e['dataOffset']); raw=f.read(e['compressedSize'])
data=zlib.decompress(raw) if e['compressionMethod']==1 else raw
assert len(data)==e['length']
out=SOURCE/'audio-by-hash'/('30583e2cda806b52.bnk'); out.parent.mkdir(exist_ok=True)
if out.exists(): assert sha(out)==hashlib.sha256(data).hexdigest()
else: out.write_bytes(data)
e.update(sha256=sha(out),output=str(out.relative_to(ROOT)),candidatePaths=candidates,
         archiveSha256=next(r['sha256'] for r in json.loads((ROOT/'manifests/source-inventory.json').read_text())['archives'] if r['path']==e['archive']))
(ROOT/'manifests/lightning-bank-source.json').write_text(json.dumps(match,indent=2))
print(json.dumps(match,indent=2))
