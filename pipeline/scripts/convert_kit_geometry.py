"""Original bh_energy_shield_02 geometry (sphere_hexagon_shield.gr2), converted for the class-kit shield effect."""
import bpy,addon_utils,sys,json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools/addons'))
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
prefs=bpy.context.preferences.addons['io_scene_gr2'].preferences
prefs.gr2_scale_object=False;prefs.gr2_apply_axis_conversion=True;prefs.gr2_import_collision=False
records=[]
for name in ['sphere_hexagon_shield']:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    source=f'/resources/art/fx/geometry/all/{name}.gr2'
    bpy.ops.import_mesh.gr2(filepath=str(ROOT/'source-assets/april2012'/source.lstrip('/')))
    materials=[];bounds=[]
    for o in bpy.context.scene.objects:
        if o.type!='MESH':continue
        materials.extend(m.name for m in o.data.materials)
        bounds.extend([list(o.matrix_world@__import__('mathutils').Vector(b)) for b in o.bound_box])
        o.data.materials.clear()
    out=ROOT/f'godot/Assets/D3/{name}.glb'
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=str(out),export_format='GLB',use_selection=True,export_yup=True,export_animations=False)
    records.append(dict(source=source,materials=materials,bounds=bounds,output=str(out.relative_to(ROOT)),sha256=hashlib.sha256(out.read_bytes()).hexdigest()))
(ROOT/'manifests/kit-geometry.json').write_text(json.dumps(records,indent=2))
print(json.dumps(records))
