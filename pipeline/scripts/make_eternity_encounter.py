import json
from pathlib import Path
root=Path('pipeline/godot/Gameplay');out=root/'Planets/EternityVault';out.mkdir(parents=True,exist_ok=True)
template=(root/'Planets/Korriban/SpittingTukata.tscn').read_text()
stride=json.loads(Path('pipeline/manifests/eternity-stride-calibration.json').read_text())
# The common actor composition retains existing collision, health, presentation and reward paths.
def scene(name,model,title,hp,damage,speed,xp,scale=1,radius=.035,height=.18,pattern=None,tier=0,ref=1):
 t=(root/'Planets/Korriban/ProtectorBoss.tscn').read_text();t=t.replace('res://Assets/Korriban/D4/Enemies/protector.glb',f'res://Assets/Maps/eternity_vault/Enemies/{model}.glb')
 import re
 def line(key,value):
  nonlocal t
  t=re.sub(r'^'+key+r' = .*$',key+' = '+value,t,flags=re.M)
 for key,value in [('DisplayName',json.dumps(title)),('ExperienceReward',str(xp)),('Maximum',str(hp)),('AttackDamage',str(damage)),('MoveSpeed',str(speed)),('RewardTier',str(tier)),('CollisionRadius',str(radius)),('CollisionHeight',str(height)),('radius',str(radius)),('height',str(height)),('AimHeight',str(height*.65)),('KnockbackResistance',str(1 if tier==2 else .15)),('GuaranteedReward','true' if tier else 'false'),('ScaleWithWaves','true')]:line(key,value)
 t=t.replace('position = Vector3(0,0.1,0)',f'position = Vector3(0,{height/2},0)').replace('scale = Vector3(0.95,0.95,0.95)',f'scale = Vector3({scale},{scale},{scale})')
 t=t.replace('WeaponScene = "res://Assets/D3/pistol.glb"','WeaponScene = "res://Assets/Maps/eternity_vault/Enemies/staff.glb"' if model=='guard' else '')
 if pattern=='xrr3':
  t=t.replace('res://Gameplay/TombAttack.cs','res://Gameplay/Xrr3Attack.cs');t=t[:t.index('[node name="TombAttack"')]+ '[node name="Xrr3Attack" type="Node" parent="Actor"]\nscript = ExtResource("5")\n'
 elif pattern:
  line('Pattern',json.dumps(pattern));line('Damage',str(7 if tier==0 else 16));line('Windup','1.5');line('Cooldown','5.5');line('TriggerRange','1.2')
  t=t.replace('WarningCue = "d4_droid"','WarningCue = "burrow_warning"')
  if model=='guard':
   t=t.replace('WarningCue = "burrow_warning"','WarningCue = "ev_guard_warning"')
   t+='ImpactCue = "ev_guard_impact"\n'
  if model=='guard' and pattern=='bolt':t+='InstructionOverride = "SECURITY LOCK · SIDESTEP THE FIRING LANE"\n'
 else:
  t=t[:t.index('[node name="TombAttack"')]
 if tier==2:
  nominal=stride[model]['unitScaleStrideSpeed']*scale
  t=t.replace('[node name="Original" parent="Actor/Visual"',f'RunClip = "Walk"\nLocomotionStrideSpeed = {nominal:.6f}\n[node name="Original" parent="Actor/Visual"')
  t=t.replace('position = Vector3(0,0.275,0)',f'position = Vector3(0,{height+.075:.3f},0)')
  if name!='Xrr3':line('MeleeRange','0.248')
 if name=='IceCat':
  t=t.replace('load_steps=8','load_steps=9').replace('[ext_resource type="Script"', '[ext_resource type="Script" path="res://Gameplay/NpcVocalPresentation.cs" id="vocals"]\n[ext_resource type="Script"',1)
  vocal='[node name="Vocals" type="Node" parent="Actor"]\nscript = ExtResource("vocals")\n'
  for key,media in [('HurtClips',[41823499,764195414,110314226,526138516]),('DeathClips',[806132088,44205520,846499615])]:
   vocal+=key+' = Array[String]('+json.dumps([f'res://Assets/NpcVocals/Manka/{mid}.wav' for mid in media])+')\n'
  t=t.replace('[node name="Visual"',vocal+'[node name="Visual"',1)
 (out/(name+'.tscn')).write_text(t,encoding='utf-8')
