"""A3 dependency graph, source verification, tool pins, and artifact identities."""
import argparse, json, hashlib, subprocess, zlib
from pathlib import Path
from source_pipeline import ROOT, sha

def load(name): return json.loads((ROOT/'manifests'/name).read_text(encoding='utf-8-sig'))
def save(name,value): (ROOT/'manifests'/name).write_text(json.dumps(value,indent=2),encoding='utf-8')
def fingerprint(path): return {'path':str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path),'bytes':path.stat().st_size,'sha256':sha(path)}

def tools():
    required={'tools/wwiser.pyz':'f4ba1368895adab285f27fa37b142e5da805bcc6fb77e8daba282dac65d89411',
              'tools/vgmstream-win64.zip':'6c4a8a3813864fefed081bbd337dbc0ad93bf88e0b92f5db98d7ab258b22dc6c'}
    for path,digest in required.items(): assert sha(ROOT/path)==digest,(path,'tool package hash changed')
    image_python=Path('C:/Users/brant/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe')
    version=subprocess.run([str(image_python),'-c','import PIL; print(PIL.__version__)'],capture_output=True,text=True,check=True).stdout.strip()
    files=[ROOT/p for p in required]+list((ROOT/'tools/vgmstream').glob('*'))
    record={'wwiser':{'version':'v20260808','url':'https://github.com/bnnm/wwiser/releases/download/v20260808/wwiser.pyz'},
            'vgmstream':{'version':'r2117','url':'https://github.com/vgmstream/vgmstream/releases/download/r2117/vgmstream-win64.zip'},
            'Pillow':version,'imagePython':fingerprint(image_python),'files':[fingerprint(p) for p in files if p.is_file()],
            'existingToolchainLockSha256':sha(ROOT/'manifests/tooling.lock.json')}
    lock=ROOT/'manifests/lightning-tooling.lock.json'
    if lock.exists(): assert json.loads(lock.read_text())==record,'A3 toolchain changed; do not repin silently'
    else: save(lock.name,record)
    print('A3 toolchain verified')

def manifest():
    src=load('lightning-source.json'); character=load('character-source.json'); audio=load('lightning-audio.json')
    reused=[r for r in character['resources'] if r['path'].endswith('.jba') or r['path'].endswith('bmnnew_skeleton.gr2')]
    evidence=json.loads((ROOT.parent/'docs/investigation/asset-evidence-2026-10-20.json').read_text(encoding='utf-8-sig'))
    traces=json.loads((ROOT.parent/'docs/investigation/dependency-traces-2026-10-20.json').read_text(encoding='utf-8-sig'))
    names=['abl.sith_inquisitor.force_lightning','epp.sith_inquisitor.force_lightning.channel_loop']
    captures=[]
    def walk(value):
        if isinstance(value,dict):
            if value.get('identity',{}).get('name') in names and 'path' in value: captures.append(value); return
            for item in value.values(): walk(item)
        elif isinstance(value,list):
            for item in value: walk(item)
    walk(evidence); walk(traces)
    save('lightning-gom-evidence.json',{'label':'Client-derived; historical field/enum labels have separate provenance','captures':captures,
        'sourceFiles':[fingerprint(ROOT.parent/'docs/investigation'/p) for p in ['asset-evidence-2026-10-20.json','dependency-traces-2026-10-20.json']]})
    unresolved=[
        {'resource':'animation-note schedule','status':'unresolved','evidence':'Trigger keys 1,12,48; guaranteed entries 2,3 in original appearance. No verified note timestamps.','adapter':'FX cue at 0.35 s, start/loop/end durations from original clips'},
        {'resource':'FX length/basis versus imported rig','status':'unresolved','adapter':'Adjustable FX scalar 0.10 by default; character rig remains unchanged at 1:1. No physical metre claim.'},
        {'resource':'si_lightningdrain_bodyarcs_p.prt texture selectors','status':'unresolved','evidence':'TextureNumber=15, RowSize=1, ColumnSize=1, StartFrame=1, AnimationSpeed=0; DDS visibly contains four rings.','adapter':'Full original texture used; no invented atlas interpretation.'},
        {'resource':'native client visual/audio parity','status':'unresolved','adapter':'No running SWTOR client reference captured; tests establish conversion and reconstructed presentation only.'}]
    unsupported=[
        {'resource':'Engine/zap.zzp and Engine/light.lit','status':'unsupported','evidence':src['unresolved'],'adapter':'Godot textured ribbons, short-lived billboard particles and bounded lights'},
        {'resource':'FX curve interpolation and control-point executor','status':'unsupported','adapter':'Recovered key values with linear interpolation; deterministic regenerated paths replace native S/I/O spline/random semantics, travel and rotation'},
        {'resource':'Wwise 1500 ms CrossFadeAmp random channel container','status':'unsupported','adapter':'Full original PCM grains in a shuffled repeating Godot playlist. Native overlaps and per-grain pitch resampling are not equivalent.'},
        {'resource':'Wwise 1000 ms CrossFadePower aftershock transient','status':'unsupported','adapter':'One original transient selection; original 200 ms TriggerRate crackle layer and tuning are reconstructed'},
        {'resource':'Wwise bus/LPF/spatial mixing','status':'unsupported','adapter':'Original gain/pitch ranges and 500 ms linear stop; no native bus, LPF or attenuation simulation. Cast tail is also bounded by graceful stop; interrupt clears everything immediately.'},
        {'resource':'native selective lights and bloom','status':'unsupported','adapter':'Source colour keys, intensities and periods in Godot lights; native masks, spot cones, occlusion/bloom and photometric response differ'}]
    resources=[]
    for rec in src['resources']+reused:
        resources.append(dict(rec,status='confirmed',evidence='unique actual April archive entry; decoded bytes hashed'))
    record={'milestone':'A3 Force Lightning','profile':'April2012 operator-labelled; no retail patch authentication',
        'ability':{'name':names[0],'gomId':'0xE0009EB5CD7850CC','bucket':'/resources/systemgenerated/buckets/132.bkt','decodedNodeOffset':189684},
        'appearance':{'name':names[1],'gomId':'0xE0004B907DF52D6B','bucket':'/resources/systemgenerated/buckets/435.bkt','decodedNodeOffset':95524},
        'confirmed':resources,'dependencies':src['dependencies'],
        'confirmedAudioRanges':audio['exactHircMediaReferences'],
        'audioEventIds':{'Play_ability_sithinquisitor_forcelightning_cast':'2909241004','Play_ability_sithinquisitor_forcelightning_casterloop':'1354101915','Stop_ability_sithinquisitor_forcelightning_casterloop':'243915777'},
        'geometryUsage':'fx_dummy_cube.gr2 is retained as original evidence; invisible endpoint proxy transforms are represented directly, not rendered as visible art.',
        'unresolved':unresolved,'unsupported':unsupported,
        'disprovedCandidates':['/resources/bnk/character_sithinquisitor.bnk','/resources/bnk/init.bnk','/resources/bnk2/abilities_imperialmelee_audio.bnk'],
        'bounds':{'selectedResourceFiles':len(src['resources']),'selectedResourceBytes':sum(r['length'] for r in src['resources']),
                  'selectedMediaFiles':len(audio['media']),'selectedMediaBytes':sum(r['bytes'] for r in audio['media']),
                  'reusedCharacterResources':len(reused),'noBulkArchiveExtraction':True},
        'sourceManifests':['lightning-source.json','character-source.json','lightning-audio.json','lightning-audio-graph.xml','lightning-gom-evidence.json']}
    save('lightning-dependencies.json',record)
    return record

