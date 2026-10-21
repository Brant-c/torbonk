import json,subprocess,sys,xml.etree.ElementTree as ET
import character_source
from source_pipeline import ROOT,SOURCE
from probe_lightning_audio import fnv
character_source.MANIFEST=ROOT/'manifests/d3-presentation-source.json';s=character_source.SelectedSource()
p='/resources/bnk2/weapons_data.bnk';s.get(p,'D3 saber sounds','original weapon event bank')
subprocess.run([sys.executable,str(ROOT/'tools/wwiser.pyz'),str(SOURCE/p.lstrip('/')),'-d','xml','-dn',str(ROOT/'work/d3/weapons')],check=True,stdout=subprocess.DEVNULL)
# Inspect only metadata strings from the original weapon definitions/FX roots.
from lightning_source import decode_xml
for stem in ['audio_weapon_melee_swingright_normal','audio_weapon_melee_swingleft_normal','abilities/blasterfire/dynamic_standard_blaster_muzzle_flash']:
    p='/resources/art/fx/fxspec/'+stem+'.fxspec'
    if s.has(p):
        raw=s.get(p,'D3 weapon audio','source appearance weapon FX');root=decode_xml(raw)
        (ROOT/f'work/d3/{stem.split("/")[-1]}.xml').write_text(ET.tostring(root,encoding='unicode'))
        print(stem,[n.text for n in root.iter() if n.text and ('Play_' in n.text or 'weapon' in n.text.lower())][:18])
tree=ET.parse(ROOT/'work/d3/weapons.xml')
events={f.get('value') for ob in tree.findall('.//object[@name="CAkEvent"]') for f in ob.findall('field[@name="ulID"]')}
names=__import__('pathlib').Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').read_text().splitlines()
# Event names are often retained in the bank's STID name table.
print('STID',[(f.get('name'),f.get('value')) for f in tree.iter('field') if any(t in (f.get('value') or '').lower() for t in ['saber','swing','rifle'])][:40])
print('EVENT COUNT',len(events))
