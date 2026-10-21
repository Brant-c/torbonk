"""B5 original Wwise event -> HIRC -> exact bank DIDX media -> PCM pipeline."""
import json, re, struct, subprocess, wave, hashlib
import xml.etree.ElementTree as ET
from pathlib import Path
from source_pipeline import ROOT, SOURCE, pathhash
from probe_lightning_audio import fnv
import character_source

def main(profile='abilities', asset_dir='Abilities', extra_names=None):
    character_source.MANIFEST=ROOT/f'manifests/{profile}-source.json'
    src=character_source.SelectedSource()
    config=json.loads((ROOT/f'godot/Assets/{asset_dir}/source-parameters.json').read_text())
    names={s for fields in config.values() for k,v in fields for s in v.split(',') if s.startswith(('Play_','Stop_'))}
    names.update(extra_names if extra_names is not None else ['Play_ability_sithinquisitor_shock_buildup','Play_ability_sithinquisitor_shock_explode'])
    # Existing wwiser XML is the unchanged April Imperial melee data bank, not a current client bank.
    tree=ET.parse(ROOT/'work/lightning/bank.xml');objects=tree.find('.//list[@name="listLoadedItem"]')
    by_id={o.find('field[@name="ulID"]').get('value'):o for o in objects}
    events={n:str(fnv(n)) for n in sorted(names)}
    found={n:i for n,i in events.items() if i in by_id and by_id[i].get('name')=='CAkEvent'}
    work=ROOT/f'work/{profile}/events';work.mkdir(parents=True,exist_ok=True)
    args=[str(ROOT/'tools/wwiser.pyz'),str(SOURCE/'resources/bnk2/abilities_imperialmelee_data.bnk'),
        str(SOURCE/'resources/bnk2/abilities_imperialmelee_media.bnk'),str(SOURCE/'resources/bnk2/init.bnk'),
        '-d','none','-g','-gf',*found.values(),'-go',str(work),'-gw','.']
    subprocess.run([__import__('sys').executable,*args],check=True,stdout=subprocess.DEVNULL)
    banknames={}
    for line in Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').read_text().splitlines():
        if '/bnk2/' in line and line.endswith('.bnk'):banknames[str(fnv(Path(line).stem))]=line
    records=[]; graphs={}; selected=set(); output=ROOT/f'godot/Assets/{asset_dir}/Audio';output.mkdir(exist_ok=True)
    original_dir=SOURCE/f'{profile}-media';original_dir.mkdir(exist_ok=True)
    bankcache={}
    for txtp in work.glob('*.txtp'):
        body=txtp.read_text();event=re.search(r'CAkEvent\[\d+\] (\d+)',body).group(1)
        media=[int(x) for x in re.findall(r'##(\d+)\.wem',body)]
        graphs[event]=dict(media=media,wwiserGraph=body)
        selected.update(re.findall(r'#\s+CAk\w+\[(\d+)\]',body))
    for ob in objects:
        if ob.get('name')!='CAkSound' or ob.get('index') not in selected:continue
        fields={f.get('name'):f.get('value') for f in ob.iter('field')}
        mid=int(fields['sourceID']);bid=fields['uFileID'];path=banknames[bid]
        if bid not in bankcache:
            e=src.index[f'0x{pathhash(path):016X}'];assert len(e)==1;e=e[0];assert e['compressionMethod']==0
            index={};cursor=0;datapos=None
            with open(e['archive'],'rb') as f:
                f.seek(e['dataOffset']);header=f.read(24);assert struct.unpack_from('<I',header,12)[0]==int(bid)
                while cursor+8<=e['length']:
                    f.seek(e['dataOffset']+cursor);tag,n=struct.unpack('<4sI',f.read(8))
                    if tag==b'DIDX':
                        block=f.read(n)
                        for p in range(0,n,12):
                            m,off,size=struct.unpack_from('<III',block,p);index[m]=(off,size)
                    if tag==b'DATA':datapos=cursor+8
                    cursor+=8+n
            bankcache[bid]=(e,index,datapos)
        e,index,datapos=bankcache[bid];off,size=index[mid]
        assert off+datapos==int(fields['uFileOffset']) and size==int(fields['uInMemoryMediaSize'])
        with open(e['archive'],'rb') as f:f.seek(e['dataOffset']+datapos+off);data=f.read(size)
        assert data[:4]==b'RIFF' and len(data)==size
        original=original_dir/f'{mid}.wem';original.write_bytes(data)
        dest=output/f'{mid}.wav'
        if not dest.exists():subprocess.run([str(ROOT/'tools/vgmstream/vgmstream-cli.exe'),'-i','-o',str(dest),str(original)],check=True,stdout=subprocess.DEVNULL)
        with wave.open(str(dest)) as w:seconds=w.getnframes()/w.getframerate()
        records.append(dict(mediaId=mid,bankPath=path,bankId=bid,entry=e,bankDecodedOffset=datapos+off,bytes=size,
            sourceSha256=hashlib.sha256(data).hexdigest(),output=str(dest.relative_to(ROOT)),seconds=seconds))
        dest.with_suffix('.wav.import').write_text('[remap]\nimporter="wav"\ntype="AudioStreamWAV"\n\n[params]\ncompress/mode=0\nedit/trim=false\nedit/normalize=false\n')
    result=dict(evidence='Client-derived: original April FXSpec event names, typed HIRC Event ID, wwiser graph and exact HIRC/DIDX bank offsets',events=events,
        missingEvents=[n for n in names if n not in found],graphs=graphs,media=records,
        limits=['Godot PCM playback approximates Wwise containers, bus gains, spatialization and random history.','Stop events identify lifecycle; bounded fade/release is authored.'])
    (ROOT/f'manifests/{profile}-audio.json').write_text(json.dumps(result,indent=2))
    (output.parent/'audio.json').write_text(json.dumps(result,indent=2))
    print(json.dumps(dict(events=found,missing=result['missingEvents'],media=len(records))))
if __name__=='__main__':
    if '--b8' in __import__('sys').argv:
        main('b8','B8',['Play_ability_sithinquisitor_backlash_chargeup','Play_ability_sithinquisitor_darkgift_cast',
            'Play_ability_general_forceinquisitor_forcebuild_small'])
    else:main()
