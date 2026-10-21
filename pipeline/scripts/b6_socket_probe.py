import bpy,json,sys,math,struct
from pathlib import Path
from mathutils import Matrix,Vector,Quaternion
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools/addons'))
import addon_utils
addon_utils.enable('io_scene_gr2',default_set=True,persistent=False)
from io_scene_gr2.ops.import_gr2 import read
class R:
 def report(self,*a):pass
source=read(R(),str(ROOT/'source-assets/april2012/resources/art/dynamic/spec/bmnnew_skeleton.gr2'))
bone=next(b for b in source.bone_buffer.values() if b.name=='RightWeapon')
native=Matrix([bone.root_to_bone[i*4:i*4+4] for i in range(4)]).transposed().inverted()
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(ROOT/'godot/Assets/sorcerer.glb'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
converted=rig.matrix_world @ rig.data.bones['RightWeapon'].matrix_local
# Blender world back to Godot; a local bone frame needs the same basis convention as child geometry.
C=Matrix.Rotation(-math.pi/2,4,'X')
print('NATIVE',list(map(list,native)));print('GLTF_GODOT',list(map(list,C @ converted @ C.inverted())))
data=(ROOT/'godot/Assets/sorcerer.glb').read_bytes();doc=json.loads(data[20:20+struct.unpack_from('<I',data,12)[0]])
world={}
def visit(idx,parent):
 n=doc['nodes'][idx]
 if 'matrix' in n:m=Matrix([n['matrix'][i*4:i*4+4] for i in range(4)]).transposed()
 else:
  q=n.get('rotation',[0,0,0,1]);m=Matrix.LocRotScale(Vector(n.get('translation',[0,0,0])),Quaternion((q[3],*q[:3])),Vector(n.get('scale',[1,1,1])))
 world[idx]=parent@m
 for child in n.get('children',[]):visit(child,world[idx])
for idx in doc['scenes'][0]['nodes']:visit(idx,Matrix.Identity(4))
idx=next(i for i,n in enumerate(doc['nodes']) if n.get('name')=='RightWeapon')
correction=world[idx].inverted()@native
print('DIRECT_GLTF',list(map(list,world[idx])));print('DIRECT_CORRECTION',list(map(list,correction)))
(ROOT/'manifests/b6-weapon-socket.json').write_text(json.dumps(dict(sourceBone='RightWeapon',nativeRest=list(map(list,native)),gltfRest=list(map(list,world[idx])),attachmentCorrection=list(map(list,correction))),indent=2))
