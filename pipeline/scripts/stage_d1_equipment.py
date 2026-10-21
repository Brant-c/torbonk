"""Stage preconverted meshes and class-independent item/appearance catalog."""
import json, shutil, struct, hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
read=lambda p:json.loads((ROOT/p).read_text())
recipe=read('d1-character-recipe.json');old_recipe=read('character-recipe.json')
old=read('manifests/character-probe.json');new=read('manifests/d1-character-probe.json')
output=ROOT/'godot/Assets/Equipment';output.mkdir(exist_ok=True)
shutil.copyfile(ROOT/'generated/d1-sorcerer.glb',output/'inquisitor-level1.glb')
slots=['head','chest','hands','legs','feet','waist','wrists','main_hand','off_hand','relic_1','relic_2','ear','implant_1','implant_2']
mapping={'chest':'chest','hand':'hands','leg':'legs','boot':'feet','waist':'waist'}
compat=dict(RigProfiles=['april_bmnnew_cloth'],BodyTypes=['bmn'],Species=['human'],BaseClasses=[],AdvancedClasses=[],AnimationProfiles=['april_bmnnew_force'])
logic=dict(RigProfiles=[],BodyTypes=[],Species=[],BaseClasses=[],AdvancedClasses=[],AnimationProfiles=[])
catalog=dict(Slots=[],Items=[],Appearances=[],Presets=[],Rigs=[],Classes=[])
for slot in slots:
    cats=['lightsaber','double_lightsaber','pistol','rifle','sniper_rifle','assault_cannon','vibrosword','techblade','focus','shield','generator'] if slot in ['main_hand','off_hand'] else ['light_armor','medium_armor','heavy_armor','accessory']
    catalog['Slots'].append(dict(Id=slot,Categories=cats,EmptyAppearance='',CanHide=slot not in mapping.values()))
sources={'chest':('0xE000DD4FCFCC757D','itm.eq.class.inq.base.chest.light.cheap.class_quest_01_a'),
    'legs':('0xE0002314C3EE1FEA','itm.eq.class.inq.base.legs.light.cheap.class_quest_01_a'),
    'feet':('0xE000F141271037FA','itm.eq.class.inq.base.feet.light.cheap.class_quest_01_a')}
for family,report,rec,scene in [('level1',new,recipe,'res://Assets/Equipment/inquisitor-level1.glb'),('ss_a04',old,old_recipe,'res://Assets/sorcerer.glb')]:
    for raw,slot in mapping.items():
        part=next(p for p in rec['parts'] if p['slot']==raw)
        names=[m['name'] for m in report['meshes'] if m['slot']==raw]
        source=sources.get(slot) if family=='level1' else None
        icon=''
        if 'iconSource' in part:
            # Blender's original DDS reader is used by conversion; icons are converted by the same engine offline.
            icon='res://Assets/Equipment/'+slot+'.png'
        catalog['Appearances'].append(dict(Id=family+'.'+slot,Slot=slot,Compatibility=compat,Scene=scene,MeshNames=names,
            RequiredBones=[],CoversSlots=[],HidesBaseGroups=[slot],Attachment=None))
        catalog['Items'].append(dict(Id=family+'.'+slot,DisplayName=('Level 1 Inquisitor ' if family=='level1' else 'SS A04 ')+slot.title(),
            Slot=slot,Category='light_armor',Rarity='cheap' if family=='level1' else 'appearance_reference',Compatibility=logic,
            Source=dict(GomId=source[0],Name=source[1],Evidence='Client-derived level-1 item -> starting appearance; full spawn inventory unverified') if source else None,
            Appearance=family+'.'+slot,Icon=icon,StatModifiers={},CosmeticOnly=family=='ss_a04',BlocksSlots=[]))
