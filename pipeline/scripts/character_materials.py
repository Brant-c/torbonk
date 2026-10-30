"""Invoked by convert_character.py: original shader reference + portable bake.

Albedo is evaluated from the maintained shader's diffuse-color graph on a UV
plane, with original palette values. Specular and lighting remain approximated.
"""
area=next(a for a in bpy.context.screen.areas if a.type=='VIEW_3D')
source_mats={}
def palette_values(info):
    if 'xml' in info:
        tree=ET.fromstring(info['xml']); return {
            'Hue':float(tree.findtext('Hue') or 0),'Saturation':float(tree.findtext('Saturation') or .5),
            'Brightness':float(tree.findtext('Brightness') or 0),'Contrast':float(tree.findtext('Contrast') or 1),
            'Specular':[float(v) for v in tree.findtext('Specular').split(',')][:3],
            'MetallicSpecular':[float(v) for v in tree.findtext('Metallicspecular').split(',')][:3]}
    return {k:info['values'].get('appPalette'+k,default) for k,default in
        [('Hue',0),('Saturation',.5),('Brightness',0),('Contrast',1),('Specular',[1,1,1]),('MetallicSpecular',[1,1,1])]}
def set_palette(hero,idx,values):
    for suffix,key in [('hue','Hue'),('saturation','Saturation'),('brightness','Brightness'),('contrast','Contrast'),
                       ('specular','Specular'),('metallic_specular','MetallicSpecular')]:
        prop=f'palette{idx}_{suffix}'
        if hasattr(hero,prop):
            val=values[key]
            if isinstance(val,list):val=val[:3]+[1]
            setattr(hero,prop,val)
def original_material(info,slot):
    key=info['path']
    if key in source_mats:return source_mats[key]
    mat=bpy.data.materials.new(Path(key).stem); mat.use_nodes=True
    nodes=mat.node_tree.nodes; nodes.clear()
    out=nodes.new('ShaderNodeOutputMaterial')
    with bpy.context.temp_override(area=area):
        hero=nodes.new('ShaderNodeHeroEngine'); hero.derived=info['derived'].upper()
        mat.node_tree.links.new(hero.outputs[0],out.inputs['Surface'])
        xml=ET.parse(local(key)).getroot(); alpha=xml.findtext('AlphaMode')
        hero.alpha_mode={'None':'OPAQUE','Test':'CLIP','Full':'BLEND'}.get(alpha,'OPAQUE')
        hero.alpha_test_value=float(xml.findtext('AlphaTestValue') or .5)
        mat.use_backface_culling=xml.findtext('IsTwoSided')!='True'
        for semantic,path in info['textures'].items():
            if semantic=='GuildInsigniaMap':
                assert path=='/resources/art/defaultassets/black.dds'
                continue # original blank insignia; maintained shader has no insignia input
            prop='rotationMap' if semantic=='RotationMap1' else semantic[0].lower()+semantic[1:]
            assert hasattr(hero,prop),(key,semantic)
            setattr(hero,prop,bpy.data.images.load(local(path),check_existing=True))
        for i in [1,2]:
            raw=info['values'].get('palette'+str(i),'0,0.5,0,1')
            p=[float(v) for v in raw.split(',')]
            set_palette(hero,i,dict(zip(['Hue','Saturation','Brightness','Contrast'],p),
                Specular=[float(v) for v in info['values'].get(f'palette{i}Specular','1,1,1').split(',')][:3],
                MetallicSpecular=[float(v) for v in info['values'].get(f'palette{i}MetallicSpecular','1,1,1').split(',')][:3]))
        if slot in ['chest','leg','hand','boot','waist']:
            for i,hue in enumerate(['1000768','1000760'],1):set_palette(hero,i,palette_values(recipe['colorScheme']['hues'][hue]))
        elif slot in ['head','hair','eye']:
            c={'head':'skincolor','hair':'haircolor','eye':'eyecolor'}[slot]
            set_palette(hero,1,palette_values(recipe['customization'][c]))
        if slot=='head':hero.complexionMap=bpy.data.images.load(local(recipe['customization']['complexion']['path']),check_existing=True)
        for semantic,prop in [('FlushTone','flush_tone'),('FleshBrightness','flesh_brightness')]:
            if semantic in info['values'] and hasattr(hero,prop):
                raw=info['values'][semantic]; setattr(hero,prop,[float(v) for v in raw.split(',')][:3]+[1] if ',' in raw else float(raw))
    source_mats[key]=(mat,hero,info)
    report['materials'].append({'path':key,'slot':slot,'derived':info['derived'],'alphaMode':alpha,'textures':info['textures']})
    return source_mats[key]
