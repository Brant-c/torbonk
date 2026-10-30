"""Build Agent resources using existing recovered presentation adapters. Balance is an adaptation."""
import json,re
from pathlib import Path
if (Path(__file__).resolve().parents[1]/"godot/Gameplay/Talents/agent.json").exists():
 raise SystemExit("Initial expansion already applied; edit resources directly to preserve refinements.")
R=Path(__file__).resolve().parents[1]; G=R/'godot/Gameplay'
def edit(path,values):
 s=path.read_text(encoding='utf-8')
 for k,v in values.items():
  line=k+' = '+(json.dumps(v) if isinstance(v,str) else str(v).lower())
  s=re.sub(r'^'+k+r' = .*$',lambda m:line,s,flags=re.M) if re.search(r'^'+k+r' = ',s,re.M) else s+'\n'+line+'\n'
 path.write_text(s,encoding='utf-8')
def ability(id,title,base,**kw):
 p=G/f'Abilities/{id}.tres'
 if id!=base:p.write_text((G/f'Abilities/{base}.tres').read_text())
 edit(p,dict(Id=id,Title=title,StartsUnlocked=False,**kw))
shared=['overloadshot','fragmentationgrenade','flashbang','debilitate','adrenalineprobe','agentescape','shieldprobe','dart','corrosivegrenade','weakeningblast','agentcull']
op=['shiv','backstab','hiddenstrike','laceration','agentstealth','cloakingscreen','stimboost','koltoprobe','koltoinjection','surgicalprobe']
sn=['ambush','seriesofshots','followthrough','suppressivefire','orbitalstrike','explosiveprobe','interrogationprobe','plasmaprobe','empdischarge','coverpulse','entrench','ballisticshield']
ability('shiv','Shiv','dart',Icon='toxicdart',Damage=27,PeriodicDamage=0,PeriodicDuration=0,Range=.38,Radius=.22,Area=5,SplashMultiplier=1.0,CooldownSeconds=3,StartClip='Agent/Shiv',LoopClip='Agent/Shiv',PresentationKind='saber',Description='Close knife sweep. Grants Tactical Advantage once per activation; hits a group without talents.')
for id,title,clip,dmg,cd in [('backstab','Backstab','Backstab',52,7),('hiddenstrike','Hidden Strike','Backstab',70,12),('laceration','Laceration','Laceration',38,3)]:
 ability(id,title,'shiv',Damage=dmg,CooldownSeconds=cd,StartClip='Agent/'+clip,LoopClip='Agent/'+clip,Description='Close-range multi-target knife attack. Hidden Strike requires an opening; Laceration spends Tactical Advantage for bonus damage, but always remains usable.')
ability('overloadshot','Overload Shot','snipershot',Damage=26,Range=1.15,CooldownSeconds=3,Area=4,Radius=.16,SplashMultiplier=.7)
ability('fragmentationgrenade','Fragmentation Grenade','corrosivegrenade',Damage=42,PeriodicDamage=0,PeriodicDuration=0,DamageType=1,Radius=.34,CooldownSeconds=7,Description='Explosive cluster damage; baseline crowd clearing.')
ability('flashbang','Flash Bang','fragmentationgrenade',Damage=1,CooldownSeconds=18,Category=1,Description='Blinds nearby enemies for four seconds; damage breaks control.')
ability('debilitate','Debilitate','shiv',Damage=14,CooldownSeconds=12,Category=1,Description='Close-range three-second stun.')
ability('agentcull','Cull','seriesofshots',Damage=12,Range=1.15,Area=4,Radius=.22,SplashMultiplier=.8,Description='Repeated fire gains bonus damage for each of your two Agent poisons on the target.')
for id,title in [('adrenalineprobe','Adrenaline Probe'),('agentescape','Escape'),('agentstealth','Stealth'),('cloakingscreen','Cloaking Screen'),('stimboost','Stim Boost'),('entrench','Entrench')]:
 ability(id,title,'adrenalstim',CooldownSeconds=22 if id!='agentstealth' else 12,Description='Agent utility. Stealth and Cloaking Screen create a brief offensive opening without restricting movement.')
