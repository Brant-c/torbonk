"""Use the established weighted GR2/JBA converter with isolated Soa outputs."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/convert_b6.py').read_text()
for a,b in [("ROOT/'b6-recipe.json'","ROOT/'soa-enemies-recipe.json'"),("ROOT/'godot/Assets/Enemies'","ROOT/'godot/Assets/Maps/soa_p3/Enemies'"),("ROOT/'generated/b6'","ROOT/'generated/soa-enemies'"),("ROOT/'manifests/b6-conversion.json'","ROOT/'manifests/soa-enemies-conversion.json'")]:code=code.replace(a,b)
(ROOT/'godot/Assets/Maps/soa_p3/Enemies').mkdir(parents=True,exist_ok=True)
(ROOT/'generated/soa-enemies').mkdir(parents=True,exist_ok=True)
cloth='''
 # Original cloth bind bones, without a cloth simulation.
 import xml.etree.ElementTree as ET
 from mathutils import Matrix,Quaternion
 originals={b.name:b.matrix_local.copy() for b in rig.data.bones}
 bpy.ops.object.mode_set(mode='EDIT')
 for name_,matrix_ in originals.items():
  bone=rig.data.edit_bones[name_];bone.length=.005;bone.matrix=matrix_
 axis=Matrix.Rotation(math.pi/2,4,'X')
 for path_ in sorted({p for part_ in info['parts'] for p in part_.get('cloth',[])}):
  for entry_ in ET.parse(local(path_)).findall('Bones/Bone'):
   n_=entry_.get('name')
   if n_ in rig.data.edit_bones:continue
   parent_=entry_.findtext('parent');assert parent_ in rig.data.edit_bones,(n_,parent_)
   t_=[float(v) for v in entry_.findtext('m_rootToBoneTrans').split(',')]
   q_=[float(v) for v in entry_.findtext('m_rootToBoneRot').split(',')]
   inverse_=Matrix.Translation(Vector(t_))@Quaternion((q_[3],*q_[:3])).to_matrix().to_4x4()
   bone=rig.data.edit_bones.new(n_);bone.parent=rig.data.edit_bones[parent_];bone.length=.005;bone.matrix=axis@inverse_.inverted()
 bpy.ops.object.mode_set(mode='OBJECT')
'''
code=code.replace(' cache={};rig.animation_data_clear()',cloth+' cache={};rig.animation_data_clear()')
# Registry -1 overrides are eye material bindings. Explicit positive indices stay exact.
code=code.replace("override=next((a['material'] for a in part['overrides'] if a['index']==idx),part['material'])", "override=next((a['material'] for a in part['overrides'] if a['index']==idx or a['index']==-1 and 'eye' in o.data.materials[idx].name.lower()),part['material'])")
code=code.replace("cache={}\ndef material(info):",(ROOT/'scripts/soa_palette_bake.py').read_text()+"\ncache={}\ndef material(info):")
code=code.replace("link(image(stem+'_base',base,'sRGB'),'Base Color')","base=palette_albedo(info,base,stem)\n link(image(stem+'_base',base,'sRGB'),'Base Color')")
code=code.replace(" meshes=[]", "\n for p_ in info['parts']:\n  p_['material']['palettes']=p_.get('palettes',{})\n meshes=[]")
exec(compile(code,str(ROOT/'scripts/convert_b6.py'),'exec'))
