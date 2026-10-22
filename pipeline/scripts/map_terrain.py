"""Bake source splat weights into bounded portable maps; stage original grid data."""
import json,math,re
from pathlib import Path
import numpy as np
from PIL import Image
from source_pipeline import ROOT,sha
from maps_common import local,work,asset_dir,manifest,map_id

def linear(a):return np.where(a<=.04045,a/12.92,((a+.055)/1.055)**2.4)
def srgb(a):return np.where(a<=.0031308,12.92*a,1.055*np.maximum(a,0)**(1/2.4)-.055)
def sample(path,u,v):
    a=np.asarray(Image.open(local(path)).convert('RGBA'),dtype=np.float32)/255
    h,w=a.shape[:2];x=(u%1)*w-.5;y=(v%1)*h-.5
    ix=np.floor(x).astype(int);iy=np.floor(y).astype(int);tx=(x-ix)[...,None];ty=(y-iy)[...,None]
    return ((a[iy%h,ix%w]*(1-tx)+a[iy%h,(ix+1)%w]*tx)*(1-ty)+
            (a[(iy+1)%h,ix%w]*(1-tx)+a[(iy+1)%h,(ix+1)%w]*tx)*ty)

def main():
    selection=json.loads((work()/'selection.json').read_text())
    models=json.loads(manifest('model-conversion').read_text())
    out=asset_dir()
    for model in models['models']:
        path=ROOT/'godot'/model['output'].removeprefix('res://')
        imp=path.with_suffix('.glb.import')
        settings={'meshes/generate_lods':'false','meshes/force_disable_compression':'true','animation/import':'false'}
        text=imp.read_text() if imp.exists() else '[remap]\nimporter="scene"\ntype="PackedScene"\n\n[params]\n'
        for key,value in settings.items():
            if key+'=' in text:text=re.sub(re.escape(key)+r'=[^\n]*',key+'='+value,text)
            else:text+='\n'+key+'='+value+'\n'
        imp.write_text(text)
    report=dict(patches=[],adjacency=[],lighting='authored warm directional/ambient; no native parity',
        limitations=['World-space UV phase/scale reconstruction is not native-client verified.',
            'SWTOR material lighting translated to portable PBR; original vertex color used as multiplicative tint.',
            'DYD density data retained; procedural native billboard/mesh scattering is not reproduced.',
            'Static collision is derived from converted visible triangles, not original physics shapes.'])
    for patch in selection['terrain']:
        id=patch['id'];w=patch['width'];d=patch['depth'];p=patch['position']
        verts=np.array(patch['vertices']);sw=patch['splatWidth'];sd=patch['splatDepth']
        n=1024
        ux=(np.arange(n)+.5)/n;uvx,uvz=np.meshgrid(ux,ux)
        x=verts[0,0]+p[0]+uvx*.2*(w-1);z=verts[0,2]+p[2]+uvz*.2*(d-1)
        rgb=np.zeros((n,n,3),np.float32);rough=np.zeros((n,n),np.float32)
        normal=np.zeros_like(rgb);total=np.zeros((n,n),np.float32)
        weight_maps={};dropped={}
        for idx,values in patch['weights'].items():
            if max(values)==0:continue
            if idx not in selection['terrainLayers']:
                dropped[idx]=int((np.asarray(values)>0).sum());continue
            raw=np.asarray(values,dtype=np.uint8).reshape(sd,sw)
            Image.fromarray(raw).save(out/f'{id}_weight_{idx}.png')
            # Pixel centers at grid samples, bilinear resample without changing raw evidence.
            gx=uvx*(sw-1);gz=uvz*(sd-1);ix=np.floor(gx).astype(int);iz=np.floor(gz).astype(int)
            tx=gx-ix;tz=gz-iz
            wt=((raw[iz,ix]*(1-tx)+raw[iz,np.minimum(ix+1,sw-1)]*tx)*(1-tz)+
                (raw[np.minimum(iz+1,sd-1),ix]*(1-tx)+raw[np.minimum(iz+1,sd-1),np.minimum(ix+1,sw-1)]*tx)*tz)/255
            weight_maps[idx]=wt;total+=wt
        rawRange=[float(total.min()),float(total.max())]
        # Some April patches carry stray unmapped layers or slightly over-full sums: renormalize, report both.
        gap=total<0.02
        if gap.any():
            best=max(weight_maps,key=lambda k:float(weight_maps[k].sum()))
            weight_maps[best]=np.where(gap,1.0,weight_maps[best]);total=sum(weight_maps.values())
        assert total.min()>0.01,('blend gap',id,rawRange)
        for idx,wt in weight_maps.items():
            wt=wt/total;layer=selection['terrainLayers'][idx]
            info=layer.get('material');tex=info['textures'] if info else {'DiffuseMap':layer['texture']}
            scale=[float(v) for v in (info['values'].get('UVScaling',info['values'].get('UvScaling','2,2')) if info else '2,2').split(',')]
            u=x*scale[0];v=z*scale[1]
            diffuse=sample(tex['DiffuseMap'],u,v)
            # The established Blender adapter reads normalized DDS channels, then applies gamma.
            # Decode once: treating those source values as already-linearized would double gamma.
            rgb+=np.maximum(diffuse[:,:,:3],0)**2.1*wt[:,:,None]
            if 'GlossMap' in tex:
                gloss=sample(tex['GlossMap'],u,v);r=(2/(3+63*gloss[:,:,3]))**.25
            else:r=np.full((n,n),.9)
            rough+=r*wt
            if 'RotationMap1' in tex:
                rot=sample(tex['RotationMap1'],u,v);nx=rot[:,:,3]*2-1;ny=1-2*rot[:,:,1]
                nn=np.stack([nx,ny,np.sqrt(np.maximum(0,1-nx*nx-ny*ny))],-1)
                nn/=np.maximum(np.linalg.norm(nn,axis=-1,keepdims=True),1e-8)
            else:nn=np.dstack([np.zeros((n,n)),np.zeros((n,n)),np.ones((n,n))])
            normal+=nn*wt[:,:,None]
        if patch['colors']:
            color=np.asarray(patch['colors'],dtype=np.uint8).reshape(d,w,3)
            Image.fromarray(color).save(out/f'{id}_source_color.png')
            tint=np.asarray(Image.fromarray(color).resize((n,n),Image.Resampling.BILINEAR),np.float32)/255
            rgb*=tint
        normal/=np.maximum(np.linalg.norm(normal,axis=-1,keepdims=True),1e-8)
        maps={}
        for name,a in [('albedo',srgb(rgb)),('normal',(normal+1)*.5),('roughness',rough)]:
            file=out/f'{id}_{name}.png';Image.fromarray(np.uint8(np.clip(a,0,1)*255+.5)).save(file)
            maps[name]='res://Assets/Maps/'+map_id()+'/'+file.name
        holes=bytes.fromhex(patch['holes'])
        def solid(x,z):
            # Source bit order is x-major/z-minor, MSB first (STE2 + reader).
            i=x*d+z
            return not holes or bool(holes[i>>3]&(1<<(7-(i&7))))
        indices=[]
        for z0 in range(d-1):
            for x0 in range(w-1):
                a=z0*w+x0;b=a+1;c=a+w;dd=c+1
                if not (solid(x0+1,z0) and solid(x0,z0+1)):continue
                # Selected patches have an empty tessellation field: use STE2's fixed diagonal.
                # Explicit SHORTEST is supported when actually authored on a selected patch.
                if patch['tessellation']=='SHORTEST' and solid(x0,z0) and solid(x0+1,z0+1) and np.linalg.norm(verts[a]-verts[dd])<np.linalg.norm(verts[b]-verts[c]):
                    faces=[(a,c,dd),(a,dd,b)]
                else:faces=([(a,c,b)] if solid(x0,z0) else [])+([(b,c,dd)] if solid(x0+1,z0+1) else [])
                # Godot clockwise front faces reverse the source CCW triangle order.
                for face in faces:indices.extend([face[0],face[2],face[1]])
        patch['indices']=indices;patch['maps']=maps
        report['patches'].append(dict(id=id,room=selection['room'],width=w,depth=d,position=p,
            version=patch['flag'],vertices=len(verts),triangles=len(indices)//3,sourceHeightEnd=patch['heightEnd'],
            embeddedSha256=patch['embeddedSha256'],decodedSha256=patch['decodedSha256'],
            sourceTessellation=patch['tessellation'],topology='STE2 fixed diagonal when source field empty; native default unverified',
            sourceHeightUnit='border float32; interior uint16 / 512; STE2 approximate scalar compared separately',
            splatSize=[sw,sd],layers=list(weight_maps),blendSumRange=rawRange,droppedUnmappedLayerTexels=dropped,
            legacyDenseCopy=patch['legacyDense'],holes=bool(holes),dyd=patch['dyd'],maps=maps,
            embeddedPosition=patch['embeddedPosition']))
    # Patches are staged exactly as authored (no weld/repair); overlap and hole-fill patches are expected.
    report['adjacency']=[dict(note='authored patches staged unmodified; seams not repaired',patchCount=len(selection['terrain']))]
    selection['modelOutputs']={m['path']:m['output'] for m in models['models']}
    selection['expectedMatrices']={p['id']:p['worldMatrix'] for p in models['placements']}
    # Keep exact raw source weights/colors in the source manifest/work selection;
    # runtime only needs geometry, maps, transforms and provenance identifiers.
    for p in selection['terrain']:
        for key in ['weights','colors','tail','trailing']:p.pop(key,None)
    selection['lightingEvidence']=[]
    (out/'environment.json').write_text(json.dumps(selection,separators=(',',':')))
    report['passed']=True
    manifest('terrain-conversion').write_text(json.dumps(report,indent=2))
    print('MAP_TERRAIN_CONVERTED',len(selection['terrain']))

if __name__=='__main__':main()
