"""Retarget Agent talents to core attacks while preserving saved talent IDs."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
p=ROOT/'godot/Gameplay/Talents/agent.json';data=json.loads(p.read_text())
def e(kind,ability='',amount=1,key=''):return dict(Kind=kind,Ability=ability,Amount=amount,Key=key)
for t in data['Talents']:
 id=t['Id']
 if id=='concealment.revitalizers':
  t.update(Name='Venomous Blades',Icon='res://Assets/UI/icons/toxicdart.png',Description='Laceration replaces your separately cast Corrosive Dart and applies its full poison to every enemy hit. The poison needs no separate ability slot.',Effects=[e('hook',key='agent_venomous_blades')])
 if id=='concealment.meticulously_kept_blades':
  t.update(Description='Backstab, Hidden Strike and both Laceration contacts deal 20% more damage. Backstab cleaves 30% farther.',Effects=[e('damage',a,.2) for a in ['backstab','hiddenstrike','laceration']]+[e('radius','backstab',.3)])
 if id=='concealment.tactical_opportunity':
  t.update(Description='Backstab and Hidden Strike grant Tactical Advantage. Every fourth Backstab, Hidden Strike or Laceration activation creates a four-second Hidden Strike opening.',Effects=[e('hook',key='agent_opportunity')])
 if id=='concealment.acid_blade':
  t.update(Description='Backstab applies Corrosive Dart to every target hit. If the target already has your Dart, it bursts for all remaining poison damage, then receives a fresh full-duration Dart. No separately equipped Dart is needed.',Effects=[e('hook',key='agent_acid')])
 if id=='marksmanship.sector_ranger':
  t.update(Name='Focused Barrage',Icon='res://Assets/UI/icons/barrage.png',Description='Series of Shots ricochets to two additional enemies. Suppressive Fire covers 30% more area.',Effects=[e('bounces','seriesofshots',2),e('radius','suppressivefire',.3)])
 if id=='marksmanship.snipers_nest':
  t.update(Name='Entrenched Firepower',Description='While in cover, Ambush, Series of Shots, Followthrough and Suppressive Fire deal 25% more damage.',Effects=[e('hook',key='agent_nest')])
 if id=='marksmanship.deployed_cover':
  t.update(Description='Ambush, Series of Shots, Followthrough and Suppressive Fire hits grant an 8-health shield, at most once every six seconds.',Effects=[e('hook',key='agent_cover_screen')])
 if id in ['concealment.revitalizers','concealment.meticulously_kept_blades','concealment.tactical_opportunity','concealment.acid_blade','marksmanship.sector_ranger','marksmanship.snipers_nest','marksmanship.deployed_cover']:
  t['Source']='TorBonk core-ability adaptation using recovered SWTOR artwork; stable talent ID retains saved allocation.'
p.write_text(json.dumps(data,indent=2)+'\n')
