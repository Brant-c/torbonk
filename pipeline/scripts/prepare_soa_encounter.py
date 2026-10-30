"""Resolve the April jungle, Council and Soa appearances into an isolated recipe."""
import json,re,struct,xml.etree.ElementTree as ET
from pathlib import Path
import character_source
from source_pipeline import ROOT
character_source.MANIFEST=ROOT/'manifests/soa-encounter-source.json'
src=character_source.SelectedSource()
src.manifest.update(maximumFiles=1200,maximumDecodedBytes=700*1024*1024,selection='April jungle/Council/Soa NPC appearances; authored Survivors run')
research=json.loads((ROOT/'manifests/soa-npc-expanded-research.json').read_text())
registries={};recipe={};issues=[]
def canon(p,bt='bmn'):
    return '/resources/'+p.replace('\\','/').lower().lstrip('/').removeprefix('resources/').replace('[bt]',bt).replace('[gen]',bt[1] if bt.startswith('b') else 'u')
def get(p,bt='bmn'):
    p=canon(p,bt);return src.get(p,'soa-npc-research.json','exact selected April appearance dependency')
def registry(slot):
    if slot not in registries:registries[slot]=ET.fromstring(get(f'/resources/art/dynamic/{slot}/index.xml'))
    return registries[slot]
def mat(path,bt):
    path=canon(path,bt);xml=ET.fromstring(get(path));t={};v={}
    for inp in xml.findall('input'):
        key=inp.findtext('semantic');value=inp.findtext('value') or ''
        if inp.findtext('type')=='texture' and value and not value.startswith('#'):
            tex=canon(value,bt)
            if not tex.endswith('.dds'):tex+='.dds'
            get(tex);t[key]=tex
        else:v[key]=value
    return dict(path=path,derived=xml.findtext('Derived'),textures=t,values=v)
def resolve_part(slot,fields,bt):
    mid=fields['appAppearanceSlotModelID'];tid=fields.get('appAppearanceSlotMaterialIndex')
    if not tid:return None
    a=next(a for a in registry(slot) if a.findtext('ID')==mid)
    m=next(m for m in a.findall('Materials/Material') if m.get('id')==tid)
    paths=[canon(a.findtext('BaseFile'),bt)]
    selected=set(fields.get('appAppearanceSlotAttachments',{}).values())
    paths += [canon(t.get('filename'),bt) for t in a.findall('Attachments/Attachment') if t.get('id') in selected]
    cloth=[]
    for p in paths:
        get(p)
        cp=p.removesuffix('.gr2')+'.clo'
        if src.has(cp):get(cp);cloth.append(cp)
    palettes={}
    for i,key in enumerate(['appAppearanceSlotHuePrimary','appAppearanceSlotHueSecondary'],1):
        if key in fields:
            entry=next((a for a in registry('garmenthue') if a.findtext('ID')==fields[key]),None)
            if entry is not None:
                p=canon(entry.findtext('BaseFile'),bt)
                if src.has(p):palettes[str(i)]=get(p).decode('utf-8-sig')
                else:issues.append(dict(slot=slot,palette=fields[key],reason='compiled palette; portable material retains its defaults'))
    return dict(slot=slot,modelId=mid,materialId=tid,models=paths,material=mat(m.get('filename'),bt),
        overrides=[dict(index=int(o.get('index')),material=mat(o.get('filename'),bt)) for o in m.findall('MaterialOverrides/MaterialOverride')],palettes=palettes,cloth=cloth)
names={'manka':'jungle_cat','rakata':'jungle_rakata_warrior','watcher':'jungle_rakata_hunter','acklay':'jungle_acklay','soa':'boss_soa',
    'luthro':'temple_boss_sith_marauder','alarii':'temple_boss_sith_marauder_b','fahren':'temple_boss_sith_marauder_c',
    'yshaar':'temple_boss_sith_juggernaut','zaheen':'temple_boss_sith_juggernaut_b','kahesh':'temple_boss_sith_assassin_droid',
    'jael':'temple_boss_sith_assassin_droid_b','serrod':'temple_boss_sith_assassin_droid_c'}
for key,stem in names.items():
    npc=research['npcs'][stem];visual=npc['npcVisualDataList']['0'];app=research['appearances'][visual['npcTemplateVisualDataAppearance']];bt=app['nppBodyType']
    dyc=get(f'/resources/art/dynamic/spec/{bt}.dyc').decode();rig=re.search('Skeleton=(.*)',dyc)[1].strip()
    if bt=='cat':rig='catboss_skeleton.gr2'
    skeleton='/resources/art/dynamic/spec/'+rig;get(skeleton)
    parts=[]
    for variants in app['nppAppearanceSlotMap_ForPrototype'].values():
        field=variants['0'];label=field.get('appAppearanceSlotType','')
        slot=label.removeprefix('appSlot').lower()
        if slot in ['creature','head','hair','facehair','face','chest','leg','boot','hand','waist','bracer']:
            part=resolve_part(slot,field,bt)
            if part:parts.append(part)
    creature=bt in ['cat','acklay'];folder='creature/'+bt if creature else 'humanoid/'+rig.removesuffix('_skeleton.gr2')
    stems=dict(Idle='cb_ready_1',Run='mv_run_forward',Walk='mv_walk_forward',Attack='cb_attack_1',Hit='dg_damage_1',Death='dg_death_1') if creature else dict(Idle='cb_knight_saber_idle_1',Run='mv_saber_run_forward_01',Attack='cb_saber_attack_right_1',Hit='dg_unarmed_crit_damage_right_1',Death='dg_death_1')
    if key=='soa':stems.update(Idle='cb_unarmed_idle_right_1',Run='mv_cb_unarmed_run_forward',Attack='cb_unarmed_attack_right_1')
    clips={}
    for label,stem in stems.items():
        path='/resources/anim/'+folder+'/'+stem+'.jba';get(path);clips[label]=path
    recipe[key]=dict(skeleton=skeleton,parts=parts,clips=clips,sourceVisual=visual,bodyType=bt)
    print('SOA_SELECTED',key,bt,len(parts),flush=True)
recipe['weapons']=json.loads((ROOT/'eternity-enemies-recipe.json').read_text())['weapons']
(ROOT/'soa-enemies-recipe.json').write_text(json.dumps(recipe,indent=2))
(ROOT/'manifests/soa-appearance-limits.json').write_text(json.dumps(issues,indent=2))
print('SOA_SOURCE_COMPLETE',len(src.manifest['resources']))
