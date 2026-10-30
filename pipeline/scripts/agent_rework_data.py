"""Apply the Agent balance pass without regenerating unrelated class data."""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
GAME=ROOT/'godot/Gameplay'
def tune(id, **fields):
 p=GAME/f'Abilities/{id}.tres';s=p.read_text(encoding='utf-8-sig')
 for key,value in fields.items():
  value=json.dumps(value) if isinstance(value,str) else str(value).lower()
  line=f'{key} = {value}'
  s=re.sub(r'^'+key+r' = .*$',lambda m:line,s,flags=re.M) if re.search(r'^'+key+r' = ',s,re.M) else s+'\n'+line+'\n'
 p.write_text(s,encoding='utf-8')
for id in ['snipershot','ambush','seriesofshots','followthrough','suppressivefire']:
 # Fixed values keep this script repeatable.
 values={'snipershot':48,'ambush':110,'seriesofshots':27,'followthrough':65,'suppressivefire':23}
 tune(id,Damage=values[id],Description={'suppressivefire':'Requires cover to begin. Sustained suppression marks a fixed area; the active barrage continues after leaving cover.'}.get(id,'Requires cover to begin. Heavy precision fire rewards holding position; an active attack may finish after leaving cover.'))
tune('suppressivefire',PersistentArea=True)
tune('plasmaprobe',Damage=24,Area=1,ChannelSeconds=8,TickSeconds=1,PeriodicDamage=0,PeriodicDuration=0,DetachedArea=True,PersistentArea=True,StartClip='Agent/Detonator',LoopClip='Agent/Detonator',CastAudio='res://Assets/Agent/Audio/85963594.wav',Description='Requires cover to deploy. A stationary plasma probe burns all enemies in its marked area for eight seconds, persisting after you leave cover.')
tune('laceration',Damage=34,Radius=.34,Range=.42,CooldownSeconds=2.6,WindupSeconds=.2,ChannelSeconds=.6,TickSeconds=.6,GestureSeconds=1.2,RecoverySeconds=.1,FinishGesture=True,StartClip='Agent/LacerationDouble',LoopClip='Agent/LacerationDouble',WeaponAudio='',ImpactAudio='res://Assets/Agent/Audio/laceration_impact_layered.wav',Description='Two knife swipes deal 34 damage each to every enemy in the cone. Each swipe has its own impact sound. Tactical Advantage grants 50% bonus damage to both swipes.')
tune('explosiveprobe',CastAudio='res://Assets/Agent/Audio/149086699.wav',ImpactAudio='res://Assets/Agent/Audio/explosiveprobe_explode_layered.wav')
tune('debilitate',Category=0,Damage=28,Description='Offensive close-range stun lasting three seconds. Mass Debilitation turns it into an uncapped area stun and exposes enemies for six seconds.')
def create(id,title,icon,**fields):
 p=GAME/f'Abilities/{id}.tres'
 p.write_text('[gd_resource type="Resource" script_class="AbilityDefinition" load_steps=2 format=3]\n\n[ext_resource type="Script" path="res://Gameplay/AbilityDefinition.cs" id="1"]\n\n[resource]\nscript = ExtResource("1")\n')
 tune(id,Id=id,Title=title,Icon=icon,StartsUnlocked=False,NearbyTriggerCount=1,KnockbackDistance=0,RequireGesture=True,**fields)
 u=GAME/f'Upgrades/unlock_{id}.tres'
 u.write_text(f'[gd_resource type="Resource" script_class="UpgradeDefinition" load_steps=2 format=3]\n[ext_resource type="Script" path="res://Gameplay/UpgradeDefinition.cs" id="1"]\n[resource]\nscript = ExtResource("1")\nId = "unlock_{id}"\nTitle = "{title}"\nIcon = "{icon}"\nEffect = 7\nAbilityId = "{id}"\nMaximumRank = 1\n')
