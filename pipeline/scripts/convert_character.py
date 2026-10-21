"""Original modular character + maintained GR2/JBA tools, Blender 5.2.1."""
import sys, json, math, tempfile, traceback, hashlib, xml.etree.ElementTree as ET
from pathlib import Path
import bpy, addon_utils, numpy as np
from mathutils import Vector, Matrix, Quaternion
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/addons'))
(ROOT/'work/temp').mkdir(exist_ok=True); tempfile.tempdir=str(ROOT/'work/temp')
recipe=json.loads((ROOT/'character-recipe.json').read_text())
SOURCE=ROOT/'source-assets/april2012'
def local(p): return str(SOURCE/p.lstrip('/'))
def write(name,data): (ROOT/'manifests'/name).write_text(json.dumps(data,indent=2))
report={'blender':bpy.app.version_string,'buildHash':bpy.app.build_hash.decode(),
        'meshes':[],'animations':[],'materials':[], 'settings':{
        'scale':1,'applyAxisConversion':True,'ignoreFacialTranslations':False,
        'delete180':False,'keepAllBones':True,'automaticWeights':False}}
for addon in ['io_scene_gr2','zg_swtor_tools']:
    addon_utils.enable(addon,default_set=True,persistent=False)
prefs=bpy.context.preferences.addons['io_scene_gr2'].preferences
prefs.gr2_scale_object=False; prefs.gr2_scale_factor=1
prefs.gr2_apply_axis_conversion=True; prefs.gr2_import_collision=False
bpy.context.preferences.addons['zg_swtor_tools'].preferences.swtor_resources_folderpath=str(SOURCE/'resources')
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_mesh.gr2(filepath=local(recipe['skeleton']))
rig=bpy.context.object; assert rig.type=='ARMATURE'
rig.name='bmnnew'; rig.show_in_front=True
from io_scene_gr2.ops.import_gr2 import read as read_gr2
class Reporter:
    def report(self,kind,message): print(kind,message)
original_gr2=read_gr2(Reporter(),local(recipe['skeleton']))
axis=Matrix.Rotation(math.pi/2,4,'X')
source_rest={b.name:axis @ Matrix([b.root_to_bone[i*4:i*4+4] for i in range(4)]).transposed().inverted()
             for b in original_gr2.bone_buffer.values()}
report['skeleton']={'bones':[{'name':b.name,'parent':b.parent.name if b.parent else None,
 'matrixLocal':[list(row) for row in b.matrix_local],
 'sourceInverseBindRest':[list(row) for row in source_rest[b.name]]} for b in rig.data.bones],
 'restEvidence':'maintained GR2 imported rest preserved; raw original inverse bind matrices retained for comparison'}
report['clothBones']=[]
# April CLO is XML, unlike the binary-only maintained CLO operator. Import
# original rest/bind data without inventing weights or a cloth simulation.
bpy.ops.object.mode_set(mode='EDIT')
# The importer uses 1e-5 display tails. A second edit-mode round trip can
# quantize their orientation (notably sockets). Restore the imported matrices
# and use longer display tails; heads, hierarchy and bind transforms stay put.
for original in report['skeleton']['bones']:
    bone=rig.data.edit_bones[original['name']]
    bone.length=.005;bone.matrix=Matrix(original['matrixLocal'])
for path in recipe['clothBoneSources']:
    for entry in ET.parse(local(path)).findall('Bones/Bone'):
        name=entry.get('name'); assert name not in rig.data.edit_bones
        parent=entry.findtext('parent'); assert parent in rig.data.edit_bones,(name,parent)
        def floats(tag): return [float(v) for v in entry.findtext(tag).split(',')]
        def transform(t,r):
            q=floats(r); return Matrix.Translation(Vector(floats(t))) @ Quaternion((q[3],*q[:3])).to_matrix().to_4x4()
        inverse=transform('m_rootToBoneTrans','m_rootToBoneRot')
        world=axis @ inverse.inverted()
        relative=rig.data.edit_bones[parent].matrix @ transform('m_boneToParentTrans','m_boneToParentRot')
        error=max(abs(world[i][j]-relative[i][j]) for i in range(4) for j in range(4))
        # In this source set the CLO's cached parent-relative transform differs
        # from its inverse bind and current bmnnew rest. Preserve inverse bind,
        # record the discrepancy, and derive local rest against the actual rig.
        # This leaves skin vertices unchanged at bind pose.
        assert all(math.isfinite(x) for row in world for x in row)
        b=rig.data.edit_bones.new(name); b.head=(0,0,0); b.tail=(0,0.005,0)
        b.matrix=world; b.parent=rig.data.edit_bones[parent]
        report['clothBones'].append({'name':name,'parent':parent,'source':path,
            'cachedLocalMatrixDifference':error,'bindPolicy':'inverse m_rootToBone; local derived against original bmnnew',
            'matrixLocal':[list(row) for row in world]})
