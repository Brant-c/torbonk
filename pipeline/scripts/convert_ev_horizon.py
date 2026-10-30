"""Convert original horizon mesh without generated geometry or collision."""
import bpy,addon_utils,sys,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools/addons'))
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
prefs=bpy.context.preferences.addons['io_scene_gr2'].preferences
prefs.gr2_scale_object=False;prefs.gr2_scale_factor=1;prefs.gr2_apply_axis_conversion=True;prefs.gr2_import_collision=False
record=json.loads((ROOT/'godot/Assets/Maps/eternity_vault/surface-fidelity.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_mesh.gr2(filepath=str(ROOT/'source-assets/april2012'/record['sourceModel'].lstrip('/')))
for o in bpy.context.scene.objects:
    if o.type=='MESH':
        for i in range(len(o.data.materials)):o.data.materials[i]=None
bpy.ops.export_scene.gltf(filepath=str(ROOT/'godot/Assets/Maps/eternity_vault/surface-horizon.glb'),export_format='GLB',export_animations=False,export_normals=True,export_texcoords=True,export_materials='NONE',export_extras=True)
print('EV_HORIZON_CONVERTED')
