"""B6: original GR2 weights, rig-family JBA clips and embedded portable materials."""
import bpy,addon_utils,sys,json,math,hashlib,tempfile
from pathlib import Path
import numpy as np
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools/addons'))
tempfile.tempdir=str(ROOT/'work/temp')
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
prefs=bpy.context.preferences.addons['io_scene_gr2'].preferences
prefs.gr2_scale_object=False;prefs.gr2_apply_axis_conversion=True;prefs.gr2_import_collision=False
from io_scene_gr2.ops.import_jba import read as read_jba
recipe=json.loads((ROOT/'b6-recipe.json').read_text());OUT=ROOT/'godot/Assets/Enemies';OUT.mkdir(exist_ok=True)
TEX=ROOT/'generated/b6';TEX.mkdir(exist_ok=True)
def local(p):return str(ROOT/'source-assets/april2012'/p.lstrip('/'))
def pixels(p,color=False):
 im=bpy.data.images.load(local(p),check_existing=True);im.colorspace_settings.name='sRGB' if color else 'Non-Color';im.alpha_mode='CHANNEL_PACKED'
 a=np.empty(len(im.pixels),np.float32);im.pixels.foreach_get(a);return a.reshape(im.size[1],im.size[0],4)
def image(stem,a,cs):
 h,w=a.shape[:2];v=np.ones((h,w,4),np.float32);v[:,:,:a.shape[2]]=a
 im=bpy.data.images.new(stem,width=w,height=h,alpha=True);im.colorspace_settings.name=cs;im.pixels.foreach_set(v.ravel());im.filepath_raw=str(TEX/(stem+'.png'));im.file_format='PNG';im.save();return im
def srgb(x):return np.where(x<=.0031308,x*12.92,1.055*np.maximum(x,0)**(1/2.4)-.055)
cache={}
def material(info):
 p=info['path']
 if p in cache:return cache[p]
 t=info['textures'];stem=Path(p).stem;mat=bpy.data.materials.new(stem);mat.use_nodes=True
 mat['sourcePath']=p;mat['sourceFamily']=info['derived'];nodes=mat.node_tree.nodes;links=mat.node_tree.links;b=nodes.get('Principled BSDF')
 def link(im,target):
  n=nodes.new('ShaderNodeTexImage');n.image=im;links.new(n.outputs['Color'],b.inputs[target]);return n
 d=pixels(t['DiffuseMap'],True);base=srgb(np.maximum(d[:,:,:3],0)**2.1)
 link(image(stem+'_base',base,'sRGB'),'Base Color')
 if 'RotationMap1' in t:
  r=pixels(t['RotationMap1']);x=r[:,:,3]*2-1;y=1-2*r[:,:,1];n=np.stack([x,y,np.sqrt(np.maximum(0,1-x*x-y*y))],-1);n/=np.maximum(np.linalg.norm(n,axis=-1,keepdims=True),1e-8)
  tex=nodes.new('ShaderNodeTexImage');tex.image=image(stem+'_normal',(n+1)*.5,'Non-Color');normal=nodes.new('ShaderNodeNormalMap');links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],b.inputs['Normal'])
 if 'GlossMap' in t:
  g=pixels(t['GlossMap']);rough=(2/(3+63*g[:,:,3]))**.25;link(image(stem+'_rough',np.repeat(rough[:,:,None],3,-1),'Non-Color'),'Roughness')
 cache[p]=mat;return mat
def export(name):
 output=OUT/(name+'.glb')
 bpy.ops.export_scene.gltf(filepath=str(output),export_format='GLB',use_selection=True,export_yup=True,export_animations=True,export_animation_mode='ACTIONS',export_force_sampling=True,export_anim_slide_to_zero=True,export_frame_range=False,export_frame_step=1,export_skins=True,export_def_bones=False,export_leaf_bone=False,export_extras=True)
 return dict(output=str(output.relative_to(ROOT)),sha256=hashlib.sha256(output.read_bytes()).hexdigest())
