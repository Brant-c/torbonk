"""Reuse the proven original-weight / JBA converter with D4-only inputs and outputs."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/convert_b6.py').read_text()
code=code.replace("ROOT/'b6-recipe.json'", "ROOT/'d5-enemies-recipe.json'")
code=code.replace("ROOT/'godot/Assets/Enemies'", "ROOT/'godot/Assets/Maps/hutta/Enemies'")
code=code.replace("ROOT/'generated/b6'", "ROOT/'generated/d5-enemies'")
code=code.replace("ROOT/'manifests/b6-conversion.json'", "ROOT/'manifests/d5-enemies-conversion.json'")
(ROOT/'godot/Assets/Maps/hutta/Enemies').mkdir(parents=True,exist_ok=True)
exec(compile(code,str(ROOT/'scripts/convert_b6.py'),'exec'))

