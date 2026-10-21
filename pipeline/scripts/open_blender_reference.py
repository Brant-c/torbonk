"""Interactive entry point; project-local modules, no saved user preferences."""
import sys
from pathlib import Path
import bpy,addon_utils
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'tools/addons'))
for module in ['io_scene_gr2','zg_swtor_tools']:
    addon_utils.enable(module,default_set=True,persistent=False)
bpy.context.preferences.addons['zg_swtor_tools'].preferences.swtor_resources_folderpath=str(root/'source-assets/april2012/resources')
bpy.ops.wm.open_mainfile(filepath=str(root/'work/door-reference.blend'))
