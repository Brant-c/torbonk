"""Bounded B6 original resource selection. Archives are only opened read-only."""
import json,struct,xml.etree.ElementTree as ET
from pathlib import Path
import character_source
from character_source import ROOT
character_source.MANIFEST=ROOT/'manifests/b6-source.json'
s=character_source.SelectedSource();s.manifest.update(maximumFiles=400,maximumDecodedBytes=200*1024*1024,selection='B6 Korriban roster and original equipment')
def canon(p):return '/resources/'+p.replace('\\','/').lstrip('/').removeprefix('resources/').replace('[bt]','bmn').replace('[gen]','m')
def mat(p):
 p=canon(p);x=ET.fromstring(s.get(p));t={}
 for a in x.findall('input'):
  if a.findtext('type')=='texture' and (a.findtext('value') or '').strip('/'):
   q=canon(a.findtext('value'));q+= '' if q.endswith('.dds') else '.dds';s.get(q,p,a.findtext('semantic'));t[a.findtext('semantic')]=q
 return dict(path=p,derived=x.findtext('Derived'),textures=t)
def part(slot,mid,tid):
 r=ET.fromstring(s.get(f'/resources/art/dynamic/{slot}/index.xml'))
 a=next(a for a in r if a.findtext('ID')==mid)
 m=next(m for m in a.findall('Materials/Material') if m.get('id')==tid)
 paths=[canon(a.findtext('BaseFile'))]+[canon(t.get('filename')) for t in a.findall('Attachments/Attachment')]
 for p in paths:s.get(p,f'{slot}:{mid}','registry model')
 return dict(slot=slot,modelId=mid,materialId=tid,models=paths,material=mat(m.get('filename')),overrides=[dict(index=int(o.get('index')),material=mat(o.get('filename'))) for o in m.findall('MaterialOverrides/MaterialOverride')])
out={}
for name,mid,tid,rig,anim in [('broodling','1006619','1006620','klorslug','slug'),('shyrack','1003627','1003628','dragonbat','dragonbat'),('broodmatron','1000128','1001446','klorslug','slug')]:
 sk=f'/resources/art/dynamic/spec/{rig}_skeleton.gr2';s.get(sk)
 clips={}
 for key,stem in [('Idle','cb_ready_1'),('Run','mv_run_forward'),('Attack','cb_attack_1'),('Hit','dg_damage_1'),('Death','dg_death_1')]:
  p=f'/resources/anim/creature/{anim}/{stem}.jba';s.get(p,name,key);clips[key]=p
 out[name]=dict(skeleton=sk,parts=[part('creature',mid,tid)],clips=clips)
out['looter']=dict(skeleton='/resources/art/dynamic/spec/bmnnew_skeleton.gr2',parts=[part(*p) for p in [('boot','1000540','1001143'),('chest','1000543','1001305'),('hand','1000374','1150780'),('leg','1000544','1001727')]],clips={})
for key,stem in [('Idle','cb_knight_saber_idle_1'),('Run','mv_saber_run_forward_01'),('Attack','cb_saber_attack_right_1'),('Hit','dg_unarmed_crit_damage_right_1'),('Death','dg_death_1')]:
 p=f'/resources/anim/humanoid/bmnnew/{stem}.jba';s.get(p,'looter',key);out['looter']['clips'][key]=p
out['weapons']={}
for name,p in [('electroblade','/resources/art/dynamic/weapon/model/electroblade_low03_a01_v01.gr2'),('saber','/resources/art/dynamic/weapon/model/saber_sithlow03_a01_v01.gr2')]:
 d=s.get(p);count=struct.unpack_from('<H',d,26)[0];off=struct.unpack_from('<I',d,88)[0];m=[]
 for i in range(count):
  o=struct.unpack_from('<I',d,off+i*4)[0];stem=d[o:d.index(b'\0',o)].decode();m.append(mat(f'/art/shaders/materials/{stem}.mat'))
 out['weapons'][name]=dict(model=p,materials=m)
(ROOT/'b6-recipe.json').write_text(json.dumps(out,indent=2))
print('Selected rigs and models',list(out))