scene('IceCat','icecat','Prowling Ice Cat',26,4,.245,4,scale=1,height=.14,radius=.035)
scene('Tauntaun','tauntaun','Roaming Tauntaun',38,5,.19,5,scale=1.2,height=.22,radius=.04)
scene('Guard','guard','Prison Guard',100,8,.20,10,scale=1.05,height=.23,radius=.04,pattern='cleave')
scene('Gunner','guard','Prison Suppressor',145,7,.17,12,scale=1.05,height=.23,radius=.04,pattern='bolt')
scene('IceMatriarch','icecat','Whitefang Matriarch',550,7,.30,90,scale=3.2,height=.44,radius=.10,pattern='cleave',tier=2)
scene('GuardCaptain','guard','Prison Guard Captain',2200,10,.28,160,scale=2.6,height=.56,radius=.10,pattern='crossfire',tier=2)
scene('SiegeWarden','guard','Rakata Siege Warden',4400,12,.26,220,scale=2.9,height=.60,radius=.11,pattern='trial',tier=2)
scene('Xrr3','xrr3','Annihilation Droid XRR-3',18000,14,.18,600,scale=4.5,height=.65,radius=.16,pattern='xrr3',tier=2)
base='res://Gameplay/Planets/EternityVault/'
for name,title in [('IceMatriarch','Whitefang Matriarch'),('GuardCaptain','Prison Guard Captain'),('SiegeWarden','Rakata Siege Warden'),('Xrr3','Annihilation Droid XRR-3')]:
 (out/(name+'Encounter.tres')).write_text(f'''[gd_resource type="Resource" script_class="BossEncounterDefinition" load_steps=3 format=3]
[ext_resource type="Script" path="res://Gameplay/BossEncounterDefinition.cs" id="1"]
[ext_resource type="PackedScene" path="{base+name}.tscn" id="2"]
[resource]
script = ExtResource("1")
Id = "eternity_vault.{name.lower()}"
Title = "{title}"
Scene = ExtResource("2")
IntroductionSeconds = 1.8
RegularSpawnIntervalMultiplier = 1.0
RegularCapFraction = 1.0
ClearRegularEnemiesOnStart = false
''')
k=json.loads((root/'Planets/Korriban/run.json').read_text());d=dict(CoreLevel=50,FirstLevelExperience=k['FirstLevelExperience'],ExperienceGrowth=k['ExperienceGrowth'],Difficulty=dict(k['Difficulty']),ArtifactCatalog=k['ArtifactCatalog'],AudioTuning=k['AudioTuning'],Endless=False,EnemyExperienceDropEvery=2,Caches=[])
d['AudioTuning']=base+'audio.json'
audio=json.loads((root/'Planets/Korriban/audio.json').read_text())
audio['cues']['ev_guard_warning']=dict(audio['cues']['burrow_warning'],category='Combat',gainDb=-9,cooldown=1.5,duration=.65,priority=False)
audio['cues']['ev_guard_impact']=dict(audio['cues']['burrow_impact'],category='Combat',gainDb=-18,cooldown=1.0,duration=.65,priority=False)
(out/'audio.json').write_text(json.dumps(audio,indent=2)+'\n')
d['EnemyExperienceMultiplier']=2
d['ContinuousEncounters']=True
d['Difficulty'].update(InitialPopulation=8,MaximumPopulation=24,InitialSpawnInterval=.6,MinimumSpawnInterval=.20,RecoverySeconds=0,ReturnRampSeconds=0,ExponentialDamageScaling=True)
stages=[(1,'Snow Wilderness',[('IceCat',1)],'b:enc.location.flashpoint.raid.eternity_vault.flavor.enc_snow_wilderness'),(4,'Wildlife Migration',[('IceCat',3),('Tauntaun',2)],'a:enc.location.flashpoint.raid.eternity_vault.flavor.enc_snow_wilderness'),(11,'Prison Guard Patrol',[('Guard',4),('IceCat',2),('Tauntaun',1)],'a:enc.location.flashpoint.raid.eternity_vault.enemy.enc_trash_prison_guards'),(16,'Security Response',[('Guard',4),('Gunner',2),('IceCat',1)],'authored mixed security response'),(26,'Rakata Lockdown',[('Guard',4),('Gunner',3),('IceCat',2)],'authored mixed lockdown'),(36,'Siege Protocol',[('Guard',3),('Gunner',4),('IceCat',2),('Tauntaun',1)],'authored siege pressure'),(46,'Annihilation Protocol',[('Guard',4),('Gunner',3),('IceCat',1)],'authored final approach')]
d['Stages']=[dict(Level=l,Region=n,Description=('XRR-3 activates at level 50. Keep routes open and evade security fire.' if l>=46 else 'Hold the surface. Kill hostiles, collect experience and prepare for the next security escalation.'),Focus=[-64,2.8,30.7],AmbientGain=-22,SourceEncounter=src,Enemies=[dict(Scene=base+e+'.tscn',Weight=w) for e,w in mix]) for l,n,mix,src in stages]
d['Milestones']=[dict(Level=l,Encounter=base+n+'Encounter.tres') for l,n in [(10,'IceMatriarch'),(25,'GuardCaptain'),(40,'SiegeWarden'),(50,'Xrr3')]]
(out/'run.json').write_text(json.dumps(d,indent=2)+'\n')
# Preserve small-map migration and source labels in the destination description.
p=Path('pipeline/godot/Frontend/Data/catalog.json');c=json.loads(p.read_text());next(m for m in c['Maps'] if m['Id']=='eternity_vault')['Description']='Fight from level 1 to 50 across the frozen surface. Survive wildlife, prison guards and the Annihilation Droid XRR-3.';p.write_text(json.dumps(c,indent=2)+'\n')
print('ETERNITY RUN',len(d['Stages']),'stages,',len(d['Milestones']),'milestones')
