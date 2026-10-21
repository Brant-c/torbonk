import json,xml.etree.ElementTree as ET,itertools
from source_pipeline import ROOT
from probe_lightning_audio import fnv
root=ET.parse(ROOT/'work/b7/gui.xml');obs=root.find('.//list[@name="listLoadedItem"]');ids={int(o.find('field[@name="ulID"]').get('value')) for o in obs if o.get('name')=='CAkEvent'}
words=['button','buttons','mouseover','hover','click','accept','cancel','select','selection','confirm','reward','achievement','mission','quest','item','inventory','loot','pickup','rollover','mouse','notification','alert','warning','combat','levelup','open','close','error','skill','skilltree','ability','unlock','purchase','default','generic','standard','positive','negative','enter','exit','popup','complete','victory','defeat','over','down','up']
found={}
for n in [f'Play_gui_{a}' for a in words]+[f'Play_gui_{a}_{b}' for a,b in itertools.product(words,repeat=2)]+[f'Play_gui_{a}_{b}_{c}' for a,b,c in itertools.product(['state','action','button','inventory','skilltree','mouse','event','generic'],words,words)]:
 if fnv(n) in ids:found[n]=fnv(n)
r=json.loads((ROOT/'work/b7/research.json').read_text())
for f in r['fx'].values():
 for k,v in f:
  if v:
   for n in v.split(','):
    if n.startswith('Play_') and fnv(n) in ids:found[n]=fnv(n)
(ROOT/'work/b7/gui-events.json').write_text(json.dumps(found,indent=2));print(found)
