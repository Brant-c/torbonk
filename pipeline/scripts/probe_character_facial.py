"""Compare preserved facial translations to the maintained importer's default."""
import bpy,addon_utils,sys,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools/addons'))
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'work/sorcerer-reference.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
head=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.get('slot')=='head')
recipe=json.loads((ROOT/'character-recipe.json').read_text());scene=bpy.context.scene
result=[]
for clip in recipe['animations']:
    baseline=bpy.data.actions[clip['name']];rig.animation_data.action=baseline;scene.frame_set(1)
    before={b.name:b.matrix.translation.copy() for b in rig.pose.bones if b.name.startswith('fc_')}
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.import_animation.jba(filepath=str(ROOT/'source-assets/april2012'/clip['path'].lstrip('/')),
        ignore_facial_bones=False,delete_180=False,scale_animation=False,scale_factor=1)
    scene.frame_set(1);bpy.context.view_layer.update()
    delta=max((b.matrix.translation-before[b.name]).length for b in rig.pose.bones if b.name in before)
    ev=head.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
    size=[max(v.co[i] for v in me.vertices)-min(v.co[i] for v in me.vertices) for i in range(3)];ev.to_mesh_clear()
    result.append({'clip':clip['name'],'maxFacialPositionChange':delta,'headDimensions':size})
    bpy.ops.object.mode_set(mode='OBJECT')
    if clip['name']=='LightningLoop':
        scene.camera.location=(-.08,.34,.15);target=Vector((0,0,.105))
        scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(ROOT/'generated/character/facial-raw.png');bpy.ops.render.render(write_still=True)
(ROOT/'manifests/character-facial-probe.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
