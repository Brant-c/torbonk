"""Read-only bank discovery: metadata and bank HIRC only, no media extraction."""
import struct, json, zlib
from source_pipeline import ROOT, ARCHIVES, entries, pathhash

def fnv(text):
    h=2166136261
    for b in text.lower().encode(): h=((h*16777619)^b)&0xffffffff
    return h

EVENTS=['Play_ability_sithinquisitor_forcelightning_cast',
        'Play_ability_sithinquisitor_forcelightning_casterloop',
        'Stop_ability_sithinquisitor_forcelightning_casterloop',
        'Play_ability_sithinquisitor_shock_aftershock','Stop_ability_sithinquisitor_shock_aftershock']

def main():
    needles={fnv(s):s for s in EVENTS}; found=[]; banks=[]
    for archive in sorted(ARCHIVES.glob('*bnk*.tor')):
        for e in entries(archive):
            # Bank structures are small. Large entries are media, not bank graphs.
            if e['length']>4*1024*1024: continue
            with archive.open('rb') as f:
                f.seek(e['dataOffset']); raw=f.read(e['compressedSize'])
            data=zlib.decompress(raw) if e['compressionMethod']==1 else raw
            if data[:4]!=b'BKHD': continue
            version=struct.unpack_from('<I',data,8)[0]; bankid=struct.unpack_from('<I',data,12)[0]
            p=0; hits=[]; chunks=[]
            while p+8<=len(data):
                tag=data[p:p+4]; size=struct.unpack_from('<I',data,p+4)[0]
                chunks.append({'tag':tag.decode('ascii','replace'),'offset':p+8,'size':size})
                if tag==b'HIRC':
                    q=p+12
                    for _ in range(struct.unpack_from('<I',data,p+8)[0]):
                        typ=data[q]; n=struct.unpack_from('<I',data,q+1)[0]; oid=struct.unpack_from('<I',data,q+5)[0]
                        if typ==4 and oid in needles: hits.append({'event':needles[oid],'id':oid,'offset':q,'hex':data[q:q+5+n].hex()})
                        q+=5+n
                    assert q==p+8+size
                p+=8+size
            banks.append({'entry':e,'version':version,'bankId':bankid,'chunks':chunks})
            if hits:
                found.append({'entry':e,'bankId':bankid,'version':version,'events':hits})
                print(json.dumps(found[-1],indent=2),flush=True)
    out={'eventHashes':needles,'bankCount':len(banks),'banks':banks,'matches':found,
         'coverage':'Audio-named TORs; BKHD entries <=4MiB; no sound payload written. FNV-1 lowercase names matched typed HIRC Event IDs.'}
    (ROOT/'manifests/lightning-audio-discovery.json').write_text(json.dumps(out,indent=2))
    print('Banks',len(banks),'matched',len(found))

if __name__=='__main__': main()
