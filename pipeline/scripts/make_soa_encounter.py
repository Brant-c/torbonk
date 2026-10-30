"""Authored level 1–50 Soa run using separately verified April NPC art."""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];game=ROOT/'godot/Gameplay';out=game/'Planets/Soa';out.mkdir(parents=True,exist_ok=True)
base='res://Gameplay/Planets/Soa/'
def scene(key,model,title,hp,damage,speed,xp,scale=1,height=.23,radius=.04,pattern=None,tier=0,weapon='',image=False):
    attack='SoaAttack' if model=='soa' else 'TombAttack'
    extra=1 if pattern or model=='soa' else 0
    text=f'''[gd_scene load_steps={7+extra+(model=='manka')} format=3]
[ext_resource type="Script" path="res://Gameplay/EnemyAgent.cs" id="1"]
[ext_resource type="Script" path="res://Gameplay/Health.cs" id="2"]
[ext_resource type="Script" path="res://Gameplay/EnemyPresentation.cs" id="3"]
[ext_resource type="PackedScene" path="res://Assets/Maps/soa_p3/Enemies/{model}.glb" id="4"]
[ext_resource type="FontFile" path="res://Assets/UI/EurostileBold.ttf" id="6"]
'''
    if extra:text+=f'[ext_resource type="Script" path="res://Gameplay/{attack}.cs" id="5"]\n'
    if model=='manka':text+='[ext_resource type="Script" path="res://Gameplay/NpcVocalPresentation.cs" id="vocals"]\n'
    text+=f'''
[sub_resource type="CapsuleShape3D" id="Capsule"]
radius = {radius}
height = {height}
margin = 0.0005
[node name="{key}" type="CharacterBody3D"]
[node name="CollisionShape3D" type="CollisionShape3D" parent="."]
position = Vector3(0,{height/2},0)
shape = SubResource("Capsule")
[node name="Actor" type="Node3D" parent="."]
script = ExtResource("1")
Team = 2
DisplayName = {json.dumps(title)}
ExperienceReward = {xp}
CollisionRadius = {radius}
CollisionHeight = {height}
MoveSpeed = {speed}
AttackDamage = {damage}
AttackInterval = 1.65
DetectionRange = 12.0
MeleeRange = {radius+.09}
AimHeight = {height*.65}
RewardTier = {tier}
GuaranteedReward = {'true' if tier==2 else 'false'}
KnockbackResistance = {1 if tier==2 else .45 if tier==1 else .15}
ScaleWithWaves = true
[node name="Health" type="Node" parent="Actor"]
script = ExtResource("2")
Maximum = {hp}
[node name="Visual" type="Node3D" parent="Actor"]
script = ExtResource("3")
'''
    if weapon=='staff':text+='WeaponScene = "res://Assets/Maps/soa_p3/Enemies/staff.glb"\n'
    elif weapon:text+=f'Weapon = "{weapon}"\nRedBlade = true\n'
    text+=f'''[node name="Original" parent="Actor/Visual" instance=ExtResource("4")]
scale = Vector3({scale},{scale},{scale})
[node name="Nameplate" type="Label3D" parent="Actor"]
position = Vector3(0,{height+.06},0)
pixel_size = 0.00030
billboard = 1
font = ExtResource("6")
font_size = 20
outline_size = 6
modulate = Color(1,0.42,0.23,1)
'''
    if extra:
        text+=f'[node name="{attack}" type="Node" parent="Actor"]\nscript = ExtResource("5")\n'
        if model=='soa':text+=f'Image = {str(image).lower()}\n'
        else:
            text+=f'Pattern = "{pattern}"\nDamage = {damage}\nWindup = {1.6 if tier else 1.5}\nCooldown = {6 if tier else 7}\nTriggerRange = 2.0\n'
            text+='InstructionOverride = "'+('PUZZLE GUARDIAN · EVADE THE CLAW SWEEP' if model=='acklay' else 'RAKATA WATCHER · SIDESTEP THE FIRING LANE' if model=='watcher' else 'INFERNAL COUNCIL · LEAVE THE LOCKED WARNING')+'"\n'
            if model=='watcher':text+='WarningCue = "ev_guard_warning"\nImpactCue = "ev_guard_impact"\n'
    if model=='manka':
        original=(game/'Planets/EternityVault/IceCat.tscn').read_text()
        clips='\n'.join(line for line in original.splitlines() if line.startswith(('HurtClips =','DeathClips =')))
        vocals='[node name="Vocals" type="Node" parent="Actor"]\nscript = ExtResource("vocals")\n'+clips+'\n'
        text=text.replace('[node name="Visual"',vocals+'[node name="Visual"',1)
    (out/(key+'.tscn')).write_text(text,encoding='utf-8')
scene('Manka','manka','Primeval Manka Patriarch',28,4,.24,4,height=.14)
scene('Rakata','rakata','Rakata Eternal Guardian',68,6,.21,7,weapon='staff')
scene('Watcher','watcher','Rakata Eternal Watcher',90,6,.18,9,pattern='bolt',weapon='staff')
scene('Acklay','acklay','Proto Acklay',72,6,.21,9,scale=1.0,height=.26,radius=.06)
scene('PuzzleGuardian','acklay','Ancient Pylon Guardian',800,10,.23,120,scale=1.8,height=.43,radius=.10,pattern='cleave',tier=2)
council=[('Luthro','luthro','Luthro the Kinslayer','cleave'),('Kahesh','kahesh','Kahesh, the Reborn','crossfire'),('Jekhop','yshaar','Jekhop, the Bowed Lord','cleave'),('Alarii','alarii','Alarii, Sister of Sorrow','bolt'),('Fahren','fahren','Fahren Jaj, the Laughing Lord','trial'),('Yshaar','yshaar','Yshaar Kael, Betrayer of Adas','cleave'),('Zaheen','zaheen',"Za\'heen Kaur, the Bowed Lord",'crossfire'),('Jael','jael','Jael, the Silencer','bolt'),('Serrod','serrod','Serrod the Messenger','trial'),
 ('Kyyrah','serrod','Kyyrah, Jewel of Xo','trial'),('Rhoteb','zaheen',"R\'hoteb, the Bowed Lord",'cleave'),('Doruk','kahesh','Doruk, the Uncaged','crossfire'),('WeivalLesser','alarii',"Wei\'val the Lesser",'bolt'),('WeivalElder','fahren',"Wei\'val the Elder",'trial'),('Cyrisop','luthro','Cyrisop, the Exiled','cleave'),('Keryha','alarii','Keryha, Sister of Sorrow','bolt')]
