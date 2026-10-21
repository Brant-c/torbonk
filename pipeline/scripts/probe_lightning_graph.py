import xml.etree.ElementTree as E
from source_pipeline import ROOT
r=E.parse(ROOT/'work/lightning/bank.xml')
items=r.find('.//list[@name="listLoadedItem"]')
for index in [61,64,65,79,80,81,82,905,911,926,927]:
    ob=items[index]
    print('\nOBJECT',index,ob.get('name'))
    if index in [64,79,81,911,926]:
        print(E.tostring(ob.find('.//object[@name="NodeInitialParams"]'),encoding='unicode'))
from probe_lightning_audio import fnv
from pathlib import Path
ids={2409073383,3535516216,1799371716,426962037}
with Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').open() as f:
    for line in f:
        p=line.strip()
        if '/bnk2/' in p and p.endswith('.bnk') and fnv(Path(p).stem) in ids: print('MEDIA BANK',fnv(Path(p).stem),p)
