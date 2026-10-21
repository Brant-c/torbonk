"""Pillow DDS decode + literal FXSpec parameters, no invented art."""
import sys, json, re, hashlib
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'scripts'))
from lightning_source import decode_xml
from source_pipeline import sha

source=ROOT/'source-assets/april2012/resources'
out=ROOT/'godot/Assets/Lightning'; out.mkdir(parents=True,exist_ok=True)
texture_records=[]
for name in ['512_singlearcb_bw','electric_rings_bw_02']:
    original=source/f'art/fx/texture/electricity/{name}.dds'
    image=Image.open(original); image.save(out/(name+'.png'))
    texture_records.append({'source':str(original.relative_to(ROOT)),'sourceSha256':sha(original),'output':str((out/(name+'.png')).relative_to(ROOT)),
                            'sha256':sha(out/(name+'.png')),'dimensions':list(image.size)})
def unpack(element):
    if len(element)==0: return element.text or ''
    if all(c.tag=='e' for c in element): return [unpack(c) for c in element]
    return {c.get('name',c.tag):unpack(c) for c in element}
config={}
for label,name in [('caster','si_force_lightning_caster'),('body','si_lightningdrain_bodyarcs')]:
    original=source/f'art/fx/fxspec/abilities/sith_inquisitor/{name}.fxspec'
    node=decode_xml(original.read_bytes()).find('.//marshalData/node')
    config[label]=unpack(node)
config['particles']={}
for name in ['si_lightningdrain_bodyarcs','si_lightningdrain_bodyarcs_p']:
    config['particles'][name]=json.loads((ROOT/f'work/lightning/{name}.prt.json').read_text())
config['adapter']={'fxScale':0.1,'status':'Reconstruction calibration; source FX coordinate unit relationship not native-client verified',
    'curveInterpolation':'Linear between recovered keys; native S/I/O spline semantics unresolved',
    'startFxSeconds':0.35,'startFxTiming':'Reconstruction cue; original note keys known, timestamps unavailable',
    'lightFadeSeconds':0.3,'audioStopSeconds':0.5}
config['textures']=texture_records
(out/'visuals.json').write_text(json.dumps(config,indent=2))
(ROOT/'manifests/lightning-visual-conversion.json').write_text(json.dumps(config,indent=2))
print(json.dumps(texture_records,indent=2))
