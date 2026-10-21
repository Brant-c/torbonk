"""A4 evidence, source integrity, bounded STE2 compatibility and repeat hashes."""
import sys,json,zlib,hashlib,struct
from pathlib import Path
import numpy as np
from source_pipeline import ROOT,sha
from korriban_source import source,local
from probe_korriban import room
from korriban_terrain import decode
from prepare_korriban import PATCHES

def save(name,data): (ROOT/'manifests'/name).write_text(json.dumps(data,indent=2))
def assets():
    folder=ROOT/'godot/Assets/Korriban'
    return [dict(path=str(p.relative_to(ROOT)),bytes=p.stat().st_size,sha256=sha(p)) for p in sorted(folder.iterdir()) if p.suffix in ['.glb','.png','.json']]
def ste2_input():
    reference=json.loads((ROOT/'manifests/korriban-format-references.json').read_text())['ste2']
    assert sha(ROOT/reference['source'])==reference['sha256']
    out=ROOT/'work/korriban/ste2-input';out.mkdir(exist_ok=True)
    recs=room()
    for id in PATCHES:
        b=bytes.fromhex(recs[id]['fields']['VertexData'])
        if b[:2]==b'\x1f\x8b':b=zlib.decompress(b,31)
        elif b[:1]==b'x':b=zlib.decompress(b)
        (out/(id+'.bin')).write_bytes(b)
def ste2_compare():
    records=[];recs=room()
    for id in PATCHES:
        data=decode(recs[id]);vertices=[];facecount=0
        for line in (ROOT/f'work/korriban/ste2-output/{id}.obj').read_text().splitlines():
            if line.startswith('v '):vertices.append([float(v) for v in line[2:].split()])
            elif line.startswith('f '):facecount+=1
        a=np.array(vertices);b=np.array(data['vertices']);assert a.shape==b.shape
        error=np.abs(a-b);assert error[:,[0,2]].max()<1e-5 and error[:,1].max()<.002
        records.append(dict(id=id,vertices=len(a),ste2Triangles=facecount,maxXZError=float(error[:,[0,2]].max()),maxHeightError=float(error[:,1].max()),
            reason='STE2 multiplies interior uint16 by rounded 0.0019531548; A4 preserves exact fixed-point /512. Border float32s agree.',
            topology='Same STE2 fixed diagonal and source hole mask. Selected source tessellation fields are empty; native default is unverified.'))
    save('korriban-ste2-validation.json',dict(passed=True,unmodifiedHeightmapSource=True,patches=records,
        scope='Original upstream heightmap reader/writer invoked only on two selected decompressed blobs. No planet scan.'))
def verify():
    src=source();checked=[]
    for rec in src.manifest['resources']:
        stat=Path(rec['archive']).stat();baseline=src.baseline[rec['archive']]
        assert stat.st_size==baseline['length'] and str(stat.st_mtime_ns)==baseline['mtimeNs']
        assert sha(ROOT/rec['output'])==rec['sha256']
        with open(rec['archive'],'rb') as f:f.seek(rec['dataOffset']);b=f.read(rec['compressedSize'])
        if rec['compressionMethod']==1:b=zlib.decompress(b)
        else:assert rec['compressionMethod']==0
        assert len(b)==rec['length'] and hashlib.sha256(b).hexdigest()==rec['sha256']
        checked.append(rec['path'])
    # Construction checks catch source selection drift and incompatible formats.
    s=json.loads((ROOT/'work/korriban/selection.json').read_text())
    assert len(s['terrain'])==2 and len(s['placements'])==109 and len(s['models'])==31
    assert all(p['fields']['tesselation'] in ('','SHORTEST') for p in [room()[id] for id in PATCHES])
    assert all(not room()[id]['fields']['TexWeightData'] for id in PATCHES)
    assert not src.manifest['unresolved']
    save('korriban-source-integrity.json',dict(passed=True,checkedExtractedOriginals=len(checked),
        decodedBytes=sum(r['length'] for r in src.manifest['resources']),allComparedToArchiveBytes=True,
        changes=[],archiveMetadataMatchesBaseline=True,wholeArchiveIntegrityReport='source-integrity.json',
        originalArchivesOpenedReadOnly=True,emulatorFilesEdited=False))