attachment=dict(Bone='RightWeapon',CorrectionResource='res://Assets/Enemies/weapon-socket.json',Position=[0,0,0],RotationDegrees=[0,0,0],Scale=1,RedBlade=True)
catalog['Appearances'].append(dict(Id='red_saber',Slot='main_hand',Compatibility=compat,Scene='res://Assets/Enemies/saber.glb',MeshNames=[],
    RequiredBones=['RightWeapon'],CoversSlots=[],HidesBaseGroups=[],Attachment=attachment,
    Locomotion=dict(LibraryId='EquipmentSaber',Scene='res://Assets/Enemies/looter.glb',Idle='Idle',Run='Run')))
catalog['Items'].append(dict(Id='d1.red_saber',DisplayName='Red lightsaber',Slot='main_hand',Category='lightsaber',Rarity='development',Compatibility=logic,
    Source=None,Appearance='red_saber',Icon='',StatModifiers={},CosmeticOnly=False,BlocksSlots=[]))
items={slot:'level1.'+slot for slot in mapping.values()};items['main_hand']='d1.red_saber'
catalog['Presets']=[dict(Id='inquisitor_level1',DisplayName='A · Level 1 Inquisitor (documented alternative)',Items=items,Cosmetics={},DeveloperOnly=False),
    dict(Id='sorcerer_ss_a04',DisplayName='B · Existing Sorcerer SS A04 (cosmetic)',Items=items,Cosmetics={s:'ss_a04.'+s for s in mapping.values()},DeveloperOnly=True)]
catalog['Rigs']=[dict(Id='april_bmnnew_cloth',RigFamily='april_humanoid_bmnnew',BodyType='bmn',Species='human',AnimationProfile='april_bmnnew_force',
    BaseScene='res://Assets/sorcerer.glb',BaseGroups={slot:[m['name'] for m in old['meshes'] if m['slot']==raw] for raw,slot in mapping.items()},
    AttachmentBones={'main_hand':'RightWeapon','off_hand':'LeftWeapon'},EmptyAppearances={s:'level1.'+s for s in mapping.values()})]
for base in read('godot/Frontend/Data/catalog.json')['Classes']:
    for advanced in base['AdvancedClasses']:
        # IDs for all 16 are registered; future content must explicitly author its own weapon/slot rules.
        implemented=advanced['Id']=='sorcerer'
        catalog['Classes'].append(dict(BaseClassId=base['Id'],AdvancedClassId=advanced['Id'],AllowedWeapons=['lightsaber','focus'] if implemented else [],SupportedSlots=slots if implemented else []))
path=ROOT/'godot/Gameplay/Equipment/catalog.json';path.write_text(json.dumps(catalog,indent=2))
receipt=dict(glbSha256=hashlib.sha256((output/'inquisitor-level1.glb').read_bytes()).hexdigest(),
    catalogSha256=hashlib.sha256(path.read_bytes()).hexdigest(),source='manifests/d1-source.json',recipe='d1-character-recipe.json',
    animationOwner='existing CharacterPresentation',scale=1,originalRigKept=True)
(ROOT/'manifests/d1-staging.json').write_text(json.dumps(receipt,indent=2))
locomotion_paths=[
    '/resources/anim/humanoid/bmnnew/cb_knight_saber_idle_1.jba',
    '/resources/anim/humanoid/bmnnew/mv_saber_run_forward_01.jba']
locomotion=dict(sourceManifest='manifests/b6-source.json',conversionRecipe='b6-recipe.json',
    convertedScene='godot/Assets/Enemies/looter.glb',
    convertedSceneSha256=hashlib.sha256((ROOT/'godot/Assets/Enemies/looter.glb').read_bytes()).hexdigest(),
    sources=[r for r in read('manifests/b6-source.json')['resources'] if r['path'] in locomotion_paths],
    policy='Reuse existing converted Idle/Run; duplicate library, retarget matching bone names into live rig; original source scale/root policy retained; existing AnimationTree owns all blending.',
    newExtraction=False)
assert len(locomotion['sources'])==2
(ROOT/'manifests/d1-locomotion-source.json').write_text(json.dumps(locomotion,indent=2))
print('Staged D1 catalog:',len(catalog['Items']),'items,',len(catalog['Classes']),'class profiles')
