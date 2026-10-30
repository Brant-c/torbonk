"""Bake garment diffuse color through the maintained SWTOR shader and source palettes.

Executed in the established Blender converter's globals (bpy, TEX, image helpers).
"""
import xml.etree.ElementTree as ET
_palette_bakes={}
def palette_albedo(info,base,stem):
    if info['derived']!='Garment' or not info.get('palettes'):return base
    key=info['path']+json.dumps(info['palettes'],sort_keys=True)
    if key in _palette_bakes:return _palette_bakes[key]
    material_=bpy.data.materials.new(stem+'_source_palette');material_.use_nodes=True
    nodes_=material_.node_tree.nodes;nodes_.clear()
    area_=next(a for a in bpy.context.screen.areas if a.type=='VIEW_3D')
    with bpy.context.temp_override(area=area_):hero_=nodes_.new('ShaderNodeHeroEngine');hero_.derived='GARMENT'
    output_=nodes_.new('ShaderNodeOutputMaterial');material_.node_tree.links.new(hero_.outputs[0],output_.inputs['Surface'])
    for semantic_,path_ in info['textures'].items():
        prop_='rotationMap' if semantic_=='RotationMap1' else semantic_[0].lower()+semantic_[1:]
        if hasattr(hero_,prop_):setattr(hero_,prop_,bpy.data.images.load(local(path_),check_existing=True))
    for i_,xml_ in info['palettes'].items():
        palette_=ET.fromstring(xml_)
        for name_,tag_ in [('hue','Hue'),('saturation','Saturation'),('brightness','Brightness'),('contrast','Contrast')]:
            setattr(hero_,f'palette{i_}_{name_}',float(palette_.findtext(tag_)))
    tree_=hero_.node_tree.copy();hero_.node_tree=tree_
    emission_=tree_.nodes.new('ShaderNodeEmission')
    tree_.links.new(tree_.nodes['HuePixel'].outputs['Diffuse Color'],emission_.inputs['Color'])
    groupout_=next(n for n in tree_.nodes if n.type=='GROUP_OUTPUT');tree_.links.new(emission_.outputs[0],groupout_.inputs[0])
    bpy.ops.mesh.primitive_plane_add(size=1);plane_=bpy.context.object
    plane_.data.materials.append(material_)
    diff_=hero_.diffuseMap
    baked_=bpy.data.images.new(stem+'_palette_bake',width=diff_.size[0],height=diff_.size[1],alpha=True,float_buffer=True)
    baked_.colorspace_settings.name='sRGB'
    target_=nodes_.new('ShaderNodeTexImage');target_.image=baked_;nodes_.active=target_
    bpy.ops.object.select_all(action='DESELECT');plane_.select_set(True);bpy.context.view_layer.objects.active=plane_
    bpy.context.scene.render.engine='CYCLES';bpy.context.scene.cycles.samples=1
    bpy.context.scene.render.bake.margin=0
    bpy.ops.object.bake(type='EMIT')
    values_=np.empty(len(baked_.pixels),np.float32);baked_.pixels.foreach_get(values_)
    rgb_=srgb(np.maximum(values_.reshape(baked_.size[1],baked_.size[0],4)[:,:,:3],0))
    bpy.data.objects.remove(plane_,do_unlink=True);bpy.data.materials.remove(material_);bpy.data.images.remove(baked_)
    _palette_bakes[key]=rgb_
    return rgb_
