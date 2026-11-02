"""Import the exact April ablIconSpec art for Diversion (abl.agent.skill.marksmanship.diversion -> smokegrenade)."""
import json
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE,sha
character_source.MANIFEST=ROOT/'manifests/diversion-icon-source.json'
src=character_source.SelectedSource()
path='/resources/gfx/icons/smokegrenade.dds'
assert src.has(path),'smokegrenade icon not in April source'
src.get(path,'diversion','exact ablIconSpec abl.agent.skill.marksmanship.diversion')
out=ROOT/'godot/Assets/UI/icons/smokegrenade.png'
Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(out)
print(out,sha(out))
