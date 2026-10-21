import sys, json, traceback
from pathlib import Path
import bpy, addon_utils
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/addons'))
if hasattr(bpy.context.preferences.filepaths,'use_save_preview_images'):
    bpy.context.preferences.filepaths.use_save_preview_images=False
report={'blender':bpy.app.version_string,'buildHash':bpy.app.build_hash.decode(), 'addons':{}}
try:
    for name in ['io_scene_gr2','zg_swtor_tools']:
        addon_utils.enable(name,default_set=True,persistent=False)
        assert addon_utils.check(name)[1], name
        module=sys.modules[name]
        report['addons'][name]={'version':module.bl_info['version'],'registered':True}
    prefs=bpy.context.preferences.addons['io_scene_gr2'].preferences
    prefs.gr2_scale_object=False; prefs.gr2_scale_factor=1.0
    prefs.gr2_apply_axis_conversion=True; prefs.gr2_import_collision=False
    bpy.context.preferences.addons['zg_swtor_tools'].preferences.swtor_resources_folderpath=str(ROOT/'source-assets/april2012/resources')
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    src=json.loads((ROOT/'manifests/door-source.json').read_text())
    model=ROOT/src['resources'][0]['output']
    report['importResult']=list(bpy.ops.import_mesh.gr2(filepath=str(model)))
    report['materialsBefore']=[m.name for m in bpy.data.materials if m.users]
    # GR2's update callback dereferences context.space_data in background mode.
    # Supply an existing VIEW_3D context, without modifying the upstream package.
    area=next(a for a in bpy.context.screen.areas if a.type=='VIEW_3D')
    with bpy.context.temp_override(area=area):
        report['materialResult']=list(bpy.ops.zgswtor.process_named_mats())
    report['objects']=[{'name':o.name,'type':o.type,'dimensions':list(o.dimensions),
                        'vertices':len(o.data.vertices) if o.type=='MESH' else 0,
                        'polygons':len(o.data.polygons) if o.type=='MESH' else 0} for o in bpy.context.scene.objects]
    report['materials']=[{'name':m.name,'nodes':[{'name':n.name,'type':n.bl_idname} for n in m.node_tree.nodes]} for m in bpy.data.materials if m.users]
    report['images']=[{'name':i.name,'size':list(i.size),'path':i.filepath} for i in bpy.data.images if i.source=='FILE']
    if __name__=='__main__':
        bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'work/door-reference.blend'))
except Exception:
    report['failure']=traceback.format_exc(); raise
finally:
    (ROOT/'manifests/blender-probe.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
