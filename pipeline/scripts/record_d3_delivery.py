"""Hash only D3 deliverables, never whole original archives."""
import json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
receipts=['d3-source.json','d3-fx-source.json','d3-lacerate-source.json']
sources={}
for name in receipts:
    d=json.loads((ROOT/'manifests'/name).read_text())
    for r in d['resources']:sources[r['archiveHash']]=r
files=[p for p in (ROOT/'godot/Assets/D3').rglob('*') if p.is_file() and not p.name.endswith('.import')]
files+=list((ROOT/'godot/Gameplay/Abilities').glob('*.tres'))
result=dict(sourceReceipts=receipts,uniqueSelectedResources=len(sources),decodedSelectedBytes=sum(r['length'] for r in sources.values()),
    sourcePolicy='Original TORs opened rb; baseline size/mtime checked; selected decoded bytes only hashed; historical filename hints verified by unique April path hash.',
    outputs=[dict(path=str(p.relative_to(ROOT)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in files],
    definitionEvidence=['d3-definitions.json','d3-appearance-evidence.json','d3-impact-evidence.json'],
    audioReceipts=['d3-fx-audio.json','d3-ranged-audio.json','d3-lacerate-audio.json'],
    limitation='Source-derived appearance/gesture/FX adaptations; no native MMO animation graph or FX runtime parity claimed. Exact complete spawn inventories unverified.')
(ROOT/'manifests/d3-delivery.json').write_text(json.dumps(result,indent=2))
print(result['uniqueSelectedResources'],'unique original resources;',len(files),'converted deliverables')
