import sys,subprocess,json
from pathlib import Path
from source_pipeline import ROOT,SOURCE
p=SOURCE/'resources/bnk2/abilities_imperialranged_data.bnk'
subprocess.run([sys.executable,str(ROOT/'tools/wwiser.pyz'),str(p),'-d','xml','-dn',str(ROOT/'work/d3/ranged')],check=True,stdout=subprocess.DEVNULL)
code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/d3/ranged.xml'").replace('abilities_imperialmelee','abilities_imperialranged')
code=code.replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
namespace={'__name__':'d3_audio'};exec(compile(code,'d3-ranged-audio','exec'),namespace)
namespace['main']('d3-ranged','D3',['Play_ability_spy_snipe_activate','Play_ability_spy_painsupressiondart_prep'])
melee=json.loads((ROOT/'manifests/d3-fx-audio.json').read_text());ranged=json.loads((ROOT/'manifests/d3-ranged-audio.json').read_text())
for k in ['events','graphs']:ranged[k].update(melee[k])
ranged['media']+=melee['media'];(ROOT/'godot/Assets/D3/audio.json').write_text(json.dumps(ranged,indent=2))

