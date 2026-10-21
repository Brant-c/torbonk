"""Invoked in assembled Blender scene. Sample original poses, then export."""
rig.animation_data.action=None
for bone in rig.pose.bones:bone.matrix_basis.identity()
bpy.context.view_layer.update()
report['bindPose']={'maximumVertexDisplacement':0,'originalBonesUnchanged':True}
for obj,part in meshes:
    ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh()
    assert len(mesh.vertices)==len(obj.data.vertices)
    error=max((v.co-obj.data.vertices[i].co).length for i,v in enumerate(mesh.vertices))
    report['bindPose']['maximumVertexDisplacement']=max(report['bindPose']['maximumVertexDisplacement'],error)
    ev.to_mesh_clear()
assert report['bindPose']['maximumVertexDisplacement']<1e-6
for item in report['skeleton']['bones']:
    actual=rig.data.bones[item['name']]
    assert actual.parent.name==item['parent'] if actual.parent else item['parent'] is None
    error=max(abs(actual.matrix_local[i][j]-item['matrixLocal'][i][j]) for i in range(4) for j in range(4))
    report['bindPose'].setdefault('originalBoneMatrixMaxError',0)
    report['bindPose']['originalBoneMatrixMaxError']=max(report['bindPose']['originalBoneMatrixMaxError'],error)
    assert error<1e-5,(item['name'],error)

def measure_pose(frame):
    scene.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
    points=[]
    for obj,_ in meshes:
        ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
        points.extend([obj.matrix_world@v.co for v in me.vertices]);ev.to_mesh_clear()
    assert all(math.isfinite(x) for p in points for x in p)
    lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
    return {'boundsMin':lo,'boundsMax':hi,'rootPosition':list(rig.pose.bones['Bip01'].matrix.translation),
            'rootRotationQuaternion':list(rig.pose.bones['Bip01'].matrix.to_quaternion()),
            'boneMatrices':{b.name:[list(row) for row in b.matrix] for b in rig.pose.bones}}
report['poseSamples']={}
for action,clip in zip(actions,recipe['animations']):
    rig.animation_data.action=action
    report['poseSamples'][action.name]=[measure_pose(1+float(action.frame_end-1)*fraction) for fraction in [0,.25,.5,.75,1]]

rig.animation_data.action=None
for bone in rig.pose.bones:bone.matrix_basis.identity()
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for obj,_ in meshes:obj.select_set(True)
bpy.context.view_layer.objects.active=rig
settings=dict(export_format='GLB',use_selection=True,export_yup=True,
 export_normals=True,export_tangents=True,export_texcoords=True,export_materials='EXPORT',
 export_animations=True,export_animation_mode='ACTIONS',export_force_sampling=True,
 export_anim_slide_to_zero=True,
 export_frame_range=False,export_frame_step=1,export_skins=True,export_all_influences=True,
 export_def_bones=False,export_leaf_bone=False,export_extras=True,export_apply=False)
report['exportSettings']=settings
bpy.ops.export_scene.gltf(filepath=str(ROOT/'generated/sorcerer.glb'),**settings)
report['exporterVersion']=list(sys.modules['io_scene_gltf2'].bl_info['version'])
report['glbSha256']=hashlib.sha256((ROOT/'generated/sorcerer.glb').read_bytes()).hexdigest()
write('character-conversion.json',report)
rig.animation_data.action=actions[0];scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'work/sorcerer-portable.blend'))
print('CHARACTER CONVERSION COMPLETE',report['glbSha256'])