for id,title,heal,cd in [('koltoinjection','Kolto Injection',24,9),('koltoprobe','Kolto Probe',6,12),('surgicalprobe','Surgical Probe',16,6)]:
 ability(id,title,'shieldprobe',Effect=2,Healing=heal,HealingCap=30,HealthThreshold=.85,CooldownSeconds=cd,PeriodicHealing=4 if id=='koltoprobe' else 0,PeriodicDuration=8 if id=='koltoprobe' else 0,Description='Tactical self healing. Surgical Probe consumes Tactical Advantage.')
ability('followthrough','Followthrough','snipershot',Damage=44,CooldownSeconds=4,Area=2,BounceCount=3,BounceRange=.4,Description='Ricocheting follow-up shot after Snipe, Ambush or Series of Shots.')
ability('suppressivefire','Suppressive Fire','seriesofshots',Area=1,Radius=.42,Range=2.2,Damage=14,ChannelSeconds=3,TickSeconds=.5,CooldownSeconds=9,TargetRule=1,Description='Sustained rifle suppression across an enemy cluster; firing remains movement compatible.')
ability('orbitalstrike','Orbital Strike','suppressivefire',Damage=44,Radius=.55,Range=2.5,ChannelSeconds=3,TickSeconds=1,CooldownSeconds=24,PersistentArea=True,DetachedArea=True,Description='Three artillery strikes in a persistent ground area.')
for id,title,dmg,pd in [('explosiveprobe','Explosive Probe',64,0),('interrogationprobe','Interrogation Probe',8,10),('plasmaprobe','Plasma Probe',8,13),('empdischarge','EMP Discharge',38,0)]:
 ability(id,title,'corrosivegrenade',Damage=dmg,PeriodicDamage=pd,PeriodicDuration=8 if pd else 0,Range=2.2,Radius=.38,CooldownSeconds=12,DamageType=0,Description='Engineering area attack. EMP Discharge accelerates equipped probe cooldowns.')
ability('coverpulse','Cover Pulse','flashbang',Area=3,Radius=.4,Range=.4,KnockbackDistance=.3,StaggerSeconds=2,Description='Repels enemies in close range without entering cover.')
ability('ballisticshield','Ballistic Shield','shieldprobe',ShieldCapacity=95,ShieldDuration=10,CooldownSeconds=32)
for id in ['snipershot','ambush','seriesofshots']:
 edit(G/f'Abilities/{id}.tres',dict(Area=2,BounceCount=3,BounceRange=.4,Range=2.4,Description='Long-range precision fire ricochets through groups. Brief stationary firing rewards positioning; movement remains unrestricted.'))
edit(G/'Abilities/snipershot.tres',dict(Title='Snipe'))
edit(G/'Abilities/corrosivegrenade.tres',dict(Radius=.34))
profiles=json.loads((G/'classes.json').read_text())
for p in profiles:
 if p['Id'] not in ['operative','sniper']:continue
 p['StartingAbility']='shiv' if p['Id']=='operative' else 'snipershot'
 p['Kit']=[x for x in shared+(op if p['Id']=='operative' else sn) if x!=p['StartingAbility']]
 p['TalentPool']=['damage','range','cooldown','health','speed']+['unlock_'+x for x in p['Kit']]
 for id in p['Kit']:
  data=(G/f'Abilities/{id}.tres').read_text(); title=re.search('Title = (.*)',data)[1];icon=re.search('Icon = (.*)',data)[1]
  (G/f'Upgrades/unlock_{id}.tres').write_text('[gd_resource type="Resource" script_class="UpgradeDefinition" load_steps=2 format=3]\n[ext_resource type="Script" path="res://Gameplay/UpgradeDefinition.cs" id="1"]\n[resource]\nscript = ExtResource("1")\nId = "unlock_'+id+'"\nTitle = '+title+'\nIcon = '+icon+'\nEffect = 7\nAbilityId = "'+id+'"\nMaximumRank = 1\n')
