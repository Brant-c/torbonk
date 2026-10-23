"""Bounded D2 dependency traversal using the existing TOR/XML/DDS readers."""
import json,re
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE,sha
from lightning_source import decode_xml
from prepare_abilities import unpack

OUT=ROOT/'godot/Assets/Powertech'
WORK=ROOT/'work/powertech'
CLIPS={}

def main():
    OUT.mkdir(parents=True,exist_ok=True);WORK.mkdir(parents=True,exist_ok=True)
    character_source.MANIFEST=ROOT/'manifests/powertech-source.json'
    if not character_source.MANIFEST.exists():
        character_source.MANIFEST.write_text(json.dumps(dict(profile='April2012 (operator labelled)',maximumFiles=240,
            maximumDecodedBytes=60*1024*1024,resources=[],dependencies=[],unresolved=[],selection='D2 Chain Lightning / Overload / run item icons')))
    src=character_source.SelectedSource();conversions=[]
    for name,clip in CLIPS.items():src.get(f'/resources/anim/humanoid/bmnnew/{clip}.jba',name,'April appearance semantic gesture; matching original electrical JBA adapter')
    for icon in ['carbonize','oilslick','rocketpunch','flamethrower','incendia']:
        path=f'/resources/gfx/icons/{icon}.dds'
        if src.has(path):
            src.get(path,icon,'Powertech original artwork')
            Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(ROOT/f'godot/Assets/UI/icons/{icon}.png')
    queue=['/resources/art/fx/fxspec/abilities/bounty_hunter/'+p+'.fxspec' for p in ['bh_carbonize_target','bh_carbonize_cast','bh_incendiary_missile_target','bh_oilslick_puddle','bh_oilslick_target','bh_flamethrower','bh_flamethrower_lod_02','bh_rocketpunch_01','bh_rocketpunch_impact','bh_flame_burst_cast','bh_bracerweapon_flameburst_01']]
    seen=set();configs={};fx={}
    while queue:
        path=queue.pop(0)
        if path in seen:continue
        seen.add(path)
        if not src.has(path):
            src.manifest['unresolved'].append(dict(path=path,status='No unique April entry'));continue
        data=src.get(path,'D2 selected dependency traversal','FXSpec / particle / material dependency')
        if path.endswith('.dds'):
            dest=OUT/(Path(path).stem+'.png')
            if not dest.exists():Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(dest)
            conversions.append(dict(source=path,output=str(dest.relative_to(ROOT)),sha256=sha(dest)));continue
        if path.endswith(('.gr2','.jba')):continue
        if path.endswith('.prt'):fields=re.findall(r'^\s*\.(\w+)=(.*)$',data.decode().replace('\r',''),re.M)
        else:
            xml=decode_xml(data);fields=[(f.get('name',f.tag),f.text or '') for f in xml.iter() if len(f)==0]
            if path.endswith('.mat'):
                fields += [(n.findtext('semantic'),n.findtext('value').replace('\\','/')+'.dds') for n in xml.findall('input') if n.findtext('type')=='texture']
            if path.endswith('.fxspec'):fx[Path(path).stem]=unpack(xml.find('.//marshalData/node'))
        configs[path]=fields
        for field,value in fields:
            value=value.strip().strip('"').replace('\\','/').lower()
            if value.endswith(('.dds','.gr2','.prt','.mat','.fxspec','.lit','.zzp')):
                target='/resources/'+value.lstrip('/')
                if '/' not in value and value.endswith('.prt'):target='/resources/art/fx/particles/'+value
                if '/' not in value and value.endswith('.fxspec'):target='/resources/art/fx/fxspec/'+value
                if src.has(target):
                    src.get(target,path,field);queue.append(target)
                else:
                    row=dict(source=path,field=field,target=target,status='Engine primitive' if '/engine/' in target else 'Missing unique April entry')
                    if row not in src.manifest['unresolved']:src.manifest['unresolved'].append(row)
    character_source.MANIFEST.write_text(json.dumps(src.manifest,indent=2))
    (OUT/'source-parameters.json').write_text(json.dumps(configs,indent=2));(OUT/'fx.json').write_text(json.dumps(fx,indent=2))
    (ROOT/'manifests/powertech-conversion.json').write_text(json.dumps(dict(clips=CLIPS,files=conversions),indent=2))
    print(json.dumps(dict(resources=len(src.manifest['resources']),textures=len(conversions),unresolved=src.manifest['unresolved'])))
if __name__=='__main__':main()



