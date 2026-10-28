"""Reuse the established rig and JBA conversion for an additive pole run library."""
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
code = (ROOT / 'scripts/convert_warrior_locomotion.py').read_text().replace('warrior-locomotion', 'assassin-locomotion').replace('WarriorLocomotion', 'AssassinLocomotion')
exec(compile(code, str(ROOT / 'scripts/convert_warrior_locomotion.py'), 'exec'))
