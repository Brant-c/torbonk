import sys,subprocess,json
from source_pipeline import ROOT
code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/d3/weapons.xml'").replace('abilities_imperialmelee','weapons').replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
code=code.replace("names={s for fields in config.values() for k,v in fields for s in v.split(',') if s.startswith(('Play_','Stop_'))}","names=set()")
n={'__name__':'fixaudio'};exec(compile(code,'d3-weapon-audio','exec'),n)
n['main']('d3-weapon','D3',['play_weapon_melee_righthand_swing_normal','play_weapon_melee_lefthand_swing_normal','play_weapon_lightsaber_awesomeness_right','play_weapon_lightsaber_awesomeness_left'])
code=code.replace("ROOT/'work/d3/weapons.xml'","ROOT/'work/d3/ranged.xml'").replace('weapons_data.bnk','abilities_imperialranged_data.bnk').replace('weapons_media.bnk','abilities_imperialranged_media.bnk')
n={'__name__':'fixaudio'};exec(compile(code,'d3-fire-audio','exec'),n)
n['main']('d3-fire','D3',['Play_ability_spy_snipe_fire','Play_ability_spy_poisondart_fire','Play_ability_spy_poisondart_impact'])
# Keep the original merged event catalog and add corrected fire and weapon graphs.
base=json.loads((ROOT/'manifests/d3-ranged-audio.json').read_text())
for receipt in ['d3-fx-audio','d3-lacerate-audio','d3-weapon-audio','d3-fire-audio']:
    other=json.loads((ROOT/f'manifests/{receipt}.json').read_text())
    for k in ['events','graphs']:base[k].update(other[k])
    base['media']+=other['media']
(ROOT/'godot/Assets/D3/audio.json').write_text(json.dumps(base,indent=2))
