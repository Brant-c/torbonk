"""Blender entry point: original reference + explicit Uber-to-glTF recipe.

Unit policy: preserve raw source units (1:1); no inferred metre calibration.
Only a static prop is exported. Shader approximation is labelled in the report.
"""
import sys, json, runpy, math, hashlib, tempfile
from pathlib import Path
import bpy, numpy as np
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
(ROOT/'work/temp').mkdir(exist_ok=True)
tempfile.tempdir=str(ROOT/'work/temp')
runpy.run_path(str(ROOT/'scripts/blender_probe.py'),run_name='pipeline_probe')
scene=bpy.context.scene
obj=next(o for o in scene.objects if o.type=='MESH')
bpy.context.view_layer.update()
mesh=obj.data; mesh.calc_loop_triangles()
world=[obj.matrix_world@v.co for v in mesh.vertices]
lo=Vector([min(v[i] for v in world) for i in range(3)])
hi=Vector([max(v[i] for v in world) for i in range(3)])
rawlo=[min(v.co[i] for v in mesh.vertices) for i in range(3)]
rawhi=[max(v.co[i] for v in mesh.vertices) for i in range(3)]
report={'blender':bpy.app.version_string,'buildHash':bpy.app.build_hash.decode(),
 'axisPolicy':'GR2 importer +90 degrees X, applied to mesh; glTF exporter Y-up; scale 1 source unit',
 'physicalMetresCalibrated':False,
 'importSettings':{'gr2_scale_object':False,'gr2_scale_factor':1.0,'gr2_apply_axis_conversion':True,'gr2_import_collision':False},
 'geometry':{'vertices':len(mesh.vertices),'triangles':len(mesh.loop_triangles),'uvLayers':[u.name for u in mesh.uv_layers],
             'minBlender':list(lo),'maxBlender':list(hi),'dimensionsBlender':list(hi-lo),'meshMin':rawlo,'meshMax':rawhi,
             'nonFiniteVertices':sum(not all(math.isfinite(x) for x in v.co) for v in mesh.vertices),
             'degenerateTriangles':sum(t.area<1e-12 for t in mesh.loop_triangles),
             'hasCustomNormals':mesh.has_custom_normals},
 'materialRecipe':{'family':'Uber, opaque, one-sided',
   'baseColor':'DDS diffuse RGB ^ 2.1 linear, encoded sRGB PNG (matches GR2 Uber gamma node)',
   'normal':'OpenGL tangent vector normalize(2A-1,1-2G,sqrt(max(0,1-(2A-1)^2-(1-2G)^2))); encode to [0,1] RGB',
   'roughness':'(2/(3+63*glossAlpha))^0.25; Phong-to-GGX approximation',
   'specular':'gloss RGB luminance mapped to Principled IOR Level; colored specular retained as source PNG',
   'emission':'diffuse^2.1 * rotation.B; excludes view-dependent Uber specular contribution',
   'limitations':['SWTOR view-dependent shader and reflection parameters are not equivalent to glTF PBR.',
                 'No original-client visual reference; parity needs human comparison.']}}

mat=obj.active_material
hero=next(n for n in mat.node_tree.nodes if n.bl_idname=='ShaderNodeHeroEngine')
assert hero.derived=='UBER',hero.derived
images={k:hero.node_tree.nodes[k].image for k in ['_d','_n','_s']}
assert all(images.values())
def pixels(image):
    p=np.empty(len(image.pixels),dtype=np.float32); image.pixels.foreach_get(p)
    return p.reshape(image.size[1],image.size[0],4)
diff=pixels(images['_d']); rot=pixels(images['_n']); gloss=pixels(images['_s'])
def srgb(x): return np.where(x<=0.0031308,12.92*x,1.055*np.maximum(x,0)**(1/2.4)-0.055)
def save_image(name,rgb,colorspace):
    h,w=rgb.shape[:2]; rgba=np.ones((h,w,4),np.float32); rgba[:,:,:3]=rgb
    image=bpy.data.images.new(name,width=w,height=h,alpha=True,float_buffer=False)
    image.colorspace_settings.name=colorspace; image.alpha_mode='CHANNEL_PACKED'
    image.pixels.foreach_set(rgba.ravel()); image.filepath_raw=str(ROOT/'generated'/f'{name}.png'); image.file_format='PNG'
    image.save(); return image