create('severtendon','Sever Tendon','severtendon',Damage=30,CooldownSeconds=7,Range=.42,Radius=.32,Area=5,SplashMultiplier=1,SlowFactor=.3,SlowSeconds=5,WindupSeconds=.35,ChannelSeconds=0,GestureSeconds=1.066667,RecoverySeconds=1.066667,StartClip='Agent/Shiv',LoopClip='Agent/Shiv',PresentationKind='agent',ImpactAudio='res://Assets/Agent/Audio/142346317.wav')
tune('severtendon',Description='Cuts nearby tendons, dealing knife damage and slowing enemies by 70% for five seconds.')
create('carbineburst','Carbine Burst','barrage',Damage=25,CooldownSeconds=6,Range=.85,Radius=.75,Area=5,SplashMultiplier=1,WindupSeconds=.12,ChannelSeconds=1.2,TickSeconds=.3,GestureSeconds=1.2,RecoverySeconds=.2,StartClip='Agent/RifleAutoStart',LoopClip='Agent/RifleAutoLoop',EndClip='Agent/RifleAutoEnd',PresentationKind='rifle',ShotAudio='res://Assets/D3/Audio/28023533.wav',MaintainChannelGesture=True)
tune('carbineburst',Description='Short-range automatic cone fire. Full damage to every enemy in the cone; Concealment talents reward firing into controlled packs.')
create('blowforblow','Blow for Blow','shieldprobe',Damage=1,CooldownSeconds=20,Range=.65,Radius=0,Area=0,Effect=3,BuffSeconds=4,GestureSeconds=.5,RecoverySeconds=.2,WindupSeconds=0,ChannelSeconds=0,StartClip='Agent/Detonator',LoopClip='Agent/Detonator',PresentationKind='agent',Category=1)
tune('blowforblow',Slotless=True,Description='Talent-granted countermeasure lasting four seconds, occupying no ability slot. Return 150% of actual health damage to the attacker. Incoming damage is still received.')
p=GAME/'classes.json';classes=json.loads(p.read_text())
op=next(c for c in classes if c['Id']=='operative')
for id in ['severtendon','carbineburst','blowforblow']:
 if id not in op['Kit']:op['Kit'].append(id)
 if id!='blowforblow' and 'unlock_'+id not in op['TalentPool']:op['TalentPool'].append('unlock_'+id)
p.write_text(json.dumps(classes,indent=2)+'\n')
p=GAME/'Talents/agent.json';data=json.loads(p.read_text())
def effect(kind,ability='',amount=1,key=''):return dict(Kind=kind,Ability=ability,Amount=amount,Key=key)
for t in data['Talents']:
 id=t['Id']
 if id=='concealment.advanced_cloaking':
  t.update(Name='Mass Debilitation',Description='Debilitate hits every enemy within a 0.55 radius around you, stunning for three seconds and making affected targets take 40% more damage from you for six seconds. Stun-immune targets still receive the damage mark.',Effects=[effect('hook',key='agent_mass_debilitation')],Icon='res://Assets/UI/icons/hamstring.png')
 if id=='concealment.flanking':
  t.update(Name='Counterstrike Training',Description='Grants Blow for Blow: for four seconds, return 150% of health damage received to the attacker. 20-second cooldown. Reflected damage cannot reflect again.',Effects=[effect('hook',key='agent_counterstrike')],Icon='res://Assets/UI/icons/shieldprobe.png')
 if id=='concealment.collateral_strike':
  t.update(Description='Both Laceration swipes cleave 50% farther. Carbine Burst covers 25% more area and deals 30% more damage.',Effects=[effect('radius','laceration',.5),effect('radius','carbineburst',.25),effect('damage','carbineburst',.3)])
 if id=='concealment.culling':
  t.update(Description='Laceration deals 40% more damage. Laceration and Carbine Burst deal 50% extra damage to stunned enemies or targets marked by Mass Debilitation.',Effects=[effect('damage','laceration',.4),effect('hook',key='agent_controlled_burst')])
 if id in ['concealment.advanced_cloaking','concealment.flanking','concealment.collateral_strike','concealment.culling']:
  t['Source']='TorBonk Concealment adaptation; recovered SWTOR artwork. Retained talent ID preserves saved allocations.'
  t['IconSource']='Related recovered SWTOR artwork for adapted talent.'
p.write_text(json.dumps(data,indent=2)+'\n')
# Follow the live April ability's cast_instant schedule, rather than the unused new_instant prototype.
p=ROOT/'manifests/agent-conversion.json';c=json.loads(p.read_text());c['clips']['Laceration']='cb_rifle_disk_left_1';p.write_text(json.dumps(c,indent=2)+'\n')
