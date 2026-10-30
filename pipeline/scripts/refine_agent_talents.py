"""One-time Agent 3x3 talent update. Effects use the common stat/runtime hooks."""
import json
from pathlib import Path
G=Path(__file__).resolve().parents[1]/'godot/Gameplay'
p=G/'Talents/agent.json';d=json.loads(p.read_text())
def effect(kind,amount,ability='',key=''):return dict(Kind=kind,Amount=amount,Ability=ability,Key=key)
def hook(key):return effect('hook',1,key=key)
new={
 'medicine':[
 ('enduring_kolto','Enduring Kolto','Poison ticks restore 2 health, at most once per second.',[hook('agent_enduring')],'surgicalprobe'),
 ('medical_screen','Medical Screen','Kolto Injection and Surgical Probe grant a 10-health shield, at most once every six seconds.',[hook('agent_medical_screen')],'advancedmedicalprobe'),
 ('emergency_response','Emergency Response','Taking damage below 30% health restores 15 health and readies Kolto Injection. Once every 25 seconds.',[hook('agent_emergency')],'surgicalprobe')],
 'concealment':[
 ('tactical_opportunity','Tactical Opportunity','Every fourth knife activation creates a four-second Hidden Strike opening.',[hook('agent_opportunity')],'backstab'),
 ('meticulously_kept_blades','Meticulously Kept Blades','Knife attacks deal 20% more damage and Shiv cleaves 30% farther.',[effect('damage',.2,x) for x in ['shiv','backstab','hiddenstrike','laceration']]+[effect('radius',.3,'shiv')],'criticalblade'),
 ('revitalizers','Revitalizers','Spending Tactical Advantage restores 4 health and grants 10% mitigation for four seconds.',[hook('agent_revitalizers')],'shiv')],
 'marksmanship':[
 ('sector_ranger','Sector Ranger','Snipe and Rifle Shot ricochet to two additional enemies.',[effect('bounces',2,'snipershot'),hook('agent_rifle_chain')],'snipe'),
 ('reactive_shots','Reactive Shots','Followthrough reduces Ambush and Series of Shots cooldowns by two seconds.',[hook('agent_reactive_shots')],'followthrough'),
 ('deployed_cover','Deployed Cover','Precision hits grant an 8-health shield, at most once every six seconds. Works while moving.',[hook('agent_cover_screen')],'snipe')],
 'engineering':[
 ('explosive_engineering','Explosive Engineering','Probes and Orbital Strike deal 20% more damage; Orbital Strike covers 25% more area radius.',[effect('damage',.2,x) for x in ['explosiveprobe','interrogationprobe','plasmaprobe','orbitalstrike']]+[effect('radius',.25,'orbitalstrike')],'plasmaprobe'),
 ('interrogation_tactics','Interrogation Tactics','Probe hits slow enemies by 35% for three seconds. EMP Discharge also restores 12 health.',[hook('agent_probe_control')],'sonicprobe'),
 ('energy_overrides','Energy Overrides','EMP Discharge recovers four seconds of Orbital Strike and Ballistic Shield cooldowns.',[hook('agent_energy_overrides')],'detonateprobe')],
 'agent_lethality':[
 ('lethal_training','Lethal Training','Corrosive Dart, Corrosive Grenade and Cull deal 15% more damage.',[effect('damage',.15,x) for x in ['dart','corrosivegrenade','agentcull']],'cull'),
 ('targeted_demolition','Targeted Demolition','Cull spreads your existing Agent poisons to two nearby enemies. At most once every two seconds.',[hook('agent_toxic_spread')],'toxicdart'),
 ('toxic_resilience','Toxic Resilience','Hitting a target with your Agent poison restores 2 health and recovers half a second of Shield Probe cooldown, at most once every two seconds.',[hook('agent_toxic_resilience')],'toxicdart')]
}
for tree,rows in new.items():
 for i,(key,name,desc,fx,icon) in enumerate(rows):
  id=tree+'.'+key
  if any(t['Id']==id for t in d['Talents']):continue
  d['Talents'].append(dict(Id=id,Classes=['operative','sniper'] if tree=='agent_lethality' else ['operative'] if tree in ['medicine','concealment'] else ['sniper'],Tree=tree,Tier=2,Column=i,Name=name,Description=desc,Effects=fx,Icon='res://Assets/UI/icons/'+icon+'.png',MaxRank=1,Cost=1,RequiredPoints=0,Prerequisites=[],Source='Recovered Agent artwork; survivors adaptation, names are not all exact original talent bindings.'))
# Replace mechanical/placeholder text with readable, accurate effects.
titles={'dart':'Corrosive Dart','agentcull':'Cull','koltoinjection':'Kolto Injection','koltoprobe':'Kolto Probe','surgicalprobe':'Surgical Probe','backstab':'Backstab','hiddenstrike':'Hidden Strike','laceration':'Laceration','followthrough':'Followthrough','ambush':'Ambush','seriesofshots':'Series of Shots','explosiveprobe':'Explosive Probe','plasmaprobe':'Plasma Probe','empdischarge':'EMP Discharge','fragmentationgrenade':'Fragmentation Grenade','corrosivegrenade':'Corrosive Grenade','cloakingscreen':'Cloaking Screen'}
for t in d['Talents']:
 if len(t['Effects'])==1:
  e=t['Effects'][0];ab=titles.get(e.get('Ability'),e.get('Ability'));v=e['Amount'];kind=e['Kind']
  if kind!='hook' and ab:
   t['Description']=f'{ab} '+(f'lasts {v:g} seconds longer.' if kind=='duration' else f'ricochets to {v:g} additional enemies.' if kind=='bounces' else f'{dict(damage="damage increases",cooldown="cooldown recovers faster",radius="area radius increases",healing="healing increases",periodic_rate="poison ticks occur faster").get(kind,kind)} by {v*100:g}%.')
  if e.get('Ability')=='laceration' and kind=='radius':e['Amount']=.4;t['Description']='Laceration cleaves across a 40% wider radius.'
 # Use relevant original art rather than the same placeholder across a tree.
 if t['Tier']<2:
  tree=t['Tree'];t['Icon']='res://Assets/UI/icons/'+({'medicine':'surgicalprobe','concealment':'criticalblade','marksmanship':'followthrough','engineering':'plasmaprobe','agent_lethality':'toxicdart'}[tree])+'.png'
p.write_text(json.dumps(d,indent=2)+'\n')
p=G/'classes.json';rows=json.loads(p.read_text())
for r in rows:
 if r['Id']=='operative':r['BasicAttack']=''
p.write_text(json.dumps(rows,indent=2)+'\n')
