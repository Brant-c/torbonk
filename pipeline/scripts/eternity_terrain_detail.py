"""Stage source terrain layers with aligned, unmodified blend data and blended relief."""
import json,sys,re
from pathlib import Path
import numpy as np
from PIL import Image
from maps_common import work,asset_dir,local,map_id,manifest
s=json.loads((work()/'selection.json').read_text());e=json.loads((asset_dir()/'environment.json').read_text());out=asset_dir();base='res://Assets/Maps/'+map_id()+'/'
report=[]
# The snow overlay's diffuse alpha is opaque; SWTOR packs opacity in RotationMap red.
overlay='/resources/art/static/area/hth_hoth/terrain_map/ter_hth_snow_highfreq01_alpha_'
overlay_color=Image.open(local(overlay+'d.dds')).convert('RGBA')
overlay_mask=Image.open(local(overlay+'n.dds')).convert('RGBA').getchannel('R')
overlay_color.putalpha(overlay_mask)
overlay_color.save(out/'snow-overlay-cutout.png')
def data_import(file,mipmaps=False):
 p=(out/file).with_suffix('.png.import')
 text=p.read_text() if p.exists() else '[remap]\nimporter="texture"\ntype="CompressedTexture2D"\n\n[params]\n'
 for key,value in {'process/fix_alpha_border':'false','process/premult_alpha':'false','compress/mode':'0','mipmaps/generate':'true' if mipmaps else 'false'}.items():
  if key+'=' in text:text=re.sub(re.escape(key)+r'=[^\n]*',key+'='+value,text)
  else:text+='\n'+key+'='+value+'\n'
 p.write_text(text)
for patch in s['terrain']:
 used=[k for k,v in patch['weights'].items() if max(v)>0 and k in s['terrainLayers']];w=patch['splatWidth'];h=patch['splatDepth']
 raw=np.array([patch['weights'][k] for k in used],dtype=np.float32).reshape(len(used),h,w)
 total=raw.sum(axis=0);assert total.min()>0,patch['id']
 weights=raw/total
 uniforms=['shader_type spatial;','render_mode cull_disabled;','varying vec3 source_pos;','void vertex(){source_pos=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;}']
 params=[]
 def texture(name,file,hints):
  params.append((name,file,len(params)+1));uniforms.append(f'uniform sampler2D {name} : {hints};')
 for i in range(0,len(used),4):
  packed=np.zeros((h,w,4),dtype=np.uint8)
  for j in range(min(4,len(used)-i)):packed[:,:,j]=(weights[i+j]*255+.5).astype(np.uint8)
  name=f'{patch["id"]}_weights{i//4}.png';Image.fromarray(packed).save(out/name);data_import(name);texture(f'weights{i//4}',name,'filter_linear, repeat_disable')
 body=[f'vec2 blend_uv=(UV*vec2({w-1}.0,{h-1}.0)+vec2(0.5))/vec2({w}.0,{h}.0);']
 for i in range((len(used)+3)//4):body.append(f'vec4 w{i}=texture(weights{i},blend_uv);')
 for i in range(len(used)):body.append(f'float b{i}=w{i//4}.{ "rgba"[i%4]};')
 body.append('float total='+ '+'.join(f'b{i}' for i in range(len(used)))+';')
 colors=[];normals=[];roughness=[]
 for i,k in enumerate(used):
  info=s['terrainLayers'][k];mat=info.get('material');tex=mat['textures']['DiffuseMap'] if mat else info['texture'];name=Path(tex).stem+'.png'
  Image.open(local(tex)).convert('RGBA').save(out/name);texture(f'layer{i}',name,'source_color, filter_linear_mipmap_anisotropic, repeat_enable')
  scale=[float(x) for x in (mat['values'].get('UVScaling',mat['values'].get('UvScaling','2,2')) if mat else '2,2').split(',')]
  uv=f'source_pos.xz*vec2({scale[0]},{scale[1]})'
  colors.append(f'texture(layer{i},{uv}).rgb*b{i}')
  normal=mat['textures'].get('RotationMap1') if mat else None
  if normal:
   a=np.asarray(Image.open(local(normal)).convert('RGBA'),dtype=np.float32)/255;nx=a[:,:,3]*2-1;ny=1-2*a[:,:,1];nn=np.stack([nx,ny,np.sqrt(np.maximum(0,1-nx*nx-ny*ny))],-1);nn/=np.maximum(np.linalg.norm(nn,axis=-1,keepdims=True),1e-8)
   gloss=mat['textures'].get('GlossMap')
   if gloss:
    g=np.asarray(Image.open(local(gloss)).convert('RGBA').resize((a.shape[1],a.shape[0]),Image.Resampling.BILINEAR),dtype=np.float32)/255
    rough=(2/(3+63*g[:,:,3]))**.25
   else:rough=np.full(a.shape[:2],.9)
   packed=np.dstack(((nn+1)*.5,rough))
   name=Path(normal).stem+'_surface.png';Image.fromarray(np.uint8(np.clip(packed*255,0,255))).save(out/name);data_import(name,True)
   texture(f'normal{i}',name,'filter_linear_mipmap_anisotropic, repeat_enable')
   body.append(f'vec4 surface{i}=texture(normal{i},{uv});')
   normals.append(f'(surface{i}.rgb*2.0-1.0)*b{i}');roughness.append(f'surface{i}.a*b{i}')
  else:
   normals.append(f'vec3(0.0,0.0,1.0)*b{i}');roughness.append(f'0.9*b{i}')
 # Presentation tuning: the portable gloss conversion was too shiny for the snow surface.
 body+=['ALBEDO=('+ '+'.join(colors)+')/max(total,0.00001)*COLOR.rgb;','ROUGHNESS=max(0.88,('+ '+'.join(roughness)+')/max(total,0.00001));','SPECULAR=0.12;','NORMAL_MAP=normalize('+ '+'.join(normals)+')*0.5+0.5;']
 uniforms.append('void fragment(){'+''.join(body)+'}')
 assert len(params)<=16,(patch['id'],len(params))
 shader=f'{patch["id"]}_detail.gdshader';(out/shader).write_text('\n'.join(uniforms))
 text=['[gd_resource type="ShaderMaterial" load_steps='+str(len(params)+2)+' format=3]',f'[ext_resource type="Shader" path="{base+shader}" id="shader"]']
 for name,file,i in params:text.append(f'[ext_resource type="Texture2D" path="{base+file}" id="{i}"]')
 text+=['[resource]','shader = ExtResource("shader")']+[f'shader_parameter/{name} = ExtResource("{i}")' for name,file,i in params]
 material=f'{patch["id"]}_detail.tres';(out/material).write_text('\n'.join(text)+'\n')
 target=next(p for p in e['terrain'] if p['id']==patch['id']);target['detailMaterial']=base+material;target['vertexColors']=patch['colors']
 report.append(dict(id=patch['id'],sourceBlendSum=[float(total.min()),float(total.max())],layers=used,samplers=len(params),vertexColors=len(patch['colors'])))
(out/'environment.json').write_text(json.dumps(e,separators=(',',':')))
manifest('detail-blending').write_text(json.dumps(dict(patches=report,policy='Source blend-grid endpoints aligned to texel centres; alpha is data; normal layers share diffuse weights; source vertex tint interpolated on triangles.'),indent=2))
print('TILED_TERRAIN',len(e['terrain']))
