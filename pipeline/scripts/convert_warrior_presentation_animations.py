"""Bounded additive recovery of Warrior semantic attack clips on the existing rig.

Ravage's recovered appearance requests SaberSpecial3Attack6; Vicious Slash requests
SaberSpecial1Attack1. Preserve the single/dual weapon variant and original JBA timing.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
code = (ROOT / 'scripts/convert_warrior_animations.py').read_text()
old = "clips={k:v for k,v in clips.items() if v in direct or v in ('cb_saber_attack_right_1','cb_2saber_attack_right_1')}"
assert old in code
code = code.replace(old, "clips={'SaberRavage':'cb_saber_sp3_attack_right_6','DualViciousSlash':'cb_2saber_sp1_attack_right_1'}")
code = code.replace('godot/Assets/WarriorLibrary/casts.glb', 'godot/Assets/WarriorPresentation/casts.glb')
code = code.replace("ROOT/f'manifests/{profile}-animation-conversion.json'", "ROOT/'manifests/warrior-presentation-animation-conversion.json'")
(ROOT / 'godot/Assets/WarriorPresentation').mkdir(exist_ok=True)
exec(compile(code, str(ROOT / 'scripts/convert_warrior_animations.py'), 'exec'))
