from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/convert_ability_animations.py').read_text().replace("profile='d2' if", "profile='d3' if '--d3' in sys.argv else 'd2' if")
code=code.replace("ROOT/('godot/Assets/D2/casts.glb'", "ROOT/('godot/Assets/D3/casts.glb' if profile=='d3' else 'godot/Assets/D2/casts.glb'")
exec(compile(code,'d3-animations','exec'))
