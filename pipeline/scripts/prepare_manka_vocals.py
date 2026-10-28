"""Bounded April manka vocal selection. Assignments are authored, not native event claims."""
import json,struct,subprocess,wave,hashlib,shutil
from pathlib import Path
from source_pipeline import ROOT
import character_source

def main():
    character_source.MANIFEST=ROOT/'manifests/manka-vocals-source.json'
    src=character_source.SelectedSource()
    raw=src.get('/resources/bnk2/voc_manka.bnk','EV ice cat vocal trial','original manka vocal bank')
    src.get('/resources/bnk2/voc_all.bnk','EV ice cat vocal trial','vocal event/container evidence')
    work=ROOT/'work/manka';work.mkdir(exist_ok=True)
    pos=0;index=[];data=None
    while pos+8<=len(raw):
        tag,n=struct.unpack_from('<4sI',raw,pos)
        if tag==b'DIDX':index=[struct.unpack_from('<III',raw,p) for p in range(pos+8,pos+8+n,12)]
        if tag==b'DATA':data=pos+8
        pos+=8+n
    records=[]
    for mid,offset,size in index:
        wem=work/f'{mid}.wem';payload=raw[data+offset:data+offset+size];wem.write_bytes(payload)
        dest=work/f'{mid}.wav'
        subprocess.run([str(ROOT/'tools/vgmstream/vgmstream-cli.exe'),'-i','-o',str(dest),str(wem)],check=True,stdout=subprocess.DEVNULL)
        with wave.open(str(dest)) as w:seconds=w.getnframes()/w.getframerate()
        records.append(dict(mediaId=mid,bankOffset=data+offset,bytes=size,sourceSha256=hashlib.sha256(payload).hexdigest(),seconds=seconds))
    (work/'clips.json').write_text(json.dumps(records,indent=2))
    selection={'hurt':[41823499,764195414,110314226,526138516], 'death':[806132088,44205520,846499615]}
    output=ROOT/'godot/Assets/NpcVocals/Manka';output.mkdir(parents=True,exist_ok=True)
    selected=[]
    for reaction,media in selection.items():
        for mid in media:
            dest=output/f'{mid}.wav';shutil.copyfile(work/f'{mid}.wav',dest)
            dest.with_suffix('.wav.import').write_text('[remap]\nimporter="wav"\ntype="AudioStreamWAV"\n\n[params]\ncompress/mode=0\nedit/trim=false\nedit/normalize=false\n')
            selected.append(dict(next(r for r in records if r['mediaId']==mid),reaction=reaction,output=str(dest.relative_to(ROOT)),outputSha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
    (ROOT/'manifests/manka-vocals.json').write_text(json.dumps(dict(evidence='Client-derived April voc_manka bank bytes. Reaction assignments authored for EV trial; native event meanings unverified.',selection=selected),indent=2))
    print([(r['mediaId'],round(r['seconds'],2)) for r in records])
if __name__=='__main__':main()
