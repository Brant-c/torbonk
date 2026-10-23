"""Convert only the recovered probe and scream meshes on the established axes."""
import bpy, addon_utils, sys, json, hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools/addons'))
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
prefs=bpy.context.preferences.addons['io_scene_gr2'].preferences
prefs.gr2_scale_object=False;prefs.gr2_apply_axis_conversion=True;prefs.gr2_import_collision=False
records=[]
for relative in ['probes/vfx_probe_mini01','inquisitor/fx_force_scream_blast','inquisitor/fx_force_scream_shout']:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 source=ROOT/('source-assets/april2012/resources/art/fx/geometry/'+relative+'.gr2')
 bpy.ops.import_mesh.gr2(filepath=str(source))
 for o in bpy.context.scene.objects:
  if o.type=='MESH':o.data.materials.clear()
 out=ROOT/('godot/Assets/PresentationRepairs/'+Path(relative).name+'.glb')
 bpy.ops.export_scene.gltf(filepath=str(out),export_format='GLB',export_yup=True,export_animations=False)
 records.append(dict(source=str(source.relative_to(ROOT)),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),output=str(out.relative_to(ROOT))))
(ROOT/'manifests/presentation-repairs-geometry.json').write_text(json.dumps(records,indent=2))
