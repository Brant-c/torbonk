"""Resolve remaining events through the already recovered April Republic melee bank."""
import json
from source_pipeline import ROOT
out=ROOT/'godot/Assets/WarriorLibrary'
a=json.loads((out/'audio.json').read_text())
code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/assassin-refinement/republic-bank.xml'").replace('abilities_imperialmelee','abilities_republicmelee').replace("str(SOURCE/'resources/bnk2/abilities_republicmelee_media.bnk'),",'')
code=code.replace("names.update(extra_names if extra_names is not None else", "names.intersection_update(extra_names or []); names.update(extra_names if extra_names is not None else")
namespace={'__name__':'warrior_shared_audio'};exec(compile(code,'warrior_shared_audio','exec'),namespace)
namespace['main']('warrior-library-republic','WarriorLibrary',a['missingEvents'])
b=json.loads((out/'audio.json').read_text())
a['graphs'].update(b['graphs']);a['media']+=b['media'];a['missingEvents']=[n for n in a['missingEvents'] if b['events'][n] not in b['graphs']]
a['additionalBank']='April Republic melee data graph; exact source-bank/media offsets retained'
(out/'audio.json').write_text(json.dumps(a,indent=2));(ROOT/'manifests/warrior-library-audio.json').write_text(json.dumps(a,indent=2))
print('Shared-bank records',len(b['media']),'Remaining events',a['missingEvents'])
