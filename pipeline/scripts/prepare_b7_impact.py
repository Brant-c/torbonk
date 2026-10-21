import json,struct,subprocess,sys,re,xml.etree.ElementTree as ET,wave
from pathlib import Path
from source_pipeline import ROOT,SOURCE,pathhash,sha
from probe_lightning_audio import fnv
import character_source
character_source.MANIFEST=ROOT/'manifests/b7-source.json';src=character_source.SelectedSource()
p='/resources/bnk2/abilities_creatures_data.bnk';src.get(p,'Burrow Eruption','original generic ground impact events');bank=SOURCE/p.lstrip('/');work=ROOT/'work/b7/creature-events';work.mkdir(exist_ok=True)
names=['Play_ability_creature_generic_spawn_rockfountain','Play_ability_creature_generic_spawnlarge_groundland']
subprocess.run([sys.executable,str(ROOT/'tools/wwiser.pyz'),str(bank),'-d','xml','-dn',str(ROOT/'work/b7/creatures'),'-g','-gf',*[str(fnv(n)) for n in names],'-go',str(work),'-gw','.'],check=True,stdout=subprocess.DEVNULL)
objects=ET.parse(ROOT/'work/b7/creatures.xml').find('.//list[@name="listLoadedItem"]');sel=set();graphs={}
for txtp in work.glob('*.txtp'):
 body=txtp.read_text();eid=re.search(r'CAkEvent\[\d+\] (\d+)',body).group(1);graphs[eid]={'media':[int(x) for x in re.findall(r'#\s+Source (\d+)',body)],'wwiserGraph':body};sel.update(re.findall(r'#\s+CAk\w+\[(\d+)\]',body))
bankpaths={str(fnv(Path(n).stem)):n for n in Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').read_text().splitlines() if '/bnk2/' in n and n.endswith('.bnk')};cache={};records=[]
# One original media variation per selected event keeps this milestone small; full graph retained.
chosen={mid for graph in graphs.values() for mid in graph['media'][:1]}
for o in objects:
 if o.get('name')!='CAkSound' or o.get('index') not in sel:continue
 fields={f.get('name'):f.get('value') for f in o.iter('field')};mid=int(fields['sourceID'])
 if mid not in chosen:continue
 bid=fields['uFileID'];path=bankpaths[bid]
 if bid not in cache:
  e=src.index[f'0x{pathhash(path):016X}'];assert len(e)==1;e=e[0];assert e['compressionMethod']==0;index={};cursor=0;datapos=None
  with open(e['archive'],'rb') as f:
   f.seek(e['dataOffset']);head=f.read(24);assert struct.unpack_from('<I',head,12)[0]==int(bid)
   while cursor+8<=e['length']:
    f.seek(e['dataOffset']+cursor);tag,n=struct.unpack('<4sI',f.read(8))
    if tag==b'DIDX':
     block=f.read(n)
     for p in range(0,n,12):
      m,off,size=struct.unpack_from('<III',block,p);index[m]=(off,size)
    if tag==b'DATA':datapos=cursor+8
    cursor+=8+n
  cache[bid]=(e,index,datapos)
 e,index,datapos=cache[bid];off,size=index[mid];assert off+datapos==int(fields['uFileOffset']) and size==int(fields['uInMemoryMediaSize'])
 with open(e['archive'],'rb') as f:f.seek(e['dataOffset']+datapos+off);data=f.read(size)
 original=SOURCE/f'b7-media/{mid}.wem';original.write_bytes(data);dest=ROOT/f'godot/Assets/Presentation/Audio/{mid}.wav'
 subprocess.run([str(ROOT/'tools/vgmstream/vgmstream-cli.exe'),'-i','-o',str(dest),str(original)],check=True,stdout=subprocess.DEVNULL)
 with wave.open(str(dest)) as w:duration=w.getnframes()/w.getframerate()
 records.append({'mediaId':mid,'bankPath':path,'entry':e,'bankDecodedOffset':off+datapos,'bytes':size,'sourceSha256':sha(original),'sha256':sha(dest),'seconds':duration,'output':str(dest.relative_to(ROOT))});print(mid,duration)
result={'events':{n:fnv(n) for n in names},'graphs':graphs,'media':records,'limits':'Semantic reuse for authored Burrow Eruption; original generic rock spawn/land cues, not a native Broodmatron attack binding.'}
(ROOT/'manifests/b7-impact-audio.json').write_text(json.dumps(result,indent=2));print('EVENTS',[(n,graphs.get(str(fnv(n)),{}).get('media',[])[:1]) for n in names])

