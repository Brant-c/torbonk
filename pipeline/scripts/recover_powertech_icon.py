import sys,json
from pathlib import Path
sys.path.insert(0,'pipeline/scripts')
import character_source
from source_pipeline import ROOT,SOURCE,sha
from PIL import Image
character_source.MANIFEST=ROOT/'manifests/powertech-source.json';s=character_source.SelectedSource();p='/resources/gfx/icons/bountyhunter.dds';s.get(p,'abl.bounty_hunter.flame_thrower','ablIconSpec=bountyhunter from April ability 0xE00091FA04AA6C91');dest=ROOT/'godot/Assets/UI/icons/bountyhunter.png';Image.open(SOURCE/p.lstrip('/')).convert('RGBA').save(dest)
(ROOT/'manifests/powertech-flamethrower-icon.json').write_text(json.dumps({'evidence':'Client-derived','ability':'abl.bounty_hunter.flame_thrower','id':'0xE00091FA04AA6C91','ablIconSpec':'bountyhunter','bucket':'/resources/systemgenerated/buckets/461.bkt','resourceSha256':'91a7ead62d35f92a4a45b186f2596e9427d68bc20d3f254d95f4cb606408b4f0','output':str(dest.relative_to(ROOT)),'outputSha256':sha(dest)},indent=2))