bpy.ops.object.mode_set(mode='OBJECT')
meshes=[]
for part in recipe['parts']:
    for model in part['models']:
        before=set(bpy.data.objects)
        assert bpy.ops.import_mesh.gr2(filepath=local(model['path']))=={'FINISHED'}
        new=[o for o in bpy.data.objects if o not in before and o.type=='MESH']
        assert new
        for obj in new:
            obj['sourcePath']=model['path']; obj['sourceModelId']=model['id']; obj['slot']=part['slot']
            obj.parent=rig; mod=obj.modifiers.new('OriginalWeights','ARMATURE'); mod.object=rig
            missing=[v.name for v in obj.vertex_groups if v.name not in rig.data.bones]
            weightedMissing=sorted({obj.vertex_groups[g.group].name for v in obj.data.vertices for g in v.groups
                                    if g.weight>0 and obj.vertex_groups[g.group].name in missing})
            sums=[sum(g.weight for g in v.groups) for v in obj.data.vertices]
            report['meshes'].append({'name':obj.name,'slot':part['slot'],'vertices':len(obj.data.vertices),
                'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),
                'groups':len(obj.vertex_groups),'missingGroups':missing,'weightedMissing':weightedMissing,
                'weightSumRange':[min(sums),max(sums)],'materials':[m.name for m in obj.data.materials]})
            meshes.append((obj,part))
write('character-probe.json',report)
assert not any(m['weightedMissing'] for m in report['meshes']), 'weighted bones absent; inspect probe'

from io_scene_gr2.ops.import_jba import read as read_jba
scene=bpy.context.scene
actions=[]
for clip in recipe['animations']:
    bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.context.view_layer.objects.active=rig
    raw=read_jba(Reporter(),local(clip['path']))
    assert bpy.ops.import_animation.jba(filepath=local(clip['path']),ignore_facial_bones=False,
            delete_180=False,scale_animation=False,scale_factor=1)=={'FINISHED'}
    action=rig.animation_data.action; action.name=clip['name']; action.use_fake_user=True
    action['sourcePath']=clip['path']; action['sourceDuration']=raw.length
    # JBA sample times include frame 1 but exclude duration endpoint. Extend the
    # final held sample to source duration; never shorten a clip by one sample.
    # All actions share a 60 fps export scene, including 15/30 fps sources.
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for curve in bag.fcurves:
                    for key in curve.keyframe_points:
                        key.co.x=1+(key.co.x-1)*60/raw.fps
                        key.handle_left.x=1+(key.handle_left.x-1)*60/raw.fps
                        key.handle_right.x=1+(key.handle_right.x-1)*60/raw.fps
    action.use_frame_range=True; action.frame_start=1; action.frame_end=1+round(raw.length*60)
    actions.append(action)
    report['animations'].append({'name':clip['name'],'sourceDuration':raw.length,'fps':raw.fps,
        'sourceFrames':raw.num_frames,'actionRange':list(action.frame_range),
        'worldTrajectory':{'applied':False,'translationRaw':[list(v) for v in raw.world_space.translations],
             'translationFirstLastDeltaRaw':list(raw.world_space.translations[-1]-raw.world_space.translations[0]),
             'rotationDecoderCaveat':'Maintained reader steps world rotations by 8 bytes but advances block by 6 per frame. World rotation is not exported or asserted.'},
        'animatedBones':[getattr(b,'name','') for b in raw.bones],
        'unmatchedBones':[getattr(b,'name','') for b in raw.bones if getattr(b,'name','') not in rig.data.bones]})
    bpy.ops.object.mode_set(mode='OBJECT')
scene.render.fps=60
rig.animation_data.action=None
for b in rig.pose.bones: b.matrix_basis.identity()
bpy.context.view_layer.update()
write('character-probe.json',report)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'work/sorcerer-probe.blend'))
print('CHARACTER PROBE COMPLETE')
exec(compile((ROOT/'scripts/character_materials.py').read_text(),str(ROOT/'scripts/character_materials.py'),'exec'))
