from pathlib import Path
root=Path('pipeline');code=(root/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/d3/ranged.xml'").replace('abilities_imperialmelee','abilities_imperialranged').replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
n={'__name__':'powertechaudio'};exec(compile(code,'powertech-audio','exec'),n);n['main']('powertech','Powertech',['Play_ability_bountyhunter_carbonize_chargeup'])
