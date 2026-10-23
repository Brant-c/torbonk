from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/convert_b6.py').read_text().replace("ROOT/'b6-recipe.json'","ROOT/'head-weapons-recipe.json'").replace("ROOT/'godot/Assets/Enemies'","ROOT/'godot/Assets/Equipment/Arsenal'").replace("ROOT/'generated/b6'","ROOT/'generated/arsenal'").replace("ROOT/'manifests/b6-conversion.json'","ROOT/'manifests/head-weapons-conversion.json'")
exec(compile(code,'arsenal','exec'))
