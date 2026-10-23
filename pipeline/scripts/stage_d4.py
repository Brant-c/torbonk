"""D4-only scene/config authoring. Original A4 and other planet outputs are preserved."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];GAME=ROOT/'godot/Gameplay';OUT=GAME/'Planets/Korriban';OUT.mkdir(parents=True,exist_ok=True)

def scene(name,model,hp,speed,damage,xp,radius=.03,height=.185,scale=1,pattern='',tier=0,weapon='',**attacks):
    text=f'''[gd_scene load_steps={8 if pattern else 7} format=3]
[ext_resource type="Script" path="res://Gameplay/EnemyAgent.cs" id="1"]
[ext_resource type="Script" path="res://Gameplay/Health.cs" id="2"]
[ext_resource type="Script" path="res://Gameplay/EnemyPresentation.cs" id="3"]
[ext_resource type="PackedScene" path="{model}" id="4"]
[ext_resource type="FontFile" path="res://Assets/UI/EurostileBold.ttf" id="6"]
'''
    if pattern:text+='[ext_resource type="Script" path="res://Gameplay/TombAttack.cs" id="5"]\n'
    text+=f'''
[sub_resource type="CapsuleShape3D" id="Capsule"]
radius = {radius}
height = {height}
margin = 0.0005
[node name="{name.replace(' ','').replace(chr(39),'')}" type="CharacterBody3D"]
[node name="CollisionShape3D" type="CollisionShape3D" parent="."]
position = Vector3(0,{height/2},0)
shape = SubResource("Capsule")
[node name="Actor" type="Node3D" parent="."]
script = ExtResource("1")
Team = 2
DisplayName = "{name}"
ExperienceReward = {xp}
CollisionRadius = {radius}
CollisionHeight = {height}
MoveSpeed = {speed}
AttackDamage = {damage}
AttackInterval = 1.65
MeleeRange = {max(.11,radius+.09)}
AimHeight = {height*.6}
RewardTier = {tier}
GuaranteedReward = {'true' if tier else 'false'}
KnockbackResistance = {1 if tier==2 else .65 if tier else .15}
ScaleWithWaves = {'false' if tier else 'true'}
[node name="Health" type="Node" parent="Actor"]
script = ExtResource("2")
Maximum = {hp}.0
[node name="Visual" type="Node3D" parent="Actor"]
script = ExtResource("3")
'''
    if weapon:text+=f'WeaponScene = "{weapon}"\n'
    text+=f'''[node name="Original" parent="Actor/Visual" instance=ExtResource("4")]
scale = Vector3({scale},{scale},{scale})
[node name="Nameplate" type="Label3D" parent="Actor"]
position = Vector3(0,{height+.075},0)
pixel_size = 0.00030
billboard = 1
font = ExtResource("6")
font_size = 20
outline_size = 6
modulate = Color(1,0.42,0.23,1)
'''
    if pattern:
        text+='[node name="TombAttack" type="Node" parent="Actor"]\nscript = ExtResource("5")\nPattern = "'+pattern+'"\n'
        for k,v in attacks.items():text+=f'{k} = {json.dumps(v)}\n'
    return text

def main():
    # Three new silhouette families, plus source-supported elite variants of the existing slug/shyrack.
    enemies={
    'SpittingTukata':scene("Spitting Tuk'ata","res://Assets/Korriban/D4/Enemies/tukata.glb",48,.30,3,7,.035,.15,.9,'spit',Damage=7,Windup=1.15,Cooldown=5.5,WarningCue='d4_tukata'),
    'AncientDroid':scene('Ancient Droid Protector','res://Assets/Korriban/D4/Enemies/protector.glb',95,.14,4,10,.034,.20,.95,'bolt',weapon='res://Assets/D3/Weapons/blaster_low01_a01_v01.glb',Damage=8,Windup=1.4,Cooldown=5,WarningCue='d4_droid'),
    'Terentatek':scene('Terentatek Mauler','res://Assets/Korriban/D4/Enemies/beast.glb',150,.115,7,14,.055,.25,1.0,'cleave',Damage=12,Windup=1.5,Cooldown=7,TriggerRange=.5,WarningCue='d4_beast'),
    'BroodMother':scene("K'lor'slug Brood Mother",'res://Assets/Korriban/D4/Enemies/broodmother.glb',240,.13,5,40,.055,.13,1.0,'brood',1,Damage=4,Windup=1.5,Cooldown=7,TriggerRange=.6,SummonScene='res://Gameplay/Enemies/Broodling.tscn'),
    'Screecher':scene('Shyrack Screecher','res://Assets/Enemies/shyrack.glb',380,.23,5,65,.034,.16,.75,'screech',1,Damage=10,Windup=1.4,Cooldown=6,TriggerRange=.6,SummonScene='res://Gameplay/Enemies/Shyrack.tscn'),
    'MaulerElite':scene('Terentatek Mauler','res://Assets/Korriban/D4/Enemies/beast.glb',750,.13,8,100,.055,.25,1.0,'cleave',1,Damage=17,Windup=1.6,Cooldown=5.5,TriggerRange=.6,WarningCue='d4_beast'),
    'ProtectorBoss':scene('Ancient Droid Protector','res://Assets/Korriban/D4/Enemies/protector.glb',1550,.17,7,150,.034,.20,.95,'crossfire',2,weapon='res://Assets/D3/Weapons/blaster_low01_a01_v01.glb',Damage=18,Windup=1.65,Cooldown=5.5,TriggerRange=1.2,WarningCue='d4_droid'),
    'AncientBeast':scene('Ancient Beast','res://Assets/Korriban/D4/Enemies/beast.glb',2800,.14,10,250,.075,.34,1.5,'trial',2,Damage=23,Windup=1.8,Cooldown=5,TriggerRange=1.0,WarningCue='d4_beast')}
    for n,t in enemies.items():(OUT/(n+'.tscn')).write_text(t.replace('res://Assets/D3/Weapons/blaster_low01_a01_v01.glb','res://Assets/D3/pistol.glb'))
    brood=(GAME/'Enemies/Broodmatron.tscn').read_text().replace('ExperienceReward = 80','ExperienceReward = 90\nGuaranteedReward = true\nScaleWithWaves = false')
    (OUT/'Broodmatron.tscn').write_text(brood)
    encounters=[(5,'BroodMother',"K'lor'slug Brood Mother",False),(10,'Broodmatron',"K'lor'slug Broodmatron",True),
                (15,'Screecher','Shyrack Screecher',False),(20,'ProtectorBoss','Ancient Droid Protector',True),
                (25,'MaulerElite','Terentatek Mauler',False),(30,'AncientBeast','Ancient Beast',True)]
    milestones=[]
    for level,n,title,major in encounters:
        (OUT/(n+'Encounter.tres')).write_text(f'''[gd_resource type="Resource" script_class="BossEncounterDefinition" load_steps=3 format=3]
[ext_resource type="Script" path="res://Gameplay/BossEncounterDefinition.cs" id="1"]
[ext_resource type="PackedScene" path="res://Gameplay/Planets/Korriban/{n}.tscn" id="2"]
[resource]
script = ExtResource("1")
Id = "korriban.{n.lower()}"
Title = "{title}"
Scene = ExtResource("2")
IntroductionSeconds = {1.6 if major else 1.0}
RegularSpawnIntervalMultiplier = 3.0
RegularCapFraction = 0.35
''')
        milestones.append(dict(Level=level,Encounter=f'res://Gameplay/Planets/Korriban/{n}Encounter.tres'))
    def enemy(n,w):return dict(Scene=f'res://Gameplay/{"Enemies" if n in ["Broodling","Shyrack","CryptLooter"] else "Planets/Korriban"}/{n}.tscn',Weight=w)
    stages=[]
    for level,title,desc,focus,roster,gain in [
        (1,'Valley of the Dark Lords','The old tomb roads stir. Survive the Valley.',[36,-17.1,12.6],[('Broodling',8),('Shyrack',1)],-24),
        (6,'Tomb Precinct','Burial stones and hungry broods. The Broodmatron approaches.',[39,-17,17],[('Broodling',5),('Shyrack',3),('CryptLooter',2)],-22),
        (11,'Tomb Precinct','Ancient sentries awaken among the tomb ruins.',[41,-17,20],[('CryptLooter',4),('AncientDroid',2),('Shyrack',3),('Broodling',2)],-21),
        (16,'Lower Wilds','Tuk’ata hunt the broken cliffs. Keep clear of their spit.',[48,-17,23],[('SpittingTukata',5),('Shyrack',3),('Terentatek',1),('AncientDroid',2)],-19),
        (21,'Lower Wilds','Maulers gather while ancient security patrols the ruins.',[51,-17,25],[('SpittingTukata',4),('Terentatek',3),('AncientDroid',3),('CryptLooter',2)],-18),
        (26,'Forbidden Tombs','The final trial wakes. Ancient Fury follows the first wounds.',[51,-15.8,24.5],[('Terentatek',3),('AncientDroid',4),('SpittingTukata',2),('Shyrack',2)],-17)]:
        stages.append(dict(Level=level,Region=title,Description=desc,Focus=focus,AmbientGain=gain,Enemies=[enemy(n,w) for n,w in roster]))
    config=dict(CoreLevel=30,FirstLevelExperience=16,ExperienceGrowth=1.105,ArtifactCatalog='res://Gameplay/Planets/Korriban/artifacts.json',
                AudioTuning='res://Gameplay/Planets/Korriban/audio.json',Stages=stages,Milestones=milestones,Caches=[
        dict(Id='valley',Title='Valley Burial Cache',Position=[35.4,-17.1,13.5],Level=1,Reward='xp'),
        dict(Id='precinct',Title='Guarded Sith Relic',Position=[40,-17,18.5],Level=8,Reward='artifact',Guard='res://Gameplay/Planets/Korriban/AncientDroid.tscn'),
        dict(Id='wilds',Title='Wilds Restorative Cache',Position=[48,-17,23.5],Level=15,Reward='heal'),
        dict(Id='forbidden',Title='Forbidden Tomb Cache',Position=[51,-15.8,24.5],Level=23,Reward='artifact',Guard='res://Gameplay/Planets/Korriban/Terentatek.tscn')])
    (OUT/'run.json').write_text(json.dumps(config,indent=2))
    items=json.loads((GAME/'RunItems/artifacts.json').read_text())
    for id,name,icon,desc,effect,amount,tier in [
        ('stride','Pilgrim’s Seal','darksiderelic1','Move 12% faster. All classes.', 'stride',.12,1),
        ('leech','Predator’s Relic','darksiderelic3','Ability hits restore 2 HP, at most once per second. All classes.', 'leech',2,2),
        ('execution','Tomb Executioner’s Sigil','holocron','Deal 40% more damage to enemies below 35% health. All classes.', 'execution',.4,2)]:
        items.append(dict(Id='run.korriban.'+id,Name=name,Icon=icon,Source=f'/resources/gfx/icons/{icon}.dds',Description=desc,
                          Rarity=2 if tier==2 else 1,MinimumTier=tier,RequiresAbility='',Effect=effect,Amount=amount,HealthCost=0,PowerItem=False))
    next(a for a in items if a['Id']=='run.codex')['Description']='Deal 30% more ability damage. Maximum health is reduced by 20%.'
    (OUT/'artifacts.json').write_text(json.dumps(items,indent=2))
    # Derive a D4 scene compiler from today's map compiler, retaining A4's actual scene as its foundation.
    t=(ROOT/'godot/BuildMap.gd').read_text()
    a=t.index('func _initialize():');b=t.index('func build():')
    t=t[:a]+'func _initialize(): call_deferred("build")\n'+t[b:]
    t=t.replace('"res://Assets/Maps/"+map_id+"/environment.json"','"res://Assets/Korriban/D4/environment.json"')
    t=t.replace('var root = Node3D.new(); root.name = "MapEnvironment"; get_root().add_child(root)',
                'var root = load("res://Assets/Korriban/KorribanEnvironment.tscn").instantiate(); get_root().add_child(root)')
    t=t.replace('var terrain = Node3D.new(); terrain.name = "Terrain"; root.add_child(terrain); terrain.owner = root','var terrain = root.get_node("Terrain")')
    t=t.replace('var props = Node3D.new(); props.name = "Props"; root.add_child(props); props.owner = root','var props = root.get_node("Props")')
    t=t.replace('for p in data.terrain:\n','for p in data.terrain:\n\t\tif terrain.has_node("Terrain_"+p.id): continue\n',1)
    t=t.replace('var nodes = {}','var nodes = {}\n\tfor node in props.get_children():\n\t\tindex_existing(node,nodes)\n\tvar existing_ids = nodes.keys()')
    t=t.replace('"res://../manifests/map-"+map_id+"-model-conversion.json"','"res://../manifests/d4-environment-model-conversion.json"')
    t=t.replace('for p in data.placements:\n\t\tvar node = Node3D.new()', 'for p in data.placements:\n\t\tif nodes.has(p.id): continue\n\t\tvar node = Node3D.new()',1)
    t=t.replace('if not parent.is_inside_tree(): continue','if nodes[p.id].is_inside_tree(): pending.erase(p); continue\n\t\t\tif not parent.is_inside_tree(): continue')
    t=t.replace('if data.modelOutputs.has(p.path)', 'if p.id in existing_ids: continue\n\t\tif data.modelOutputs.has(p.path)',1)
    t=t.replace('for id in nodes:\n\t\tvar t:', 'for id in nodes:\n\t\tif not data.expectedMatrices.has(id): continue\n\t\tvar t:')
    t=t.replace('"res://Assets/Maps/"+map_id+"/MapEnvironment.tscn"','"res://Assets/Korriban/D4/KorribanTrials.tscn"')
    t=t.replace('"res://../manifests/map-"+map_id+"-godot-build.json"','"res://../manifests/d4-environment-godot-build.json"')
    t+='\nfunc index_existing(node: Node,nodes: Dictionary):\n\tif node.has_meta("source_instance_id"): nodes[str(node.get_meta("source_instance_id"))] = node\n\tfor child in node.get_children(): index_existing(child,nodes)\n'
    (ROOT/'godot/BuildD4.gd').write_text(t)

if __name__=='__main__':main()
