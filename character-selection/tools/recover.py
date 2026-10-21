"""C1 bounded TOR recovery. All writes resolve inside this workspace; archives are rb.
Never imports or executes shared conversion modules, ActionScript, Godot or game code.
"""
from pathlib import Path
import ast, json, struct, zlib, hashlib, re, sys
ROOT = Path(__file__).resolve().parents[1]
SHARED = ROOT.parent / 'pipeline'
ARCHIVES = Path('D:/SWTORClassic/swtoremu/Assets2012April')
NAMES = Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt')
# Freeze just the two pure reader functions, avoiding the shared module's globals.
if not (ROOT/'tools/tor_reader.py').exists():
    source = (SHARED/'scripts/source_pipeline.py').read_text()
    tree = ast.parse(source)
    pure = [ast.get_source_segment(source, n) for n in tree.body if isinstance(n, ast.FunctionDef) and n.name in ('pathhash','entries')]
    (ROOT/'tools/tor_reader.py').write_text('import struct\n\n'+'\n\n'.join(pure)+'\n')
from tor_reader import entries, pathhash
from PIL import Image
sys.path.insert(0, str(ROOT/'tools'))
from ui_formats import tags
def sha(data): return hashlib.sha256(data).hexdigest()
def write(path, data):
    p = (ROOT/path).resolve(); assert p.is_relative_to(ROOT)
    p.parent.mkdir(parents=True, exist_ok=True); p.write_bytes(data)
    return str(p.relative_to(ROOT)).replace('\\','/')
index = {}
# Entry tables only: no payload scan or whole-archive integrity scan.
for archive in sorted(ARCHIVES.glob('*.tor')):
    for e in entries(archive): index.setdefault(e['archiveHash'], []).append(e)
manifest = {'profile':'operator-labelled April2012', 'resources':[], 'missing':[], 'definitions':{}, 'conversions':[]}
def get(path):
    path = path.lower().replace('\\','/')
    previous = next((r for r in manifest['resources'] if r['path']==path), None)
    if previous: return (ROOT/previous['output']).read_bytes()
    found = index.get(f'0x{pathhash(path):016X}', [])
    if len(found)!=1:
        manifest['missing'].append({'path':path,'matches':len(found)}); return None
    e=dict(found[0]); assert e['length'] < 24*1024*1024
    assert sum(x['length'] for x in manifest['resources'])+e['length'] < 180*1024*1024
    with open(e['archive'],'rb') as f: f.seek(e['dataOffset']); packed=f.read(e['compressedSize'])
    if e['compressionMethod']==1:
        dec=zlib.decompressobj(); data=dec.decompress(packed,e['length']+1)
        assert dec.eof and not dec.unconsumed_tail
    else: assert e['compressionMethod']==0; data=packed
    assert len(data)==e['length']
    e.update(path=path,sha256=sha(data),output=write('source-assets/'+path.lstrip('/'), data),evidence='Client-derived: historical path hash matched unique April entry; selected decoded bytes hashed')
    manifest['resources'].append(e); return data
def texture(path, output=None):
    data=get(path)
    if data is None: return None
    original=ROOT/'source-assets'/path.lstrip('/')
    image=Image.open(original).convert('RGBA')
    dest=ROOT/(output or 'Frontend/Assets/Recovered/'+Path(path).stem+'.png')
    assert dest.resolve().is_relative_to(ROOT); dest.parent.mkdir(parents=True,exist_ok=True); image.save(dest)
    manifest['conversions'].append({'source':path,'output':str(dest.relative_to(ROOT)).replace('\\','/'),'sha256':sha(dest.read_bytes()),'size':image.size,'conversion':'Pillow DDS -> RGBA PNG, native pixels, no tint/flip/resampling'})
    return image
def movie(path):
    data=get(path)
    if data is None:return
    images={};subs=[];exports=[];imports=[]
    for tag,b in tags(data):
        if tag==1009:
            iid=struct.unpack_from('<H',b)[0]
            n=b[11];name=b[12:12+n].rstrip(b'\0').decode(); tex='/resources/gfx/gfx_production/'+name.lower()
            images[iid]=tex;texture(tex)
        elif tag==1008:subs.append(struct.unpack('<6H',b))
        elif tag==56:
            p=2
            for _ in range(struct.unpack_from('<H',b)[0]):
                iid=struct.unpack_from('<H',b,p)[0];p+=2;e=b.index(b'\0',p)
                exports.append({'id':iid,'name':b[p:e].decode()});p=e+1
        elif tag in (57,71):imports.append(b.split(b'\0')[0].decode(errors='replace'))
    rec={'images':images,'subimages':[],'exports':exports,'imports':imports}
    for sub,iid,x0,y0,x1,y1 in subs:
        tex=images.get(iid)
        if not tex:continue
        original=ROOT/'source-assets'/tex.lstrip('/')
        if not original.is_file():continue
        im=Image.open(original).convert('RGBA'); assert x1<=im.width and y1<=im.height
        crop=im.crop((x0,y0,x1,y1));dest=ROOT/'Frontend/Assets/Slices'/f'{Path(path).stem}_{sub}.png'
        dest.parent.mkdir(parents=True,exist_ok=True);crop.save(dest)
        rec['subimages'].append({'id':sub,'source':tex,'rect':[x0,y0,x1,y1],'output':str(dest.relative_to(ROOT)).replace('\\','/'),'sha256':sha(dest.read_bytes())})
    manifest['definitions'][Path(path).stem]=rec
