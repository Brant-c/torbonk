"""Use the established weighted-rig/JBA converter with isolated raid outputs."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/convert_b6.py').read_text()
for a,b in [("ROOT/'b6-recipe.json'","ROOT/'eternity-enemies-recipe.json'"),("ROOT/'godot/Assets/Enemies'","ROOT/'godot/Assets/Maps/eternity_vault/Enemies'"),("ROOT/'generated/b6'","ROOT/'generated/eternity-enemies'"),("ROOT/'manifests/b6-conversion.json'","ROOT/'manifests/eternity-enemies-conversion.json'")]:code=code.replace(a,b)
(ROOT/'godot/Assets/Maps/eternity_vault/Enemies').mkdir(parents=True,exist_ok=True)
exec(compile(code,str(ROOT/'scripts/convert_b6.py'),'exec'))
