"""Selected April helmet definitions and weapon bytes; bounded extraction."""
import json, struct, xml.etree.ElementTree as ET
from pathlib import Path
import character_source
from character_source import ROOT
character_source.MANIFEST=ROOT/'manifests/head-weapon-source.json'
s=character_source.SelectedSource()
s.manifest['maximumFiles']=250;s.manifest['maximumDecodedBytes']=180*1024*1024
def canon(p):return '/resources/'+p.replace('\\','/').lstrip('/').removeprefix('resources/').replace('[bt]','bmn').replace('[gen]','m')
def mat(p):
 p=canon(p);x=ET.fromstring(s.get(p));t={};v={}
 for n in x.findall('input'):
  k=n.findtext('semantic');q=n.findtext('value') or ''
  if n.findtext('type')=='texture' and q.strip('/'):
   q=canon(q);q+='' if q.endswith('.dds') else '.dds';s.get(q,p,k);t[k]=q
  else:v[k]=q
 return dict(path=p,derived=x.findtext('Derived'),textures=t,values=v)
defs=json.loads((ROOT/'manifests/head-weapon-definitions.json').read_text())
reg=s.registry('face')
for family,d in zip(['war','inq','spy','bh'],defs):
 f={c['key']:c['value'] for c in d['children']};v=lambda k:f[k]['signed64']
 a=next(a for a in reg if a.findtext('ID')==v('appAppearanceSlotModelID'))
 m=next(m for m in a.findall('Materials/Material') if m.get('id')==v('appAppearanceSlotMaterialIndex'))
 r=json.loads((ROOT/f'wardrobe-{family}-recipe.json').read_text());r['parts']=[];r['animations']=r['animations'][:1]
 part=dict(slot='helmet',modelId=v('appAppearanceSlotModelID'),materialId=v('appAppearanceSlotMaterialIndex'),artName=a.findtext('ArtName'),skinMaterialIndex=int(a.findtext('SkinMaterialIndex') or '-1'),material=mat(m.get('filename')),paletteIds=[v('appAppearanceSlotHuePrimary'),v('appAppearanceSlotHueSecondary')],models=[],overrides=[],sourceAppearance=d['identity'])
 for hue in part['paletteIds']:
  if hue not in r['colorScheme']['hues']:
   h=next(h for h in s.registry('garmenthue') if h.findtext('ID')==hue);p=canon(h.findtext('BaseFile'));r['colorScheme']['hues'][hue]=dict(id=hue,xml=s.get(p).decode())
 p=canon(a.findtext('BaseFile'));s.get(p);part['models'].append(dict(path=p,id=part['modelId']))
 icon='/resources/gfx/icons/'+d['identity']['name']+'.dds';s.get(icon);part['iconSource']=icon
 s.get(d['identity']['source']['path']);r['parts']=[part]
 (ROOT/f'wardrobe-head-{family}-recipe.json').write_text(json.dumps(r,indent=2));print(family,a.findtext('ArtName'),p)
names=Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').read_text().splitlines()
out={}
for kind,stem in [('saber','saber'),('staff','dualsaber'),('pistol','blaster'),('rifle','rifle')]:
 candidates=[p for p in names if f'/weapon/model/{stem}_' in p and p.endswith('_a01_v01.gr2') and any(t in p for t in ['_high01_','_high02_']) and s.has(p)]
 assert len(candidates)>=2,(kind,candidates)
 for n,p in enumerate(sorted(candidates)[:2],1):
  data=s.get(p);count=struct.unpack_from('<H',data,26)[0];off=struct.unpack_from('<I',data,88)[0];ms=[]
  for i in range(count):
   o=struct.unpack_from('<I',data,off+i*4)[0];stem2=data[o:data.index(b'\0',o)].decode();ms.append(mat('/art/shaders/materials/'+stem2+'.mat'))
  out[f'{kind}{n}']=dict(model=p,materials=ms)
(ROOT/'head-weapons-recipe.json').write_text(json.dumps(dict(weapons=out),indent=2))
