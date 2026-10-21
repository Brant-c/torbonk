"""Bounded April TOR extraction. Python stdlib only; archives opened rb.

Index/header layout independently implemented from the local extracTOR reader.
lookup3 is Bob Jenkins' public-domain algorithm. No bulk payload decoding.
"""
import argparse, hashlib, json, struct, zlib
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
RECIPE = json.loads((ROOT/'recipe.json').read_text())
ARCHIVES = Path(RECIPE['archiveRoot'])
EVIDENCE = ROOT.parent / 'docs/investigation/asset-evidence-2026-10-20.json'
DOOR = RECIPE['model']
SOURCE = ROOT / 'source-assets/april2012'

def sha(path):
    with open(path, 'rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()

def save(name, obj):
    (ROOT / 'manifests' / name).write_text(json.dumps(obj, indent=2), encoding='utf-8')

def pathhash(path):
    data = path.lower().encode('utf-8'); mask = 0xffffffff
    def rot(x,n): return ((x << n) | (x >> (32-n))) & mask
    a = b = c = (0xdeadbeef + len(data)) & mask
    i = 0
    while len(data)-i > 12:
        aa,bb,cc = struct.unpack_from('<III',data,i)
        a=(a+aa)&mask; b=(b+bb)&mask; c=(c+cc)&mask
        a=((a-c)&mask)^rot(c,4); c=(c+b)&mask
        b=((b-a)&mask)^rot(a,6); a=(a+c)&mask
        c=((c-b)&mask)^rot(b,8); b=(b+a)&mask
        a=((a-c)&mask)^rot(c,16); c=(c+b)&mask
        b=((b-a)&mask)^rot(a,19); a=(a+c)&mask
        c=((c-b)&mask)^rot(b,4); b=(b+a)&mask
        i+=12
    rem=data[i:]
    if rem:
        a=(a+int.from_bytes(rem[:4],'little'))&mask
        b=(b+int.from_bytes(rem[4:8],'little'))&mask
        c=(c+int.from_bytes(rem[8:12],'little'))&mask
        c=((c^b)-rot(b,14))&mask; a=((a^c)-rot(c,11))&mask
        b=((b^a)-rot(a,25))&mask; c=((c^b)-rot(b,16))&mask
        a=((a^c)-rot(c,4))&mask; b=((b^a)-rot(a,14))&mask
        c=((c^b)-rot(b,24))&mask
    return (b<<32)|c

def entries(path):
    with path.open('rb') as f:
        magic,version,endian,table,capacity,count=struct.unpack('<IIIQII',f.read(28))
        assert magic == 0x0050594d and version == 5, (path,hex(magic),version)
        visited=set()
        while table:
            assert table not in visited; visited.add(table)
            f.seek(table); n,following=struct.unpack('<IQ',f.read(12))
            assert n < 10000000
            for j in range(n):
                record=f.read(34)
                offset,adjust,packed,size,h,crc,compression=struct.unpack('<QIIIQIH',record)
                if offset == 0: continue
                yield {'archive':str(path), 'archiveHash':f'0x{h:016X}',
                       'entryOffset':offset, 'headerSize':adjust, 'dataOffset':offset+adjust,
                       'compressedSize':packed, 'length':size, 'compressionMethod':compression,
                       'crc32Index':f'{crc:08x}'}
            table=following

def inventory():
    if (ROOT/'manifests/source-inventory.json').exists():
        print('Preserving existing baseline; verify stage checks every archive hash.',flush=True)
        return
    records=[]
    for p in sorted(ARCHIVES.glob('*.tor')):
        s=p.stat()
        records.append({'path':str(p),'length':s.st_size,'mtimeNs':str(s.st_mtime_ns), 'sha256':sha(p)})
    save('source-inventory.json', {'profile':'April2012 (operator labelled; not retail authenticated)',
        'archives':records,'count':len(records),'totalBytes':sum(x['length'] for x in records),
        'evidenceSha256':sha(EVIDENCE), 'writableOutputRoot':str(ROOT),
        'nameMap':{'path':'D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt',
                   'usage':'not used for selected exact dependency paths; newer names are not presence evidence'}})
    print('Inventoried',len(records),'archives',flush=True)

def extract():
    evidence=json.loads(EVIDENCE.read_text(encoding='utf-8-sig'))
    verified=next(x['result'] for x in evidence['resources'] if x['query']==DOOR)
    assert pathhash(DOOR)==int(verified['archiveHash'],16), 'path hash calibration failed'
    index={}
    for archive in sorted(ARCHIVES.glob('*.tor')):
        for e in entries(archive): index.setdefault(e['archiveHash'],[]).append(e)
    outputs=[]; edges=[]
    def get(path, parent, relation, expected=None):
        path=path.lower().replace('\\','/')
        assert path.startswith('/resources/') and '..' not in path.split('/')
        h=f'0x{pathhash(path):016X}'; matches=index.get(h,[])
        assert len(matches)==1, (path, 'missing or ambiguous variants',matches)
        e=dict(matches[0]); assert e['length']<=16*1024*1024
        if not any(x['path']==path for x in outputs):
            assert len(outputs)<RECIPE['maximumExtractedFiles'], 'extraction file budget exceeded'
            assert sum(x['length'] for x in outputs)+e['length']<=RECIPE['maximumDecodedBytes'], 'extraction byte budget exceeded'
        with open(e['archive'],'rb') as f:
            f.seek(e['dataOffset']); payload=f.read(e['compressedSize'])
        if e['compressionMethod']==1:
            decoder=zlib.decompressobj(); data=decoder.decompress(payload,e['length']+1)
            assert decoder.eof and not decoder.unconsumed_tail
        else:
            assert e['compressionMethod']==0; data=payload
        digest=hashlib.sha256(data).hexdigest()
        assert len(data)==e['length']
        if expected:
            for field in ('archiveHash','entryOffset','length'):
                assert e[field]==expected[field], (field,e[field],expected[field])
            assert digest==expected['sha256']
        out=SOURCE/path.lstrip('/')
        assert out.resolve().is_relative_to(SOURCE.resolve())
        out.parent.mkdir(parents=True,exist_ok=True)
        if out.exists(): assert sha(out)==digest, 'refuse to overwrite changed original'
        else: out.write_bytes(data)
        e.update(path=path,sha256=digest,output=str(out.relative_to(ROOT)),
                 lookupEvidence='exact canonical path → lookup3 archive hash; unique April entry')
        if not any(x['path']==path for x in outputs): outputs.append(e)
        edges.append({'from':parent,'to':path,'relation':relation})
        return data
    data=get(DOOR,'investigation evidence','verified static prop',verified)
    assert data[:4]==b'GAWB' and struct.unpack_from('<I',data,4)[0]==4
    count=struct.unpack_from('<H',data,26)[0]; namesoff=struct.unpack_from('<I',data,88)[0]
    names=[]
    for i in range(count):
        offset=struct.unpack_from('<I',data,namesoff+i*4)[0]
        name=data[offset:data.index(b'\0',offset)].decode('ascii'); names.append(name)
        path=f'/resources/art/shaders/materials/{name}.mat'
        material=get(path,DOOR,'GR2 material table')
        xml=ET.fromstring(material)
        for inp in xml.findall('.//input') + xml.findall('.//Input'):
            if (inp.findtext('type') or inp.findtext('Type') or '').lower()=='texture':
                value=inp.findtext('value') or inp.findtext('Value')
                tex='/resources/'+value.replace('\\','/').lstrip('/')
                if not tex.lower().endswith('.dds'): tex+='.dds'
                get(tex,path,inp.findtext('semantic') or inp.findtext('Semantic') or 'texture')
    save('door-source.json', {'profile':'April2012','model':DOOR,'materialNames':names,
                             'resources':outputs,'dependencies':edges,'unresolved':[]})
    print(json.dumps({'materialNames':names,'files':len(outputs),'decodedBytes':sum(x['length'] for x in outputs)},indent=2))

def verify():
    before=json.loads((ROOT/'manifests/source-inventory.json').read_text())
    changes=[]
    for rec in before['archives']:
        p=Path(rec['path']); s=p.stat()
        if s.st_size!=rec['length'] or str(s.st_mtime_ns)!=rec['mtimeNs'] or sha(p)!=rec['sha256']:
            changes.append(str(p))
    originals=json.loads((ROOT/'manifests/door-source.json').read_text())['resources']
    for rec in originals:
        if sha(ROOT/rec['output'])!=rec['sha256']: changes.append(rec['output'])
    assert len(list(ARCHIVES.glob('*.tor')))==before['count']
    save('source-integrity.json',{'checkedArchives':before['count'],'wholeArchiveSha256Rechecked':True,
                                'checkedExtractedOriginals':len(originals),'changes':changes,'passed':not changes})
    assert not changes, changes
    print('Source integrity passed',flush=True)

if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('stage',choices=['inventory','extract','verify'])
    globals()[parser.parse_args().stage]()