base=np.maximum(diff[:,:,:3],0)**2.1
base_image=save_image('door_basecolor',srgb(base),'sRGB')
nx=rot[:,:,3]*2-1; ny=1-2*rot[:,:,1]
nz=np.sqrt(np.maximum(0,1-nx*nx-ny*ny))
# DDS block compression can place XY outside the unit disc. Match the normal
# node's normalization before encoding conventional glTF tangent normals.
nvec=np.stack((nx,ny,nz),axis=-1)
nvec/=np.maximum(np.linalg.norm(nvec,axis=-1,keepdims=True),1e-8)
normal_image=save_image('door_normal',(nvec+1)*.5,'Non-Color')
rough=(2/(3+63*gloss[:,:,3]))**.25
rough_image=save_image('door_roughness',np.repeat(rough[:,:,None],3,axis=2),'Non-Color')
emission_image=save_image('door_emission',srgb(base*rot[:,:,2:3]),'sRGB')
spec=np.clip(gloss[:,:,:3]@np.array([.2126,.7152,.0722],np.float32),0,1)
spec_image=save_image('door_specular_level',np.repeat(spec[:,:,None],3,axis=2),'Non-Color')
save_image('door_source_specular_rgb',gloss[:,:,:3],'Non-Color')
report['textureValidation']={'sourceSizes':{k:list(i.size) for k,i in images.items()},
 'normalZClampedPixels':int(np.count_nonzero(nx*nx+ny*ny>1)),
 'normalLengthMaxError':float(np.max(np.abs(np.linalg.norm(nvec,axis=-1)-1))),
 'roughnessRange':[float(rough.min()),float(rough.max())],
 'emissionMaskRange':[float(rot[:,:,2].min()),float(rot[:,:,2].max())]}

def setup_camera():
    camdata=bpy.data.cameras.new('InspectionCamera'); cam=bpy.data.objects.new('InspectionCamera',camdata); scene.collection.objects.link(cam)
    center=(lo+hi)/2; span=max(hi-lo)
    cam.location=center+Vector((span*2.3,span*.65,span*.4))
    cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
    camdata.lens=50; scene.camera=cam
    world=bpy.data.worlds.new('Neutral Studio'); scene.world=world; world.use_nodes=True
    world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.12,.12,1)
    world.node_tree.nodes['Background'].inputs[1].default_value=.6
    for name,offset,power,size in [('Key',(-1,-2,3),350,3),('Fill',(2,-1,1),100,2),('Rim',(0,2,2),250,2)]:
        ld=bpy.data.lights.new(name,'AREA'); ld.energy=power*span*span; ld.shape='DISK'; ld.size=size*span
        light=bpy.data.objects.new(name,ld); scene.collection.objects.link(light)
        light.location=center+Vector(offset)*span
        light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
    scene.render.engine='CYCLES'; scene.cycles.samples=24; scene.cycles.use_denoising=True
    scene.render.resolution_x=1000; scene.render.resolution_y=800; scene.render.resolution_percentage=100
    scene.view_settings.view_transform='Standard'
    scene.render.image_settings.file_format='PNG'
setup_camera()
# Reference contains original DDS and maintained smart shader, camera and lights.
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'work/door-reference.blend'))
scene.render.filepath=str(ROOT/'generated/blender-reference.png')
bpy.ops.render.render(write_still=True)

portable=bpy.data.materials.new('kor_acad_higharch_glTF_approximation'); portable.use_nodes=True
portable.use_backface_culling=True
nodes=portable.node_tree.nodes; links=portable.node_tree.links
bsdf=nodes.get('Principled BSDF')
bsdf.inputs['Metallic'].default_value=0
for image,target in [(base_image,'Base Color'),(rough_image,'Roughness'),(emission_image,'Emission Color'),(spec_image,'Specular IOR Level')]:
    node=nodes.new('ShaderNodeTexImage'); node.image=image; links.new(node.outputs['Color'],bsdf.inputs[target])
bsdf.inputs['Emission Strength'].default_value=1
node=nodes.new('ShaderNodeTexImage'); node.image=normal_image
normal=nodes.new('ShaderNodeNormalMap'); links.new(node.outputs['Color'],normal.inputs['Color']); links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
obj.data.materials.clear(); obj.data.materials.append(portable)
obj['source_sha256']='d4708978fdf34720f310795db695776f156e4515917ff08fc2fa7d871374db17'
obj['source_profile']='April2012'; obj['units']='uncalibrated source units; 1:1'
bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
settings=dict(export_format='GLB',use_selection=True,export_yup=True,export_normals=True,export_tangents=True,
              export_texcoords=True,export_materials='EXPORT',export_animations=False,export_extras=True,
              export_image_format='AUTO',export_apply=False)
report['exportSettings']=settings
bpy.ops.export_scene.gltf(filepath=str(ROOT/'generated/sith_academy_door.glb'),**settings)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'work/door-portable.blend'))
scene.render.filepath=str(ROOT/'generated/blender-portable.png'); bpy.ops.render.render(write_still=True)
report['exporterVersion']=list(sys.modules['io_scene_gltf2'].bl_info['version'])
(ROOT/'manifests/conversion.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('CONVERSION_COMPLETE')