(G/'classes.json').write_text(json.dumps(profiles,indent=2))
edit(G/'Abilities/shiv.tres',dict(StartsUnlocked=True))
# Six freely selectable one-rank talents per tree; shared Lethality implementation.
trees={'medicine':[('Medical Consult','healing','koltoinjection',.3),('Medical Engineering','hook','',1,'agent_medical'),('Surgical Precision','hook','',1,'agent_surgical'),('Medical Therapy','healing','koltoprobe',.4),('Patient Studies','cooldown','koltoinjection',.25),('Accomplished Doctor','healing','surgicalprobe',.4)],'concealment':[('Collateral Strike','bounces','laceration',2),('Culling','damage','laceration',.3),('Acid Blade','hook','',1,'agent_acid'),('Flanking','radius','backstab',.5),('Waylay','cooldown','hiddenstrike',.3),('Advanced Cloaking','cooldown','cloakingscreen',.4)],'marksmanship':[('Ballistic Dampers','hook','',1,'agent_dampers'),('Recoil Control','cooldown','followthrough',.4),('Penetrating Blasts','bounces','ambush',2),('Snipers Nest','hook','',1,'agent_nest'),('Rapid Fire','cooldown','seriesofshots',.3),('Imperial Assassin','damage','ambush',.35)],'engineering':[('Cluster Bombs','radius','explosiveprobe',.5),('Imperial Methodology','hook','',1,'agent_methodology'),('Efficient Engineering','cooldown','plasmaprobe',.3),('Experimental Explosives','damage','fragmentationgrenade',.4),('Electrified Railgun','hook','',1,'agent_railgun'),('Stroke of Genius','cooldown','empdischarge',.4)],'agent_lethality':[('Lethal Injectors','duration','dart',4),('Corrosive Microbes','periodic_rate','dart',.3),('Lingering Toxins','duration','corrosivegrenade',4),('Devouring Microbes','hook','',1,'agent_microbes'),('Lethal Purpose','hook','',1,'agent_purpose'),('License to Kill','damage','agentcull',.35)]}
talents=[]
for tree,rows in trees.items():
 classes=['operative','sniper'] if tree=='agent_lethality' else ['operative'] if tree in ['medicine','concealment'] else ['sniper']
 for i,row in enumerate(rows):
  name,kind,ab,amount,*key=row
  desc={'agent_medical':'Kolto Injection generates Tactical Advantage.','agent_surgical':'Surgical Probe refunds Advantage below 30% health.','agent_acid':'Backstab applies your equipped Corrosive Dart.','agent_dampers':'Stationary firing reduces incoming damage by 15%.','agent_nest':'Stationary precision damage bonus increases by 15%.','agent_methodology':'EMP Discharge also refreshes Explosive Probe.','agent_railgun':'Series of Shots applies your equipped Interrogation Probe.','agent_microbes':'Cull deals 20% more damage below 30% enemy health.','agent_purpose':'Poison ticks recover Cull cooldown once every two seconds.'}.get(key[0] if key else '',f'{ab}: {kind} improves by {amount*100:g}%.' if kind!='duration' else f'{ab} lasts {amount:g} seconds longer.')
  talents.append(dict(Id=tree+'.'+name.lower().replace(' ','_'),Classes=classes,Tree=tree,Tier=i//3,Column=i%3,Name=name,Icon='res://Assets/UI/icons/toxicdart.png' if tree=='agent_lethality' else 'res://Assets/UI/icons/snipe.png' if classes==['sniper'] else 'res://Assets/UI/icons/criticalblade.png',Description=desc,MaxRank=1,Cost=1,RequiredPoints=0,Effects=[dict(Kind=kind,Ability=ab,Amount=amount,Key=key[0] if key else '')],Source='April Agent talent names where confirmed; survivors effects are adaptations. See agent-april-research.json.'))
(G/'Talents/agent.json').write_text(json.dumps(dict(Version=1,Budget=10,Trees=[dict(Id=x,Name='Lethality' if x=='agent_lethality' else x.title()) for x in trees],Talents=talents),indent=2))
