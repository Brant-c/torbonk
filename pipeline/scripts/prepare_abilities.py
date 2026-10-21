"""B5 bounded dependency extraction; uses the established read-only TOR pipeline."""
import json, re
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT, SOURCE, sha
from lightning_source import decode_xml

character_source.MANIFEST=ROOT/'manifests/abilities-source.json'
OUT=ROOT/'godot/Assets/Abilities'
WORK=ROOT/'work/abilities'

def unpack(element):
    if len(element)==0:return element.text or ''
    if all(c.tag=='e' for c in element):return [unpack(c) for c in element]
    return {c.get('name',c.tag):unpack(c) for c in element}

def main():
    OUT.mkdir(parents=True,exist_ok=True); WORK.mkdir(parents=True,exist_ok=True)
    if not character_source.MANIFEST.exists():
        character_source.MANIFEST.write_text(json.dumps(dict(profile='April2012 (operator labelled)',
            maximumFiles=160,maximumDecodedBytes=100*1024*1024,resources=[],dependencies=[],unresolved=[],selection='B5 Shock and Force Storm')))
    src=character_source.SelectedSource()
    src.manifest['unresolved']=[]
    clips={'Shock':'fp_1h_elect_instant_right_1','StormStart':'fp_lightning_storm_start',
           'StormLoop':'fp_lightning_storm_loop','StormEnd':'fp_lightning_storm_end'}
    for name,clip in clips.items(): src.get(f'/resources/anim/humanoid/bmnnew/{clip}.jba',name,'April appearance PlayAnim')
    conversions=[]
    for icon in ['shock','forcestorm']:
        path=f'/resources/gfx/icons/{icon}.dds'; src.get(path,icon,'April ablIconSpec')
        original=SOURCE/path.lstrip('/'); dest=ROOT/f'godot/Assets/UI/icons/{icon}.png'
        Image.open(original).convert('RGBA').save(dest)
        conversions.append(dict(source=path,output=str(dest.relative_to(ROOT)),sha256=sha(dest)))
    queue=['/resources/art/fx/fxspec/'+p+'.fxspec' for p in ['si_shock_01','si_shock_target_01',
        'abilities/sith_inquisitor/si_forcestorm_cast','abilities/sith_inquisitor/si_forcestorm_targetarea',
        'abilities/all/all_imp_target_reticle_gtaoe_persist']]
    seen=set(); configs={}; fx={}
    while queue:
        path=queue.pop(0)
        if path in seen: continue
        seen.add(path)
        if not src.has(path):
            src.manifest['unresolved'].append(dict(path=path,status='No unique April hash entry')); continue
        data=src.get(path,'B5 dependency traversal','FXSpec / particle / material dependency')
        if path.endswith('.dds'):
            dest=OUT/(Path(path).stem+'.png'); Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(dest)
            conversions.append(dict(source=path,output=str(dest.relative_to(ROOT)),sha256=sha(dest))); continue
        if path.endswith(('.gr2','.jba')): continue
        if path.endswith('.prt'):
            fields=re.findall(r'^\s*\.(\w+)=(.*)$',data.decode().replace('\r',''),re.M)
        else:
            xml=decode_xml(data); fields=[(f.get('name',f.tag),f.text or '') for f in xml.iter() if len(f)==0]
            if path.endswith('.fxspec'):
                fx[Path(path).stem]=unpack(xml.find('.//marshalData/node'))
            (WORK/(Path(path).name+'.xml')).write_text(__import__('xml.etree.ElementTree',fromlist=['tostring']).tostring(xml,encoding='unicode'),encoding='utf-8')
        configs[path]=fields
        for field,value in fields:
            value=value.strip().strip('"').replace('\\','/').lower()
            if value.endswith(('.dds','.gr2','.prt','.mat','.fxspec','.lit','.zzp')):
                target='/resources/'+value.lstrip('/')
                if '/' not in value and value.endswith('.prt'): target='/resources/art/fx/particles/'+value
                if '/' not in value and value.endswith('.fxspec'): target='/resources/art/fx/fxspec/'+value
                if src.has(target):
                    src.get(target,path,field); queue.append(target)
                else:
                    row=dict(source=path,field=field,target=target,status='Engine primitive' if '/engine/' in target else 'Missing unique April entry')
                    if row not in src.manifest['unresolved']: src.manifest['unresolved'].append(row)
    character_source.MANIFEST.write_text(json.dumps(src.manifest,indent=2))
    (OUT/'source-parameters.json').write_text(json.dumps(configs,indent=2))
    (OUT/'fx.json').write_text(json.dumps(fx,indent=2))
    (ROOT/'manifests/abilities-conversion.json').write_text(json.dumps(dict(clips=clips,files=conversions,parameters=configs),indent=2))
    print(json.dumps(dict(resources=len(src.manifest['resources']),conversions=len(conversions),unresolved=src.manifest['unresolved'])))

if __name__=='__main__': main()
