"""Separate animation libraries from original JBA on the established Blender rig."""
import bpy, sys, json, hashlib, tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
tempfile.tempdir=str(ROOT/'work/temp')
sys.path.insert(0,str(ROOT/'tools/addons'))
import addon_utils
addon_utils.enable('io_scene_gr2',default_set=False,persistent=False)
from io_scene_gr2.ops.import_jba import read as read_jba
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'work/sorcerer-portable.blend'))
rig=bpy.data.objects['bmnnew']; rig.animation_data_clear()
for action in list(bpy.data.actions): bpy.data.actions.remove(action)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
class Reporter:
    def report(self,kind,message): print(kind,message)
b8='--b8' in sys.argv
jump='--jump' in sys.argv
profile='basic-attacks' if '--basic-attacks' in sys.argv else 'sorcerer' if '--sorcerer' in sys.argv else 'powertech' if '--powertech' in sys.argv else 'd2' if '--d2' in sys.argv else 'b9-jump' if jump else 'b8' if b8 else 'abilities'
clips=json.loads((ROOT/f'manifests/{profile}-conversion.json').read_text())['clips']
records=[]
for name,stem in clips.items():
    original=ROOT/f'source-assets/april2012/resources/anim/humanoid/bmnnew/{stem}.jba'
    raw=read_jba(Reporter(),str(original))
    bpy.ops.import_animation.jba(filepath=str(original),ignore_facial_bones=True,delete_180=False,scale_animation=False,scale_factor=1)
    action=rig.animation_data.action;action.name=name;action.use_fake_user=True
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for curve in bag.fcurves:
                    for key in curve.keyframe_points:
                        key.co.x=1+(key.co.x-1)*60/raw.fps
                        key.handle_left.x=1+(key.handle_left.x-1)*60/raw.fps
                        key.handle_right.x=1+(key.handle_right.x-1)*60/raw.fps
    action.use_frame_range=True;action.frame_start=1;action.frame_end=1+round(raw.length*60)
    records.append(dict(name=name,path=str(original.relative_to(ROOT)),seconds=raw.length,sourceFps=raw.fps))
rig.animation_data.action=None
if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
for bone in rig.pose.bones:bone.matrix_basis.identity()
bpy.context.scene.render.fps=60;bpy.context.scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
# A retained original skinned mesh makes glTF's joint/skin identity explicit to Godot.
next(o for o in bpy.data.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers)).select_set(True)
bpy.context.view_layer.objects.active=rig
# Preserve the established skeleton/node hierarchy. No mesh or animation in sorcerer.glb is changed.
output=ROOT/('godot/Assets/BasicAttacks/casts.glb' if profile=='basic-attacks' else 'godot/Assets/Sorcerer/casts.glb' if profile=='sorcerer' else 'godot/Assets/Powertech/casts.glb' if profile=='powertech' else 'godot/Assets/D2/casts.glb' if profile=='d2' else 'godot/Assets/Jump/jump.glb' if jump else 'godot/Assets/B8/casts.glb' if b8 else 'godot/Assets/Abilities/casts.glb')
bpy.ops.export_scene.gltf(filepath=str(output),export_format='GLB',use_selection=True,export_yup=True,
    export_animations=True,export_animation_mode='ACTIONS',export_force_sampling=True,export_anim_slide_to_zero=True,
    export_frame_range=False,export_frame_step=1,export_skins=True,export_def_bones=False,export_leaf_bone=False,export_extras=True)
(ROOT/f'manifests/{profile}-animation-conversion.json').write_text(json.dumps(dict(clips=records,output=str(output.relative_to(ROOT)),sha256=hashlib.sha256(output.read_bytes()).hexdigest(),
    policy='Same A2 rig/JBA importer and sample timing; additive animation library, no replacement original GLB'),indent=2))
print(profile+' animation conversion complete')
