from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/d3/ranged.xml'").replace('abilities_imperialmelee','abilities_imperialranged').replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
n={'__name__':'agent_audio'};exec(compile(code,__file__,'exec'),n)
n['main']('agent','Agent',['Play_ability_spy_backstab_impact','Play_ability_spy_snipe_fire'])
