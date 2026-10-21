import bpy,sys,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/addons'))
import addon_utils
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
for kind in ['reference','portable']:
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/f'work/sorcerer-{kind}.blend'))
    scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE')
    rig.animation_data.action=bpy.data.actions['LightningLoop'];scene.frame_set(33)
    cam=scene.camera;cam.location=(-.08,.34,.15);target=Vector((0,0,.105))
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.25
    scene.render.resolution_x=900;scene.render.resolution_y=900
    scene.render.filepath=str(ROOT/f'generated/character/blender-{kind}.png')
    scene.cycles.samples=16
    bpy.ops.render.render(write_still=True)
