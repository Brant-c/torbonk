"""Resolve one registry-backed bmn Sorcerer appearance; no guessed asset fallback."""
import json, xml.etree.ElementTree as ET
from character_source import SelectedSource, ROOT

src=SelectedSource()
def canonical(p):
    return '/resources/'+p.replace('\\','/').lstrip('/').replace('[bt]','bmn').replace('[gen]','m')
def registry(slot, identity):
    root=src.registry(slot)
    found=[a for a in root if a.findtext('ID')==identity or a.findtext('ArtName')==identity]
    assert len(found)==1,(slot,identity)
    a=found[0]; assert 'bmn' in [b.text for b in a.findall('Bodytypes/Bodytype')]
    return a
def material(path, parent):
    path=canonical(path) if not path.startswith('/resources/') else path
    xml=ET.fromstring(src.get(path,parent,'material assignment'))
    textures={}; values={}
    for inp in xml.findall('input'):
        semantic=inp.findtext('semantic'); value=inp.findtext('value') or ''
        if inp.findtext('type')=='texture' and value.strip('/'):
            tex=canonical(value)
            if not tex.endswith('.dds'):tex+='.dds'
            src.get(tex,path,semantic); textures[semantic]=tex
        else: values[semantic]=value
    return {'path':path,'derived':xml.findtext('Derived'),'textures':textures,'values':values}

scheme=registry('colorscheme','1002880')
hues={}
for key in ['1000768','1000760']:
    a=registry('garmenthue',key); p=canonical(a.findtext('BaseFile'))
    hues[key]={'id':key,'path':p,'xml':src.get(p,'colorscheme:1002880','primary/secondary palette').decode()}
selection={
 'profile':'April2012','bodyType':'bmn','skeleton':'/resources/art/dynamic/spec/bmnnew_skeleton.gr2',
 'outfit':'ss_a04 c01 s01; original modular Sorcerer robe, skirt, gauntlets, boots and belt',
 'coordinatePolicy':'scale 1; GR2 +90 X applied; glTF Y up; JBA original root rotation retained',
 'colorScheme':{'id':'1002880','xml':ET.tostring(scheme,encoding='unicode'),'hues':hues},
 'parts':[], 'customization':{}, 'animations':[],
 'excluded':{'head:1000247':'bma, incompatible; use bmn 1000257',
             'fullNakedBody':'body replacement uses equipped modular meshes; no hidden duplicate naked torso',
             'weapon':'unarmed inspection clips; no weapon selected',
             'faceHairAndFacePaint':'clean shaven, no optional tattoo/scar/paint slot',
             'clothSimulation':'original weighted cloth only; no runtime CLO simulation'}}
for slot,modelid,matid in [('head','1000257','1583105'),('hair','1000620','1027364'),
 ('chest','1142760','1143685'),('leg','1142831','1118482'),('hand','1142609','1118481'),
 ('boot','1142516','1118479'),('waist','1142643','1118475')]:
    a=registry(slot,modelid); mats=[m for m in a.findall('Materials/Material') if m.get('id')==matid]
    assert len(mats)==1; m=mats[0]
    part={'slot':slot,'modelId':modelid,'materialId':matid,'artName':a.findtext('ArtName'),
          'registryPath':f'/resources/art/dynamic/{slot}/index.xml','registryXml':ET.tostring(a,encoding='unicode'),
          'models':[], 'skinMaterialIndex':int(a.findtext('SkinMaterialIndex')),
          'material':material(m.get('filename'),f'{slot}:{modelid}:{matid}'),'overrides':[]}
    for path,ident in [(a.findtext('BaseFile'),modelid)]+[(x.get('filename'),x.get('id')) for x in a.findall('Attachments/Attachment')]:
        p=canonical(path); src.get(p,f'{slot}:{modelid}','base model' if ident==modelid else 'registry attachment:'+ident)
        part['models'].append({'path':p,'id':ident})
    for over in m.findall('MaterialOverrides/MaterialOverride'):
        part['overrides'].append({'index':int(over.get('index')),'material':material(over.get('filename'),f'{slot}:{modelid}:override')})
    selection['parts'].append(part)
for slot,identity in [('skincolor','1437794'),('eyecolor','1001086'),('haircolor','1000998'),('complexion','1001008')]:
    a=registry(slot,identity); path=canonical(a.findtext('BaseFile'))
    selection['customization'][slot]={'id':identity,'path':path}
    if src.has(path):
        data=src.get(path,f'{slot}:{identity}','customization palette or texture')
        if path.endswith('.xml'): selection['customization'][slot]['xml']=data.decode()
    else:
        # These three registry XMLs were compiled into April GOM prototypes.
        # Preserve the read-only decoder evidence and verify its bucket bytes.
        evidence=json.loads((ROOT/'manifests/character-palette-evidence.json').read_text())
        ev=next(e for e in evidence if e['root']['identity']['name']=='dynamic.'+slot+'.'+a.findtext('ArtName'))
        p=ev['root']['identity']['source']['path']; data=src.get(p,f'{slot}:{identity}','compiled palette bucket')
        import hashlib
        assert hashlib.sha256(data).hexdigest()==ev['root']['identity']['source']['resourceSha256']
        values={c['key']:c['value'] for c in ev['root']['children'] if c['key'].startswith('appPalette') and not c['expandable']}
        for expanded in ev['expanded']:
            v={c['key']:c['value'] for c in expanded['children'] if c['key'] in ['r','g','b','a']}
            values[expanded['path'].lstrip('/')]=[v[k] for k in ['r','g','b','a']]
        selection['customization'][slot].update(resolution='compiled April GOM palette; registry XML absent',
             gomIdentity=ev['root']['identity'],values=values)
for p in [selection['skeleton'],'/resources/art/dynamic/spec/bmnnew.dat','/resources/art/dynamic/spec/bmnnew.dyc']:
    src.get(p,relation='original bmnnew rig specification')
selection['clothBoneSources']=[]
for p in ['/resources/art/dynamic/chest/model/chest_robe04_bmn_archetype.clo','/resources/art/dynamic/leg/model/leg_skirt_bmn_archetype.clo']:
    src.get(p,relation='original XML cloth bone hierarchy and rest transforms')
    selection['clothBoneSources'].append(p)
for label,name,loop in [('Idle','cb_unarmed_idle_right_1',True),('Run','mv_cb_unarmed_run_forward',True),
 ('LightningStart','fp_2h_elect_drain_start',False),('LightningLoop','fp_2h_elect_drain_loop',True),
 ('Hit','dg_unarmed_crit_damage_right_1',False),('Death','dg_death_1',False),('LightningEnd','fp_2h_elect_drain_end',False)]:
    p='/resources/anim/humanoid/bmnnew/'+name+'.jba'; src.get(p,relation='original selected JBA:'+label)
    selection['animations'].append({'name':label,'sourceName':name,'path':p,'loop':loop})
(ROOT/'character-recipe.json').write_text(json.dumps(selection,indent=2))
print('Selected',len(src.manifest['resources']),'files;',sum(r['length'] for r in src.manifest['resources']),'decoded bytes')
print('Material families',[(p['slot'],p['material']['derived']) for p in selection['parts']])
