import json,struct,re,zlib
from pathlib import Path
from source_pipeline import ROOT,pathhash
from probe_lightning_audio import fnv
r=json.loads((ROOT/'work/b7/research.json').read_text())
for p,f in r['fx'].items():
 print(p);print([(k,v) for k,v in f if v and (k in ('_fxResourceName','_fxTextureName') or v.endswith(('.prt','.dds','.gr2')))])
names=Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').read_text().splitlines()
hints={pathhash(n):n for n in names if n.endswith('.bnk')}
discovery=json.loads((ROOT/'manifests/lightning-audio-discovery.json').read_text())
print('BANK CANDIDATES',[(hints.get(int(b['entry']['archiveHash'],16)),b['bankId']) for b in discovery['banks'] if any(k in hints.get(int(b['entry']['archiveHash'],16),'').lower() for k in ['gui','interface','general','global','init','frontend'])])
events=['Play_gui_state_levelup_core','Play_gui_state_levelup_imperialprobes_in','Play_gui_state_levelup_loop','Play_gui_state_levelup','Play_gui_inventory_item_pickup','Play_gui_mouseover','Play_gui_button_click']
needles={fnv(n):n for n in events};found=[]
for b in discovery['banks']:
 e=b['entry']
 if not any(c['tag']=='HIRC' for c in b['chunks']):continue
 with open(e['archive'],'rb') as f:f.seek(e['dataOffset']);raw=f.read(e['compressedSize'])
 data=zlib.decompress(raw) if e['compressionMethod'] else raw
 for chunk in b['chunks']:
  if chunk['tag']!='HIRC':continue
  p=chunk['offset']+4
  for _ in range(struct.unpack_from('<I',data,chunk['offset'])[0]):
   t=data[p];n=struct.unpack_from('<I',data,p+1)[0];oid=struct.unpack_from('<I',data,p+5)[0]
   if t==4 and oid in needles:found.append({'name':needles[oid],'entry':e,'bankPath':hints.get(int(e['archiveHash'],16)),'eventId':oid})
   p+=5+n
(ROOT/'work/b7/event-discovery.json').write_text(json.dumps(found,indent=2));print('EVENTS',[(x['name'],x['bankPath']) for x in found])
