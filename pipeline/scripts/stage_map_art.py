"""Extract Balmorra's original codex illustration and planet surface (same sources as the other planets)."""
import hashlib, json, sys
from io import BytesIO
from pathlib import Path
from PIL import Image
import map_scan as scan
from source_pipeline import ROOT, SOURCE

OUT = ROOT/'godot/Frontend/Assets/Recovered'
ITEMS = {'/resources/gfx/codex/cdx.planets.balmorra.dds': 'cdx.planets.balmorra.png',
         '/resources/art/planets/texture/planets_balmorra_d.dds': 'planets_balmorra_d.png'}

def main():
    idx = scan.index(); report = []
    for path, name in ITEMS.items():
        data = scan.read(idx, path); assert data
        src = SOURCE/path.lstrip('/'); src.parent.mkdir(parents=True, exist_ok=True); src.write_bytes(data)
        im = Image.open(BytesIO(data)); im.load()
        im.save(OUT/name)
        report.append(dict(path=path, sha256=hashlib.sha256(data).hexdigest(), output=f'godot/Frontend/Assets/Recovered/{name}', size=im.size, mode=im.mode))
        print(path, im.size, im.mode)
    (ROOT/'manifests/map-balmorra-art.json').write_text(json.dumps(report, indent=2))

if __name__ == '__main__': main()
