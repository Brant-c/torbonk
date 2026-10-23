"""Reuse the proven original-weight / JBA converter with D4-only inputs and outputs."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/convert_b6.py').read_text()
code=code.replace("ROOT/'b6-recipe.json'", "ROOT/'d4-enemies-recipe.json'")
code=code.replace("ROOT/'godot/Assets/Enemies'", "ROOT/'godot/Assets/Korriban/D4/Enemies'")
code=code.replace("ROOT/'generated/b6'", "ROOT/'generated/d4-enemies'")
code=code.replace("ROOT/'manifests/b6-conversion.json'", "ROOT/'manifests/d4-enemies-conversion.json'")
(ROOT/'godot/Assets/Korriban/D4/Enemies').mkdir(parents=True,exist_ok=True)
exec(compile(code,str(ROOT/'scripts/convert_b6.py'),'exec'))
