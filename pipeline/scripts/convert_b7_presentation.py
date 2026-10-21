import json,struct,re,subprocess,sys,wave,hashlib,xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image
from source_pipeline import ROOT,SOURCE,sha
from probe_lightning_audio import fnv
from lightning_source import decode_xml
import character_source
character_source.MANIFEST=ROOT/'manifests/b7-source.json';src=character_source.SelectedSource();out=ROOT/'godot/Assets/Presentation'
for name,path in [('loot_glow','/resources/art/fx/texture/glow/bw_flare_gray.dds'),('loot_ring','/resources/art/fx/texture/symbol/tri_ring_blurred.dds')]:
 src.get(path,'loot_common.fxspec / loot_core_glow_p.prt','original loot texture binding');Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(out/(name+'.png'))
path='/resources/art/fx/fxspec/creatures/generic_impacts/cr_generic_aoe_ground_pound.fxspec'
if src.has(path):
 data=src.get(path,'B7 Burrow Eruption','source impact candidate');fields=[(f.get('name'),f.text) for f in decode_xml(data).iter('f')];(ROOT/'work/b7/ground-pound.json').write_text(json.dumps(fields,indent=2));print('GROUND AUDIO',[(k,v) for k,v in fields if v and 'Play_' in v])
bank=SOURCE/'audio-by-hash/b7-gui-d634b5f5260713cd.bnk';data=bank.read_bytes();chunks={};p=0
while p+8<len(data):
 tag,n=struct.unpack_from('<4sI',data,p);chunks[tag]=(p+8,n);p+=8+n
pos,n=chunks[b'DIDX'];idx={}
for p in range(pos,pos+n,12):
 mid,off,size=struct.unpack_from('<III',data,p);idx[mid]=(off,size)
graphs={};selected=set()
for txtp in (ROOT/'work/b7/events').glob('*.txtp'):
 body=txtp.read_text();eid=re.search(r'CAkEvent\[\d+\] (\d+)',body).group(1);mids=[int(x) for x in re.findall(r'##(\d+)\.wem',body)];graphs[eid]={'media':mids,'wwiserGraph':body};selected.update(mids)
audio=out/'Audio';audio.mkdir(exist_ok=True);originals=SOURCE/'b7-media';originals.mkdir(exist_ok=True);records=[]
for mid in sorted(selected):
 off,size=idx[mid];start=chunks[b'DATA'][0]+off;wem=data[start:start+size];original=originals/f'{mid}.wem';original.write_bytes(wem);dest=audio/f'{mid}.wav'
 subprocess.run([str(ROOT/'tools/vgmstream/vgmstream-cli.exe'),'-i','-o',str(dest),str(original)],check=True,stdout=subprocess.DEVNULL)
 with wave.open(str(dest)) as w:duration=w.getnframes()/w.getframerate()
 records.append({'mediaId':mid,'bankDecodedOffset':start,'bytes':size,'sourceSha256':sha(original),'output':str(dest.relative_to(ROOT)),'sha256':sha(dest),'seconds':duration});print(mid,duration)
 dest.with_suffix('.wav.import').write_text('[remap]\nimporter="wav"\ntype="AudioStreamWAV"\n\n[params]\ncompress/mode=0\nedit/trim=false\nedit/normalize=false\n')
result={'bank':'manifests/b7-gui-bank.json','events':json.loads((ROOT/'work/b7/gui-events.json').read_text()),'graphs':graphs,'media':records,'limits':['Exact original PCM; runtime gains, selected one-shot durations and semantic reuse are authored.','Pickup event name is a candidate verified against a typed HIRC event hash; level events are additionally referenced by original FXSpec.','No original UI hover/upgrade/boss event binding asserted.']}
(ROOT/'manifests/b7-audio.json').write_text(json.dumps(result,indent=2));(out/'audio.json').write_text(json.dumps(result,indent=2))
