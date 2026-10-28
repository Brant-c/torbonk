"""Stage native-resolution tiled terrain textures and packed source blend weights."""
import json,sys
from pathlib import Path
import numpy as np
from PIL import Image
from maps_common import work,asset_dir,local
s=json.loads((work()/'selection.json').read_text());e=json.loads((asset_dir()/'environment.json').read_text());out=asset_dir();base='res://Assets/Maps/eternity_vault/'
for patch in s['terrain']:
 used=[k for k,v in patch['weights'].items() if max(v)>0 and k in s['terrainLayers']];w=patch['splatWidth'];h=patch['splatDepth']
 weights=np.array([patch['weights'][k] for k in used],dtype=np.float32).reshape(len(used),h,w)
 weights/=np.maximum(weights.sum(axis=0),1e-8)
 uniforms=['shader_type spatial;','render_mode cull_disabled;','varying vec3 source_pos;','void vertex(){source_pos=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;}']
 params=[];count=0
 def texture(name,file,hints):
  global count
  count+=1;uniforms.append(f'uniform sampler2D {name} : {hints};')
  params.append((name,file,count))
 for i in range(0,len(used),4):
  packed=np.zeros((h,w,4),dtype=np.uint8)
  for j in range(min(4,len(used)-i)):packed[:,:,j]=(weights[i+j]*255+.5).astype(np.uint8)
  name=f'{patch["id"]}_weights{i//4}.png';Image.fromarray(packed).save(out/name);texture(f'weights{i//4}',name,'filter_linear, repeat_disable')
 terms=[]
 for i,k in enumerate(used):
  info=s['terrainLayers'][k];mat=info.get('material');tex=mat['textures']['DiffuseMap'] if mat else info['texture'];name=Path(tex).stem+'.png'
  Image.open(local(tex)).convert('RGBA').save(out/name);texture(f'layer{i}',name,'source_color, filter_linear_mipmap_anisotropic, repeat_enable')
  scale=[float(x) for x in (mat['values'].get('UVScaling',mat['values'].get('UvScaling','2,2')) if mat else '2,2').split(',')]
  terms.append(f'texture(layer{i},source_pos.xz*vec2({scale[0]},{scale[1]})).rgb*texture(weights{i//4},UV).{ "rgba"[i%4]}')
 tint=f'{patch["id"]}_source_color.png'
 if (out/tint).exists():texture('tint',tint,'source_color, filter_linear, repeat_disable')
 # Source packed normal on the strongest layer preserves close-up relief without a patch-resolution bake.
 k=used[int(np.argmax(weights.sum(axis=(1,2))))];info=s['terrainLayers'][k].get('material');normal=info['textures'].get('RotationMap1') if info else None
 normal_line=''
 if normal:
  a=np.asarray(Image.open(local(normal)).convert('RGBA'),dtype=np.float32)/255;nx=a[:,:,3]*2-1;ny=1-2*a[:,:,1];nn=np.stack([nx,ny,np.sqrt(np.maximum(0,1-nx*nx-ny*ny))],-1);nn/=np.maximum(np.linalg.norm(nn,axis=-1,keepdims=True),1e-8)
  name=Path(normal).stem+'_detail.png';Image.fromarray(np.uint8((nn+1)*127.5)).save(out/name);texture('detail_normal',name,'hint_normal, filter_linear_mipmap_anisotropic, repeat_enable');scale=info['values'].get('UVScaling',info['values'].get('UvScaling','2,2'));normal_line=f'NORMAL_MAP=texture(detail_normal,source_pos.xz*vec2({scale})).rgb;'
 uniforms.append('void fragment(){ALBEDO=('+ '+'.join(terms)+')'+('*texture(tint,UV).rgb' if (out/tint).exists() else '')+';ROUGHNESS=0.88;'+normal_line+'}')
 shader=f'{patch["id"]}_detail.gdshader';(out/shader).write_text('\n'.join(uniforms))
 text=['[gd_resource type="ShaderMaterial" load_steps='+str(count+2)+' format=3]',f'[ext_resource type="Shader" path="{base+shader}" id="shader"]']
 for name,file,i in params:text.append(f'[ext_resource type="Texture2D" path="{base+file}" id="{i}"]')
 text+=['[resource]','shader = ExtResource("shader")']+[f'shader_parameter/{name} = ExtResource("{i}")' for name,file,i in params]
 material=f'{patch["id"]}_detail.tres';(out/material).write_text('\n'.join(text)+'\n')
 next(p for p in e['terrain'] if p['id']==patch['id'])['detailMaterial']=base+material
(out/'environment.json').write_text(json.dumps(e,separators=(',',':')))
print('TILED_TERRAIN',len(e['terrain']))
