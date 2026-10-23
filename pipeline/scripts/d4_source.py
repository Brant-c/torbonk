"""Bounded D4 selection. Exact April paths; archives remain read-only."""
import json, struct, sys, xml.etree.ElementTree as ET
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import character_source
from source_pipeline import ROOT, SOURCE

def source():
    character_source.MANIFEST = ROOT/'manifests/d4-source.json'
    if not character_source.MANIFEST.exists():
        character_source.MANIFEST.write_text(json.dumps(dict(profile='April2012 (operator labelled)', maximumFiles=430,
            maximumDecodedBytes=320*1024*1024,resources=[],dependencies=[],unresolved=[],selection='D4 selected Korriban rigs, burial props and adjacent Valley sectors')))
    return character_source.SelectedSource()

def canon(p): return '/resources/'+p.replace('\\','/').lower().lstrip('/').removeprefix('resources/')

def enemies():
    src=source()
    def material(path):
        path=canon(path); x=ET.fromstring(src.get(path)); textures={}
        for i in x.findall('.//input'):
            v=i.findtext('value') or ''
            if i.findtext('type')=='texture' and v.strip('/') and not v.startswith('#'):
                p=canon(v)
                if not p.endswith('.dds'):p+='.dds'
                src.get(p,path,i.findtext('semantic'));textures[i.findtext('semantic')]=p
        return dict(path=path,derived=x.findtext('Derived'),textures=textures)
    reg=ET.fromstring(src.get('/resources/art/dynamic/creature/index.xml'))
    recipe={}
    for name,model,mat,rig,anim in [('tukata','1000123','1001442','dog','dog'),
                                  ('protector','1002841','1002844','assassin','assassin'),
                                  ('beast','1000158','1001462','terentatek','terentatek'),
                                  ('broodmother','1006617','1006618','klorslug','slug')]:
        entry=next(a for a in reg if a.findtext('ID')==model)
        m=next(a for a in entry.findall('Materials/Material') if a.get('id')==mat)
        models=[canon(entry.findtext('BaseFile'))]
        for p in models:src.get(p,'D4 '+name,'April appearance registry')
        part=dict(slot='creature',modelId=model,materialId=mat,models=models,material=material(m.get('filename')),
                  overrides=[dict(index=int(o.get('index')),material=material(o.get('filename'))) for o in m.findall('MaterialOverrides/MaterialOverride')])
        skeleton=f'/resources/art/dynamic/spec/{rig}_skeleton.gr2'
        if rig=='assassin':
            src.get('/resources/art/dynamic/spec/assassin.dyc',name,'declared skeleton BMN')
            skeleton='/resources/art/dynamic/spec/bmnnew_skeleton.gr2'
        src.get(skeleton,name,'original skeleton')
        clips={}
        for label,stems in [('Idle',['cb_ready_1','cb_idle_1']),('Run',['mv_run_forward','mv_run_forward_01']),
                            ('Attack',['cb_attack_1','cb_attack_right_1']),('Hit',['dg_damage_1']),('Death',['dg_death_1'])]:
            group='humanoid' if rig=='assassin' else 'creature'
            if rig=='assassin':
                stems={'Idle':['cb_2pistol_2h_idle_1','cb_pistol_idle_1'],'Run':['mv_2pistol_run_forward','mv_cb_pistol_run_forward'],'Attack':['cb_2pistol_2h_fire_big_1','cb_pistol_fire_1'],
                       'Hit':['dg_unarmed_crit_damage_right_1','dg_damage_1'],'Death':['dg_death_1']}[label]
            paths=[f'/resources/anim/{group}/{"bmnnew" if rig=="assassin" else anim}/{s}.jba' for s in stems]
            found=next((p for p in paths if src.has(p)),None)
            if not found: raise RuntimeError((name,label,'No compatible original clip',paths))
            src.get(found,name,label);clips[label]=found
        recipe[name]=dict(skeleton=skeleton,parts=[part],clips=clips)
        print(name,models,clips)
    recipe['weapons']={}
    (ROOT/'d4-enemies-recipe.json').write_text(json.dumps(recipe,indent=2))

