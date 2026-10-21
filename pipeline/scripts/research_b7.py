import json, struct, re
from pathlib import Path
import character_source
from source_pipeline import ROOT,sha,pathhash
from ui_formats import tags
from lightning_source import decode_xml
character_source.MANIFEST=ROOT/'manifests/b7-source.json'
src=character_source.SelectedSource()
src.manifest.update(selection='B7 bounded presentation research',maximumFiles=55,maximumDecodedBytes=64*1024*1024)
result={'movies':{},'fx':{},'banks':{}}
for name in ['bwaui_levelup_popup_window','bwaui_messageitem','bwaui_system_message','bwaui_achievement_popup_window','bwaui_lib_banners']:
 p='/resources/gfx/gfx_production/'+name+'.gfx'
 if not src.has(p):result['movies'][name]={'found':False};continue
 b=src.get(p,'B7','notification movie');tt=list(tags(b));raw=b if b[:3] not in (b'CFX',b'CWS') else b[:8]+__import__('zlib').decompress(b[8:]);pos=8+(5+(raw[8]>>3)*4+7)//8
 frames=struct.unpack_from('<HH',raw,pos)
 images=[];sub=[];sprites=[]
 for tag,d in tt:
  if tag==1009:
   filename=d[12:12+d[11]].rstrip(b'\0').decode();path='/resources/gfx/gfx_production/'+filename
   images.append({'id':struct.unpack_from('<H',d)[0],'path':path});src.get(path,p,'GFX external image')
  if tag==1008:sub.append(list(struct.unpack('<6H',d)))
  if tag==39:sprites.append({'id':struct.unpack_from('<H',d)[0],'frames':struct.unpack_from('<H',d,2)[0]})
 result['movies'][name]={'found':True,'fps':frames[0]/256,'frames':frames[1],'images':images,'subimages':sub,'sprites':sprites,'strings':[x.decode('ascii','replace') for x in re.findall(rb'[\x20-\x7e]{8,}',raw) if any(k in x.lower() for k in [b'level',b'fade',b'anim',b'frame',b'popup'])][:35]}
for p in ['/resources/art/fx/fxspec/abilities/all/all_level_up_red.fxspec','/resources/art/fx/fxspec/items/loot_generic.fxspec','/resources/art/fx/fxspec/items/loot_common.fxspec','/resources/art/fx/fxspec/loot_glow.fxspec','/resources/art/fx/fxspec/audio/audio_pickup_holocron.fxspec']:
 if src.has(p):
  data=src.get(p,'B7','level/pickup candidate');fields=[(f.get('name'),f.text) for f in decode_xml(data).iter('f')];result['fx'][p]=fields
for p in ['/resources/bnk2/abilities_general_data.bnk','/resources/bnk2/abilities_creatures_data.bnk','/resources/bnk/user_interface.bnk','/resources/bnk2/user_interface.bnk']:
 if src.has(p):
  data=src.get(p,'B7','selected bank graph');result['banks'][p]={'bytes':len(data)}
(ROOT/'work/b7/research.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2)[:14000])
