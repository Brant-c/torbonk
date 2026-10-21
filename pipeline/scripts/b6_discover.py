import sys,json,xml.etree.ElementTree as ET
from pathlib import Path
from character_source import SelectedSource,ROOT
import character_source
character_source.MANIFEST=ROOT/'manifests/b6-source.json'
s=SelectedSource();s.manifest['maximumFiles']=300;s.manifest['maximumDecodedBytes']=160*1024*1024
for slot in ['creature','weapon']:
 p=f'/resources/art/dynamic/{slot}/index.xml'
 if s.has(p):
  root=ET.fromstring(s.get(p))
  for a in root:
   if any(x in ET.tostring(a,encoding='unicode').lower() for x in ['klorslug','dragonbat','electroblade.low03','lightsaber']):
    print(slot,ET.tostring(a,encoding='unicode')[:3800])
for p in ['/resources/art/dynamic/spec/klorslug.spc','/resources/art/dynamic/spec/dragonbat.spc']:
 print(p,s.has(p))
 if s.has(p):print(s.get(p).decode(errors='replace')[:8000])
