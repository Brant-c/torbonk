"""Two-resource April reticle recovery using the existing read-only SelectedSource path."""
import json
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT, SOURCE, sha
from lightning_source import decode_xml
from prepare_abilities import unpack

MANIFEST = ROOT / 'manifests/storm-reticle-source.json'
FX = '/resources/art/fx/fxspec/abilities/all/all_ui_targeting_reticle_green.fxspec'

def main():
    character_source.MANIFEST = MANIFEST
    if not MANIFEST.exists():
        MANIFEST.write_text(json.dumps(dict(profile='April2012 (operator labelled)',
            maximumFiles=2, maximumDecodedBytes=1024*1024, resources=[], dependencies=[],
            unresolved=[], selection='Original green player targeting projector for Force Storm')))
    src = character_source.SelectedSource()
    raw = src.get(FX, 'Force Storm player area indicator', 'Original green targeting FXSpec')
    xml = decode_xml(raw)
    fx = unpack(xml.find('.//marshalData/node'))
    projector = fx['_fxProjectorList'][0]
    texture = '/resources/' + projector['_fxProjectionTexture'].replace('\\', '/').lstrip('/').lower()
    src.get(texture, FX, '_fxProjectionTexture')
    output = ROOT / 'godot/Assets/Abilities' / (Path(texture).stem + '.png')
    image = Image.open(SOURCE / texture.lstrip('/')).convert('RGBA')
    image.save(output)
    # Find the bright outer ridge in the original texture, without resampling its pixels.
    import math
    w, h = image.size
    sums, counts = [0.] * (w//2), [0] * (w//2)
    for y in range(h):
        for x in range(w):
            radius = int(math.hypot(x-(w-1)/2, y-(h-1)/2))
            if radius < w//2:
                sums[radius] += max(image.getpixel((x,y))[:3])
                counts[radius] += 1
    ridge = max(range(w//4, w//2), key=lambda r: sums[r]/max(1, counts[r]))
    config = dict(fxSpec=FX, projector=projector, texture=texture,
        ringRidgeFraction=(ridge+0.5)/(w/2), textureSize=[w,h],
        conversion=dict(output=str(output.relative_to(ROOT)), sha256=sha(output), resampled=False))
    (ROOT/'godot/Assets/Abilities/storm-reticle.json').write_text(json.dumps(config, indent=2))
    (ROOT/'work/abilities/all_ui_targeting_reticle_green.fxspec.xml').write_text(
        __import__('xml.etree.ElementTree',fromlist=['tostring']).tostring(xml,encoding='unicode'),encoding='utf-8')
    print(json.dumps(dict(resources=2,texture=texture,size=image.size,ridgeFraction=config['ringRidgeFraction'],color=projector['_fxProjectionColor'])))

if __name__ == '__main__':
    main()