class Reporter:
 def report(self,kind,msg):print(kind,msg)
report={}
for name,info in recipe.items():
 if name=='weapons':continue
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 if name=='looter':
  bpy.ops.wm.open_mainfile(filepath=str(ROOT/'work/sorcerer-portable.blend'))
  rig=bpy.data.objects['bmnnew']
  # These exact head/hair IDs occur among the tomb robber's source variants.
  for o in list(bpy.data.objects):
   if o.type=='MESH' and o.get('slot') not in ['head','hair']:bpy.data.objects.remove(o,do_unlink=True)
 else:
  bpy.ops.import_mesh.gr2(filepath=local(info['skeleton']));rig=bpy.context.object;rig.name='Rig'
 cache={};rig.animation_data_clear()
 for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 meshes=[]
 for part in info['parts']:
  for p in part['models']:
   before=set(bpy.data.objects);bpy.ops.import_mesh.gr2(filepath=local(p))
   for o in set(bpy.data.objects)-before:
    if o.type!='MESH':continue
    o.parent=rig;o['sourcePath']=p;mod=o.modifiers.new('OriginalWeights','ARMATURE');mod.object=rig
    missing={o.vertex_groups[g.group].name for v in o.data.vertices for g in v.groups if g.weight>0 and o.vertex_groups[g.group].name not in rig.data.bones}
    assert not missing,(p,missing)
    for idx in range(len(o.data.materials)):
     override=next((a['material'] for a in part['overrides'] if a['index']==idx),part['material']);o.data.materials[idx]=material(override)
    meshes.append(o)
 clips=[]
 for clip,p in info['clips'].items():
  bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
  raw=read_jba(Reporter(),local(p));bpy.ops.import_animation.jba(filepath=local(p),ignore_facial_bones=True,delete_180=False,scale_animation=False,scale_factor=1)
  a=rig.animation_data.action;a.name=clip;a.use_fake_user=True
  for layer in a.layers:
   for strip in layer.strips:
    for bag in strip.channelbags:
     for curve in bag.fcurves:
      for k in curve.keyframe_points:
       for v in [k.co,k.handle_left,k.handle_right]:v.x=1+(v.x-1)*60/raw.fps
  a.use_frame_range=True;a.frame_start=1;a.frame_end=1+round(raw.length*60)
  unmatched=[b.name for b in raw.bones if b.name not in rig.data.bones];assert not unmatched,(name,unmatched)
  clips.append(dict(name=clip,path=p,seconds=raw.length,unmatched=unmatched))
  bpy.ops.object.mode_set(mode='OBJECT')
 rig.animation_data.action=None
 for b in rig.pose.bones:b.matrix_basis.identity()
 bpy.context.scene.render.fps=60;bpy.context.scene.frame_set(1)
 bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
 for o in bpy.context.scene.objects:
  if o.type=='MESH':o.select_set(True)
 bpy.context.view_layer.objects.active=rig
 report[name]=dict(**export(name),bones=len(rig.data.bones),clips=clips,meshCount=len(meshes),sockets=[b.name for b in rig.data.bones if any(k in b.name.lower() for k in ['weapon','attach','hand'])])
for name,info in recipe['weapons'].items():
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);cache={}
 bpy.ops.import_mesh.gr2(filepath=local(info['model']))
 for o in bpy.context.scene.objects:
  if o.type!='MESH':continue
  for i,slot in enumerate(o.material_slots):
   stem=slot.material.name.split('.')[0];m=next(m for m in info['materials'] if Path(m['path']).stem==stem);o.data.materials[i]=material(m)
 bpy.ops.object.select_all(action='SELECT')
 report[name]=dict(**export(name),bounds=[list(o.bound_box[0]) for o in bpy.context.scene.objects if o.type=='MESH'])
(ROOT/'manifests/b6-conversion.json').write_text(json.dumps(report,indent=2))
print('B6 CONVERSION COMPLETE')
