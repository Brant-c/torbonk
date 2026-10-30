import bpy,addon_utils,sys,json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools/addons'))
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
prefs=bpy.context.preferences.addons['io_scene_gr2'].preferences
prefs.gr2_scale_object=False;prefs.gr2_apply_axis_conversion=True;prefs.gr2_import_collision=False
records=[]
for name in ['spy/spy_shiv_low01','spy/sp_plasma_probe_top','spy/sp_plasma_probe_bottom','all/fx_invis_sphere_double','spy/sp_portableshield_device_whole_01','spy/sp_portableshield_field_01','spy/sp_portableshield_device_main_01','spy/sp_portableshield_device_left_01','spy/sp_portableshield_device_right_01','spy/sp_portableshield_projection_01','spy/sp_portableshield_projection_02']:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 source='/resources/art/fx/geometry/'+name+'.gr2'
 bpy.ops.import_mesh.gr2(filepath=str(ROOT/'source-assets/april2012'/source.lstrip('/')))
 out=ROOT/f'godot/Assets/Agent/{Path(name).name}.glb'
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.export_scene.gltf(filepath=str(out),export_format='GLB',use_selection=True,export_yup=True,export_animations=False)
 records.append(dict(source=source,output=str(out.relative_to(ROOT)),sha256=hashlib.sha256(out.read_bytes()).hexdigest()))
(ROOT/'manifests/agent-geometry.json').write_text(json.dumps(records,indent=2))
