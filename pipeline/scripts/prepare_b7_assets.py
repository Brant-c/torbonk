import json,struct,subprocess,sys
from source_pipeline import ROOT,SOURCE,sha
from ui_formats import tags
from PIL import Image,ImageDraw
import character_source
character_source.MANIFEST=ROOT/'manifests/b7-source.json';src=character_source.SelectedSource()
out=ROOT/'godot/Assets/Presentation';out.mkdir(exist_ok=True)
r=json.loads((ROOT/'work/b7/research.json').read_text());movie=r['movies']['bwaui_levelup_popup_window'];records=[]
for sub,img,x0,y0,x1,y1 in movie['subimages']:
 p=next(x['path'] for x in movie['images'] if x['id']==img);im=Image.open(SOURCE/p.lstrip('/')).convert('RGBA').crop((x0,y0,x1,y1));dest=out/f'levelup_{sub}.png';im.save(dest);records.append({'source':p,'movie':'bwaui_levelup_popup_window.gfx','subimage':sub,'crop':[x0,y0,x1,y1],'output':str(dest.relative_to(ROOT)),'sha256':sha(dest)})
for path in ['/resources/art/fx/particles/loot_core_glow.prt','/resources/art/fx/particles/loot_core_glow_p.prt','/resources/art/fx/particles/all_holo_empire_level_up.prt']:
 if src.has(path):
  data=src.get(path,'B7','selected loot/level particle');(ROOT/'work/b7'/__import__('pathlib').Path(path).name).write_bytes(data);print(path,data.decode()[:2000])
large=json.loads((ROOT/'work/b7/large-bank-discovery.json').read_text());e=large['matches'][0]['entry']
with open(e['archive'],'rb') as f:f.seek(e['dataOffset']);data=f.read(e['length'])
dest=SOURCE/'audio-by-hash/b7-gui-d634b5f5260713cd.bnk';dest.write_bytes(data)
e.update(output=str(dest.relative_to(ROOT)),sha256=sha(dest),evidence='Exact typed level-up events in BKHD/HIRC; original bank filename unresolved')
(ROOT/'manifests/b7-gui-bank.json').write_text(json.dumps(e,indent=2))
subprocess.run([sys.executable,str(ROOT/'tools/wwiser.pyz'),str(dest),'-d','xml','-dn',str(ROOT/'work/b7/gui')],check=True,stdout=subprocess.DEVNULL)
(ROOT/'manifests/b7-visuals.json').write_text(json.dumps({'assets':records,'movie':movie,'limits':'Original level popup is static; Godot fade/hold/layout authored.'},indent=2))
sheet=Image.new('RGB',(520,300),(12,20,32));draw=ImageDraw.Draw(sheet)
for i,rec in enumerate(records):
 im=Image.open(ROOT/rec['output']);sheet.paste(im,(10,i*100),im);draw.text((455,i*100+10),str(rec['subimage']),fill='white')
sheet.save(ROOT/'work/b7/levelup-art.png')
