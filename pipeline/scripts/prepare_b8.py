"""Bounded B8 dependency traversal using the existing TOR/XML/DDS readers."""
import json,re
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE,sha
from lightning_source import decode_xml
from prepare_abilities import unpack

OUT=ROOT/'godot/Assets/B8'
WORK=ROOT/'work/b8'
CLIPS={'DeathField':'cb_buff_consumption_exit','Barrier':'cb_buff_unleash','DarkHeal':'cb_buff_consumption_enter',
       'DarkHealLoop':'cb_buff_consumption_loop','DarkHealEnd':'fp_1h_elect_instant_right_4'}
def main():
    OUT.mkdir(parents=True,exist_ok=True);WORK.mkdir(parents=True,exist_ok=True)
    character_source.MANIFEST=ROOT/'manifests/b8-source.json'
    if not character_source.MANIFEST.exists():
        character_source.MANIFEST.write_text(json.dumps(dict(profile='April2012 (operator labelled)',maximumFiles=140,
            maximumDecodedBytes=60*1024*1024,resources=[],dependencies=[],unresolved=[],selection='B8 Death Field / Static Barrier / Dark Heal')))
    src=character_source.SelectedSource();conversions=[]
    for name,clip in CLIPS.items():src.get(f'/resources/anim/humanoid/bmnnew/{clip}.jba',name,'April appearance PlayAnim')
    for icon in ['deathfield','electricshield','darkgift']:
        path=f'/resources/gfx/icons/{icon}.dds';src.get(path,icon,'April ablIconSpec')
        dest=ROOT/f'godot/Assets/UI/icons/{icon}.png'
        if not dest.exists():Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(dest)
        conversions.append(dict(source=path,output=str(dest.relative_to(ROOT)),sha256=sha(dest)))
    queue=['/resources/art/shaders/materials/fx_si_backlash_partialshieldcentered.mat',
        '/resources/art/shaders/materials/fx_si_death_field.mat']+['/resources/art/fx/fxspec/'+p+'.fxspec' for p in [
        'abilities/sith_inquisitor/si_death_field_hand_cast','abilities/sith_inquisitor/si_death_field_aoe',
        'abilities/sith_inquisitor/si_death_field_cast','abilities/sith_inquisitor/si_death_field_impact_target',
        'abilities/sith_inquisitor/si_backlash_caster_shield','abilities/sith_inquisitor/si_backlash_caster_tick',
        'si_darkgift_caster_01','si_darkgift_target_01']]
    seen=set();configs={};fx={}
    while queue:
        path=queue.pop(0)
        if path in seen:continue
        seen.add(path)
        if not src.has(path):
            src.manifest['unresolved'].append(dict(path=path,status='No unique April entry'));continue
        data=src.get(path,'B8 selected dependency traversal','FXSpec / particle / material dependency')
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
    (ROOT/'manifests/b8-conversion.json').write_text(json.dumps(dict(clips=CLIPS,files=conversions),indent=2))
    print(json.dumps(dict(resources=len(src.manifest['resources']),textures=len(conversions),unresolved=src.manifest['unresolved'])))
if __name__=='__main__':main()
