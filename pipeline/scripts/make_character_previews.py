"""Optional presentation of actual Godot captures; requires Pillow."""
from pathlib import Path
import json
from PIL import Image,ImageOps,ImageDraw
ROOT=Path(__file__).resolve().parents[1];folder=ROOT/'generated/character'
clips=['Idle','Run','LightningStart','LightningLoop','Hit','Death','LightningEnd','BindPose']
canvas=Image.new('RGB',(1400,700),'#10141c');draw=ImageDraw.Draw(canvas)
for i,name in enumerate(clips):
    im=Image.open(folder/f'godot-{name}.png').convert('RGB')
    im=ImageOps.contain(im.crop((190,145,1000,800)),(350,315))
    x=i%4*350;y=i//4*350
    canvas.paste(im,(x+(350-im.width)//2,y+28));draw.text((x+12,y+8),name,fill='white')
canvas.save(folder/'inspection-contact-sheet.png')
strip=Image.new('RGB',(900,840),'#10141c');draw_strip=ImageDraw.Draw(strip)
evidence=json.loads((ROOT/'manifests/character-playback-captures.json').read_text())
for name in ['Run','LightningLoop','Death']:
    paths=[folder/Path(record['image']).name for record in evidence if record['clip']==name]
    frames=[Image.open(p).convert('RGB').resize((625,500)) for p in paths]
    frames[0].save(folder/('playback-'+name+'.gif'),save_all=True,append_images=frames[1:],duration=200,loop=0)
    row=['Run','LightningLoop','Death'].index(name)
    for column,index in enumerate([0,len(paths)//2,len(paths)-1]):
        im=Image.open(paths[index]).convert('RGB')
        im=ImageOps.contain(im.crop((190,145,1000,800)),(300,245))
        x=column*300;y=row*280
        strip.paste(im,(x+(300-im.width)//2,y+28))
        draw_strip.text((x+12,y+8),name+' frame '+str(index),fill='white')
strip.save(folder/'playback-review.png')
print('Preview sheet/GIFs made from actual runtime captures')
