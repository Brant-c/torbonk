"""Use the established JBA/rig converter for an additive locomotion library."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
code = (ROOT / 'scripts/convert_ability_animations.py').read_text()
old = "clips=json.loads((ROOT/f'manifests/{profile}-conversion.json').read_text())['clips']"
assert old in code
code = code.replace(old, "clips=json.loads((ROOT/'manifests/warrior-locomotion-conversion.json').read_text())['clips']")
start = code.index('output=ROOT/(')
end = code.index('\n', start)
code = code[:start] + "output=ROOT/'godot/Assets/WarriorLocomotion/casts.glb'" + code[end:]
code = code.replace("ROOT/f'manifests/{profile}-animation-conversion.json'", "ROOT/'manifests/warrior-locomotion-animation-conversion.json'")
(ROOT / 'godot/Assets/WarriorLocomotion').mkdir(exist_ok=True)
exec(compile(code, str(ROOT / 'scripts/convert_ability_animations.py'), 'exec'))
