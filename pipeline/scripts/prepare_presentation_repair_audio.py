"""Recover exact April HIRC/DIDX media for repaired presentations."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
original=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
extras=['Play_ability_bountyhunter_flamethrower_start','Play_ability_bountyhunter_flamethrower_loop','Play_ability_bountyhunter_flamethrower_end',
        'Play_ability_sithwarrior_forcescream_cast','Play_ability_spy_seriesofshots_fire','Play_ability_spy_unload_fire']
merged={'events':{},'graphs':{},'media':[]}
for suffix,xml,bank in [('melee','work/lightning/bank.xml','abilities_imperialmelee'),('ranged','work/d3/ranged.xml','abilities_imperialranged')]:
 code=original.replace("ROOT/'work/lightning/bank.xml'",'ROOT/'+repr(xml)).replace('abilities_imperialmelee',bank)
 namespace={'__name__':'repair_audio'};exec(compile(code,__file__,'exec'),namespace)
 profile='presentation-repairs-'+suffix
 namespace['main'](profile,'PresentationRepairs',extras)
 result=json.loads((ROOT/f'manifests/{profile}-audio.json').read_text())
 for n,i in result['events'].items():
  if result['graphs'].get(i,{}).get('media'):merged['events'][n]=i;merged['graphs'][i]=result['graphs'][i]
 merged['media']+=result['media']
(ROOT/'godot/Assets/PresentationRepairs/audio.json').write_text(json.dumps(merged,indent=2))
print({n:merged['graphs'][i]['media'] for n,i in merged['events'].items()})
