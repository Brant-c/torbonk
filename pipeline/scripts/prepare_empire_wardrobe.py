"""Bounded original D3 outfits; no archive writes or broad extraction."""
import json,copy,xml.etree.ElementTree as ET
from pathlib import Path
import character_source
from character_source import ROOT
character_source.MANIFEST=ROOT/'manifests/empire-wardrobe-source.json'
src=character_source.SelectedSource()
src.manifest['maximumFiles']=380;src.manifest['maximumDecodedBytes']=220*1024*1024
defs=json.loads((ROOT/'manifests/empire-wardrobe-selected.json').read_text())
def canon(p):return '/resources/'+p.replace('\\','/').lstrip('/').replace('[bt]','bmn').replace('[gen]','m')
def registry(slot,ident):return next(a for a in src.registry(slot) if a.findtext('ID')==ident)
def material(path,parent):
    path=canon(path);xml=ET.fromstring(src.get(path,parent,'original material'));textures={};values={}
    for n in xml.findall('input'):
        k=n.findtext('semantic');v=n.findtext('value') or ''
        if n.findtext('type')=='texture' and v.strip('/'):
            p=canon(v);p+= '' if p.endswith('.dds') else '.dds';src.get(p,path,k);textures[k]=p
        else:values[k]=v
    return dict(path=path,derived=xml.findtext('Derived'),textures=textures,values=values)
def main():
    for cl,spec in [('war','mdps2'),('inq','cdps1'),('spy','tdps1'),('bh','rdps1')]:
        recipe=json.loads((ROOT/'d1-character-recipe.json').read_text());recipe['parts']=[p for p in recipe['parts'] if p['slot'] in ['head','hair']]
        recipe['outfit']=f'April {cl} heroic tier 1 appearance family; full spawn inventory unverified';hues={}
        for slot,suffix in [('chest','chest'),('hand','hands'),('leg','legs'),('boot','feet'),('waist','waist')]:
            tier='t1x1' if cl=='bh' else 't1x2'
            d=next(d for d in defs if d['identity']['name']==f'ipp.class.{cl}.heroic.{spec}.{tier}.{suffix}')
            f={c['key']:c['value'] for c in d['children']};v=lambda k:f[k]['signed64']
            a=registry(slot,v('appAppearanceSlotModelID'));m=next(m for m in a.findall('Materials/Material') if m.get('id')==v('appAppearanceSlotMaterialIndex'))
            part=dict(slot=slot,modelId=v('appAppearanceSlotModelID'),materialId=v('appAppearanceSlotMaterialIndex'),artName=a.findtext('ArtName'),registryXml=ET.tostring(a,encoding='unicode'),skinMaterialIndex=int(a.findtext('SkinMaterialIndex')),material=material(m.get('filename'),cl),paletteIds=[v('appAppearanceSlotHuePrimary'),v('appAppearanceSlotHueSecondary')],models=[],overrides=[],sourceAppearance=d['identity'])
            for hue in part['paletteIds']:
                if hue not in hues:
                    h=registry('garmenthue',hue);p=canon(h.findtext('BaseFile'))
                    if src.has(p):hues[hue]=dict(id=hue,xml=src.get(p,cl,'original palette').decode())
                    else:hues[hue]=dict(id=hue,values={});print('COMPILED PALETTE REQUIRED',hue,h.findtext('ArtName'))
            selected=[str(c['value']['signed64']) for c in d.get('attachments',{}).get('children',[])]
            for p,ident in [(a.findtext('BaseFile'),part['modelId'])]+[(x.get('filename'),x.get('id')) for x in a.findall('Attachments/Attachment') if x.get('id') in selected]:
                p=canon(p);src.get(p,cl,'selected starter modular mesh');part['models'].append(dict(path=p,id=ident))
            icon='/resources/gfx/icons/'+d['identity']['name']+'.dds'
            src.get(icon,d['identity']['name'],'matching appearance inventory icon');part['iconSource']=icon
            recipe['parts'].append(part)
            src.get(d['identity']['source']['path'],d['identity']['name'],'definition bucket')
        scheme=v('ippColorScheme')
        recipe['colorScheme']=dict(id=scheme,xml=ET.tostring(registry('colorscheme',scheme),encoding='unicode'),hues=hues)
        (ROOT/f'wardrobe-{cl}-recipe.json').write_text(json.dumps(recipe,indent=2))
        print(cl,[(p['slot'],p['artName']) for p in recipe['parts']])
    character_source.MANIFEST.write_text(json.dumps(src.manifest,indent=2))
if __name__=='__main__':main()

