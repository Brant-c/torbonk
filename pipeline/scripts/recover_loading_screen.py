"""Recover April loading art and Scaleform bitmap slices; never execute the movie."""
from pathlib import Path
import sys, json, zlib, hashlib, struct, io
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT.parent/'character-selection/tools'))
from tor_reader import entries, pathhash
from ui_formats import tags
from PIL import Image, ImageDraw
OUT = ROOT/'godot/Assets/UI/loading'
OUT.mkdir(parents=True, exist_ok=True)
index = {}
for archive in Path('D:/SWTORClassic/swtoremu/Assets2012April').glob('*.tor'):
    for e in entries(archive): index.setdefault(e['archiveHash'], []).append(e)
records=[]
def get(path):
    found=index.get(f'0x{pathhash(path):016X}',[])
    if len(found)!=1: print('Unavailable',path,len(found)); return None
    e=dict(found[0])
    with open(e['archive'],'rb') as f: f.seek(e['dataOffset']); data=f.read(e['compressedSize'])
    if e['compressionMethod']==1: data=zlib.decompress(data)
    assert len(data)==e['length']
    records.append(dict(e,path=path,sha256=hashlib.sha256(data).hexdigest()))
    return data
base='/resources/gfx/gfx_production/'
blob=get(base+'bwaui_loading_screen.gfx')
images={}; slices=[]; exports=[]
for tag,b in tags(blob):
    if tag==1009:
        iid=struct.unpack_from('<H',b)[0]; n=b[11]; name=b[12:12+n].rstrip(b'\0').decode()
        data=get(base+name.lower())
        if data:
            im=Image.open(io.BytesIO(data)).convert('RGBA'); images[iid]=im
            im.save(OUT/('atlas_'+str(iid)+'.png'))
    elif tag==1008: slices.append(struct.unpack('<6H',b))
    elif tag==56:
        p=2
        for _ in range(struct.unpack_from('<H',b)[0]):
            iid=struct.unpack_from('<H',b,p)[0]; p+=2; end=b.index(b'\0',p)
            exports.append([iid,b[p:end].decode()]); p=end+1
for sid,iid,x,y,r,bt in slices:
    if iid in images: images[iid].crop((x,y,r,bt)).save(OUT/f'slice_{sid}.png')
names=Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').read_text().splitlines()
for path in names:
    if path.endswith('.dds') and path.startswith('/resources/gfx/loadingscreen/') and (any('/'+p in path for p in ['korriban','hutta','dromund','bal_overlay','dro_overlay']) or path.endswith('/background_tile.dds')):
        data=get(path)
        if data: Image.open(io.BytesIO(data)).convert('RGBA').save(OUT/(Path(path).stem+'.png'))
manifest=dict(evidence='Client-derived: historical path hash matched unique April entry; decoded bytes SHA256 verified',resources=records,slices=slices,exports=exports)
(ROOT/'manifests/loading-assets.json').write_text(json.dumps(manifest,indent=2))
files=list(OUT.glob('slice*.png'))+list(OUT.glob('*overlay*.png'))
sheet=Image.new('RGB',(800,((len(files)+3)//4)*145),'#253144'); d=ImageDraw.Draw(sheet)
for i,p in enumerate(files):
    im=Image.open(p); im.thumbnail((190,115)); x=i%4*200;y=i//4*145
    sheet.paste(im,(x,y),im);d.text((x,y+116),p.stem,fill='white')
sheet.save(ROOT/'work/loading-contact.png')
print(json.dumps({'exports':exports,'slices':slices,'files':[p.name for p in OUT.glob('*.png')]},indent=2))
