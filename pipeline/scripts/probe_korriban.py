import re, json, zlib, struct, collections
from korriban_source import AREA, local
from source_pipeline import ROOT

def sections(text):
    result={}; section=None
    for line in text.splitlines():
        if line.startswith('['): section=line.strip('[]'); result[section]=[]
        elif section: result[section].append(line)
    return result

def records(text):
    result={}; current=None
    for i,line in enumerate(text.splitlines(),1):
        m=re.match(r'^  (\d+)=(.*)',line)
        if m:
            current=dict(id=m[1],asset=m[2],line=i,fields={}); result[m[1]]=current
        elif current and (m:=re.match(r'^\s+\.(\w+)=(.*)',line)):
            current['fields'][m[1]]=m[2]
    return result

def room(name='baseroom'):
    return records(local(f'/resources/world/areas/{AREA}/{name}.dat').read_text())

def area():
    return sections(local(f'/resources/world/areas/{AREA}/area.dat').read_text())

if __name__=='__main__':
    sec=area(); print('SECTIONS',[(k,len(v)) for k,v in sec.items()])
    print('ASSETS',sec['ASSETS'][:12]); print('TEX',sec.get('TERRAINTEXTURES'))
    recs=room(); print('RECORDS',len(recs))
    patches=[r for r in recs.values() if 'VertexData' in r['fields']]
    print('PATCHES',len(patches))
    for r in patches:
        f=r['fields']; raw=bytes.fromhex(f['VertexData'])
        if raw[:1]==b'x': raw=zlib.decompress(raw)
        (ROOT/'work/korriban'/f"{r['id']}.vertex.bin").write_bytes(raw)
        print(r['id'],f['Position'],f['Width'],f['Depth'],len(raw),'header',raw[:96].hex())
    assets={s.strip().split('=',1)[0]:s.strip().split('=',1)[1] for s in sec['ASSETS'] if '=' in s and not s.strip().startswith('.')}
    print('ARCH/ROCKS')
    for r in recs.values():
        path=assets.get(r['asset'],'')
        if any(s in path.lower() for s in ['tomb','acad','rock','statue']):
            print(r['id'],r['fields'].get('Position'),r['fields'].get('ParentInstance'),path)
