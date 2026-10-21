"""Bounded April PvP UI recovery; data-only GFX parsing and read-only TOR access."""
from pathlib import Path
import sys,struct,zlib,json,hashlib
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT.parent/'character-selection/tools'))
from tor_reader import entries,pathhash
from ui_formats import tags
ARCHIVE=Path('D:/SWTORClassic/swtoremu/Assets2012April/swtor_main_gfx_1.tor')
INDEX={}
for e in entries(ARCHIVE): INDEX.setdefault(e['archiveHash'],[]).append(e)
MANIFEST=ROOT/'manifests/b101-pvp-source.json'
receipt=json.loads(MANIFEST.read_text()) if MANIFEST.exists() else {'profile':'operator-labelled April2012','resources':[],'definitions':{},'conversions':[]}
def get(path):
 found=INDEX.get(f'0x{pathhash(path):016X}',[])
 if len(found)!=1: print('MISSING',path,len(found)); return None
 e=found[0]; assert e['length']<8*1024*1024
 with ARCHIVE.open('rb') as f: f.seek(e['dataOffset']); b=f.read(e['compressedSize'])
 if e['compressionMethod']==1: b=zlib.decompress(b)
 else: assert e['compressionMethod']==0
 assert len(b)==e['length']
 out=ROOT/'source-assets/april2012'/path.lstrip('/'); out.parent.mkdir(parents=True,exist_ok=True)
 if out.exists(): assert out.read_bytes()==b
 else: out.write_bytes(b)
 if not any(r['path']==path for r in receipt['resources']):receipt['resources'].append(dict(e,path=path,sha256=hashlib.sha256(b).hexdigest(),output=str(out.relative_to(ROOT))))
 return b
for name in sys.argv[1:] or ['warzone_alderaan_overlay','warzone_voidstar_overlay','bwaui_scoreboard']:
 path='/resources/gfx/gfx_production/'+name+'.gfx'; b=get(path)
 if b is None:continue
 images={};subs=[];imports=[];exports=[]
 for tag,d in tags(b):
  if tag in (57,71):imports.append(d.split(b'\0')[0].decode(errors='replace'))
  if tag==1009:
   iid=struct.unpack_from('<H',d)[0]; n=d[11]; images[str(iid)]=d[12:12+n].rstrip(b'\0').decode()
  if tag==1008:subs.append(list(struct.unpack('<6H',d)))
  if tag==56:
   pos=2
   for _ in range(struct.unpack_from('<H',d)[0]):
    symbol=struct.unpack_from('<H',d,pos)[0];pos+=2;end=d.index(b'\0',pos);exports.append([symbol,d[pos:end].decode()]);pos=end+1
 record=dict(images=images,subimages=subs,imports=imports,exports=exports);receipt['definitions'][name]=record
 print(name,json.dumps(record))
MANIFEST.write_text(json.dumps(receipt,indent=2),encoding='utf-8')