names=NAMES.read_text().splitlines()
discovery=[]
for path in names:
    if re.search(r'^/resources/(gfx/.*(galaxy|faction|classpanel|character_create|character_select|sith|jedi|bounty|agent|trooper|smuggl|republic|empire|background)|art/planets/|art/.*(char_select|character_select|char_create))',path):
        found=index.get(f'0x{pathhash(path):016X}',[])
        if len(found)==1:discovery.append({'path':path,'archiveHash':found[0]['archiveHash'],'length':found[0]['length']})
(ROOT/'manifests/candidates.json').write_text(json.dumps(discovery,indent=2))
movies=['bwaui_character_create','bwaui_character_create_window','bwaui_character_create_panels','bwaui_character_create_faction_select','bwaui_character_create_customize','bwaui_character_create_breadcrumbbar','bwaui_advancedclass_selection_window','bwaui_character_select','bwaui_galaxymap','bwaui_galaxymap_shared','ui_galaxymap_infowindow']
for name in movies:movie('/resources/gfx/gfx_production/'+name+'.gfx')
for path in names:
    if re.search(r'^/resources/gfx/(codex/cdx\.(game_rules\.classes|advanced_classes)\.|codex/cdx\.planets\.(korriban|tython|dromund_kaas|coruscant|hutta|ordmantell)\.dds$)',path):texture(path)
    elif re.search(r'^/resources/gfx/icons/(sithwarrior|sithinquisitor|sithsorcerer|sithassassin|sithjuggernaut|sithmarauder|jediknight|jediconsular|jediguardian|jedisentinel|jedisage|jedishadow|imperialagent|agent|bountyhunter|smuggler|trooper|sniper|operative|mercenary|powertech|commando|vanguard|gunslinger|scoundrel)\.dds$',path):texture(path)
    elif re.search(r'^/resources/art/fx/geometry/galaxy_map/texture/(gal_map_galaxy_core_d|gal_map_galaxy_arm_d|gal_map_starfield_d|gal_map_korriban_d)\.dds$',path):texture(path)
for path in ['/resources/art/fx/fxspec/environments/galaxy_map/fx_gal_map_planet_korriban.fxspec','/resources/art/fx/fxspec/environments/galaxy_map/fx_gal_map_innerstars.fxspec','/resources/guixml/charactercreate.xml','/resources/guixml/galaxymap.xml']:get(path)
for path in ['/resources/gfx/gfx_production/ui_character_create_classpanel.gfx','/resources/gfx/gfx_production/ui_galaxymap.gfx','/resources/gfx/gfx_production/ui_galaxymap_shared.gfx']:movie(path)
for name in ['ui_hud_galaxymap','ui_galaxymap_panel','ui_galaxymap_planet_info','ui_galaxymap_scanlines','ui_galaxymap_centerbutton','ui_galaxymap_leftbutton','ui_galaxymap_closebttn']:
    movie('/resources/gfx/gfx_production/'+name+'.gfx')
for path in ['/resources/art/fx/texture/sky/char_select_stars.dds','/resources/art/fx/geometry/tython/texture/char_select_tython_high_res_d.dds','/resources/gfx/symbols/empire_indicator.dds','/resources/gfx/symbols/republic_indicator.dds']:
    texture(path)
for path in names:
    if re.search(r'^/resources/art/planets/texture/planets_(korriban|tython|hutta|dromund_kass|coruscant|ord_mantell)_d\.dds$',path):texture(path)
for path in ['/resources/art/fx/particles/char_select_milky_way_p.prt','/resources/art/fx/particles/char_select_planet_glow_p.prt','/resources/art/fx/particles/char_select_moon_01_p.prt']:
    b=get(path)
    if b:
        s=b.decode('utf-16') if b[:2] in (b'\xff\xfe',b'\xfe\xff') else b.decode('utf-8',errors='replace')
        (ROOT/'work'/Path(path).with_suffix('.txt').name).write_text(s)
for path in names:
    if re.search(r'^/resources/art/fx/texture/galaxy_map/.*(galaxy|galactic_core|arrow_indicator|planet_indicator|selection).*\.dds$',path) and '.tiny.' not in path:
        if len(index.get(f'0x{pathhash(path):016X}',[]))==1:texture(path)
for cls in ['sithwarrior','sithsorcerer','imperialagent','bountyhunter','jediknight','jediconsular','smuggler','trooper']:
    texture('/resources/gfx/icons/larger/'+cls+'.dds','Frontend/Assets/Icons/'+cls+'.png')
(ROOT/'manifests/recovery.json').write_text(json.dumps(manifest,indent=2))
print(json.dumps({'resources':len(manifest['resources']),'missing':manifest['missing'],'movies':{k:{'images':v['images'],'slices':len(v['subimages']),'exports':v['exports'][:8]} for k,v in manifest['definitions'].items()}},indent=2))
