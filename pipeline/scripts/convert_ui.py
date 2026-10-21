"""Bounded reusable B4 extraction/conversion. TOR inputs stay read-only.
Run with Pillow; fontTools 4.60.1 in tools/python-ui for embedded fonts.
"""
import json, struct, sys
from pathlib import Path
from PIL import Image, ImageDraw
import character_source
from source_pipeline import ROOT, sha
from ui_formats import tags, font_to_ttf

sys.path.insert(0,str(ROOT/'tools/python-ui'))
character_source.MANIFEST=ROOT/'manifests/ui-source.json'
OUT=ROOT/'godot/Assets/UI'
RECIPE=json.loads((ROOT/'ui-recipe.json').read_text())

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    if not character_source.MANIFEST.exists():
        character_source.MANIFEST.write_text(json.dumps({
            'profile':RECIPE['profile'],'maximumFiles':RECIPE['maximumFiles'],
            'maximumDecodedBytes':RECIPE['maximumDecodedBytes'],'resources':[],
            'dependencies':[],'unresolved':[],'selection':'B4 UI recipe'},indent=2))
    src=character_source.SelectedSource()
    records=[]; crops=[]; definitions={}
    def png(image,dest,source,**extra):
        dest.parent.mkdir(parents=True,exist_ok=True); image.save(dest)
        records.append(dict(source=source,output=str(dest.relative_to(ROOT)),sha256=sha(dest),dimensions=list(image.size),**extra))
    for movie in RECIPE['movies']:
        path=f'/resources/gfx/gfx_production/{movie}.gfx'
        stream=list(tags(src.get(path,'B4 UI recipe','selected Scaleform definition')))
        images={}; subs=[]; exports=[]; imports=[]
        for tag,data in stream:
            if tag==1009:
                imageid=struct.unpack_from('<H',data)[0]
                length=data[11]; filename=data[12:12+length].rstrip(b'\0').decode()
                texture='/resources/gfx/gfx_production/'+filename
                src.get(texture,path,f'GFX tag 1009 external image id {imageid}')
                original=ROOT/next(r['output'] for r in src.manifest['resources'] if r['path']==texture)
                images[imageid]=(texture,Image.open(original).convert('RGBA'))
                if not any(r['source']==texture and 'subImageId' not in r for r in records):
                    png(images[imageid][1],OUT/'atlases'/Path(filename).with_suffix('.png'),texture,conversion='Pillow DDS to RGBA PNG; native size; no flip, tint, resample or gamma transform')
            elif tag==1008:
                sub,imageid,x0,y0,x1,y1=struct.unpack('<6H',data)
                subs.append((sub,imageid,(x0,y0,x1,y1)))
            elif tag==56:
                p=2
                for _ in range(struct.unpack_from('<H',data)[0]):
                    symbol=struct.unpack_from('<H',data,p)[0];p+=2
                    end=data.index(b'\0',p)
                    exports.append({'id':symbol,'name':data[p:end].decode()});p=end+1
            elif tag in (57,71):
                name=data.split(b'\0')[0].decode()
                # .swf is the linkage name in GFX; archive definitions ship as .gfx.
                candidate='/resources/gfx/gfx_production/'+str(Path(name.lower()).with_suffix('.gfx')).replace('\\','/')
                matches=src.index.get(f'0x{__import__("source_pipeline").pathhash(candidate):016X}',[])
                imports.append({'linkageName':name,'candidateGfxPath':candidate,
                    'uniqueAprilEntry':len(matches)==1,
                    'archiveIdentities':[{k:e[k] for k in ('archive','archiveHash','entryOffset','length')} for e in matches],
                    'status':'linkage recorded only; no native runtime executed or import instantiated'})
        definitions[movie]={'exports':exports,'imports':imports,'subimages':[]}
        for sub,imageid,rect in subs:
            texture,image=images[imageid]
            assert 0<=rect[0]<rect[2]<=image.width and 0<=rect[1]<rect[3]<=image.height
            crop=image.crop(rect); name=f'{movie}_{sub}'
            png(crop,OUT/'slices'/(name+'.png'),texture,definition=path,subImageId=sub,imageId=imageid,crop=list(rect),conversion='GFX tag 1008 pixel rectangle; native RGBA; no resample')
            definitions[movie]['subimages'].append({'id':sub,'imageId':imageid,'crop':list(rect),'file':name+'.png'})
            crops.append((name,crop))
    for name,selection in RECIPE.get('manualSlices',{}).items():
        texture='/resources/gfx/gfx_production/'+selection['atlas']+'.dds'
        src.get(texture,'ui-recipe.json manualSlices/'+name,'visually identified atlas rectangle; not a verified GFX symbol mapping')
        original=ROOT/next(r['output'] for r in src.manifest['resources'] if r['path']==texture)
        image=Image.open(original).convert('RGBA');rect=selection['crop']
        png(image.crop(rect),OUT/'slices'/(name+'.png'),texture,crop=rect,
            conversion='Manual atlas rectangle from source pixels; no resample',
            selectionEvidence='Visual inspection: portraitbar GFX rectangles do not align with these atlas features; native semantic mapping remains unresolved')
    for icon in RECIPE['icons']:
        path=f'/resources/gfx/icons/{icon}.dds'
        native=icon in ('sithsorcerer','markofpower','forcespeed','chargedgauntlets')
        src.get(path,'B4 icon recipe','April GOM ablIconSpec mapping; see ui-gom-evidence.json' if native else 'verified April icon; survivor upgrade thematic reuse')
        original=ROOT/next(r['output'] for r in src.manifest['resources'] if r['path']==path)
        png(Image.open(original).convert('RGBA'),OUT/'icons'/(icon+'.png'),path,conversion='Pillow DDS to RGBA PNG; no resample')
    path='/resources/gfx/fonts/swtor_fonts.gfx'
    blob=src.get(path,'B4 typography','embedded original outlines')
    for tag,data in tags(blob):
        if tag==75 and str(struct.unpack_from('<H',data)[0]) in RECIPE['fonts']:
            dest=OUT/RECIPE['fonts'][str(struct.unpack_from('<H',data)[0])]
            meta=font_to_ttf(data,dest)
            records.append(dict(source=path,fontId=struct.unpack_from('<H',data)[0],output=str(dest.relative_to(ROOT)),sha256=sha(dest),**meta))
    (ROOT/'manifests/ui-conversion.json').write_text(json.dumps({'decoder':'Pillow '+Image.__version__,'fontTools':'4.60.1',
        'recipe':'ui-recipe.json','roles':RECIPE['roles'],'roleEvidence':RECIPE['roleEvidence'],
        'godotImport':{'textures':'lossless; no mipmaps, size limit, channel remap or premultiplication; default fix_alpha_border=true (import cache only)',
            'fonts':'dynamic FontFile, grayscale antialiasing, automatic hinting, system fallback for absent glyphs; no MSDF'},
        'resources':records,'definitions':definitions},indent=2))
    # One asset contact sheet for selecting the source slices; no runtime captures.
    sheet=Image.new('RGB',(1000,((len(crops)+5)//6)*115),(15,21,30)); draw=ImageDraw.Draw(sheet)
    for i,(name,im) in enumerate(crops):
        x=(i%6)*166;y=(i//6)*115
        preview=im.copy();preview.thumbnail((158,85));sheet.paste(preview,(x,y),preview)
        draw.text((x,y+85),name.replace('bwaui_',''),fill='white')
    sheet.save(ROOT/'work/ui/slices.png')
    print(json.dumps({'originals':len(src.manifest['resources']),'converted':len(records),'slices':len(crops),'contactSheet':str(ROOT/'work/ui/slices.png')}))

if __name__=='__main__': main()
