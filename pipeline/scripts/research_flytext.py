"""Bounded April flytext resource research. Archives stay read-only; no archive hashing.
Only decode explicit font/config dependencies. Historical movie names are lookup candidates.
"""
import json, re, struct, sys
from pathlib import Path
import character_source
from source_pipeline import ROOT, pathhash, sha
from ui_formats import tags, font_to_ttf

sys.path.insert(0, str(ROOT/'tools/python-ui'))

character_source.MANIFEST = ROOT / 'manifests/flytext-source.json'
src = character_source.SelectedSource()
src.manifest.update(selection='Bounded April flytext font/config research', maximumFiles=4,
    maximumDecodedBytes=2*1024*1024, unresolved=[
        'Exact native flytext font-face binding has not been recovered.',
        'Nineteen historical/production ui_flytext movie candidate lookups have no April entry match.',
        'Original outline/shadow filter and native renderer easing remain unresolved.'])
names = Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt')
candidates = [p.strip() for p in names.read_text().splitlines() if 'ui_flytext.' in p.lower()]
candidates += ['/resources/gfx/gfx_production/ui_flytext.gfx']
result = {'movieLookups': [], 'fontBundles': [], 'config': []}
for p in candidates:
    matches = src.index.get(f'0x{pathhash(p):016X}', [])
    result['movieLookups'].append({'path': p, 'matches': matches,
        'evidence': 'historical filename candidate; exact April entry-table lookup only'})
for p in ['/resources/gfx/drawtext_fonts.swf', '/resources/gfx/fonts/swtor_fonts.gfx']:
    blob = src.get(p, 'flytext research', 'original embedded font candidates; role must be verified separately')
    fonts = []
    for tag, data in tags(blob):
        if tag in (48, 75):
            fonts.append({'tag': tag, 'id': struct.unpack_from('<H', data)[0],
                'name': data[5:5+data[4]].rstrip(b'\0').decode(), 'flags': data[2]})
            if p == '/resources/gfx/drawtext_fonts.swf' and fonts[-1]['id'] == 4:
                dest = ROOT/'godot/Assets/UI/Flytext/EurostileNextLTPro.ttf'
                dest.parent.mkdir(parents=True, exist_ok=True)
                result['fontConversion'] = dict(source=p, fontId=4,
                    output=str(dest.relative_to(ROOT)), **font_to_ttf(data, dest),
                    roleEvidence='Original draw-text font candidate; exact flytext face binding unresolved',
                    sha256=sha(dest))
    result['fontBundles'].append({'path': p, 'fonts': fonts})
for p in ['/resources/guixml/defaults.xml', '/resources/guixml/stylesetgui.xml']:
    blob = src.get(p, 'flytext research', 'GUI definitions inspected for font-role binding')
    result['config'].append({'path': p, 'fontOrFlytextLines': [s for s in blob.decode().splitlines()
        if re.search(r'font|flytext', s, re.I)]})
(ROOT/'manifests/flytext-assets.json').write_text(json.dumps(result, indent=2))
character_source.MANIFEST.write_text(json.dumps(src.manifest, indent=2))
print(json.dumps(result, indent=2))
