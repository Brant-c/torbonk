"""DDS lossless pixel decode using Pillow (Blender's DDS loader rejects this April tip)."""
from PIL import Image
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
for stem in ['dynamic_saber_core_white','dynamic_saber_glow_white','dynamic_saber_tip_core_white']:
 path=ROOT/f'source-assets/april2012/resources/art/fx/texture/weapon_melee/{stem}.dds'
 if not path.exists():continue
 image=Image.open(path);image.save(ROOT/'godot/Assets/Enemies/saber-tip.png');print(stem,image.size)
