import struct

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
