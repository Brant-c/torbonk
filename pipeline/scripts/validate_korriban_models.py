"""Blender independent original GR2 -> GLB triangle-corner round trips."""
import sys,json,math,collections,struct
from pathlib import Path
import bpy,addon_utils,numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'scripts'));sys.path.insert(0,str(ROOT/'tools/addons'))
from korriban_source import local
from mathutils import Matrix,Vector,Quaternion
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
prefs=bpy.context.preferences.addons['io_scene_gr2'].preferences
prefs.gr2_scale_object=False;prefs.gr2_scale_factor=1;prefs.gr2_apply_axis_conversion=True;prefs.gr2_import_collision=False
data=json.loads((ROOT/'manifests/korriban-model-conversion.json').read_text());report=dict(models=[],passed=True)
def clear():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def triangles():
    result=collections.defaultdict(list)
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        mesh=obj.data;mesh.calc_loop_triangles()
        normalmat=obj.matrix_world.to_3x3().inverted().transposed()
        for t in mesh.loop_triangles:
            corners=[]
            for li in t.loops:
                loop=mesh.loops[li];p=obj.matrix_world@mesh.vertices[loop.vertex_index].co
                n=normalmat@mesh.corner_normals[li].vector;n.normalize()
                uv=mesh.uv_layers.active.data[li].uv
                corners.append(tuple(float(v) for v in (*p,*n,*uv)))
            # Quantized position key ignores primitive reordering but preserves triangle identity.
            key=tuple(sorted(tuple(round(v,5) for v in c[:3]) for c in corners))
            result[key].append(corners)
    return result
def glb_triangles(path):
    b=Path(path).read_bytes();jl=struct.unpack_from('<I',b,12)[0];doc=json.loads(b[20:20+jl]);binary=b[28+jl:]
    def acc(i):
        a=doc['accessors'][i];view=doc['bufferViews'][a['bufferView']]
        fmt={5126:'f',5125:'I',5123:'H',5121:'B'}[a['componentType']];n={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
        stride=view.get('byteStride',struct.calcsize(fmt)*n);offset=view.get('byteOffset',0)+a.get('byteOffset',0)
        return [struct.unpack_from('<'+fmt*n,binary,offset+j*stride) for j in range(a['count'])]
    out=collections.defaultdict(list);axis=Matrix(((1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))
    def node(i,parent):
        n=doc['nodes'][i];t=n.get('translation',[0,0,0]);q=n.get('rotation',[0,0,0,1]);s=n.get('scale',[1,1,1])
        m=parent@Matrix.LocRotScale(Vector(t),Quaternion((q[3],*q[:3])),Vector(s))
        if 'mesh' in n:
            for p in doc['meshes'][n['mesh']]['primitives']:
                attrs=p['attributes'];positions=acc(attrs['POSITION']);normals=acc(attrs['NORMAL']);uvs=acc(attrs['TEXCOORD_0']);inds=acc(p['indices'])
                for j in range(0,len(inds),3):
                    corners=[]
                    for k in range(3):
                        index=inds[j+k][0];point=axis@m@Vector(positions[index]);normal=(axis@m).to_3x3()@Vector(normals[index]);normal.normalize()
                        corners.append(tuple(float(v) for v in (*point,*normal,uvs[index][0],1-uvs[index][1])))
                    key=tuple(sorted(tuple(round(v,5) for v in c[:3]) for c in corners));out[key].append(corners)
        for child in n.get('children',[]):node(child,m)
    for i in doc['scenes'][doc.get('scene',0)]['nodes']:node(i,Matrix.Identity(4))
    return out
for info in data['models']:
    clear();bpy.ops.import_mesh.gr2(filepath=str(local(info['path'])));bpy.context.view_layer.update();before=triangles()
    direct=glb_triangles(ROOT/'godot'/info['output'].removeprefix('res://'))
    # Check the actual serialized normals separately from Blender's custom-normal re-encoding.
    direct_normal_error=0
    for key,tris in before.items():
        assert key in direct
        for tri in tris:
            for corner in tri:
                candidates=[c for t in direct[key] for c in t if max(abs(c[i]-corner[i]) for i in [0,1,2])<2e-5 and max(abs(c[i]-corner[i]) for i in [6,7])<1e-6]
                assert candidates
                error=min(max(abs(c[i]-corner[i]) for i in [3,4,5]) for c in candidates)
                direct_normal_error=max(direct_normal_error,error)
    clear();bpy.ops.import_scene.gltf(filepath=str(ROOT/'godot'/info['output'].removeprefix('res://')))
    bpy.context.view_layer.update();after=triangles()
    assert before.keys()==after.keys(),info['path']
    maxp=maxuv=maxangle=0;worst=None
    for key,source_tris in before.items():
        remaining=after[key].copy()
        for source in source_tris:
            best=None
            for i,other in enumerate(remaining):
                for shift in range(3):
                    aligned=other[shift:]+other[:shift]
                    error=max(abs(a[c]-b[c]) for a,b in zip(source,aligned) for c in range(3))
                    score=(error,max(abs(a[c]-b[c]) for a,b in zip(source,aligned) for c in [6,7]),max(abs(a[c]-b[c]) for a,b in zip(source,aligned) for c in [3,4,5]))
                    if best is None or score<best[0]:best=(score,i,aligned)
            score,i,aligned=best;remaining.pop(i);maxp=max(maxp,score[0])
            maxuv=max(maxuv,max(abs(a[c]-b[c]) for a,b in zip(source,aligned) for c in [6,7]))
            for a,b in zip(source,aligned):
                dot=sum(a[c]*b[c] for c in [3,4,5]);length=math.sqrt(sum(a[c]**2 for c in [3,4,5])*sum(b[c]**2 for c in [3,4,5]))
                angle=math.degrees(math.acos(max(-1,min(1,dot/length))))
                if angle>maxangle:maxangle=angle;worst=[a,b]
    passed=maxp<2e-5 and maxuv<1e-6 and maxangle<.35 and direct_normal_error<0.0001
    report['models'].append(dict(path=info['path'],passed=passed,maxPositionError=maxp,maxUvError=maxuv,maxNormalAngleDegrees=maxangle,
        triangles=sum(len(v) for v in before.values()),worstNormalCorner=worst,directGlbNormalMaxComponentError=direct_normal_error))
    report['passed'] &= passed
report['compared']=['original imported GR2 triangle positions','UV0','corner normals','cyclic winding']
report['normalPolicy']='Serialized GLB normals must match original imported GR2 within 0.0001 per component; Blender reimport custom-normal encoding allowed 0.35 degrees, measured explicitly. Positions/UVs/winding compared separately.'
(ROOT/'manifests/korriban-model-roundtrip.json').write_text(json.dumps(report,indent=2))
assert report['passed'],[r for r in report['models'] if not r['passed']]
print('KORRIBAN_MODEL_ROUNDTRIPS_PASSED')
