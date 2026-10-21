import bpy,addon_utils,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/addons'))
for name in ['io_scene_gr2','zg_swtor_tools']:
    addon_utils.enable(name,default_set=True,persistent=False)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'work/sorcerer-reference.blend'))
scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=101
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_distance=.35
        area.spaces.active.region_3d.view_location=(0,0,.1)
