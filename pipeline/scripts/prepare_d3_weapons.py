import json,struct,xml.etree.ElementTree as ET
from pathlib import Path
import character_source
from source_pipeline import ROOT
character_source.MANIFEST=ROOT/'manifests/d3-source.json';s=character_source.SelectedSource()
names=Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').read_text().splitlines()
def canon(p):return '/resources/'+p.replace('\\','/').lstrip('/').removeprefix('resources/')
def mat(p):
    p=canon(p);x=ET.fromstring(s.get(p));t={}
    for a in x.findall('input'):
        if a.findtext('type')=='texture' and (a.findtext('value') or '').strip('/'):
            q=canon(a.findtext('value'));q+=' ' if False else '' if q.endswith('.dds') else '.dds';s.get(q,p,a.findtext('semantic'));t[a.findtext('semantic')]=q
    return dict(path=p,derived=x.findtext('Derived'),textures=t)
out={}
for name,tokens in [('pistol',['/weapon/model/blaster_low01_a01_v01.gr2']),('rifle',['/weapon/model/rifle_low01_a01_v01.gr2']),('sniper',['/weapon/model/rifle_sniperlow01_a01_v01.gr2','/weapon/model/sniper_low01_a01_v01.gr2']),('staff',['/weapon/model/saber_duallow01_a01_v01.gr2','/weapon/model/saber_doublelow01_a01_v01.gr2','/weapon/model/doublesaber_low01_a01_v01.gr2'])]:
    candidates=[p for p in names if any(t in p for t in tokens) and s.has(p)]
    if not candidates:
        candidates=[p for p in names if '/weapon/model/' in p and p.endswith('.gr2') and ('sniper' in p if name=='sniper' else any(t in p for t in ['dual','double','pole'])) and s.has(p)]
    print(name,candidates[:8]);assert candidates,name
    p=next((p for p in candidates if '/dualsaber_low01_' in p),candidates[0]);d=s.get(p,name,'original low weapon model; full starter inventory unverified');count=struct.unpack_from('<H',d,26)[0];off=struct.unpack_from('<I',d,88)[0];materials=[]
    for i in range(count):
        o=struct.unpack_from('<I',d,off+i*4)[0];stem=d[o:d.index(b'\0',o)].decode();materials.append(mat(f'/art/shaders/materials/{stem}.mat'))
    out[name]=dict(model=p,materials=materials)
(ROOT/'d3-weapons-recipe.json').write_text(json.dumps(dict(weapons=out),indent=2))
