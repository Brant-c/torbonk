"""Blender: one portable GLB per source model, with the established Uber adapter."""
import sys,json,math,tempfile,hashlib
from pathlib import Path
import bpy,addon_utils,numpy as np
from mathutils import Matrix,Vector,Euler
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'scripts'))
from maps_common import local,work,asset_dir,manifest,map_id
from source_pipeline import sha
sys.path.insert(0,str(ROOT/'tools/addons'))
tempfile.tempdir=str(ROOT/'work/temp')
selection=json.loads((work()/'selection.json').read_text())
OUT=asset_dir();GEN=ROOT/'generated/maps'/map_id();GEN.mkdir(parents=True,exist_ok=True)
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
prefs=bpy.context.preferences.addons['io_scene_gr2'].preferences
prefs.gr2_scale_object=False;prefs.gr2_scale_factor=1;prefs.gr2_apply_axis_conversion=True;prefs.gr2_import_collision=False
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
report=dict(models=[],materials=[],placements=[],blender=bpy.app.version_string,units='source units 1:1')

def pixels(path,color=False):
    image=bpy.data.images.load(str(local(path)),check_existing=True)
    image.colorspace_settings.name='sRGB' if color else 'Non-Color'
    image.alpha_mode='CHANNEL_PACKED'
    p=np.empty(len(image.pixels),np.float32);image.pixels.foreach_get(p)
    return p.reshape(image.size[1],image.size[0],4)
def srgb(x):return np.where(x<=.0031308,12.92*x,1.055*np.maximum(x,0)**(1/2.4)-.055)
def image(name,rgb,cs):
    h,w=rgb.shape[:2];rgba=np.ones((h,w,4),np.float32);rgba[:,:,:rgb.shape[2]]=rgb
    im=bpy.data.images.new(name,width=w,height=h,alpha=True)
    im.colorspace_settings.name=cs;im.alpha_mode='CHANNEL_PACKED';im.pixels.foreach_set(rgba.ravel())
    im.filepath_raw=str(GEN/f'{name}.png');im.file_format='PNG';im.save();return im
