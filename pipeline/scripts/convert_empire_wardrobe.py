"""D1 conversion adapter, isolated per original starting family."""
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
cl=sys.argv[sys.argv.index('--family')+1]
convert=(ROOT/'scripts/convert_character.py').read_text();materials=(ROOT/'scripts/character_materials.py').read_text();export=(ROOT/'scripts/character_export.py').read_text()
convert=convert.replace("ROOT/'character-recipe.json'",f"ROOT/'wardrobe-{cl}-recipe.json'")
convert=convert.replace("(ROOT/'manifests'/name)",f"(ROOT/'manifests'/('wardrobe-{cl}-'+name))")
materials=materials.replace("enumerate(['1000768','1000760'],1)","enumerate(next(p['paletteIds'] for p in recipe['parts'] if p['slot']==slot),1)")
materials=materials.replace("ROOT/'generated/character'",f"ROOT/'generated/wardrobe-{cl}'")
for name in ['sorcerer-probe.blend','sorcerer-reference.blend','sorcerer-portable.blend','sorcerer.glb']:
    convert=convert.replace(name,'wardrobe-'+cl+'-'+name);materials=materials.replace(name,'wardrobe-'+cl+'-'+name);export=export.replace(name,'wardrobe-'+cl+'-'+name)
materials=materials.replace("exec(compile((ROOT/'scripts/character_export.py').read_text(),str(ROOT/'scripts/character_export.py'),'exec'))","exec(compile(d3_export,'wardrobe-export','exec'))")
convert=convert.replace("exec(compile((ROOT/'scripts/character_materials.py').read_text(),str(ROOT/'scripts/character_materials.py'),'exec'))","exec(compile(d3_materials,'wardrobe-materials','exec'))")
# Retain original emissive sources in the receipt; the portable albedo bake has no glow channel.
materials=materials.replace("prop='rotationMap'", "if semantic=='EmissiveMap':continue\n            prop='rotationMap'")
globals().update(d3_export=export,d3_materials=materials)
exec(compile(convert,'wardrobe-convert','exec'))