for obj,part in meshes:
    mat,_,_=original_material(part['material'],part['slot'])
    for i in range(len(obj.data.materials)):obj.data.materials[i]=mat
    if part['slot']=='head':
        assert len(obj.data.materials)==2
        obj.data.materials[1]=original_material(part['overrides'][0]['material'],'eye')[0]

def setup_studio():
    scene.render.engine='CYCLES'; scene.cycles.samples=16; scene.cycles.use_denoising=True
    scene.view_settings.view_transform='Standard'
    scene.world.use_nodes=True; scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.12,.12,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
    cam=bpy.data.objects.new('CharacterInspectionCamera',bpy.data.cameras.new('CharacterInspectionCamera'))
    scene.collection.objects.link(cam); cam.location=(.23,-.34,.18)
    target=Vector((0,0,.095)); cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=.24;scene.camera=cam
    for name,pos,power in [('Key',(.15,-.25,.3),8),('Fill',(-.2,-.1,.15),3),('Rim',(.1,.2,.25),6)]:
        d=bpy.data.lights.new(name,'AREA'); d.energy=power;d.size=.2
        o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos
        o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
setup_studio()
rig.animation_data.action=actions[0];scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'work/sorcerer-reference.blend'))

outdir=ROOT/'generated/character';outdir.mkdir(exist_ok=True)
portable={}
def image_array(img):
    p=np.empty(len(img.pixels),np.float32);img.pixels.foreach_get(p);return p.reshape(img.size[1],img.size[0],4)
def save_image(name,rgba,cs):
    h,w=rgba.shape[:2];img=bpy.data.images.new(name,width=w,height=h,alpha=True)
    img.colorspace_settings.name=cs;img.alpha_mode='CHANNEL_PACKED';img.pixels.foreach_set(rgba.ravel())
    img.filepath_raw=str(outdir/(name+'.png'));img.file_format='PNG';img.save();return img
def rgba(rgb):
    a=np.ones((*rgb.shape[:2],4),np.float32);a[:,:,:3]=rgb;return a

