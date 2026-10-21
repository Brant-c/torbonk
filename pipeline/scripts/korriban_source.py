"""Bounded A4 extraction; all archive access uses the proven read-only reader."""
import json
import character_source
from source_pipeline import ROOT, SOURCE

AREA='4611686022825732973'
MANIFEST=ROOT/'manifests/korriban-source.json'

def source():
    character_source.MANIFEST=MANIFEST
    if not MANIFEST.exists():
        MANIFEST.write_text(json.dumps(dict(profile='April2012 (operator labelled)',
            maximumFiles=300,maximumDecodedBytes=160*1024*1024,resources=[],
            dependencies=[],unresolved=[],selection='bounded Korriban outdoor crop')))
    return character_source.SelectedSource()

def local(path): return SOURCE/path.lstrip('/')

if __name__=='__main__':
    src=source()
    for room in ['area','baseroom']:
        p=f'/resources/world/areas/{AREA}/{room}.dat'
        data=src.get(p,'Korriban verified investigation','area/room original data')
        print(room,len(data),data[:500].decode())
