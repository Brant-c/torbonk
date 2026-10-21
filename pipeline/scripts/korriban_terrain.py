"""April embedded height grid adapter; STE2-compatible geometry, bounded inputs."""
import struct, zlib, math, hashlib

def decode(record):
    f=record['fields']; b=bytes.fromhex(f['VertexData']);embeddedSha=hashlib.sha256(b).hexdigest()
    if b[:2]==b'\x1f\x8b': b=zlib.decompress(b,31)
    elif b[:1]==b'x': b=zlib.decompress(b)
    assert len(b)<4*1024*1024
    decodedSha=hashlib.sha256(b).hexdigest();pos=0
    def read(fmt):
        nonlocal pos
        v=struct.unpack_from('<'+fmt,b,pos); pos+=struct.calcsize('<'+fmt)
        return v[0] if len(v)==1 else v
    flag=read('B'); w=read('I'); d=read('I')
    assert flag in (1,9),('unsupported terrain version',flag)
    assert w==int(float(f['Width'])) and d==int(float(f['Depth']))
    assert 2<=w<=128 and 2<=d<=128
    embeddedPosition=list(read('fff')) if flag&8 else None
    holes=read('B'); holemap=b[pos:pos+(w*d+7)//8] if holes else b''; pos+=len(holemap)
    height=[0.]*(w*d)
    for x in range(w): height[x],height[x+(d-1)*w]=read('ff')
    for z in range(1,d-1): height[z*w],height[z*w+w-1]=read('ff')
    for x in range(1,w-1):
        for z in range(1,d-1): height[x+z*w]=read('H')/512
    assert all(math.isfinite(h) for h in height)
    vertices=[[.2*(x-.5*(w-1))-(.1 if w%2==0 else 0),height[z*w+x],
               .2*(z-.5*(d-1))-(.1 if d%2==0 else 0)] for z in range(d) for x in range(w)]
    heightEnd=pos
    weights={}; legacy=False
    if flag&8:
        declared=read('B')
        for x in range(w):
            for z in range(d):
                for _ in range(read('B')):
                    idx,val=read('bB')
                    if idx>=0:
                        weights.setdefault(idx,[0]*(w*d))[z*w+x]=val
        # April sparse patches also preserve twice-resolution paint with padded rows.
        if pos+9<=len(b) and 1<=struct.unpack_from('<I',b,pos)[0]<=64 and struct.unpack_from('<I',b,pos+5)[0]==(2*d-1)*2*w:
            legacy=True; count=read('I'); weights={}
            for _ in range(count):
                idx=read('b'); length=read('I'); assert length==(2*d-1)*2*w
                data=b[pos:pos+length];pos+=length
                weights[idx]=[data[z*2*w+x] for z in range(2*d-1) for x in range(2*w-1)]
        sw,sd=(2*w-1,2*d-1) if legacy else (w,d)
        dyd=[]
        count=read('B')
        for _ in range(count):
            idx=read('B'); assert idx<=63
            density=b[pos:pos+w*d];pos+=w*d
            dyd.append(dict(id=idx,painted=sum(v>0 for v in density)))
        hasColor=read('B'); colors=[]
        if hasColor:
            colors=[[0,0,0] for _ in height]
            for x in range(w):
                for z in range(d):colors[z*w+x]=list(read('BBB'))
        trailing=b[pos:].hex()
    else:
        count=read('B'); ids=[read('b') for _ in range(count)]
        sw,sd=2*w-1,2*d-1
        # Dense splat samples are row-major, then texture (reader preview indexing).
        for idx in ids:
            if idx>=0:weights[idx]=[0]*(sw*sd)
        for z in range(sd):
            for x in range(sw):
                for idx in ids:
                    value=read('B')
                    if idx>=0:weights[idx][z*sw+x]=value
        colors=[];dyd=[]
        if read('B'):
            colors=[[0,0,0] for _ in height]
            for x in range(w):
                for z in range(d):colors[z*w+x]=list(read('BBB'))
            for _ in range(read('B')):
                idx=read('B'); assert idx<=63
                density=b[pos:pos+w*d];pos+=w*d
                dyd.append(dict(id=idx,painted=sum(v>0 for v in density)))
        trailing=b[pos:].hex()
    assert len(trailing)<=8,(record['id'],'unparsed tail',len(trailing)//2)
    return dict(width=w,depth=d,flag=flag,holes=holemap.hex(),vertices=vertices,
        embeddedSha256=embeddedSha,decodedSha256=decodedSha,
        heightEnd=heightEnd,rawLength=len(b),embeddedPosition=embeddedPosition,
        weights=weights,splatWidth=sw,splatDepth=sd,legacyDense=legacy,colors=colors,
        dyd=dyd,trailing=trailing)

if __name__=='__main__':
    from probe_korriban import room
    from source_pipeline import ROOT
    import json
    out=[]
    for r in room().values():
        if 'VertexData' not in r['fields']:continue
        a=decode(r)
        print(r['id'],a['width'],a['depth'],'weights',[(k,max(v)) for k,v in a['weights'].items()])
        out.append(dict(id=r['id'],position=r['fields']['Position'],**a))
    (ROOT/'work/korriban/terrain-probe.json').write_text(json.dumps(out))
