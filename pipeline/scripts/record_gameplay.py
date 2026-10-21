"""B1 preservation receipts. Never writes source assets or emulator files."""
import hashlib, json, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'manifests'
def digest(p):
    with p.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()
def snapshot():
    # Protect all generated assets and pre-B1 authored pipelines/inspection scenes.
    paths = list((ROOT/'godot/Assets').rglob('*'))
    paths += list((ROOT/'godot').glob('*.*'))
    paths += [p for p in (ROOT/'scripts').rglob('*') if p.suffix in ('.py','.cs','.csproj')]
    paths += list(ROOT.glob('*.ps1'))
    return {str(p.resolve()):digest(p) for p in sorted(set(paths)) if p.is_file()
            and p.suffix not in ('.uid','.import','.pyc') and p.name != 'record_gameplay.py'}
if sys.argv[1] == 'artifacts':
    authored=[p for p in (ROOT/'godot/Gameplay').iterdir() if p.suffix in ('.cs','.tscn')]
    authored += [ROOT/p for p in ('Open-Gameplay.ps1','Run-Gameplay-Validation.ps1','scripts/record_gameplay.py',
                  'GAMEPLAY-README.md','GAMEPLAY-RESULTS.md','godot/project.godot') if (ROOT/p).exists()]
    files=lambda paths:[dict(path=str(p.relative_to(ROOT)),bytes=p.stat().st_size,sha256=digest(p)) for p in sorted(paths)]
    report=dict(authoredFiles=files(authored),captures=files((ROOT/'generated/gameplay').glob('*.png')),
                environment=dict(path='godot/Assets/Korriban/KorribanEnvironment.tscn',sha256=digest(ROOT/'godot/Assets/Korriban/KorribanEnvironment.tscn')),
                sorcerer=dict(path='godot/Assets/sorcerer.glb',sha256=digest(ROOT/'godot/Assets/sorcerer.glb')))
    (OUT/'gameplay-artifacts.json').write_text(json.dumps(report,indent=2))
elif sys.argv[1] == 'before':
    (OUT/'gameplay-preservation-before.json').write_text(json.dumps(snapshot(), indent=2))
else:
    before=json.loads((OUT/'gameplay-preservation-before.json').read_text())
    expected=[]; changes=[]
    for p,h in before.items():
        path=Path(p)
        if path.is_file() and digest(path)==h: continue
        if path == ROOT/'godot/project.godot':
            # The sole intended edit to a pre-existing file is selection of the bundled Jolt engine.
            text=path.read_text().replace('\n[physics]\n\n3d/physics_engine="Jolt Physics"\n','\n').rstrip('\n')
            variants=[(text+'\n'*n).replace('\n',newline).encode() for n in range(4) for newline in ('\n','\r\n')]
            if any(hashlib.sha256(b).hexdigest()==h for b in variants):
                expected.append(dict(path=p,reason='Bundled Jolt physics avoids tiny-capsule terrain stalls; all earlier config retained.',beforeSha256=h,afterSha256=digest(path)))
                continue
        changes.append(p)
    result=dict(passed=not changes, filesCompared=len(before), changes=changes,
                expectedAuthoredChanges=expected,
                scope='Pre-existing Godot assets, inspection scenes and pipelines. Archives verified against A1 SHA-256 baseline separately. No emulator or extractor writes are performed by B1.')
    (OUT/'gameplay-preservation.json').write_text(json.dumps(result,indent=2))
    print(json.dumps(result)); assert not changes
