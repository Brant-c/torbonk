"""Bounded D1 recovery: definition -> registry -> exact April entry, separate receipts."""
import json, hashlib, xml.etree.ElementTree as ET
from pathlib import Path
import character_source
from character_source import ROOT

character_source.MANIFEST=ROOT/'manifests/d1-source.json'
src=character_source.SelectedSource()
evidence=json.loads((ROOT/'manifests/d1-definition-evidence.json').read_text())
defs=[json.loads(e['result']['content'][0]['text']) for e in evidence]
def canonical(p): return '/resources/'+p.replace('\\','/').lstrip('/').replace('[bt]','bmn').replace('[gen]','m')
def registry(slot,identity):
    a=next(a for a in src.registry(slot) if a.findtext('ID')==identity)
    assert 'bmn' in [b.text for b in a.findall('Bodytypes/Bodytype')]
    return a
def material(path,parent):
    path=canonical(path); xml=ET.fromstring(src.get(path,parent,'original material'))
    textures={}; values={}
    for inp in xml.findall('input'):
        key=inp.findtext('semantic'); value=inp.findtext('value') or ''
        if inp.findtext('type')=='texture' and value.strip('/'):
            p=canonical(value)
            if not p.endswith('.dds'):p+='.dds'
            src.get(p,path,key);textures[key]=p
        else:values[key]=value
    return dict(path=path,derived=xml.findtext('Derived'),textures=textures,values=values)

recipe=json.loads((ROOT/'character-recipe.json').read_text())
recipe['outfit']='April level-1 Inquisitor class_quest_01_a / starting.cdps1.t1x1; complete appearance-family alternative, not proven full spawn inventory'
recipe['parts']=[p for p in recipe['parts'] if p['slot'] in ['head','hair']]
palettes=json.loads((ROOT/'manifests/d1-palette-evidence.json').read_text())
hues={}
for identity,ev in zip(['1002161','1002144','1002164'],palettes):
    root=json.loads(ev['result']['content'][0]['text']); values={c['key']:c['value'] for c in root['children'] if not c['expandable']}
    for expanded in ev['expanded']:
        color={c['key']:c['value'] for c in expanded['children'] if c['key'] in ['r','g','b','a']}
        values[expanded['path'].lstrip('/')]=[color[k] for k in ['r','g','b','a']]
    data=src.get(root['identity']['source']['path'],root['identity']['name'],'compiled original palette')
    assert hashlib.sha256(data).hexdigest()==root['identity']['source']['resourceSha256']
    hues[identity]=dict(id=identity,values=values,gomIdentity=root['identity'])
recipe['colorScheme']=dict(id='1003284',xml=ET.tostring(registry('colorscheme','1003284'),encoding='unicode'),hues=hues)
for slot,suffix in [('chest','chest'),('leg','legs'),('hand','hands'),('boot','feet'),('waist','waist')]:
    definition=next(d for d in defs if d['identity']['name']=='ipp.class.inq.starting.cdps1.t1x1.'+suffix and d['path']=='')
    fields={c['key']:c['value'] for c in definition['children']}
    val=lambda k:fields[k]['signed64']
    modelid=val('appAppearanceSlotModelID');matid=val('appAppearanceSlotMaterialIndex');a=registry(slot,modelid)
    m=next(m for m in a.findall('Materials/Material') if m.get('id')==matid)
    part=dict(slot=slot,modelId=modelid,materialId=matid,artName=a.findtext('ArtName'),registryXml=ET.tostring(a,encoding='unicode'),
        sourceAppearance=definition['identity'],skinMaterialIndex=int(a.findtext('SkinMaterialIndex')),material=material(m.get('filename'),slot),
        paletteIds=[val('appAppearanceSlotHuePrimary'),val('appAppearanceSlotHueSecondary')],models=[],overrides=[])
    # An IPP attachment list selects registry attachments; unselected collar/shld/shin stay absent.
    selected=['1002329'] if slot=='chest' else []
    for path,ident in [(a.findtext('BaseFile'),modelid)]+[(x.get('filename'),x.get('id')) for x in a.findall('Attachments/Attachment') if x.get('id') in selected]:
        p=canonical(path);src.get(p,definition['identity']['name'],'selected modular mesh');part['models'].append(dict(path=p,id=ident))
    recipe['parts'].append(part)
    # Preserve original inventory icon bytes when present; local label is independent of GOM identity.
    icon='/resources/gfx/icons/'+definition['identity']['name']+'.dds'
    if src.has(icon):src.get(icon,definition['identity']['name'],'inventory icon');part['iconSource']=icon
for d in defs:
    if 'identity' in d:
        source=d['identity']['source'];data=src.get(source['path'],d['identity']['name'],'definition evidence bucket')
        assert hashlib.sha256(data).hexdigest()==source['resourceSha256']
# Reuse already verified character customization, skeleton and cloth sources read-only.
(ROOT/'d1-character-recipe.json').write_text(json.dumps(recipe,indent=2))
print('D1 selected resources:',len(src.manifest['resources']))
