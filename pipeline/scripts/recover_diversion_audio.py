"""Add Diversion's exact April Wwise events (sp_diversion_* FXSpec sound lists) to the Agent audio manifest.

Re-runs the established HIRC/DIDX extraction with the already-recovered Agent events kept, so audio.json stays additive.
"""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/d3/ranged.xml'").replace('abilities_imperialmelee','abilities_imperialranged').replace('##(\d+)\.wem','[?#](\d+)\.wem')
existing=list(json.loads((ROOT/'godot/Assets/Agent/audio.json').read_text())['events'])
n={'__name__':'diversion_audio'};exec(compile(code,__file__,'exec'),n)
n['main']('agent-diversion','Agent',existing+[
 'Play_ability_agent_escape_prep','Play_ability_agent_escape_blast','Play_ability_agent_escape_smoke',
 'Play_ability_agent_escape_zappyzappy','Stop_ability_agent_escape_zappyzappy',
 'Play_ability_agent_diversion_targetloop','Stop_ability_agent_diversion_targetloop'])
