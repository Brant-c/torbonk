import bpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.mesh.primitive_plane_add()
mat=bpy.data.materials.new('constant');mat.use_nodes=True
nodes=mat.node_tree.nodes;nodes.clear()
e=nodes.new('ShaderNodeEmission');e.inputs['Color'].default_value=(.214041,.214041,.214041,1)
o=nodes.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(e.outputs[0],o.inputs[0])
bpy.context.object.data.materials.append(mat)
bpy.context.scene.render.engine='CYCLES';bpy.context.scene.cycles.samples=1
target=bpy.data.images.new('bake',width=4,height=4,float_buffer=True)
target.colorspace_settings.name='sRGB'
n=nodes.new('ShaderNodeTexImage');n.image=target;nodes.active=n
bpy.ops.object.bake(type='EMIT')
print('BAKED_PIXEL',list(target.pixels[:4]))
assert abs(target.pixels[0]-.5)<.002, 'Reproduce the original sRGB bake encoding'
target.colorspace_settings.name='Non-Color'
bpy.ops.object.bake(type='EMIT')
assert abs(target.pixels[0]-.214041)<.002, 'Linear bake must preserve the shader value'
encoded=1.055*target.pixels[0]**(1/2.4)-.055
assert abs(encoded-.5)<.002, 'Exactly one sRGB encoding must recover the expected output'
print('FACE_COLOR_REGRESSION_PASSED')
for floating in [False,True]:
    img=bpy.data.images.new('probe',width=1,height=1,float_buffer=floating)
    img.colorspace_settings.name='sRGB'
    img.pixels[:]=[.214041,.214041,.214041,1]
    img.filepath_raw=str(ROOT/f'work/color-{floating}.png');img.file_format='PNG';img.save()
    img2=bpy.data.images.load(img.filepath_raw,check_existing=False)
    print('COLOR_PROBE',floating,list(img2.pixels[:]))
