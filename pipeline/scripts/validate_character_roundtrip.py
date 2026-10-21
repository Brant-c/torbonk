"""Compare skin weights and deformed vertices after importing the exported GLB."""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/addons'))
from io_scene_gr2.ops.import_gr2 import read as read_original
class Reporter:
    def report(self,kind,message):print(kind,message)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'work/sorcerer-portable.blend'))
scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE')
source=[o for o in scene.objects if o.type=='MESH'];fps=scene.render.fps
clips=json.loads((ROOT/'character-recipe.json').read_text())['animations']
report=json.loads((ROOT/'manifests/character-conversion.json').read_text())
actions={a.name:a for a in bpy.data.actions}
rig.animation_data.action=None
for bone in rig.pose.bones:bone.matrix_basis.identity()
bpy.context.view_layer.update()
def weights(obj,v):
    return {obj.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>1e-8}
def positions(obj):
    ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
    result=[obj.matrix_world@v.co for v in me.vertices];ev.to_mesh_clear();return result
cache={};source_names={};original_weight_error=0
for obj in source:
    original=read_original(Reporter(),str(ROOT/'source-assets/april2012'/obj['sourcePath'].lstrip('/')))
    assert len(original.mesh_buffer)==1
    original_mesh=original.mesh_buffer[0]
    assert len(original_mesh.vertex_buffer)==len(obj.data.vertices)
    for index,vertex in original_mesh.vertex_buffer.items():
        raw={}
        for joint,weight in zip(vertex.bone_indices,vertex.bone_weights):
            name=original_mesh.bone_buffer[joint].name
            raw[name]=raw.get(name,0)+weight/255
        converted=weights(obj,obj.data.vertices[index])
        error=max(abs(raw.get(k,0)-converted.get(k,0)) for k in raw.keys()|converted.keys())
        original_weight_error=max(original_weight_error,error)
        assert error<1e-7,(obj.name,index,'original GR2 weights changed',error)
    key=obj['sourcePath'];source_names[obj.name]=key;cache[key]={'rest':[obj.matrix_world@v.co for v in obj.data.vertices],
        'weights':[weights(obj,v) for v in obj.data.vertices],'poses':{}}
for clip in clips:
    action=actions[clip['name']];rig.animation_data.action=action
    for sample in range(5):
        frame=1+(action.frame_end-1)*sample/4;scene.frame_set(int(round(frame)))
        for obj in source:cache[obj['sourcePath']]['poses'][(clip['name'],sample)]=positions(obj)
# Destroy only in-memory objects; original blend and originals stay unchanged.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(ROOT/'generated/sorcerer.glb'),import_pack_images=True)
rig=next(o for o in scene.objects if o.type=='ARMATURE')
imported=[o for o in scene.objects if o.type=='MESH' and o.name in source_names];import_actions=list(bpy.data.actions)
assert len(imported)==len(source_names)
print('ROUNDTRIP imported actions',[(a.name,list(a.frame_range)) for a in import_actions])
rig.animation_data.action=None
for bone in rig.pose.bones:bone.matrix_basis.identity()
bpy.context.view_layer.update()
result={'passed':False,'meshes':[],'maxOriginalGr2WeightError':original_weight_error,
        'maxRestPositionError':0,'maxWeightError':0,'maxDeformedVertexError':0,'clips':{}}
mapping={}
for obj in imported:
    obj['sourcePath']=source_names[obj.name]
    assert obj['sourcePath'] in cache;src=cache[obj['sourcePath']]
    ids=[];maxpos=maxweight=0
    for v in obj.data.vertices:
        p=obj.matrix_world@v.co;w=weights(obj,v)
        candidates=[i for i,r in enumerate(src['rest']) if (r-p).length<1e-7]
        assert candidates,(obj.name,v.index,'rest position changed',list(p))
        def error(i):return max(abs(w.get(k,0)-src['weights'][i].get(k,0)) for k in w.keys()|src['weights'][i].keys())
        idx=min(candidates,key=error);we=error(idx);assert we<1e-6,(obj.name,v.index,'skin weights changed',we)
        maxpos=max(maxpos,(src['rest'][idx]-p).length);maxweight=max(maxweight,we);ids.append(idx)
    mapping[obj.name]=ids
    result['meshes'].append({'name':obj.name,'vertices':len(ids),'sourcePath':obj['sourcePath'],'maxRestPositionError':maxpos,'maxWeightError':maxweight})
    result['maxRestPositionError']=max(result['maxRestPositionError'],maxpos)
    result['maxWeightError']=max(result['maxWeightError'],maxweight)
for clip in clips:
    # Imported actions receive an armature suffix. Original actions from the
    # opened file remain as orphans; select the new glTF action slot by prefix.
    candidates=[a for a in import_actions if a.name==clip['name']+'.001']
    assert len(candidates)==1,(clip['name'],[a.name for a in candidates])
    action=candidates[0];rig.animation_data.action=action
    maximum=0
    duration=next(a['sourceDuration'] for a in report['animations'] if a['name']==clip['name'])
    for sample in range(5):
        scene.frame_set(round(duration*fps*sample/4))
        for obj in imported:
            actual=positions(obj);expected=cache[obj['sourcePath']]['poses'][(clip['name'],sample)]
            maximum=max(maximum,max((p-expected[i]).length for p,i in zip(actual,mapping[obj.name])))
    result['clips'][clip['name']]={'maxDeformedVertexError':maximum,'samples':5}
    result['maxDeformedVertexError']=max(result['maxDeformedVertexError'],maximum)
result['passed']=result['maxDeformedVertexError']<1e-5
(ROOT/'manifests/character-roundtrip-validation.json').write_text(json.dumps(result,indent=2))
assert result['passed'],result['clips']
print('CHARACTER_ROUNDTRIP_PASSED',json.dumps(result))
