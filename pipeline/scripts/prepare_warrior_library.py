"""April Warrior resource library. No gameplay writes; exact hash extraction only."""
import json,re,hashlib
from pathlib import Path
from collections import defaultdict,deque
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE,sha
from lightning_source import decode_xml
from prepare_abilities import unpack

OUT=ROOT/'godot/Assets/WarriorLibrary'
def save(p,v):
    p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2),encoding='utf-8')
def main():
    OUT.mkdir(parents=True,exist_ok=True)
    defs=json.loads((ROOT/'manifests/warrior-definitions.json').read_text())
    nodes=json.loads((ROOT/'manifests/warrior-appearance-tree.json').read_text())+json.loads((ROOT/'manifests/warrior-effect-tree.json').read_text())
    byname={d['name']:d for d in defs};byid={d['id']:d for d in defs}
    leaves=defaultdict(list)
    for n in nodes:
        for c in n.get('children',[]):
            if not c['expandable']:leaves[n['identity']['name']].append(dict(path=c['path'],key=c['key'],value=c['value']))
    character_source.MANIFEST=ROOT/'manifests/warrior-library-source.json'
    src=character_source.SelectedSource();src.manifest.update(maximumFiles=5000,maximumDecodedBytes=1024*1024*1024,selection='Complete April Sith Warrior packages, talents and appearance dependency closure')
    missing=[];configs={};fx={};converted=[];edges=defaultdict(set);clips={};params=defaultdict(list)
    queue=deque()
    def request(path,parent,relation):
        path=path.lower().replace('\\','/').replace('//','/')
        if not path.startswith('/resources/'):path='/resources/'+path.lstrip('/')
        edges[parent].add(path);queue.append((path,parent,relation))
    for d in defs:request(d['source']['path'],d['name'],'Original April definition container')
    for name,ls in leaves.items():
        for c in ls:
            v=c['value']
            if not isinstance(v,str) or not v:continue
            if c['key'] in ('ablIconSpec','effIcon'):request('gfx/icons/'+v+'.dds',name,c['path'])
            if '/effAppStringParams/' in c['path']:
                params[name].append(c)
                if c['key'].endswith(':21') and not v.isdigit():request('art/fx/fxspec/'+v.strip('/').removesuffix('.fxspec')+'.fxspec',name,c['path'])
                if c['key'].endswith((':11',':12')) and re.match(r'^(cb_|fp_|em_|nw_)',v):
                    request('anim/humanoid/bmnnew/'+v+'.jba',name,c['path']);clips[v]=v
    # Preserve authored action providers for semantic and weapon-dependent animation selection.
    filenames=Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').read_text().splitlines()
    providers=[p.lower() for p in filenames if '/anim/humanoid/bmnnew/humanoid_action' in p.lower() and p.lower().endswith(('.mph','.amx')) and any(s in p.lower() for s in ('saber','common','force'))]
    for p in sorted(set(providers)):request(p,'animation-providers','Original action graph; semantic selection requires graph evaluation')
    for p in sorted(set(filenames)):
        p=p.lower()
        if '/anim/humanoid/bmnnew/' in p and p.endswith('.jba') and re.match(r'cb_(?:2saber|saber|warrior)_',Path(p).stem):
            request(p,'weapon-animation-library','Source-confirmed single/dual saber clip; no inferred ability assignment');clips[Path(p).stem]=Path(p).stem
    seen=set()
    while queue:
        path,parent,relation=queue.popleft()
        if path in seen:continue
        seen.add(path)
        if not src.has(path):missing.append(dict(source=parent,path=path,relation=relation,status='No unique April archive entry'));continue
        data=src.get(path,parent,relation)
        try:
            if path.endswith('.dds'):
                dest=OUT/'Textures'/Path(path.removeprefix('/resources/')).with_suffix('.png');dest.parent.mkdir(parents=True,exist_ok=True)
                Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(dest)
                converted.append(dict(source=path,output=str(dest.relative_to(ROOT)),sha256=sha(dest)));continue
            if path.endswith(('.bkt','.node','.gr2','.jba','.mph','.amx')):continue
            if path.endswith('.prt'):fields=re.findall(r'^\s*\.(\w+)=(.*)$',data.decode().replace('\r',''),re.M)
            else:
                xml=decode_xml(data);fields=[(f.get('name',f.tag),f.text or '') for f in xml.iter() if len(f)==0]
                if path.endswith('.mat'):fields += [(n.findtext('semantic'),(n.findtext('value') or '').replace('\\','/')+'.dds') for n in xml.findall('input') if n.findtext('type')=='texture']
                if path.endswith('.fxspec'):fx[path]=unpack(xml.find('.//marshalData/node'))
            configs[path]=fields
            for field,value in fields:
                value=value.strip().strip('"').replace('\\','/').lower()
                if value.endswith(('.dds','.gr2','.prt','.mat','.fxspec','.lit','.zzp','.jba')):
                    if '/' not in value:
                        value=('art/fx/particles/' if value.endswith('.prt') else 'art/fx/fxspec/' if value.endswith('.fxspec') else 'anim/humanoid/bmnnew/' if value.endswith('.jba') else '')+value
                    request(value,path,field)
        except Exception as e:missing.append(dict(source=parent,path=path,status='Decode/conversion partial',error=str(e)))
    save(OUT/'source-parameters.json',configs);save(OUT/'fx.json',fx)
    save(OUT/'appearance-parameters.json',params)
    # Package lists supply eligibility; names alone never promote unused records to player roster.
    packages={d['name']:{x['key'] for x in next((f['value'] for f in d['data'] if f['key']=='ablPackageAbilitiesList'),[]) if isinstance(x,dict)} for d in defs if d['name'].startswith('pkg.')}
    catalog=[]
    for d in defs:
        name=d['name']
        if not name.startswith('abl.') or '/' in name:continue
        fields={f['key']:f['value'] for f in d['data']};members=[p for p,ids in packages.items() if d['id'] in ids]
        eligibility=set()
        for p in members:
            if '.juggernaut.' in p:eligibility.add('Juggernaut')
            elif '.marauder.' in p:eligibility.add('Marauder')
            else:eligibility.update(['Juggernaut','Marauder'])
        related=[x['name'] for x in defs if x['name'].startswith(name+'/')]
        appearances={fields.get('ablAppearanceSpec','')}
        for rel in related:
            for c in leaves[rel]:
                v=c['value']
                if isinstance(v,str) and v.startswith('epp.'):appearances.add(v)
                if isinstance(v,dict):
                    target=v.get('definition',{}).get('name','')
                    if target.startswith('epp.'):appearances.add(target)
        appearances.discard('')
        deps=set();pending=list(appearances)+[name]+related
        for parent in pending:deps.update(edges[parent])
        work=list(deps)
        while work:
            for p in edges[work.pop()]:
                if p not in deps:deps.add(p);work.append(p)
        recovered={r['path'] for r in src.manifest['resources']}
        classification='passive' if fields.get('ablIsPassive') or fields.get('ablPassive') else 'active'
        if any(s in name for s in ('retaliation','vicious_throw','savage_kick','pommel_strike')):classification='conditional'
        if any(s in name for s in ('ward','defense','endure_pain','invincible','cloak_of_pain','undying','guard','unleash')):classification='defensive'
        if any(s in name for s in ('force_charge','obliterate','intercede','predation')):classification='movement'
        catalog.append(dict(name=name,id=d['id'],eligibility=sorted(eligibility),packages=members,classification=classification,classificationEvidence='Decoded passive flag where available; functional grouping is editorial',source=d['source'],icon=fields.get('ablIconSpec'),effects=related,appearances=sorted(appearances),dependencies=sorted(deps & recovered),missing=sorted(deps-recovered),rosterStatus='Package member' if members else 'Archive record; player availability not established',fields=fields))
    save(OUT/'catalog.json',catalog);save(OUT/'talent-packages.json',[d for d in defs if d['name'].startswith('pkg.')])
    save(ROOT/'manifests/warrior-library-conversion.json',dict(clips={k:v for k,v in clips.items() if src.has('/resources/anim/humanoid/bmnnew/'+v+'.jba')},files=converted,missing=missing,edges={k:sorted(v) for k,v in edges.items()}))
    src.manifest['unresolved']=missing;save(character_source.MANIFEST,src.manifest)
    print(json.dumps(dict(definitions=len(defs),catalog=len(catalog),packageMembers=sum(bool(c['packages']) for c in catalog),resources=len(src.manifest['resources']),textures=len(converted),clips=len(clips),missing=len(missing))))
if __name__=='__main__':main()
