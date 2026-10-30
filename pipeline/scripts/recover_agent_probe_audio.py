"""Recover the exact Explosive Probe events from the April ranged bank.

Retains the established HIRC/DIDX extraction and separate repair provenance.
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/d3/ranged.xml'").replace('abilities_imperialmelee','abilities_imperialranged').replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
n={'__name__':'agent_probe_audio'};exec(compile(code,__file__,'exec'),n)
n['main']('agent-probe-repair','Agent',[
 'Play_ability_spy_explosiveprobe_explode',
 'Play_ability_spy_explosiveprobe_droidscan',
 'Play_ability_spy_explosiveprobe_droidpulsetriple',
 'Play_ability_spy_explosiveprobe_flyaway'])
