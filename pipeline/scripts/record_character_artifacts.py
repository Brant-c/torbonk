import json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
paths=[ROOT/'generated/sorcerer.glb',ROOT/'work/sorcerer-reference.blend',ROOT/'work/sorcerer-portable.blend',
       ROOT/'character-recipe.json',ROOT/'CHARACTER-README.md',ROOT/'CHARACTER-RESULTS.md',
       ROOT/'Run-Character-Pipeline.ps1',ROOT/'Open-CharacterInspection.ps1',ROOT/'Assert-Toolchain.ps1',
       ROOT/'godot/CharacterInspection.gd',ROOT/'godot/CharacterInspection.tscn',ROOT/'godot/Assets/sorcerer.glb.import']
paths+=sorted((ROOT/'generated/character').glob('*'))
paths+=sorted(p for p in (ROOT/'scripts').glob('*.py') if 'character' in p.name)
result={'artifacts':[{'path':str(p.relative_to(ROOT)),'bytes':p.stat().st_size,
    'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in paths if p.is_file()]}
source=json.loads((ROOT/'manifests/character-source.json').read_text())
evidence=json.loads((ROOT.parent/'docs/investigation/asset-evidence-2026-10-20.json').read_text(encoding='utf-8-sig'))
matches=[]
for record in evidence['resources']:
    original=next((s for s in source['resources'] if s['path']==record['query']),None)
    if original:
        assert original['sha256']==record['result']['sha256']
        assert original['archiveHash'].lower()==record['result']['archiveHash'].lower()
        matches.append(original['path'])
result['investigationExactMatches']=matches
result['totalRecordedBytes']=sum(r['bytes'] for r in result['artifacts'])
(ROOT/'manifests/character-artifacts.json').write_text(json.dumps(result,indent=2))
print('Recorded',len(result['artifacts']),'artifacts;',len(matches),'exact investigation resource matches')