def artifact_report():
    source_manifest=json.loads((ROOT/'manifests/korriban-source.json').read_text())
    s=json.loads((ROOT/'work/korriban/selection.json').read_text())
    conversion=json.loads((ROOT/'manifests/korriban-model-conversion.json').read_text())
    scene=ROOT/'godot/Assets/Korriban/KorribanEnvironment.tscn'
    save('korriban-artifacts.json',dict(convertedAssets=assets(),scene=dict(path=str(scene.relative_to(ROOT)),sha256=sha(scene)),
        authoredFiles=[dict(path=str(p.relative_to(ROOT)),sha256=sha(p)) for p in sorted(set(list((ROOT/'scripts').glob('*korriban*.py'))+list((ROOT/'godot').glob('*Korriban*.*'))+list(ROOT.glob('*Korriban*.ps1'))+[ROOT/'korriban-recipe.json'])) if p.is_file()],
        screenshots=[dict(path=str(p.relative_to(ROOT)),sha256=sha(p)) for p in sorted((ROOT/'generated/korriban').glob('*inspection.png'))],
        originals=len(source_manifest['resources']),decodedBytes=sum(r['length'] for r in source_manifest['resources']),
        sourceModels=[dict(path=m['path'],sourceSha256=m['sourceSha256'],output=m['output'],outputSha256=m['sha256']) for m in conversion['models']]))
    bypath={r['path']:r for r in source_manifest['resources']}
    save('korriban-dependencies.json',dict(area=s['area'],room=s['room'],patchIds=PATCHES,
        sourceRoom=bypath[f"/resources/world/areas/{s['area']}/baseroom.dat"],
        sourceArea=bypath[f"/resources/world/areas/{s['area']}/area.dat"],
        terrainLayers=s['terrainLayers'],models=s['models'],placements=s['placements'],
        dependencies=source_manifest['dependencies'],unresolved=[],
        unsupportedPresentation=[dict(id=p['id'],asset=p['asset'],path=p['path'],status='Transform/fields retained; native FX execution omitted') for p in s['placements'] if p['path'].endswith('.fxp')],
        excluded=['other 44 embedded baseroom terrain patches','other rooms','engine utility visuals','occluders/design blockers',
            'native DYD scatter execution','native environmental FX and area soundscape'],
        transformPolicy=s['transformPolicy']))
def snapshot():
    (ROOT/'work/korriban/before-assets.json').write_text(json.dumps(assets()))
def repeat():
    before={r['path']:r['sha256'] for r in json.loads((ROOT/'work/korriban/before-assets.json').read_text())}
    after={r['path']:r['sha256'] for r in assets()}
    changed=[p for p in before if p not in after or before[p]!=after[p]]
    new=sorted(after.keys()-before.keys())
    report=dict(passed=not changed,comparedPrevious=bool(before),comparedAssets=len(before),newAssets=new,changed=changed,
        scope='31 GLBs, source-layer/portable terrain PNGs and environment input JSON; Godot PackedScene IDs and captures are not required byte-identical.')
    save('korriban-repeat-validation.json',report);assert not changed,report

def acceptance():
    names=['korriban-ste2-validation.json','korriban-model-roundtrip.json','korriban-terrain-conversion.json',
        'korriban-godot-build.json','korriban-godot-validation.json','korriban-godot-validation-visual.json',
        'korriban-source-integrity.json','korriban-repeat-validation.json','glb-validation.json',
        'roundtrip-validation.json','godot-validation.json','character-glb-validation.json',
        'character-roundtrip-validation.json','character-godot-validation.json','character-repeat-validation.json',
        'lightning-godot-validation.json','lightning-godot-validation-visual.json',
        'lightning-runtime-audio-validation.json','lightning-repeat-validation.json','source-integrity.json']
    checks=[]
    for name in names:
        path=ROOT/'manifests'/name;data=json.loads(path.read_text(encoding='utf-8-sig'));assert data['passed'],name
        checks.append(dict(path='manifests/'+name,sha256=sha(path),passed=True))
    archives=json.loads((ROOT/'manifests/source-integrity.json').read_text())
    regressions=json.loads((ROOT/'manifests/lightning-repeat-validation.json').read_text(encoding='utf-8-sig'))
    assert archives['checkedArchives']==91 and archives['wholeArchiveSha256Rechecked']
    assert regressions['regressionThisRun'] and regressions['archiveRehashThisRun'] and regressions['capturesThisRun']
    save('korriban-validation.json',dict(passed=True,milestone='A4',checks=checks,
        fullA1A3RegressionsCompletedDuringA4=True,finalA4Run='korriban-run.json',
        sourceArchivesChecked=91,sourceArchivesChanged=0,gameplayImplemented=False,
        remainingFidelityLimits=['native terrain UV and shader parity','native default terrain tessellation',
            'DYD/environment FX execution','native sky/lighting','physical-unit calibration']))

if __name__=='__main__':
    {'ste2-input':ste2_input,'ste2-compare':ste2_compare,'verify':verify,'artifacts':artifact_report,'snapshot':snapshot,'repeat':repeat,'acceptance':acceptance}[sys.argv[1]]()
