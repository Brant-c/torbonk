"""Blender: convert the original turbulent sky geometry without material approximation."""
from pathlib import Path
import sys,json
import bpy,addon_utils
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools/addons'))
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
p=bpy.context.preferences.addons['io_scene_gr2'].preferences
p.gr2_scale_object=False;p.gr2_scale_factor=1;p.gr2_apply_axis_conversion=True;p.gr2_import_collision=False
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
source=ROOT/'source-assets/april2012/resources/art/fx/geometry/skies/fx_turbulantskydome_01.gr2'
bpy.ops.import_mesh.gr2(filepath=str(source))
objects=list(bpy.context.scene.objects)
for o in objects:
 if o.type=='MESH':o.data.materials.clear()
bpy.ops.export_scene.gltf(filepath=str(ROOT/'godot/Assets/Maps/eternity_vault/raid-cloud-dome.glb'),export_format='GLB',use_selection=False,export_yup=True,export_animations=False)
print('SKY_GEOMETRY',[(o.name,len(o.data.vertices),list(o.dimensions)) for o in objects if o.type=='MESH'])
