import bpy, sys, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/addons'))
import addon_utils
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
out=ROOT/'generated/face-quality';out.mkdir(exist_ok=True)
for kind in ['reference','portable']:
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/f'work/sorcerer-{kind}.blend'))
    scene=bpy.context.scene
    rig=next(o for o in scene.objects if o.type=='ARMATURE')
    rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1)
    head=next(o for o in scene.objects if o.type=='MESH' and o.get('slot')=='head')
    ev=head.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
    points=[ev.matrix_world@v.co for v in me.vertices]
    lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    target=(lo+hi)/2
    ev.to_mesh_clear()
    cam=scene.camera;cam.location=target+Vector((-.025,.13,.005))
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.ortho_scale=(hi.z-lo.z)*1.45
    scene.render.resolution_x=600;scene.render.resolution_y=600
    scene.cycles.samples=24
    scene.render.filepath=str(out/f'{kind}.png')
    bpy.ops.render.render(write_still=True)
    if kind=='reference':
        hero=next(n for n in head.data.materials[0].node_tree.nodes if n.bl_idname=='ShaderNodeHeroEngine')
        print('FACE_GRAPH',[(n.name,n.type) for n in hero.node_tree.nodes if n.type=='MIX_RGB'])
        print('FACE_MAPS',[(p,getattr(hero,p).size[:] if getattr(hero,p,None) else None) for p in ['diffuseMap','rotationMap','ageMap','complexionMap','facepaintMap']])
