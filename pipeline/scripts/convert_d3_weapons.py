from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/convert_b6.py').read_text().replace("ROOT/'b6-recipe.json'","ROOT/'d3-weapons-recipe.json'").replace("ROOT/'godot/Assets/Enemies'","ROOT/'godot/Assets/D3'").replace("ROOT/'generated/b6'","ROOT/'generated/d3-weapons'").replace("ROOT/'manifests/b6-conversion.json'","ROOT/'manifests/d3-weapons-conversion.json'")
exec(compile(code,'d3-weapons','exec'))