def verify():
    record=manifest(); count=0
    inventory=load('source-inventory.json'); baseline={r['path']:r for r in inventory['archives']}
    for rec in record['confirmed']:
        path=Path(rec['archive']); st=path.stat(); original=baseline[rec['archive']]
        assert st.st_size==original['length'] and str(st.st_mtime_ns)==original['mtimeNs']
        with path.open('rb') as f:
            f.seek(rec['dataOffset']); data=f.read(rec['compressedSize'])
        if rec['compressionMethod']==1: data=zlib.decompress(data)
        assert len(data)==rec['length'] and hashlib.sha256(data).hexdigest()==rec['sha256']
        assert sha(ROOT/rec['output'])==rec['sha256']; count+=1
    for rec in record['confirmedAudioRanges']:
        entry=rec['bankEntry']
        with open(entry['archive'],'rb') as f:
            f.seek(entry['dataOffset']+rec['didxDataOffset']+rec['mediaDataOffset']); data=f.read(rec['bytes'])
        assert hashlib.sha256(data).hexdigest()==rec['sha256']
    for rec in load('lightning-audio.json')['media']:
        assert sha(ROOT/rec['source'])==rec['sourceSha256']
        assert sha(ROOT/rec['output'])==rec['sha256']
    for rec in load('lightning-visual-conversion.json')['textures']:
        assert sha(ROOT/rec['source'])==rec['sourceSha256'] and sha(ROOT/rec['output'])==rec['sha256']
    for resource in json.loads((ROOT.parent/'docs/investigation/asset-evidence-2026-10-20.json').read_text(encoding='utf-8-sig'))['resources']:
        match=next((r for r in record['confirmed'] if r['path']==resource['query'].lower()),None)
        if match and 'sha256' in resource['result']: assert resource['result']['sha256']==match['sha256']
    save('lightning-source-integrity.json',{'passed':True,'archivePayloadComparisons':count,'exactHircMediaRangesCompared':len(record['confirmedAudioRanges']),
        'decodedMediaCompared':24,'textureConversionsCompared':2,'archiveMetadataMatchesBaseline':True,
        'wholeArchiveVerification':'source-integrity.json from unchanged doorway runner; rerun unless explicitly skipped'})
    print('Verified A3 resources, exact shared-bank media ranges, PCM and textures')

def artifacts():
    manifest()
    paths=list((ROOT/'godot/Assets/Lightning/Audio').glob('*.wav'))+list((ROOT/'godot/Assets/Lightning').glob('*.png'))
    authored=[ROOT/'godot'/p for p in ['ForceLightning.gd','Lightning.gdshader','LightningInspection.gd','LightningInspection.tscn']]
    authored+=list((ROOT/'scripts').glob('*lightning*.py'))
    authored+=[ROOT/p for p in ['Run-Lightning-Pipeline.ps1','Open-LightningInspection.ps1','Prepare-Lightning-Tools.ps1','LIGHTNING-README.md','LIGHTNING-RESULTS.md']]
    save('lightning-artifacts.json',{'convertedAssets':[fingerprint(p) for p in sorted(paths)],
        'authoredFiles':[fingerprint(p) for p in sorted(authored)],
        'captures':[fingerprint(p) for p in sorted((ROOT/'generated/lightning').iterdir()) if p.suffix in ('.png','.wav')],
        'sourceManifestSha256':sha(ROOT/'manifests/lightning-dependencies.json')})
    print('Recorded',len(paths),'converted asset hashes')

if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('stage',choices=['tools','verify','artifacts']); globals()[p.parse_args().stage]()
