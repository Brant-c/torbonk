import json,struct,zlib
from source_pipeline import ROOT,ARCHIVES,entries
from probe_lightning_audio import fnv
names=['Play_gui_state_levelup_core','Play_gui_state_levelup_imperialprobes_in','Play_gui_state_levelup_loop']
needles={fnv(n):n for n in names};matches=[];scanned=[]
for archive in ARCHIVES.glob('*bnk*.tor'):
 for e in entries(archive):
  if e['length']<=4*1024*1024 or e['compressionMethod']!=0:continue
  with open(archive,'rb') as f:
   base=e['dataOffset'];f.seek(base);head=f.read(24)
   if head[:4]!=b'BKHD':continue
   cursor=0;bankid=struct.unpack_from('<I',head,12)[0];events=[]
   while cursor+8<=e['length']:
    f.seek(base+cursor);tag,n=struct.unpack('<4sI',f.read(8))
    if tag==b'HIRC':
     assert n<8*1024*1024
     data=f.read(n);q=4
     for _ in range(struct.unpack_from('<I',data)[0]):
      typ=data[q];sz=struct.unpack_from('<I',data,q+1)[0];oid=struct.unpack_from('<I',data,q+5)[0]
      if typ==4 and oid in needles:events.append({'name':needles[oid],'id':oid})
      q+=5+sz
    cursor+=8+n
   scanned.append({'entry':e,'bankId':bankid})
   if events:matches.append({'entry':e,'bankId':bankid,'events':events})
(ROOT/'work/b7/large-bank-discovery.json').write_text(json.dumps({'banks':scanned,'matches':matches},indent=2));print('Metadata-only large banks',len(scanned),'matches',matches)
