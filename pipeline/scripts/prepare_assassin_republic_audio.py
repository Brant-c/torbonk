import json,re,sys,subprocess
from pathlib import Path
sys.path.insert(0,str(Path('pipeline/scripts').resolve()))
import character_source
from source_pipeline import ROOT,SOURCE
character_source.MANIFEST=ROOT/'manifests/assassin-refinement-source.json';src=character_source.SelectedSource()
data=src.get('/resources/bnk2/abilities_republicmelee_data.bnk','Low Slash/Voltaic Slash','April shared Consular EPP audio graph')
work=ROOT/'work/assassin-refinement';work.mkdir(exist_ok=True)
subprocess.run([sys.executable,str(ROOT/'tools/wwiser.pyz'),str(SOURCE/'resources/bnk2/abilities_republicmelee_data.bnk'),'-d','xml','-dn',str(work/'republic-bank')],check=True,stdout=subprocess.DEVNULL)
asset=ROOT/'work/assassin-refinement/republic-assets';asset.mkdir(exist_ok=True);(asset/'source-parameters.json').write_bytes((ROOT/'godot/Assets/Assassin/source-parameters.json').read_bytes())
code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/assassin-refinement/republic-bank.xml'").replace('abilities_imperialmelee','abilities_republicmelee').replace("str(SOURCE/'resources/bnk2/abilities_republicmelee_media.bnk'),",'')
code=code.replace("ROOT/f'godot/Assets/{asset_dir}/source-parameters.json'","ROOT/'work/assassin-refinement/republic-assets/source-parameters.json'").replace("ROOT/f'godot/Assets/{asset_dir}/Audio'","ROOT/'work/assassin-refinement/republic-assets/Audio'")
ns={'__name__':'assassin_audio_helper'};exec(compile(code,'prepare_assassin_audio_helper','exec'),ns);ns['main']('assassin-republic','AssassinRepublic',[])
a=json.loads((ROOT/'godot/Assets/Assassin/audio.json').read_text());b=json.loads((asset/'audio.json').read_text())
a['graphs'].update(b['graphs']);a['media']+=b['media'];a['missingEvents']=[n for n in a['missingEvents'] if b['events'][n] not in b['graphs']]
for f in (asset/'Audio').glob('*.wav*'):(ROOT/'godot/Assets/Assassin/Audio'/f.name).write_bytes(f.read_bytes())
(ROOT/'godot/Assets/Assassin/audio.json').write_text(json.dumps(a,indent=2));(ROOT/'manifests/assassin-refinement-audio.json').write_text(json.dumps(a,indent=2));print('Remaining missing',a['missingEvents'])
