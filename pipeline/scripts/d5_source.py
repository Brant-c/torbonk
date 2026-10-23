"""Exact Hutta appearance recovery; shared converter, original source scale and weights."""
import struct
import json,xml.etree.ElementTree as ET
from pathlib import Path
import character_source
from source_pipeline import ROOT
character_source.MANIFEST=ROOT/'manifests/d5-source.json'
s=character_source.SelectedSource()
s.manifest.update(maximumFiles=240,maximumDecodedBytes=200*1024*1024,selection='Hutta Faathra security officer and murder droid')
BODY='bms'
def canon(p):return '/resources/'+p.replace('\\','/').lower().lstrip('/').removeprefix('resources/').replace('[bt]',BODY).replace('[gen]','m')
def mat(p):
 p=canon(p);x=ET.fromstring(s.get(p));t={}
 for a in x.findall('input'):
  if a.findtext('type')=='texture' and (a.findtext('value') or '').strip('/') and not a.findtext('value').startswith('#'):
   q=canon(a.findtext('value'))
   if not q.endswith('.dds'):q+='.dds'
   s.get(q,p,a.findtext('semantic'));t[a.findtext('semantic')]=q
 return dict(path=p,derived=x.findtext('Derived'),textures=t)
def part(slot,mid,tid):
 r=ET.fromstring(s.get(f'/resources/art/dynamic/{slot}/index.xml'));a=next(a for a in r if a.findtext('ID')==mid)
 m=next(m for m in a.findall('Materials/Material') if m.get('id')==tid)
 paths=[canon(a.findtext('BaseFile'))]+[canon(t.get('filename')) for t in a.findall('Attachments/Attachment')]
 for p in paths:s.get(p,'Hutta '+slot,'appearance registry')
 return dict(slot=slot,modelId=mid,materialId=tid,models=paths,material=mat(m.get('filename')),overrides=[dict(index=int(o.get('index')),material=mat(o.get('filename'))) for o in m.findall('MaterialOverrides/MaterialOverride')])
out={}
for name,rig,parts in [('guard','bmsnew',[('boot','1002351','1005096'),('chest','1002349','1005097'),('leg','1002606','1005100'),('hand','1002356','1005095'),('head','1000260','1583904'),('hair','1000620','1027364'),('waist','1002348','1005098')]),('droid','bmnnew',[('creature','1000103','1001474')])]:
 sk=f'/resources/art/dynamic/spec/{rig}_skeleton.gr2';s.get(sk)
 clips={}
 for key,stem in [('Idle','cb_pistol_idle_1'),('Run','mv_cb_pistol_run_forward'),('Attack','cb_pistol_fire_1'),('Hit','dg_unarmed_crit_damage_right_1'),('Death','dg_death_1')]:
  p=f'/resources/anim/humanoid/{rig}/{stem}.jba';s.get(p,name,key);clips[key]=p
 out[name]=dict(skeleton=sk,parts=[part(*p) for p in parts],clips=clips)
for name,rig,parts in [('gangster','bmnnew',[('boot','1000540','1001143'),('chest','1000543','1001305'),('leg','1000544','1001727'),('hand','1142620','1150778'),('head','1000255','1583904'),('waist','1000541','1001807')]),('bogsnarler','dog',[('creature','1005718','1005719')]),('chemilizard','lizard',[('creature','1000131','1001449')])]:
 BODY='bmn';sk=f'/resources/art/dynamic/spec/{rig}_skeleton.gr2';s.get(sk)
 clips={};humanoid=rig=='bmnnew'
 for key,stems in [('Idle',['cb_pistol_idle_1','cb_ready_1']),('Run',['mv_cb_pistol_run_forward','mv_run_forward','mv_run_forward_01']),('Attack',['cb_pistol_fire_1','cb_attack_1','cb_attack_right_1']),('Hit',['dg_unarmed_crit_damage_right_1','dg_damage_1']),('Death',['dg_death_1'])]:
  paths=[f'/resources/anim/{"humanoid" if humanoid else "creature"}/{'veractyl' if rig=='lizard' else rig}/{stem}.jba' for stem in stems]
  p=next((p for p in paths if s.has(p)),None)
  if p is None:raise RuntimeError((name,key,paths))
  s.get(p,name,key);clips[key]=p
 out[name]=dict(skeleton=sk,parts=[part(*p) for p in parts],clips=clips)
out['brawler']=json.loads(json.dumps(out['gangster']))
for key,stem in [('Idle','cb_unarmed_idle_right_1'),('Run','mv_run_forward'),('Attack','cb_unarmed_attack_right_1')]:
 p=f'/resources/anim/humanoid/bmnnew/{stem}.jba';s.get(p,'Hutta brawler',key);out['brawler']['clips'][key]=p
sky='/resources/art/static/area/hut_hutta/sky/hut_skyscene01.gr2'
raw=s.get(sky,'hut_main Area skyscene_room','original background');n=struct.unpack_from('<H',raw,26)[0];off=struct.unpack_from('<I',raw,88)[0];materials=[]
for i in range(n):
 o=struct.unpack_from('<I',raw,off+i*4)[0];stem=raw[o:raw.index(b'\0',o)].decode();materials.append(mat('/art/shaders/materials/'+stem+'.mat'))
out['weapons']={'sky':dict(model=sky,materials=materials)}
(ROOT/'d5-enemies-recipe.json').write_text(json.dumps(out,indent=2))


