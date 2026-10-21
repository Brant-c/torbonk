"""Read Scaleform GFX/SWF containers as data only. Never execute ActionScript."""
import struct, zlib

def tags(blob):
    if blob[:3] in (b'CFX', b'CWS'):
        blob = blob[:8] + zlib.decompress(blob[8:])
    assert blob[:3] in (b'CFX', b'CWS', b'GFX', b'FWS')
    assert len(blob) == struct.unpack_from('<I', blob, 4)[0]
    pos = 8 + (5 + (blob[8] >> 3) * 4 + 7) // 8 + 4
    while pos + 2 <= len(blob):
        h = struct.unpack_from('<H', blob, pos)[0]; pos += 2
        size = h & 63
        if size == 63:
            size = struct.unpack_from('<I', blob, pos)[0]; pos += 4
        assert pos + size <= len(blob)
        yield h >> 6, blob[pos:pos+size]
        pos += size

class Bits:
    def __init__(self, data): self.data, self.pos = data, 0
    def u(self, n):
        v = 0
        for _ in range(n):
            v = v * 2 + ((self.data[self.pos // 8] >> (7 - self.pos % 8)) & 1)
            self.pos += 1
        return v
    def s(self, n):
        v = self.u(n)
        return v - (1 << n) if n and v & (1 << (n-1)) else v

def font_to_ttf(data, output):
    """DefineFont3 quadratic outlines/layout → TrueType; scale 20480 → 1024 UPM."""
    from fontTools.fontBuilder import FontBuilder
    from fontTools.pens.ttGlyphPen import TTGlyphPen
    flags, namelen = data[2], data[4]
    name = data[5:5+namelen].rstrip(b'\0').decode('utf8')
    p = 5 + namelen
    count = struct.unpack_from('<H', data, p)[0]; p += 2
    fmt = 'I' if flags & 8 else 'H'; width = struct.calcsize(fmt)
    offsets = struct.unpack_from('<'+fmt*(count+1), data, p)
    codepos = p + offsets[-1]
    codes = struct.unpack_from('<' + ('H' if flags & 4 else 'B')*count, data, codepos)
    layout = codepos + count * (2 if flags & 4 else 1)
    assert flags & 128, 'font has no layout'
    asc, desc, leading = struct.unpack_from('<hhh', data, layout)
    advances = struct.unpack_from('<'+'h'*count, data, layout+6)
    glyphs, metrics, cmap = {}, {}, {}
    empty = TTGlyphPen(None); glyphs['.notdef'] = empty.glyph(); metrics['.notdef'] = (512,0)
    for i, code in enumerate(codes):
        pen = TTGlyphPen(None); b = Bits(data[p+offsets[i]:p+offsets[i+1]])
        fillbits, linebits = b.u(4), b.u(4)
        x=y=0; opened=False
        while True:
            if b.u(1):
                straight = b.u(1); n = b.u(4)+2
                if straight:
                    if b.u(1): dx,dy=b.s(n),b.s(n)
                    elif b.u(1): dx,dy=0,b.s(n)
                    else: dx,dy=b.s(n),0
                    x+=dx; y+=dy; pen.lineTo((round(x/20),round(-y/20)))
                else:
                    cx,cy=x+b.s(n),y+b.s(n); x,y=cx+b.s(n),cy+b.s(n)
                    pen.qCurveTo((round(cx/20),round(-cy/20)),(round(x/20),round(-y/20)))
            else:
                state=b.u(5)
                if not state: break
                assert not state & 16, 'new glyph styles unsupported'
                if state & 1:
                    if opened: pen.closePath()
                    n=b.u(5); x,y=b.s(n),b.s(n)
                    pen.moveTo((round(x/20),round(-y/20))); opened=True
                if state & 2: b.u(fillbits)
                if state & 4: b.u(fillbits)
                if state & 8: b.u(linebits)
        if opened: pen.closePath()
        key=f'uni{code:04X}'
        glyphs[key]=pen.glyph(); metrics[key]=(max(0,round(advances[i]/20)),0); cmap[code]=key
    fb=FontBuilder(1024,isTTF=True)
    fb.setupGlyphOrder(list(glyphs)); fb.setupCharacterMap(cmap); fb.setupGlyf(glyphs)
    # Preserve actual outline left bearings, rather than shifting each glyph to x=0.
    for key,g in glyphs.items():
        if g.numberOfContours: metrics[key]=(metrics[key][0],min(x for x,y in g.coordinates))
    fb.setupHorizontalMetrics(metrics)
    fb.setupHorizontalHeader(ascent=round(asc/20),descent=-abs(round(desc/20)),lineGap=round(leading/20))
    fb.setupNameTable({'familyName':name+' SWTOR Extract','styleName':'Regular','uniqueFontIdentifier':name+'-April2012','fullName':name+' SWTOR Extract','psName':name.replace(' ','')+'-SWTOR'})
    fb.setupOS2(sTypoAscender=round(asc/20),sTypoDescender=-abs(round(desc/20)),usWinAscent=round(asc/20),usWinDescent=abs(round(desc/20)))
    fb.setupPost(); fb.setupMaxp()
    from datetime import datetime
    fb.font['head'].created=fb.font['head'].modified=int((datetime(2012,4,10)-datetime(1904,1,1)).total_seconds())
    fb.font.recalcTimestamp=False
    fb.save(output)
    return {'embeddedName':name,'glyphCount':count,'unitsPerEm':1024,'conversion':'DefineFont3 outlines/advances divided by 20, Y inverted; no kerning conversion'}
