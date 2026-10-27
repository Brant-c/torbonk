"""Recover only the 24 April talent icons selected by their decoded ablIconSpec."""
import json
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE,sha

character_source.MANIFEST=ROOT/'manifests/t1-talent-art-source.json'
src=character_source.SelectedSource()
src.manifest.update(maximumFiles=30,maximumDecodedBytes=8*1024*1024,selection='T1 April talent ablIconSpec icons')
records=json.loads((ROOT/'manifests/t1-talent-client-evidence.json').read_text())
out=ROOT/'godot/Assets/UI/talents';out.mkdir(parents=True,exist_ok=True)
converted=[]
for r in records:
    icon=next(c['value'] for c in r['children'] if c['key']=='ablIconSpec').lower()
    path='/resources/gfx/icons/'+icon+'.dds'
    src.get(path,r['identity']['name'],'April ablIconSpec exact mapping')
    dest=out/(icon+'.png');Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(dest)
    converted.append(dict(talent=r['identity']['name'],source=path,output=str(dest.relative_to(ROOT)),sha256=sha(dest)))
(ROOT/'manifests/t1-talent-art-conversion.json').write_text(json.dumps(converted,indent=2))
print('Recovered',len(converted),'talent icons without resampling or recoloring')
