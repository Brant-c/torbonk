"""Exact DIDX media selection and Vorbis decode; no unrelated bank media exported."""
import json, re, struct, hashlib, subprocess, wave
import xml.etree.ElementTree as ET
from source_pipeline import ROOT, SOURCE, sha
from lightning_source import source as selected_source
from source_pipeline import pathhash

def main():
    bank=SOURCE/'resources/bnk2/abilities_imperialmelee_media.bnk'
    data=bank.read_bytes(); p=0; chunks={}
    while p+8<=len(data):
        tag=data[p:p+4]; n=struct.unpack_from('<I',data,p+4)[0]
        chunks[tag]=(p+8,n); p+=8+n
    off,n=chunks[b'DIDX']; didx={}
    for p in range(off,off+n,12):
        mid,rel,size=struct.unpack_from('<III',data,p); didx[mid]=(rel,size,p)
    events={}; ids=set(); selected_indices=set()
    for file in sorted((ROOT/'work/lightning/events').glob('*.txtp')):
        text=file.read_text()
        event=re.search(r'CAkEvent\[\d+\] (\d+)',text).group(1)
        media=[int(s) for s in re.findall(r'##(\d+)\.wem',text)]
        events[event]={'media':media,'txtp':str(file.relative_to(ROOT)),
                       'wwiserGraph':text}
        ids.update(media)
        selected_indices.update(re.findall(r'#\s+CAk\w+\[(\d+)\]',text))
    assert len(ids)==24, ids
    outdir=ROOT/'godot/Assets/Lightning/Audio'; outdir.mkdir(parents=True,exist_ok=True)
    originals=SOURCE/'lightning-media'; originals.mkdir(exist_ok=True)
    records=[]
    for mid in sorted(ids):
        rel,size,di=didx[mid]; pos=chunks[b'DATA'][0]+rel
        wem=data[pos:pos+size]; assert len(wem)==size and wem[:4]==b'RIFF'
        source=originals/f'{mid}.wem'; digest=hashlib.sha256(wem).hexdigest()
        if source.exists(): assert sha(source)==digest
        else: source.write_bytes(wem)
        output=outdir/f'{mid}.wav'
        cli=ROOT/'tools/vgmstream/vgmstream-cli.exe'
        info=subprocess.run([str(cli),'-m',str(source)],capture_output=True,text=True,check=True).stdout
        subprocess.run([str(cli),'-i','-o',str(output),str(source)],capture_output=True,text=True,check=True)
        with wave.open(str(output)) as w:
            frames=w.getnframes(); rate=w.getframerate(); channels=w.getnchannels()
            assert frames>0 and channels in (1,2) and w.getsampwidth()==2
            pcm_sha=hashlib.sha256(w.readframes(frames)).hexdigest()
        record={'mediaId':mid,'didxOffset':di,'dataRelativeOffset':rel,'bankDecodedOffset':pos,
                'bytes':size,'sourceSha256':digest,'source':str(source.relative_to(ROOT)),
                'output':str(output.relative_to(ROOT)),'sha256':sha(output),
                'frames':frames,'sampleRate':rate,'channels':channels,'seconds':frames/rate,'decoderInfo':info,'pcmSha256':pcm_sha}
        records.append(record)
        imported=output.with_suffix('.wav.import')
        if imported.exists():
            imported.write_text(re.sub(r'compress/mode=\d+', 'compress/mode=0', imported.read_text()))
        else:
            imported.write_text('[remap]\nimporter="wav"\ntype="AudioStreamWAV"\n\n[params]\ncompress/mode=0\nedit/trim=false\nedit/normalize=false\n')
        print(mid, frames/rate, 'loop' if 'loop start' in info else '')
    tree=ET.parse(ROOT/'work/lightning/bank.xml')
    objects=tree.find('.//list[@name="listLoadedItem"]')
    # HIRC can reference a shared media bank even when this bank duplicates the ID.
    # Verify those exact source bank IDs, DIDX entries and bytes before accepting.
    src=selected_source()
    bankpaths={2409073383:'abilities_creatures_media',3535516216:'abilities_republicranged_media',
               1799371716:'abilities_imperialmelee_media',426962037:'weapons_media'}
    bankcache={}; references=[]
    by_id={ob.find('field[@name="ulID"]').get('value'):ob for ob in objects}
    selected_ids={ob.find('field[@name="ulID"]').get('value') for ob in objects if ob.get('index') in selected_indices}
    pending=list(selected_ids)
    while pending:
        current=by_id[pending.pop()]
        parent=current.find('.//field[@name="DirectParentID"]')
        if parent is not None and parent.get('value') in by_id and parent.get('value') not in selected_ids:
            selected_ids.add(parent.get('value')); pending.append(parent.get('value'))
    for ob in objects:
        if ob.get('index') not in selected_indices or ob.get('name')!='CAkSound': continue
        fields={f.get('name'):f.get('value') for f in ob.iter('field')}
        mid=int(fields['sourceID']); bid=int(fields['uFileID'])
        path='/resources/bnk2/'+bankpaths[bid]+'.bnk'
        if bid not in bankcache:
            matches=src.index[f'0x{pathhash(path):016X}']; assert len(matches)==1
            entry=matches[0]; assert entry['compressionMethod']==0
            with open(entry['archive'],'rb') as f:
                base=entry['dataOffset']; f.seek(base); head=f.read(32)
                assert head[:4]==b'BKHD' and struct.unpack_from('<I',head,12)[0]==bid
                cursor=0; index={}; datapos=None
                while cursor+8<=entry['length']:
                    f.seek(base+cursor); tag,n=struct.unpack('<4sI',f.read(8))
                    if tag==b'DIDX':
                        buf=f.read(n)
                        for k in range(0,n,12):
                            m,o,l=struct.unpack_from('<III',buf,k); index[m]=(o,l)
                    if tag==b'DATA': datapos=cursor+8
                    cursor+=8+n
            bankcache[bid]=(entry,index,datapos)
        entry,index,datapos=bankcache[bid]; rel,n=index[mid]
        assert n==int(fields['uInMemoryMediaSize'])
        # v62 HIRC offsets are absolute decoded-bank positions; DIDX is DATA-relative.
        assert rel+datapos==int(fields['uFileOffset']), (mid,rel,datapos,fields['uFileOffset'])
        with open(entry['archive'],'rb') as f:
            f.seek(entry['dataOffset']+datapos+rel); exact=f.read(n)
        record=next(r for r in records if r['mediaId']==mid)
        assert hashlib.sha256(exact).hexdigest()==record['sourceSha256'], 'cross-bank media mismatch'
        references.append({'soundObjectId':int(fields['ulID']),'sourceID':mid,'uFileID':bid,
            'bankPath':path,'bankEntry':entry,'didxDataOffset':datapos,'mediaDataOffset':rel,
            'bytes':n,'sha256':record['sourceSha256'],'duplicateImperialMediaIdentical':True,
            'archiveSha256':src.baseline[entry['archive']]['sha256']})
    evidence=ET.Element('SelectedForceLightningGraph',{'sourceBankSha256':sha(SOURCE/'resources/bnk2/abilities_imperialmelee_data.bnk')})
    for ob in objects:
        identity=ob.find('field[@name="ulID"]')
        if identity is not None and (identity.get('value') in selected_ids or identity.get('value') in ('243915777','151050253','2003520004','302370299')):
            evidence.append(ob)
    ET.indent(evidence)
    ET.ElementTree(evidence).write(ROOT/'manifests/lightning-audio-graph.xml',encoding='utf-8',xml_declaration=True)
    manifest={'events':events,'media':records,'exactHircMediaReferences':references,'mediaBankSha256':sha(bank),'decoder':'vgmstream r2117',
              'mapping':'FNV-1 event -> typed HIRC Event -> Action -> containers -> Sound source ID -> media bank DIDX/DATA',
              'stop':{'eventId':243915777,'actionId':151050253,'targetId':368827915,'transitionMs':500,'curve':'Linear'},
              'limitations':['No native Wwise spatial bus/mixer parity','Godot playlist uses a bounded shuffle cycle; Wwise random weight/history semantics preserved as evidence'],
              'passed':True}
    (ROOT/'manifests/lightning-audio.json').write_text(json.dumps(manifest,indent=2))
    (outdir.parent/'audio.json').write_text(json.dumps(manifest,indent=2))

if __name__=='__main__': main()
