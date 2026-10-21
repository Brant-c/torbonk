from pathlib import Path
import json,re,zlib
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1]
m=json.loads((R/'manifests/recovery.json').read_text())
for name,d in m['definitions'].items():print(name,'imports',d['imports'])
for p in (R/'source-assets/resources/gfx/gfx_production').glob('*.gfx'):
    b=p.read_bytes()
    if b[:3]==b'CFX':b=b[:8]+zlib.decompress(b[8:])
    ss=[x.decode(errors='replace') for x in re.findall(rb'[A-Za-z_][A-Za-z0-9_./ \\-]{6,100}',b)]
    print(p.stem,[x for x in ss if re.search('faction|classpanel|galaxy|background|logo|republic|empire',x,re.I)][:35])
files=list((R/'Frontend/Assets/Slices').glob('*.png'))
files+=list((R/'Frontend/Assets/Recovered').glob('cdx.game_rules*.png'))
sheet=Image.new('RGB',(1000,((len(files)+4)//5)*145),(13,22,35));d=ImageDraw.Draw(sheet)
for i,p in enumerate(files):
    im=Image.open(p).convert('RGBA');im.thumbnail((190,115));x=(i%5)*200;y=(i//5)*145
    sheet.paste(im,(x,y),im);d.text((x,y+115),p.stem.replace('bwaui_','').replace('character_','')[:30],fill='white')
sheet.save(R/'work/asset-sheet.jpg')
print('candidates',len(json.loads((R/'manifests/candidates.json').read_text())))
