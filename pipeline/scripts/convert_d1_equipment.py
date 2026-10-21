"""Reuse maintained character conversion without replacing verified character outputs."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
convert=(ROOT/'scripts/convert_character.py').read_text()
materials=(ROOT/'scripts/character_materials.py').read_text()
export=(ROOT/'scripts/character_export.py').read_text()
convert=convert.replace("ROOT/'character-recipe.json'","ROOT/'d1-character-recipe.json'")
convert=convert.replace("(ROOT/'manifests'/name)","(ROOT/'manifests'/('d1-'+name))")
materials=materials.replace("enumerate(['1000768','1000760'],1)","enumerate(next(p['paletteIds'] for p in recipe['parts'] if p['slot']==slot),1)")
materials=materials.replace("ROOT/'generated/character'","ROOT/'generated/d1-character'")
for name in ['sorcerer-probe.blend','sorcerer-reference.blend','sorcerer-portable.blend','sorcerer.glb']:
    convert=convert.replace(name,'d1-'+name);materials=materials.replace(name,'d1-'+name);export=export.replace(name,'d1-'+name)
materials=materials.replace("exec(compile((ROOT/'scripts/character_export.py').read_text(),str(ROOT/'scripts/character_export.py'),'exec'))","exec(compile(d1_export,'d1-export','exec'))")
convert=convert.replace("exec(compile((ROOT/'scripts/character_materials.py').read_text(),str(ROOT/'scripts/character_materials.py'),'exec'))","exec(compile(d1_materials,'d1-materials','exec'))")
globals().update(d1_export=export,d1_materials=materials)
exec(compile(convert,str(ROOT/'scripts/convert_character.py'),'exec'))
