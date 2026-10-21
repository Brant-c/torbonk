"""Author reusable scene compositions; imported originals remain untouched."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'godot/Gameplay/Enemies';OUT.mkdir(exist_ok=True)
for name,asset,label,scale,radius,height,hp,speed,damage,interval,reach,aim,xp in [
 ('Broodling','broodling',"K’lor’slug Broodling",.6,.025,.055,16,.20,2,1.5,.105,.035,2),
 ('Shyrack','shyrack','Shyrack Forager',1.15,.034,.16,30,.28,3,1.35,.12,.15,4),
 ('CryptLooter','looter','Crypt Looter',1,.03,.185,55,.18,5,1.4,.14,.11,7),
 ('Broodmatron','broodmatron',"K’lor’slug Broodmatron",1.1,.065,.14,650,.14,8,1.8,.23,.065,80)]:
 boss=name=='Broodmatron'
 text=f'''[gd_scene load_steps={7 if boss else 6} format=3]
[ext_resource type="Script" path="res://Gameplay/EnemyAgent.cs" id="1"]
[ext_resource type="Script" path="res://Gameplay/Health.cs" id="2"]
[ext_resource type="Script" path="res://Gameplay/EnemyPresentation.cs" id="3"]
[ext_resource type="PackedScene" path="res://Assets/Enemies/{asset}.glb" id="4"]
'''
 if boss:text+='[ext_resource type="Script" path="res://Gameplay/BossEncounter.cs" id="5"]\n'
 text+=f'''
[sub_resource type="CapsuleShape3D" id="Capsule"]
radius = {radius}
height = {height}
margin = 0.0005
[node name="{name}" type="CharacterBody3D"]
[node name="CollisionShape3D" type="CollisionShape3D" parent="."]
position = Vector3(0, {height/2}, 0)
shape = SubResource("Capsule")
[node name="Actor" type="Node3D" parent="."]
script = ExtResource("1")
Team = 2
DisplayName = "{label}"
ExperienceReward = {xp}
CollisionRadius = {radius}
CollisionHeight = {height}
MoveSpeed = {speed}
AttackDamage = {damage}
AttackInterval = {interval}
MeleeRange = {reach}
AimHeight = {aim}
[node name="Health" type="Node" parent="Actor"]
script = ExtResource("2")
Maximum = {hp}.0
[node name="Visual" type="Node3D" parent="Actor"]
script = ExtResource("3")
'''
 if asset=='looter':text+='Weapon = "electroblade"\n'
 text+=f'''[node name="Original" parent="Actor/Visual" instance=ExtResource("4")]
scale = Vector3({scale}, {scale}, {scale})
[node name="Nameplate" type="Label3D" parent="Actor"]
position = Vector3(0, {max(.14,aim+.15)}, 0)
pixel_size = 0.00032
billboard = 1
text = "{label}"
font_size = 22
outline_size = 6
modulate = Color(1, 0.42, 0.23, 1)
'''
 if boss:text+='[node name="BossEncounter" type="Node" parent="Actor"]\nscript = ExtResource("5")\n'
 (OUT/(name+'.tscn')).write_text(text,encoding='utf-8')
(OUT/'roster.json').write_text(json.dumps(dict(enemies=[dict(scene=f'res://Gameplay/Enemies/{name}.tscn',unlockWave=wave,weight=weight,weightPerWave=growth) for name,wave,weight,growth in [('Broodling',1,8,-.25),('Shyrack',2,3,.4),('CryptLooter',3,2,.65)]]),indent=2))
# Same import policy as the accepted character rig, for only the new GLBs.
template=(ROOT/'godot/Assets/sorcerer.glb.import').read_text();params=template[template.index('[params]'):]
for p in (ROOT/'godot/Assets/Enemies').glob('*.glb'):
 p.with_suffix('.glb.import').write_text('[remap]\nimporter="scene"\nimporter_version=1\ntype="PackedScene"\n\n[deps]\nsource_file="res://Assets/Enemies/'+p.name+'"\n\n'+params)
print('Staged B6 enemy scenes and roster')