def environment(stage):
    # Reuse the current map implementation through isolated paths; no other map recipe/output is touched.
    import maps_common, runpy
    maps_common.map_id=lambda:'korriban-d4'
    maps_common.recipe=lambda:json.loads((ROOT/'d4-environment-recipe.json').read_text())
    maps_common.work=lambda:ROOT/'work/d4/environment'
    maps_common.asset_dir=lambda:ROOT/'godot/Assets/Korriban/D4'
    maps_common.manifest=lambda name:ROOT/f'manifests/d4-environment-{name}.json'
    maps_common.source=source
    maps_common.work().mkdir(parents=True,exist_ok=True);maps_common.asset_dir().mkdir(parents=True,exist_ok=True)
    if stage=='prepare':
        runpy.run_path(str(ROOT/'scripts/map_prepare.py'),run_name='__main__')
        # Animated monitor overlays belong to the adjacent camp, not the tomb expansion.
        p=maps_common.work()/'selection.json';d=json.loads(p.read_text())
        rejected={m['path'] for m in d['models'] if any(d['materials'][p]['derived']=='AnimatedUV' for p in m['materials'])}
        # Preserve transform parents even when an unsupported monitor overlay is not rendered.
        for placement in d['placements']:
            if placement['path'] in rejected:placement['fields']['Hidden']='true'
        d['models']=[m for m in d['models'] if m['path'] not in rejected]
        used={p for m in d['models'] for p in m['materials']}|{v['material']['path'] for v in d['terrainLayers'].values() if 'material' in v}
        d['materials']={p:m for p,m in d['materials'].items() if p in used}
        d['excludedUnsupportedOverlays']=sorted(rejected);p.write_text(json.dumps(d))
        src=source()
        for stem in ['kor_tomb_item_sarcophagus','kor_tomb_item_sarcophagus_ruin','kor_tomb_item_brazier_ancient','kor_tomb_item_statue_03']:
            path=f'/resources/art/static/area/kor_korriban/item/{stem}.gr2'
            add_prop(d,src,path)
        add_prop(d,src,'/resources/art/static/item/container/item_con_crate_ancient_01.gr2')
        p.write_text(json.dumps(d))
    elif stage=='models':
        runpy.run_path(str(ROOT/'scripts/map_models.py'),run_name='__main__')
        p=maps_common.manifest('model-conversion');d=json.loads(p.read_text())
        for m in d['models']: m['output']=m['output'].replace('res://Assets/Maps/korriban-d4/','res://Assets/Korriban/D4/')
        p.write_text(json.dumps(d,indent=2))
    elif stage=='terrain':
        runpy.run_path(str(ROOT/'scripts/map_terrain.py'),run_name='__main__')
        p=maps_common.asset_dir()/'environment.json'
        p.write_text(p.read_text().replace('res://Assets/Maps/korriban-d4/','res://Assets/Korriban/D4/'))

def add_prop(d,src,path):
    if any(m['path']==path for m in d['models']):return
    b=src.get(path,'Korriban area ASSETS','D4 authored burial scenery and cache');names=[]
    count=struct.unpack_from('<H',b,26)[0];offset=struct.unpack_from('<I',b,88)[0]
    for i in range(count):
        off=struct.unpack_from('<I',b,offset+4*i)[0];name=b[off:b.index(b'\0',off)].decode('ascii')
        p='/resources/art/shaders/materials/'+name+'.mat';names.append(p)
        if p in d['materials']:continue
        x=ET.fromstring(src.get(p,path,'original burial material'));textures={};values={}
        for inp in x.findall('.//input'):
            sem=inp.findtext('semantic');val=inp.findtext('value')
            if inp.findtext('type','').lower()=='texture':
                tex=canon(val)
                if not tex.endswith('.dds'):tex+='.dds'
                src.get(tex,p,sem);textures[sem]=tex
            else:values[sem]=val
        d['materials'][p]=dict(path=p,derived=x.findtext('Derived'),alpha=x.findtext('AlphaMode'),twoSided=x.findtext('IsTwoSided'),textures=textures,values=values)
    d['models'].append(dict(path=path,materials=names))

if __name__=='__main__':
    if sys.argv[-1]=='enemies': enemies()
    else:environment(sys.argv[-1])
