"""Retired engine-default candidate research, not the active gold SWTOR cursor.

Use prepare_b9_native_cursor.py for the active cursor. Archives remain read-only.
"""
import json
from PIL import Image
import character_source
from source_pipeline import ROOT, SOURCE, sha

character_source.MANIFEST = ROOT / 'manifests/b9-source.json'
src = character_source.SelectedSource()
src.manifest.update(selection='B9 rejected cyan engine-default candidate and definitions', maximumFiles=4,
                    maximumDecodedBytes=64 * 1024)
for path in ['/resources/art/defaultassets/cursor.dds', '/resources/art/defaultassets/cursor.tex',
             '/resources/guixml/_cursors.xml']:
    src.get(path, 'B9 cursor investigation', 'exact cursor resource; inspect bytes and artwork')
out = ROOT / 'godot/Assets/UI/Cursor'
out.mkdir(parents=True, exist_ok=True)
image = Image.open(SOURCE / 'resources/art/defaultassets/cursor.dds').convert('RGBA')
image.save(out / 'default.png')
(ROOT / 'manifests/b9-cursor-conversion.json').write_text(json.dumps(dict(
    source='/resources/art/defaultassets/cursor.dds', output='godot/Assets/UI/Cursor/default.png',
    status='Retired engine-default candidate; NOT the active SWTOR gold cursor. See b9-native-cursor.json.',
    sha256=sha(out / 'default.png'), dimensions=list(image.size), alphaBounds=image.getbbox(),
    conversion='DDS to RGBA PNG; no crop, recolor or scaling', hotspot=[5, 5],
    hotspotEvidence='Adapted to visible arrow tip; original hotspot not recovered',
    missingVariants=['hover/interactable', 'pressed', 'disabled', 'other contextual/animated cursors'],
    fallback='Not used by the current cursor theme.'), indent=2))
print('Recorded retired engine-default research. Active gold cursor uses prepare_b9_native_cursor.py.')