mats={}
for path,info in sorted(selection['materials'].items()):
    assert info['derived'] in ('Uber','OpacityFade'),info
    if info['derived']=='OpacityFade':info=dict(info,alpha='Full')
    tex=info['textures'];diff=pixels(tex['DiffuseMap'],True)
    base=np.maximum(diff[:,:,:3],0)**2.1
    stem=Path(path).stem
    mat=bpy.data.materials.new(stem);mat.use_nodes=True;mat.use_backface_culling=info['twoSided']!='True'
    mat['source_material']=path;mat['source_parameters']=json.dumps(info,sort_keys=True)
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;bsdf=nodes.get('Principled BSDF')
    def link(im,target):
        n=nodes.new('ShaderNodeTexImage');n.image=im;links.new(n.outputs['Color'],bsdf.inputs[target]);return n
    baseim=image(stem+'_base',srgb(base),'sRGB');baseNode=link(baseim,'Base Color')
    if info['alpha'] in ['Test','Full']:
        links.new(baseNode.outputs['Alpha'],bsdf.inputs['Alpha'])
        # Preserve diffuse alpha in the portable albedo image.
        rgba=np.ones(diff.shape,np.float32);rgba[:,:,:3]=srgb(base);rgba[:,:,3]=diff[:,:,3]
        baseim.pixels.foreach_set(rgba.ravel());baseim.save()
        mat.surface_render_method='DITHERED'
    if 'RotationMap1' in tex:
        rot=pixels(tex['RotationMap1']);nx=rot[:,:,3]*2-1;ny=1-2*rot[:,:,1]
        nvec=np.stack((nx,ny,np.sqrt(np.maximum(0,1-nx*nx-ny*ny))),-1)
        nvec/=np.maximum(np.linalg.norm(nvec,axis=-1,keepdims=True),1e-8)
        n=nodes.new('ShaderNodeTexImage');n.image=image(stem+'_normal',(nvec+1)*.5,'Non-Color')
        normal=nodes.new('ShaderNodeNormalMap');links.new(n.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
        if rot.shape[:2]==base.shape[:2]:
            link(image(stem+'_emission',srgb(base*rot[:,:,2:3]),'sRGB'),'Emission Color')
            bsdf.inputs['Emission Strength'].default_value=1
    if 'GlossMap' in tex:
        gloss=pixels(tex['GlossMap']);rough=(2/(3+63*gloss[:,:,3]))**.25
        link(image(stem+'_rough',np.repeat(rough[:,:,None],3,-1),'Non-Color'),'Roughness')
        spec=np.clip(gloss[:,:,:3]@np.array([.2126,.7152,.0722]),0,1)
        link(image(stem+'_spec',np.repeat(spec[:,:,None],3,-1),'Non-Color'),'Specular IOR Level')
    mats[path]=mat;report['materials'].append(dict(path=path,sourceSha256=sha(local(path)),family='Uber',textures=tex,
        approximation='A1 gamma, packed normal, gloss-to-GGX and specular luminance adapter'))

from io_scene_gr2.ops.import_gr2 import read as read_gr2
class Reporter:
    def report(self,kind,msg):print(kind,msg)
for info in selection['models']:
    path=info['path'];before=set(bpy.context.scene.objects)
    bpy.ops.import_mesh.gr2(filepath=str(local(path)))
    objects=sorted(set(bpy.context.scene.objects)-before,key=lambda x:x.name)
    assert all(o.type=='MESH' for o in objects)
    original=read_gr2(Reporter(),str(local(path)))
    bpy.ops.object.select_all(action='DESELECT')
    triangles=0;vertices=0;mins=Vector((1e9,)*3);maxs=-mins
    for obj in objects:
        obj.select_set(True);obj.data.calc_loop_triangles()
        triangles+=len(obj.data.loop_triangles);vertices+=len(obj.data.vertices)
        for i,slot in enumerate(obj.material_slots):
            # Name table order is the source binding; importer slot labels retain those names.
            name=slot.material.name.split('.')[0]
            candidates=[m for m in info['materials'] if Path(m).stem==name]
            assert len(candidates)==1,(path,name,info['materials'])
            obj.data.materials[i]=mats[candidates[0]]
        obj['source_path']=path;obj['source_sha256']=sha(local(path))
        for v in obj.data.vertices:
            p=obj.matrix_world@v.co
            # Blender Z-up -> glTF/Godot Y-up.
            q=Vector((p.x,p.z,-p.y))
            for i in range(3):mins[i]=min(mins[i],q[i]);maxs[i]=max(maxs[i],q[i])
    output=OUT/(Path(path).stem+'.glb')
    bpy.ops.export_scene.gltf(filepath=str(output),export_format='GLB',use_selection=True,export_yup=True,
        export_normals=True,export_tangents=True,export_texcoords=True,export_materials='EXPORT',
        export_animations=False,export_extras=True,export_image_format='AUTO')
    report['models'].append(dict(path=path,output='res://Assets/Maps/'+map_id()+'/'+output.name,
        sourceSha256=sha(local(path)),sha256=sha(output),vertices=vertices,triangles=triangles,
        boundsMin=list(mins),boundsMax=list(maxs),materials=info['materials']))
    for obj in objects:bpy.data.objects.remove(obj,do_unlink=True)

# Compose full affine transforms independently of Godot, including nonuniform scale.
placements={p['id']:p for p in selection['placements']};worlds={}
def world(id,stack=()):
    assert id not in stack,'parent cycle'
    if id not in worlds:
        p=placements[id]
        localmat=Matrix.Translation(Vector(p['position']))@Euler([math.radians(v) for v in p['rotation']],'ZXY').to_matrix().to_4x4()@Matrix.Diagonal(Vector(p['scale']+[1]))
        worlds[id]=(world(p['parent'],stack+(id,)) if p['parent']!='0' else Matrix.Identity(4))@localmat
    return worlds[id]
for id,p in placements.items():
    report['placements'].append(dict(id=id,parent=p['parent'],worldMatrix=[list(r) for r in world(id)]))
manifest('model-conversion').write_text(json.dumps(report,indent=2))
print('MAP_MODELS_CONVERTED',len(report['models']))
