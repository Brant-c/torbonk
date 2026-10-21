"""D3 content registration, additive to the D1/D2 catalogs."""
import json,copy,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
read=lambda p:json.loads((ROOT/p).read_text())
def write(p,v):(ROOT/p).write_text(json.dumps(v,indent=2))
catalog=read('godot/Gameplay/Equipment/catalog.json')
for key in ['Items','Appearances','Presets']:catalog[key]=[v for v in catalog[key] if not v['Id'].startswith('d3.')]
compat=copy.deepcopy(catalog['Appearances'][0]['Compatibility']);compat['BaseClasses']=[];compat['AdvancedClasses']=[]
mapping={'chest':'chest','hand':'hands','leg':'legs','boot':'feet','waist':'waist'}
for family in ['war','spy','bh']:
    report=read(f'manifests/d3-{family}-character-probe.json');scene=f'res://Assets/D3/{family}.glb'
    shutil.copyfile(ROOT/f'generated/d3-{family}-sorcerer.glb',ROOT/f'godot/Assets/D3/{family}.glb')
    for raw,slot in mapping.items():
        identity=f'd3.{family}.{slot}'
        catalog['Appearances'].append(dict(Id=identity,Slot=slot,Compatibility=compat,Scene=scene,MeshNames=[m['name'] for m in report['meshes'] if m['slot']==raw],RequiredBones=[],CoversSlots=[],HidesBaseGroups=[slot],Attachment=None))
        item=copy.deepcopy(catalog['Items'][0]);item.update(Id=identity,DisplayName=f'{family.upper()} April starting {slot}',Slot=slot,Category='heavy_armor' if family=='bh' else 'medium_armor',Appearance=identity,Source=None,Icon='',StatModifiers={},BlocksSlots=[]);catalog['Items'].append(item)
for name,category,slots,blade in [('saber','lightsaber',['main_hand','off_hand'],True),('staff','double_lightsaber',['main_hand'],True),('rifle','rifle',['main_hand'],False),('sniper','sniper_rifle',['main_hand'],False),('pistol','pistol',['main_hand','off_hand'],False)]:
    for slot in slots:
        identity=f'd3.{name}.{slot}';a=copy.deepcopy(next(a for a in catalog['Appearances'] if a['Id']=='red_saber'))
        a.update(Id=identity,Slot=slot,Scene='res://Assets/Enemies/saber.glb' if name=='saber' else f'res://Assets/D3/{name}.glb',Locomotion=None)
        bone='RightWeapon' if slot=='main_hand' else 'LeftWeapon';a['RequiredBones']=[bone];a['Attachment'].update(Bone=bone,RedBlade=blade,DoubleBlade=name=='staff');catalog['Appearances'].append(a)
        item=copy.deepcopy(next(i for i in catalog['Items'] if i['Id']=='d1.red_saber'));item.update(Id=identity,DisplayName=name.title(),Slot=slot,Category=category,Appearance=identity,BlocksSlots=['off_hand'] if name in ['staff','rifle','sniper'] else []);catalog['Items'].append(item)
rows=[('juggernaut','sith_warrior','war','smash','saber','','',''),('marauder','sith_warrior','war','rupture','saber','saber','DualIdle','DualRun'),('assassin','sith_inquisitor','inq','lacerate','staff','','PoleIdle','PoleRun'),('operative','imperial_agent','spy','dart','rifle','','RifleIdle','RifleRun'),('sniper','imperial_agent','spy','snipershot','sniper','','RifleIdle','RifleRun'),('mercenary','bounty_hunter','bh','tracer','pistol','pistol','PistolsIdle','PistolsRun'),('powertech','bounty_hunter','bh','flamesweep','pistol','','PistolIdle','PistolRun')]
profiles=[dict(Id='sorcerer',Faction='empire',BaseClass='sith_inquisitor',Appearance='inquisitor_level1_april2012_bmn',EquipmentPreset='inquisitor_level1',StartingAbility='lightning',Presentation='sorcerer',AnimationScene='res://Assets/Abilities/casts.glb',ArtifactEffects=[],TalentPool=[])]
for adv,base,family,ability,main,off,idle,run in rows:
    items={slot:f'd3.{family}.{slot}' if family!='inq' else 'level1.'+slot for slot in mapping.values()};items['main_hand']=f'd3.{main}.main_hand'
    if off:items['off_hand']=f'd3.{off}.off_hand'
    preset='d3.'+adv;catalog['Presets'].append(dict(Id=preset,DisplayName=f'{adv.title()} April starting family (documented alternative)',Items=items,Cosmetics={},DeveloperOnly=False))
    cl=next(c for c in catalog['Classes'] if c['AdvancedClassId']==adv);cl['AllowedWeapons']=[next(i['Category'] for i in catalog['Items'] if i['Id']==v) for k,v in items.items() if k in ['main_hand','off_hand']];cl['SupportedSlots']=next(c for c in catalog['Classes'] if c['AdvancedClassId']=='sorcerer')['SupportedSlots']
    profiles.append(dict(Id=adv,BaseClass=base,Appearance=f'd3.{family}.bmn',EquipmentPreset=preset,StartingAbility=ability,Idle=idle,Run=run,TalentPool=['damage','range','cooldown','health','speed',f'd3_{ability}']))
