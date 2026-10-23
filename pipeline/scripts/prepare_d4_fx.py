"""Recover the original Korriban torch and its two emitter/particle dependency pairs."""
import json,re,xml.etree.ElementTree as ET
from PIL import Image
from d4_source import source,canon
from source_pipeline import ROOT,SOURCE
from lightning_source import decode_xml

def main():
    src=source();fx='/resources/art/fx/fxspec/env_kor_torch.fxspec';root=decode_xml(src.get(fx,'D4 burial courts','original Korriban torch'))
    records={fx:ET.tostring(root,encoding='unicode')};pending=[];textures=[]
    for f in root.iter('f'):
        if f.get('name')=='_fxResourceName' and f.text:
            p=canon(f.text) if '/' in f.text or '\\' in f.text else '/resources/art/fx/particles/'+f.text
            pending.append(p)
    while pending:
        path=pending.pop()
        if path in records:continue
        text=src.get(path,fx,'original torch particle dependency').decode();records[path]=text
        for field in ['EmitSpec','TextureName']:
            m=re.search(r'\.'+field+r'=([^\r\n]+)',text)
            if not m:continue
            p=canon(m[1])
            if field=='EmitSpec':pending.append(p)
            else:
                src.get(p,path,'source particle texture');textures.append(p)
    out=ROOT/'godot/Assets/Korriban/D4'
    for p in textures:
        Image.open(SOURCE/p.lstrip('/')).save(out/( __import__('pathlib').Path(p).stem+'.png'))
    (ROOT/'manifests/d4-environment-fx.json').write_text(json.dumps(dict(root=fx,parameters=records,textures=textures,
        adaptation='Godot billboard flame atlas and thin smoke. Source emission/motion curves retained here, not native simulation parity.'),indent=2))

if __name__=='__main__':main()