# UV plane covers the entire source atlas, avoiding per-part bake holes.
bpy.ops.mesh.primitive_plane_add(size=1)
plane=bpy.context.object;plane.name='TemporaryUVBakePlane'
for key,(mat,hero,info) in source_mats.items():
    bake=mat.copy();bh=next(n for n in bake.node_tree.nodes if n.bl_idname=='ShaderNodeHeroEngine')
    bh.node_tree=hero.node_tree.copy();tree=bh.node_tree
    # The final diffuse-BSDF input contains view-dependent specular and flush
    # lighting. Bake the actual palette color before those lighting operations.
    if info['derived']=='SkinB':
        # Follow the diffuse composition into the specular-add node rather than
        # depending on insertion order of unrelated legacy Mix nodes.
        skin_add=next(n for n in tree.nodes if n.type=='VECT_MATH' and n.operation=='ADD'
                      and any(l.from_node.type=='MIX_RGB' for l in n.inputs[0].links))
        color=skin_add.inputs[0].links[0].from_socket
    else:
        color=tree.nodes['HuePixel'].outputs['Diffuse Color']
    emit=tree.nodes.new('ShaderNodeEmission');tree.links.new(color,emit.inputs['Color'])
    output=next(n for n in tree.nodes if n.type=='GROUP_OUTPUT');tree.links.new(emit.outputs[0],output.inputs[0])
    diff=hero.diffuseMap;w,h=diff.size
    baked=bpy.data.images.new(Path(key).stem+'_albedo',width=w,height=h,alpha=True,float_buffer=True)
    # Cycles encodes bake pixels into the target image's color space, even for
    # float buffers. Keep this linear: the explicit sRGB encoding below must
    # happen exactly once or the face (and other albedo maps) is washed out.
    baked.colorspace_settings.name='Non-Color'
    target=bake.node_tree.nodes.new('ShaderNodeTexImage');target.image=baked;bake.node_tree.nodes.active=target
    plane.data.materials.clear();plane.data.materials.append(bake)
    bpy.ops.object.select_all(action='DESELECT');plane.select_set(True);bpy.context.view_layer.objects.active=plane
    scene.render.bake.use_clear=True;scene.render.bake.margin=0
    bpy.ops.object.bake(type='EMIT')
    pixels=image_array(baked);linear=np.maximum(pixels[:,:,:3],0)
    srgb=np.where(linear<=.0031308,linear*12.92,1.055*linear**(1/2.4)-.055)
    rot=image_array(hero.rotationMap)
    albedo=rgba(srgb)
    # Albedo and rotation atlases have equal sizes for these selected materials.
    assert rot.shape[:2]==albedo.shape[:2]
    # Slicers mixes the transparent shader at factor R: R is transparency.
    if hero.alpha_mode!='OPAQUE':albedo[:,:,3]=1-rot[:,:,0]
    stem=Path(key).stem;albedoImg=save_image(stem+'_basecolor',albedo,'sRGB')
    nx=rot[:,:,3]*2-1;ny=1-2*rot[:,:,1];nz=np.sqrt(np.maximum(0,1-nx*nx-ny*ny))
    normal=np.stack([nx,ny,nz],axis=-1);normal/=np.maximum(np.linalg.norm(normal,axis=-1,keepdims=True),1e-8)
    normalImg=save_image(stem+'_normal',rgba((normal+1)*.5),'Non-Color')
    gloss=image_array(hero.glossMap);rough=(2/(3+63*gloss[:,:,3]))**.25
    roughImg=save_image(stem+'_roughness',rgba(np.repeat(rough[:,:,None],3,axis=2)),'Non-Color')
    pm=bpy.data.materials.new(stem+'_portable');pm.use_nodes=True;pm.use_backface_culling=mat.use_backface_culling
    bsdf=pm.node_tree.nodes.get('Principled BSDF');links=pm.node_tree.links;nodes=pm.node_tree.nodes
    node=nodes.new('ShaderNodeTexImage');node.image=albedoImg;links.new(node.outputs['Color'],bsdf.inputs['Base Color'])
    if hero.alpha_mode!='OPAQUE':
        links.new(node.outputs['Alpha'],bsdf.inputs['Alpha']);pm.surface_render_method='DITHERED'
        pm.alpha_threshold=hero.alpha_test_value
    node=nodes.new('ShaderNodeTexImage');node.image=normalImg
    n=nodes.new('ShaderNodeNormalMap');links.new(node.outputs['Color'],n.inputs['Color']);links.new(n.outputs[0],bsdf.inputs['Normal'])
    node=nodes.new('ShaderNodeTexImage');node.image=roughImg;links.new(node.outputs['Color'],bsdf.inputs['Roughness'])
    bsdf.inputs['Specular IOR Level'].default_value=.35
    portable[mat]=pm
    bpy.data.materials.remove(bake);bpy.data.images.remove(baked)
bpy.data.objects.remove(plane,do_unlink=True)
for obj,part in meshes:
    for slot in obj.material_slots:slot.material=portable[slot.material]
rig.animation_data.action=actions[0];scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'work/sorcerer-portable.blend'))
write('character-conversion.json',report)
exec(compile((ROOT/'scripts/character_export.py').read_text(),str(ROOT/'scripts/character_export.py'),'exec'))
