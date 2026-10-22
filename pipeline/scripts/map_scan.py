"""Read-only survey of April area/room DATs: terrain patches and prop density per room."""
import json, sys, zlib, pickle, re
from collections import Counter
from source_pipeline import ROOT, ARCHIVES, entries, pathhash

CACHE = ROOT/'work/maps/index.pkl'
OUT = ROOT/'work/maps/scan'

def index():
    if CACHE.exists(): return pickle.loads(CACHE.read_bytes())
    idx = {}
    for a in sorted(ARCHIVES.glob('*.tor')):
        for e in entries(a): idx.setdefault(e['archiveHash'], []).append(e)
    CACHE.write_bytes(pickle.dumps(idx)); return idx

def read(idx, path):
    m = idx.get(f'0x{pathhash(path):016X}', [])
    if len(m) != 1: return None
    e = m[0]
    with open(e['archive'], 'rb') as f:
        f.seek(e['dataOffset']); p = f.read(e['compressedSize'])
    return zlib.decompress(p) if e['compressionMethod'] == 1 else p

def main(areas):
    idx = index(); OUT.mkdir(parents=True, exist_ok=True)
    for name, aid in areas.items():
        raw = read(idx, f'/resources/world/areas/{aid}/area.dat')
        (OUT/f'{name}-area.dat').write_bytes(raw); text = raw.decode('utf-8', 'replace')
        rooms = []; sec = None
        for l in text.splitlines():
            if l.startswith('['): sec = l.strip('[]')
            elif sec == 'ROOMS' and l.strip(): rooms.append(l.strip())
        print('==', name, aid, len(rooms), 'rooms')
        for r in rooms:
            b = read(idx, f'/resources/world/areas/{aid}/{r}.dat')
            if b is None: continue
            (OUT/f'{name}-{r}.dat').write_bytes(b)
            t = b.decode('utf-8', 'replace')
            terr = t.count('.VertexData='); recs = len(re.findall(r'^  \d+=', t, re.M))
            if terr: print(f'  {r}: {len(b)//1024} KiB, {recs} records, {terr} terrain patches')

if __name__ == '__main__':
    main(dict(hutta='4611686019802841877', dromund='4611686019618382666', balmorra='4611686022220993772'))
