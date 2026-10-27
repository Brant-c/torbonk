"""Bounded Sorcerer dependency traversal using the existing TOR/XML/DDS readers."""
import json,re
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE,sha
from lightning_source import decode_xml
from prepare_abilities import unpack

OUT=ROOT/'godot/Assets/SorcererRefinement'
WORK=ROOT/'work/t22'
CLIPS={'Strike':'fp_1h_elect_instant_right_4','Darkness':'fp_1h_elect_instant_right_2','Polarity':'cb_buff_consumption_exit'}

def main():
    OUT.mkdir(parents=True,exist_ok=True);WORK.mkdir(parents=True,exist_ok=True)
    character_source.MANIFEST=ROOT/'manifests/t22-source.json'
    if not character_source.MANIFEST.exists():
        character_source.MANIFEST.write_text(json.dumps(dict(profile='April2012 (operator labelled)',maximumFiles=240,
            maximumDecodedBytes=60*1024*1024,resources=[],dependencies=[],unresolved=[],selection='T2.2 Lightning Strike, Crushing Darkness, Polarity Shift and Force Speed')))
    src=character_source.SelectedSource();conversions=[]
    src.manifest['selection']='T2.2 Lightning Strike, Crushing Darkness, Polarity Shift and Force Speed'
    src.manifest['unresolved']=[]
    for name,clip in CLIPS.items():src.get(f'/resources/anim/humanoid/bmnnew/{clip}.jba',name,'April appearance semantic gesture; matching original electrical JBA adapter')
    for icon in ['lightningstrike','dark_embrace','polaritycharge','forcespeed']:
        path=f'/resources/gfx/icons/{icon}.dds'
        if src.has(path):
            src.get(path,icon,'April Sorcerer ablIconSpec artwork')
            Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(ROOT/f'godot/Assets/UI/icons/{icon}.png')
    queue=['/resources/art/fx/fxspec/'+f+'.fxspec' for f in [
        'abilities/sith_inquisitor/si_lightning_strike_caster_hand_loop',
        'si_lightning_strike_target_01',
        'abilities/sith_inquisitor/si_consumingdarkness_cast',
        'si_darkaffliction_target',
        'abilities/sith_inquisitor/si_lightningdrain_bodyarcs',
        'abilities/sith_inquisitor/si_crushing_darkness_hand_cast',
        'abilities/sith_inquisitor/si_crushing_darkness_cast',
        'abilities/sith_inquisitor/si_crushing_darkness',
        'abilities/sith_inquisitor/si_forcespeed_01',
        'abilities/sith_inquisitor/si_polaritycharge_cast_01']]
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
    (ROOT/'manifests/t22-conversion.json').write_text(json.dumps(dict(clips=CLIPS,files=conversions),indent=2))
    print(json.dumps(dict(resources=len(src.manifest['resources']),textures=len(conversions),unresolved=src.manifest['unresolved'])))
if __name__=='__main__':
    main()
    import prepare_ability_audio
    prepare_ability_audio.main('t22','SorcererRefinement',['Play_ability_sithinquisitor_shock_buildup','Play_ability_sithinquisitor_crushingdarkness_cast','Play_ability_sithinquisitor_forcespeed_quickcharge','Play_ability_sithinquisitor_polaritycharge_subtlemediumgrip'])