write('godot/Gameplay/Equipment/catalog.json',catalog);write('godot/Gameplay/classes.json',profiles)
front=read('godot/Frontend/Data/catalog.json')
for c in front['Classes']:
    for a in c['AdvancedClasses']:
        if c['FactionId']=='empire':a.update(GameplayImplemented=True,PreviewScene='res://Assets/sorcerer.glb',IdleAnimation='Idle')
write('godot/Frontend/Data/catalog.json',front)
abilities=[('smash','Smash','smash','Smash',3,42,4,.34,.34,0,0,0),('rupture','Rupture','rupture','Rupture',0,14,3,.32,0,7,6,1),('lacerate','Lacerate','spinningstrike','Lacerate',3,22,2,.28,.28,0,0,0),('dart','Corrosive Dart','toxicdart','Dart',0,6,4,1.1,0,9,8,1),('snipershot','Sniper Shot','snipe','Sniper',0,32,2,1.5,0,0,0,0),('tracer','Tracer Missile','unexplodedmortarshell','Missile',4,38,3,1.3,.22,0,0,0),('flamesweep','Flame Sweep','flamingsweep','Flame',3,17,1.8,.31,.31,0,0,0)]
for id,title,icon,clip,area,damage,cd,range_,radius,dot,duration,interval in abilities:
    desc={'smash':'Heavy point-blank Force-assisted strike.','rupture':'Melee hit and bleeding damage every second for 6 seconds. Reapplication refreshes without stacking.','lacerate':'Fast double-bladed saber sweep.','dart':'Mid-range corrosive damage every second for 8 seconds. Reapplication refreshes without stacking.','snipershot':'Immediate ranged rifle hit; movement and cover are unrestricted.','tracer':'Primary missile hit and 40% splash damage. The primary target is hit once.','flamesweep':'Moving point-blank flame burst.'}[id]
    props=dict(Id=id,Title=title,Icon=icon,Damage=damage,CooldownSeconds=cd,Range=range_,Radius=radius,Area=area,NearbyTriggerCount=1,KnockbackDistance=0.,WindupSeconds=0.,ChannelSeconds=0.,RecoverySeconds=.1,GestureSeconds=.9,StartClip='D3/'+clip,LoopClip='D3/'+clip,EndClip='',PeriodicDamage=dot,PeriodicDuration=duration,PeriodicInterval=interval or 1,DamageType=2 if dot else 0,Description=desc)
    body='[gd_resource type="Resource" script_class="AbilityDefinition" load_steps=2 format=3]\n\n[ext_resource type="Script" path="res://Gameplay/AbilityDefinition.cs" id="1"]\n\n[resource]\nscript = ExtResource("1")\n'
    for k,v in props.items():body+=f'{k} = {json.dumps(v)}\n'
    (ROOT/f'godot/Gameplay/Abilities/{id}.tres').write_text(body)
    (ROOT/f'godot/Gameplay/Upgrades/d3_{id}.tres').write_text('[gd_resource type="Resource" script_class="UpgradeDefinition" load_steps=2 format=3]\n[ext_resource type="Script" path="res://Gameplay/UpgradeDefinition.cs" id="1"]\n[resource]\nscript = ExtResource("1")\n'+f'Id = "d3_{id}"\nTitle = "{title} Mastery"\nIcon = "{icon}"\nEffect = 0\nAbilityId = "{id}"\nAmountPerRank = 0.2\n')
