"""Register bounded April Empire cosmetics and their exact source inventory icons."""
import copy, hashlib, json, shutil, sys
from pathlib import Path
import character_source
from character_source import ROOT
sys.path.insert(0,str(ROOT/'tools/python-ui'))
from PIL import Image

character_source.MANIFEST=ROOT/'manifests/empire-wardrobe-source.json'
src=character_source.SelectedSource()
read=lambda p:json.loads((ROOT/p).read_text())
catalog=read('godot/Gameplay/Equipment/catalog.json')
unlocks=read('godot/Gameplay/Equipment/cosmetic-unlocks.json')
out=ROOT/'godot/Assets/Equipment/Empire';out.mkdir(exist_ok=True)
mapping={'chest':'chest','hand':'hands','leg':'legs','boot':'feet','waist':'waist'}
groups={'war':['sith_warrior','sith_inquisitor'],'inq':['sith_warrior','sith_inquisitor'],
        'spy':['imperial_agent','bounty_hunter'],'bh':['imperial_agent','bounty_hunter']}
names={'war':'Warrior','inq':'Inquisitor','spy':'Agent','bh':'Bounty Hunter'}
earning_classes={'war':'sith_warrior','inq':'sith_inquisitor','spy':'imperial_agent','bh':'bounty_hunter'}
records=[]
def icon(identity,path):
    src.get(path,identity,'exact appearance inventory icon')
    target=out/(identity+'.png')
    with Image.open(ROOT/'source-assets/april2012'/path.lstrip('/')) as im:im.convert('RGBA').save(target)
    records.append(dict(id=identity,source=path,output=str(target.relative_to(ROOT)),sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
    return 'res://Assets/Equipment/Empire/'+target.name

for key in ['Items','Appearances']:catalog[key]=[x for x in catalog[key] if not x['Id'].startswith('wardrobe.')]
unlocks['Milestones']=[m for m in unlocks['Milestones'] if not m['Id'].startswith('empire.heroic.')]
for family in groups:
    recipe=read(f'wardrobe-{family}-recipe.json');report=read(f'manifests/wardrobe-{family}-character-probe.json')
    shutil.copyfile(ROOT/f'generated/wardrobe-{family}-sorcerer.glb',out/(family+'.glb'))
    for raw,slot in mapping.items():
        identity=f'wardrobe.{family}.{slot}';part=next(p for p in recipe['parts'] if p['slot']==raw)
        a=copy.deepcopy(next(a for a in catalog['Appearances'] if a['Id']=='level1.'+slot))
        a.update(Id=identity,Scene=f'res://Assets/Equipment/Empire/{family}.glb',MeshNames=[m['name'] for m in report['meshes'] if m['slot']==raw])
        assert a['MeshNames'];a['Compatibility']['BaseClasses']=groups[family];catalog['Appearances'].append(a)
        item=copy.deepcopy(next(i for i in catalog['Items'] if i['Id']=='level1.'+slot))
        source=part['sourceAppearance']
        item.update(Id=identity,DisplayName=f'{names[family]} Heroic {slot.title()}',Appearance=identity,CosmeticOnly=True,
            Icon=icon(identity,part['iconSource']),Source=dict(GomId=source['id'],Name=source['name'],Evidence='Client-derived April appearance -> registry model/material/palettes and exact icon; local collectible rule'))
        catalog['Items'].append(item)
    for wave,slot in enumerate(['feet','hands','waist','legs','chest'],6):
        unlocks['Milestones'].append(dict(Id=f'empire.heroic.{family}.{slot}',DisplayName=f'Reach wave {wave} as {names[family]}',Kind='Wave',Value=wave,BaseClasses=[earning_classes[family]],Appearances=[f'wardrobe.{family}.{slot}']))

# Restrict existing armor appearances to their Empire class pair too.
for a in catalog['Appearances']:
    family=next((f for f in groups if a['Id'].startswith('d3.'+f+'.')),None)
    if a['Id'].startswith(('level1.','ss_a04.')):family='inq'
    if family:a['Compatibility']['BaseClasses']=groups[family]

# Recover missing starter armor icons; do not reuse the Inquisitor slot artwork.
for family in ['war','spy','bh']:
    for part in read(f'd3-{family}-recipe.json')['parts']:
        if part['slot'] not in mapping:continue
        identity=f"d3.{family}.{mapping[part['slot']]}"
        item=next(i for i in catalog['Items'] if i['Id']==identity)
        item['Icon']=icon(identity,'/resources/gfx/icons/'+part['sourceAppearance']['name']+'.dds')

old=read('character-recipe.json')
for d in read('manifests/empire-wardrobe-ss-icons.json'):
    fields={c['key']:c['value'] for c in d['children']}
    slot=d['identity']['name'].split('.')[-1].split('_')[0]
    raw=next(r for r,s in mapping.items() if s==slot)
    part=next(p for p in old['parts'] if p['slot']==raw)
    assert part['modelId']==fields['appAppearanceSlotModelID']['signed64']
    assert part['materialId']==fields['appAppearanceSlotMaterialIndex']['signed64']
    next(i for i in catalog['Items'] if i['Appearance']=='ss_a04.'+slot)['Icon']=icon('ss_a04.'+slot,'/resources/gfx/icons/'+d['identity']['name']+'.dds')

for p,data in [('godot/Gameplay/Equipment/catalog.json',catalog),('godot/Gameplay/Equipment/cosmetic-unlocks.json',unlocks)]:
    (ROOT/p).write_text(json.dumps(data,indent=2))
(ROOT/'manifests/empire-wardrobe-staging.json').write_text(json.dumps(dict(evidence='Client-derived',newPieces=20,icons=records,limitations=['Human male body type 2, existing rig only','Portable material bake omits emissive glow; original maps retained'],sourceManifest='empire-wardrobe-source.json'),indent=2))
print('Registered 20 collectible armor pieces and',len(records),'exact gear icons')
