"""Blender: compare complete triangle corners before export and after GLB reimport."""
import sys, runpy, json, math
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1]
runpy.run_path(str(ROOT/'scripts/blender_probe.py'),run_name='roundtrip_probe')
def corners(obj):
    mesh=obj.data; mesh.calc_loop_triangles()
    result=[]; opposed=0
    for t in mesh.loop_triangles:
        record=[]
        for li in t.loops:
            loop=mesh.loops[li]; p=obj.matrix_world@mesh.vertices[loop.vertex_index].co
            n=obj.matrix_world.to_3x3()@mesh.corner_normals[li].vector
            uv=mesh.uv_layers.active.data[li].uv
            record.append(tuple(float(x) for x in (*p,*n,*uv)))
        # Preserve winding, but allow exporter to rotate the first triangle index.
        result.append(min(tuple(record[i:]+record[:i]) for i in range(3)))
        if t.normal.dot(mesh.corner_normals[t.loops[0]].vector)<0: opposed+=1
    return sorted(result),opposed
bpy.context.view_layer.update()
source=next(o for o in bpy.context.scene.objects if o.type=='MESH')
before,opposed_before=corners(source)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(ROOT/'generated/sith_academy_door.glb'))
bpy.context.view_layer.update()
after,opposed_after=corners(next(o for o in bpy.context.scene.objects if o.type=='MESH'))
# Match triangles by positions, and compare cyclic index rotations so a reversed
# winding cannot pass. Keep full precision for measuring normal quantization.
remaining=list(after); max_position=max_normal=max_uv=max_angle=0.0
for tri in before:
    candidates=[]
    for index,other in enumerate(remaining):
        for shift in range(3):
            aligned=other[shift:]+other[:shift]
            delta=max(abs(a[c]-b[c]) for a,b in zip(tri,aligned) for c in range(3))
            candidates.append((delta,index,aligned))
    delta,index,aligned=min(candidates,key=lambda x:x[0]); remaining.pop(index)
    max_position=max(max_position,delta)
    max_normal=max(max_normal,max(abs(a[c]-b[c]) for a,b in zip(tri,aligned) for c in range(3,6)))
    for a,b in zip(tri,aligned):
        dot=sum(a[c]*b[c] for c in range(3,6))
        norm=math.sqrt(sum(a[c]**2 for c in range(3,6))*sum(b[c]**2 for c in range(3,6)))
        max_angle=max(max_angle,math.degrees(math.acos(max(-1,min(1,dot/norm)))))
    max_uv=max(max_uv,max(abs(a[c]-b[c]) for a,b in zip(tri,aligned) for c in range(6,8)))
passed=max_position<1e-6 and max_angle<0.1 and max_uv<1e-6 and len(before)==len(after)
result={'passed':passed,'maxPositionError':max_position,'maxNormalComponentError':max_normal,'maxUvError':max_uv,
        'maxNormalAngleDegrees':max_angle,
        'tolerances':{'position':1e-6,'normalAngleDegrees':0.1,'uv':1e-6},
        'trianglesBefore':len(before),'trianglesAfter':len(after),
        'sourceOpposedFaceNormals':opposed_before,'roundtripOpposedFaceNormals':opposed_after,
        'compared':['world positions','corner normals','UV0','triangle winding'],
        'unverified':['Original physical units','Original client shader parity','Source collision']}
(ROOT/'manifests/roundtrip-validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
assert result['passed'],result
print(json.dumps(result,indent=2))