for key,model,title,pattern in council:scene(key,model,title,650 if key=='Luthro' else 420,8,.23,70,scale=1.5,height=.32,radius=.055,pattern=pattern,tier=2 if key=='Luthro' else 1,weapon='saber')
scene('SoaImage','soa',"Soa\'s Image",7500,12,.21,350,scale=3.6,height=.62,radius=.12,tier=2,image=True)
scene('Soa','soa','Soa, the Infernal One',18000,16,.20,700,scale=4.5,height=.78,radius=.14,tier=2)
def encounter(name,title,group=None):
    scenes=[name]+(group or [])
    text=f'[gd_resource type="Resource" script_class="BossEncounterDefinition" load_steps={len(scenes)+2} format=3]\n[ext_resource type="Script" path="res://Gameplay/BossEncounterDefinition.cs" id="script"]\n'
    for i,key in enumerate(scenes):text+=f'[ext_resource type="PackedScene" path="{base+key}.tscn" id="{i}"]\n'
    text+=f'[resource]\nscript = ExtResource("script")\nId = "soa.{name.lower()}"\nTitle = {json.dumps(title)}\nScene = ExtResource("0")\nIntroductionSeconds = 1.8\nClearRegularEnemiesOnStart = false\nRegularCapFraction = 1.0\nRegularSpawnIntervalMultiplier = 1.0\n'
    if group:text+='SpawnRegularEnemies = true\nMaximumConcurrentMembers = 3\nAdditionalScenes = Array[PackedScene](['+', '.join(f'ExtResource("{i}")' for i in range(1,len(scenes)))+'])\n'
    (out/(name+'Encounter.tres')).write_text(text,encoding='utf-8')
encounter('PuzzleGuardian','Ancient Pylon Guardian')
encounter('Luthro','The Infernal Council',[x[0] for x in council[1:]])
encounter('SoaImage',"Soa\'s Image")
encounter('Soa','Soa, the Infernal One')
d=json.loads((game/'Planets/EternityVault/run.json').read_text());d['Caches']=[];d['ContinuousEncounters']=True;d['Difficulty'].update(RecoverySeconds=0,ReturnRampSeconds=0)
d['Difficulty'].update(InitialPopulation=6,MaximumPopulation=22,InitialSpawnInterval=.7,MinimumSpawnInterval=.25)
stages=[(1,'Jungle Incursion',[('Manka',3),('Rakata',1)],'a:enc.location.flashpoint.raid.eternity_vault.enemy.enc_trash_jungle'),(4,'Eternal Guardians',[('Manka',2),('Rakata',3)],'f:enc.location.flashpoint.raid.eternity_vault.enemy.enc_trash_jungle'),(11,'Pylon Defenders',[('Rakata',3),('Watcher',1),('Manka',1)],'d:enc.location.flashpoint.raid.eternity_vault.enemy.enc_boss_puzzle_guardian_2'),(18,'Ancient Hunters',[('Rakata',2),('Watcher',2),('Acklay',1)],'authored jungle escalation'),(26,'The Council\'s Legacy',[('Watcher',3),('Rakata',2),('Acklay',1)],'authored post-Council escalation'),(34,'Infernal Prison',[('Watcher',3),('Acklay',2),('Rakata',2)],'authored prison pressure'),(41,'The False God',[('Rakata',2),('Watcher',3),('Acklay',2)],'authored image aftermath'),(46,'The Infernal One',[('Rakata',3),('Watcher',3),('Acklay',2)],'authored final approach')]
d['Stages']=[dict(Level=level,Region=region,Description='Survive the ancient guardians. Defeat the Council and confront Soa in the Infernal Prison.',Focus=[82.5,-38.73,69],AmbientGain=-22,SourceEncounter=source,Enemies=[dict(Scene=base+key+'.tscn',Weight=w) for key,w in mix]) for level,region,mix,source in stages]
d['Milestones']=[dict(Level=level,Encounter=base+key+'Encounter.tres') for level,key in [(10,'PuzzleGuardian'),(25,'Luthro'),(40,'SoaImage'),(50,'Soa')]]
(out/'run.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
p=game/'GameWorld_SoaP3.tscn';text=p.read_text().replace('ExplorationOnly = true','PlanetRunPath = "res://Gameplay/Planets/Soa/run.json"');p.write_text(text)
p=ROOT/'godot/Frontend/Data/catalog.json';c=json.loads(p.read_text());next(m for m in c['Maps'] if m['Id']=='eternity_vault')['Region']='Belsavis';m=next(m for m in c['Maps'] if m['Id']=='soa_p3');m['Description']='Survive the jungle guardians, challenge the Infernal Council, and face Soa\'s image and the Infernal One.';m['PreviewModel']='res://Assets/Maps/soa_p3/Enemies/soa.glb';p.write_text(json.dumps(c,indent=2)+'\n')
print('SOA_RUN_COMPLETE',len(stages),'stages',len(council),'Council members, 4 milestones')
