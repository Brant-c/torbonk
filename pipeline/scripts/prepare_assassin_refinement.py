"""Recover exact April Assassin icon mappings and bounded appearance dependencies."""
import json,re
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE,sha
from lightning_source import decode_xml
from prepare_abilities import unpack
OUT=ROOT/'godot/Assets/Assassin';OUT.mkdir(exist_ok=True)
character_source.MANIFEST=ROOT/'manifests/assassin-refinement-source.json'
src=character_source.SelectedSource();src.manifest.update(maximumFiles=450,maximumDecodedBytes=100*1024*1024,selection='Assassin exact ablIconSpec and appearance-linked FX')
icons=json.loads((ROOT/'manifests/assassin-refinement-icon-mappings.json').read_text())+json.loads((ROOT/'manifests/assassin-refinement-extra-icons.json').read_text());converted=[]
for r in icons:
 icon=r['icon'].lower();path=f'/resources/gfx/icons/{icon}.dds';src.get(path,r['name'],'decoded April ablIconSpec');dest=ROOT/f'godot/Assets/UI/icons/{icon}.png';Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(dest);converted.append(dict(**r,output=str(dest.relative_to(ROOT)),sha256=sha(dest)))
# Inspect the original pole action provider used by Spike's semantic animation.
p='/resources/anim/humanoid/bmnnew/humanoid_action_combat_pole.mph.amx'
data=src.get(p,'SaberSpecial3Attack1','pole action provider')
print('PROVIDER',data[:80],len(data))
params=json.loads((ROOT/'manifests/assassin-refinement-appearance-evidence.json').read_text())['params']
roots={c['value'] for d in params for c in d['children'] if c['key'].endswith(':21') and c['value'] and not c['value'].startswith('audio_')}
roots.update(['abilities/sith_inquisitor/si_reflection_buff_loop','abilities/sith_inquisitor/si_mindtrick_loop_01','abilities/jedi_consular/jc_lowslash_impact_01'])
queue=['/resources/art/fx/fxspec/'+s+'.fxspec' for s in roots];seen=set();configs={};fx={};missing=[]
while queue:
 path=queue.pop(0)
 if path in seen:continue
 seen.add(path)
 if not src.has(path):missing.append(path);continue
 data=src.get(path,'Assassin appearance dependencies','original FX dependency')
 if path.endswith('.dds'):
  dest=OUT/(Path(path).stem+'.png');Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(dest);converted.append(dict(source=path,output=str(dest.relative_to(ROOT)),sha256=sha(dest)));continue
 if path.endswith(('.gr2','.jba','.mph')):continue
 try:
  if path.endswith('.prt'):fields=re.findall(r'^\s*\.(\w+)=(.*)$',data.decode().replace('\r',''),re.M)
  else:
   xml=decode_xml(data);fields=[(f.get('name',f.tag),f.text or '') for f in xml.iter() if len(f)==0]
   if path.endswith('.mat'):fields += [(n.findtext('semantic'),n.findtext('value').replace('\\','/')+'.dds') for n in xml.findall('input') if n.findtext('type')=='texture']
   if path.endswith('.fxspec'):fx[Path(path).stem]=unpack(xml.find('.//marshalData/node'))
  configs[path]=fields
  for field,value in fields:
   value=value.strip().strip('"').replace('\\','/').lower()
   if value.endswith(('.dds','.gr2','.prt','.mat','.fxspec','.lit','.zzp')):
    target='/resources/'+value.lstrip('/')
    if '/' not in value and value.endswith('.prt'):target='/resources/art/fx/particles/'+value
    if '/' not in value and value.endswith('.fxspec'):target='/resources/art/fx/fxspec/'+value
    if src.has(target):queue.append(target)
    elif '/engine/' not in target:missing.append(target)
 except Exception as e: print('DECODE',path,str(e))
(OUT/'source-parameters.json').write_text(json.dumps(configs,indent=2));(OUT/'fx.json').write_text(json.dumps(fx,indent=2))
(ROOT/'manifests/assassin-refinement-conversion.json').write_text(json.dumps(dict(files=converted,unresolved=sorted(set(missing))),indent=2))
print('RESOURCES',len(src.manifest['resources']),'FX',list(fx),'TEXTURES',len(converted),'MISSING',sorted(set(missing)))

clips={'Maul':'cb_saber_sp1_attack_right_5','VoltaicSlash':'cb_pole_throw_alternate_1','Spike':'cb_saber_sp3_attack_right_1','LowSlash':'cb_pole_sp1_attack_right_5','Deflection':'cb_buff_pole_feint_1','Wither':'fp_1h_elect_instant_right_4'}
for name,clip in clips.items():src.get(f'/resources/anim/humanoid/bmnnew/{clip}.jba',name,'April EPP clip; Spike Sp3 and LowSlash pole adapter are semantic selections')
(ROOT/'manifests/assassin-refinement-conversion.json').write_text(json.dumps(dict(clips=clips,files=converted,unresolved=sorted(set(missing))),indent=2))
import prepare_ability_audio
prepare_ability_audio.main('assassin-refinement','Assassin',[])
