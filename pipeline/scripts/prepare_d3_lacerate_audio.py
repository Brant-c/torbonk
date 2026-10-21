import sys,subprocess,json
from source_pipeline import ROOT,SOURCE
import character_source
character_source.MANIFEST=ROOT/'manifests/d3-lacerate-source.json';src=character_source.SelectedSource()
p='/resources/bnk2/abilities_republicmelee_data.bnk'
if not src.has(p):raise RuntimeError('April Republic melee bank unavailable')
src.get(p,'Lacerate original appearance audio','original source-named event bank')
subprocess.run([sys.executable,str(ROOT/'tools/wwiser.pyz'),str(SOURCE/p.lstrip('/')),'-d','xml','-dn',str(ROOT/'work/d3/republic')],check=True,stdout=subprocess.DEVNULL)
code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/d3/republic.xml'").replace('abilities_imperialmelee','abilities_republicmelee').replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
n={'__name__':'d3_audio'};exec(compile(code,'d3-lacerate-audio','exec'),n)
n['main']('d3-lacerate','D3',['Play_ability_jediconsular_hiddenstrike_mediumgrip'])
combined=json.loads((ROOT/'manifests/d3-ranged-audio.json').read_text());melee=json.loads((ROOT/'manifests/d3-fx-audio.json').read_text());lacerate=json.loads((ROOT/'manifests/d3-lacerate-audio.json').read_text())
for other in [melee,lacerate]:
    for k in ['events','graphs']:combined[k].update(other[k])
    combined['media']+=other['media']
(ROOT/'godot/Assets/D3/audio.json').write_text(json.dumps(combined,indent=2))
