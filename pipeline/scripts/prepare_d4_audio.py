"""Two named original ambience events plus one original vocal slice from each selected NPC bank."""
import json,sys,struct,subprocess,hashlib,wave,shutil
from pathlib import Path
from source_pipeline import ROOT,SOURCE
from d4_source import source

def main():
    asset=ROOT/'godot/Assets/Korriban/D4';out=asset/'Audio';out.mkdir(exist_ok=True)
    (ROOT/'work/d4').mkdir(parents=True,exist_ok=True)
    selected_source=source()
    data_path='/resources/bnk2/location_korriban_data.bnk'
    selected_source.get(data_path,'April ambience table events','selected Korriban HIRC')
    if not (ROOT/'work/d4/location_korriban_data.xml').exists():
        subprocess.run([sys.executable,str(ROOT/'tools/wwiser.pyz'),str(SOURCE/data_path.lstrip('/')),'-d','xml','-dn',str(ROOT/'work/d4/location_korriban_data')],check=True)
    (asset/'source-parameters.json').write_text('{}')
    code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/d4/location_korriban_data.xml'")
    code=code.replace('abilities_imperialmelee','location_korriban').replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
    # Preserve the full event graph, but decode only its largest embedded bed, not dozens of random accents.
    code=code.replace('    for ob in objects:\n        if ob.get', '''    chosen_media=set()
    for graph in graphs.values():
        candidates=[]
        for ob in objects:
            if ob.get('name')!='CAkSound' or ob.get('index') not in selected:continue
            f={v.get('name'):v.get('value') for v in ob.iter('field')}
            if int(f['sourceID']) in graph['media']:
                candidates.append((int(f['uInMemoryMediaSize']),int(f['sourceID'])))
        chosen_media.add(max(candidates)[1])
    for ob in objects:
        if ob.get''',1)
    code=code.replace("mid=int(fields['sourceID']);bid=fields['uFileID'];path=banknames[bid]",
        "mid=int(fields['sourceID']);bid=fields['uFileID'];path=banknames[bid]\n        if mid not in chosen_media:continue")
    code=code.replace("assert off+datapos==int(fields['uFileOffset']) and size==int(fields['uInMemoryMediaSize'])",
        "assert size==int(fields['uInMemoryMediaSize'])")
    code=code.replace('bankDecodedOffset=datapos+off,bytes=size,',"bankDecodedOffset=datapos+off,hircDeclaredOffset=int(fields['uFileOffset']),offsetDelta=datapos+off-int(fields['uFileOffset']),bytes=size,")
    (ROOT/'work/d4/audio-selected.py').write_text(code)
    ns={'__name__':'d4_audio'};exec(compile(code,'d4-ambience-audio','exec'),ns)
    ns['main']('d4','Korriban/D4',['play_amb_korriban_valley_of_the_sith_ext','Play_amb_korriban_tomb_naga_main'])
    report=json.loads((ROOT/'manifests/d4-audio.json').read_text());media={r['mediaId']:r for r in report['media']}
    beds={}
    for name,target in [('play_amb_korriban_valley_of_the_sith_ext','valley'),('Play_amb_korriban_tomb_naga_main','tomb')]:
        graph=report['graphs'][report['events'][name]]
        choices=[media[m] for m in graph['media'] if m in media]
        if not choices:raise RuntimeError(('No recovered ambience',name))
        chosen=max(choices,key=lambda r:r['seconds']);shutil.copyfile(ROOT/chosen['output'],out/(target+'.wav'))
        beds[target]=dict(event=name,chosenMediaId=chosen['mediaId'],seconds=chosen['seconds'],
                          adaptation='One original long bed from this event; native random/layer mix is not reproduced')
    src=source();vocal=[]
    for bank,target in [('voc_tukatasmall','tukata'),('voc_terentatek','beast'),('voc_assassindroidancientguardian','droid')]:
        path=f'/resources/bnk2/{bank}.bnk';raw=src.get(path,'original NPP sound package','selected vocal bank')
        pos=0;index=[];data=None
        while pos+8<=len(raw):
            tag,n=struct.unpack_from('<4sI',raw,pos)
            if tag==b'DIDX':index=[struct.unpack_from('<III',raw,p) for p in range(pos+8,pos+8+n,12)]
            if tag==b'DATA':data=pos+8
            pos+=8+n
        # First bounded source variation, not an inferred attack/death event identity.
        mid,offset,size=index[0];wem=ROOT/f'work/d4/{target}-{mid}.wem';selected=raw[data+offset:data+offset+size];wem.write_bytes(selected)
        dest=out/(target+'.wav')
        subprocess.run([str(ROOT/'tools/vgmstream/vgmstream-cli.exe'),'-i','-o',str(dest),str(wem)],check=True,stdout=subprocess.DEVNULL)
        with wave.open(str(dest)) as w:seconds=w.getnframes()/w.getframerate()
        vocal.append(dict(bank=path,mediaId=mid,decodedOffset=data+offset,length=size,sha256=hashlib.sha256(selected).hexdigest(),
                          output=str(dest.relative_to(ROOT)),seconds=seconds,adaptation='Selected original vocal variation; attack cue assignment authored'))
    tuning=json.loads((ROOT/'godot/Gameplay/presentation-audio.json').read_text())
    for target in ['tukata','beast','droid']:
        tuning['cues']['d4_'+target]=dict(category='Boss' if target!='tukata' else 'World',path=f'res://Assets/Korriban/D4/Audio/{target}.wav',gainDb=-10,
                                             cooldown=1.0,duration=1.5,pitch=1,priority=False)
    tuning['cues']['d4_region']=dict(tuning['cues']['boss_start'],gainDb=-10,cooldown=3,priority=False)
    (ROOT/'godot/Gameplay/Planets/Korriban/audio.json').write_text(json.dumps(tuning,indent=2))
    report['beds']=beds;report['vocals']=vocal
    (ROOT/'manifests/d4-audio.json').write_text(json.dumps(report,indent=2))
    for p in out.glob('*.wav'):
        p.with_suffix('.wav.import').write_text('[remap]\nimporter="wav"\ntype="AudioStreamWAV"\n\n[params]\ncompress/mode=0\nedit/trim=false\nedit/normalize=false\n')
    print('D4_AUDIO_SELECTED',beds,'vocals',len(vocal))

if __name__=='__main__':main()
