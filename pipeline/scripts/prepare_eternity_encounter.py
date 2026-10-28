"""April NPC appearance IDs -> registry models/materials and compatible rig clips."""
import sys,json,struct,re,hashlib,xml.etree.ElementTree as ET
from pathlib import Path
from source_pipeline import ROOT,SOURCE
import map_scan as scan
idx=scan.index();rows=[]
def canon(p):return '/resources/'+p.replace('\\','/').lower().lstrip('/').removeprefix('resources/')
def get(p):
    p=canon(p);b=scan.read(idx,p);assert b is not None,p
    f=SOURCE/p.lstrip('/');f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
    rows.append(dict(path=p,sha256=hashlib.sha256(b).hexdigest()));return b
def mat(p):
    p=canon(p);x=ET.fromstring(get(p));textures={}
    for i in x.findall('input'):
        if i.findtext('type')=='texture':
            t=canon(i.findtext('value'))
            if not t.endswith('.dds'):t+='.dds'
            get(t);textures[i.findtext('semantic')]=t
    return dict(path=p,derived=x.findtext('Derived'),textures=textures)
reg=ET.fromstring(get('/resources/art/dynamic/creature/index.xml'));recipe={}
for name,mid,materialid,rig,anim in [
    ('icecat','1000473','1001432','cat','creature/cat'),
    ('tauntaun','1000154','1001461','tauntaun','creature/tauntaun'),
    ('guard','1005669','1177013','bmf','humanoid/bmfnew'),
    ('xrr3','1492589','1492590','battlewarbotrakataboss','droid/battle_warbot_rakata_boss')]:
    entry=next(e for e in reg if e.findtext('ID')==mid);m=next(e for e in entry.findall('Materials/Material') if e.get('id')==materialid)
    model=canon(entry.findtext('BaseFile'));get(model)
    dyc=get(f'/resources/art/dynamic/spec/{rig}.dyc').decode();skel=re.search(r'Skeleton=(.*)',dyc)[1].strip()
    # Pantran mesh palette includes ear bones absent from the base cat rig.
    # The original catboss rig contains those plus every bone used by cat clips.
    if name=='icecat':skel='catboss_skeleton.gr2'
    skeleton='/resources/art/dynamic/spec/'+skel;get(skeleton)
    clips={}
    for label,stem in dict(Idle='cb_ready_1',Run='mv_run_forward',Attack='cb_attack_1',Hit='dg_damage_1',Death='dg_death_1').items():
        if name=='guard':stem=dict(Idle='cb_pole_idle_right_1',Run='mv_cb_pole_run_forward',Attack='cb_pole_attack_right_1',Hit='dg_unarmed_crit_damage_right_1',Death='dg_death_1')[label]
        path=f'/resources/anim/{anim}/{stem}.jba';get(path);clips[label]=path
    if name in ('icecat','guard','xrr3'):
        walk='mv_cb_pole_walk_forward' if name=='guard' else 'mv_walk_forward'
        path=f'/resources/anim/{anim}/{walk}.jba';get(path);clips['Walk']=path
    recipe[name]=dict(skeleton=skeleton,parts=[dict(slot='creature',modelId=mid,materialId=materialid,models=[model],material=mat(m.get('filename')),overrides=[])],clips=clips)
staff='/resources/art/dynamic/weapon/model/electrostaff_high04_a03_v01.gr2';b=get(staff);count=struct.unpack_from('<H',b,26)[0];off=struct.unpack_from('<I',b,88)[0];materials=[]
for i in range(count):
    p=struct.unpack_from('<I',b,off+i*4)[0];name=b[p:b.index(b'\0',p)].decode();materials.append(mat('/resources/art/shaders/materials/'+name+'.mat'))
recipe['weapons']={'staff':dict(model=staff,materials=materials)}
(ROOT/'eternity-enemies-recipe.json').write_text(json.dumps(recipe,indent=2))
(ROOT/'manifests/eternity-encounter-source.json').write_text(json.dumps(dict(evidence='Client-derived April unique archive entries. NPC/NPP appearance model/material IDs resolved separately through research MCP; stats and boss mechanics are authored.',resources=rows,appearanceIds={'icecat':['1000473','1001432'],'tauntaun':['1000154','1001461'],'guard':['1005669','1177013'],'xrr3':['1492589','1492590']}),indent=2))
print('ETERNITY SOURCE',len(rows))
