"""Only the five recovered D1 icons; no atlas/bulk extraction."""
import json,sys,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/python-ui'))
from PIL import Image
records=[]
for part in json.loads((ROOT/'d1-character-recipe.json').read_text())['parts']:
    if 'iconSource' not in part:continue
    slot={'hand':'hands','boot':'feet','leg':'legs'}.get(part['slot'],part['slot'])
    image=Image.open(ROOT/'source-assets/april2012'/part['iconSource'].lstrip('/')).convert('RGBA')
    output=ROOT/'godot/Assets/Equipment'/(slot+'.png');image.save(output)
    records.append(dict(source=part['iconSource'],output=str(output.relative_to(ROOT)),dimensions=list(image.size),sha256=hashlib.sha256(output.read_bytes()).hexdigest(),conversion='Pillow DDS -> RGBA PNG, native size, no tint/resample'))
(ROOT/'manifests/d1-icons.json').write_text(json.dumps(records,indent=2))
