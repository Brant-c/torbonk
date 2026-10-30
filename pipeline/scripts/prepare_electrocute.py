"""Bounded exact-hash April Electrocute recovery; original archives stay read-only."""
import json,re
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE,sha
from lightning_source import decode_xml
from prepare_abilities import unpack

OUT=ROOT/'godot/Assets/Electrocute'
OUT.mkdir(parents=True,exist_ok=True)
character_source.MANIFEST=ROOT/'manifests/electrocute-source.json'
src=character_source.SelectedSource()
src.manifest.update(maximumFiles=160,maximumDecodedBytes=64*1024*1024,selection='April Electrocute cast/stun and dependency closure')
queue=[f'/resources/gamedata/epp/sith_inquisitor/electrocution/{n}.epp' for n in ['cast_instant','cast_loop','stun']]
queue += [f'/resources/art/fx/fxspec/abilities/sith_inquisitor/{n}.fxspec' for n in ['si_electrocute_01','si_electrocute_target_01']]
queue += ['/resources/gfx/icons/electrocute.dds']
seen=set();configs={};fx={};converted=[];missing=[]
while queue:
 p=queue.pop(0)
 if p in seen:continue
 seen.add(p)
 if not src.has(p):missing.append(p);continue
 data=src.get(p,'Electrocute','selected original dependency')
 if p.endswith('.dds'):
  dest=(ROOT/'godot/Assets/UI/icons/electrocute.png') if '/gfx/icons/' in p else OUT/(Path(p).stem+'.png')
  Image.open(SOURCE/p.lstrip('/')).convert('RGBA').save(dest)
  converted.append(dict(source=p,output=str(dest.relative_to(ROOT)),sha256=sha(dest)));continue
 if p.endswith(('.jba','.gr2','.zzp')):continue
 try:
  if p.endswith('.prt'):fields=re.findall(r'^\s*\.(\w+)=(.*)$',data.decode().replace('\r',''),re.M)
  else:
   xml=decode_xml(data);fields=[(f.get('name',f.tag),f.text or '') for f in xml.iter() if len(f)==0]
   if p.endswith('.fxspec'):fx[Path(p).stem]=unpack(xml.find('.//marshalData/node'))
  configs[p]=fields
  for k,v in fields:
   v=v.strip().strip('"').replace('\\','/').lower()
   if v.endswith(('.dds','.prt','.fxspec','.jba')):
    if '/' not in v:v=('art/fx/particles/' if v.endswith('.prt') else 'art/fx/fxspec/' if v.endswith('.fxspec') else 'anim/humanoid/bmnnew/' if v.endswith('.jba') else '')+v
    queue.append('/resources/'+v.lstrip('/'))
 except Exception as e:missing.append(dict(path=p,error=str(e)))
(OUT/'source-parameters.json').write_text(json.dumps(configs,indent=2))
(OUT/'fx.json').write_text(json.dumps(fx,indent=2))
(ROOT/'manifests/electrocute-conversion.json').write_text(json.dumps(dict(converted=converted,unresolved=missing),indent=2))
print('Recovered',len(src.manifest['resources']),'resources; FX',list(fx),'unresolved',missing)
import prepare_ability_audio
events=sorted({n for fields in configs.values() for k,v in fields for n in re.findall(r'(?:Play|Stop)_\w+',v)})
prepare_ability_audio.main('electrocute','Electrocute',events)
audio=json.loads((OUT/'audio.json').read_text())
if audio['missingEvents']:
 import subprocess,sys
 src.get('/resources/bnk2/abilities_general_data.bnk','Electrocute generic cast','April sound event bank')
 bank=SOURCE/'resources/bnk2/abilities_general_data.bnk'
 dump=ROOT/'work/electrocute/general-bank'
 dump.parent.mkdir(parents=True,exist_ok=True)
 subprocess.run([sys.executable,str(ROOT/'tools/wwiser.pyz'),str(bank),'-d','xml','-dn',str(dump)],check=True,stdout=subprocess.DEVNULL)
 code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/electrocute/general-bank.xml'").replace('abilities_imperialmelee','abilities_general')
 code=code.replace("str(SOURCE/'resources/bnk2/abilities_general_media.bnk'),",'')
 code=code.replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
 code=code.replace("names.update(extra_names if extra_names is not None else", "names.intersection_update(extra_names or []); names.update(extra_names if extra_names is not None else")
 ns={'__name__':'electrocute_general'};exec(compile(code,'electrocute_general','exec'),ns)
 ns['main']('electrocute-general','Electrocute',audio['missingEvents'])
 general=json.loads((OUT/'audio.json').read_text())
 audio['events'].update(general['events']);audio['graphs'].update(general['graphs']);audio['media']+=general['media']
 audio['missingEvents']=[n for n in audio['missingEvents'] if audio['events'][n] not in audio['graphs']]
 (OUT/'audio.json').write_text(json.dumps(audio,indent=2))
 (ROOT/'manifests/electrocute-audio.json').write_text(json.dumps(audio,indent=2))
