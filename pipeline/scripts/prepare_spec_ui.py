"""Nine original role/spec icons from the installed SWTOR v6 archive, kept separate from April gameplay provenance."""
import json,struct,sys
from pathlib import Path
from PIL import Image
from source_pipeline import ROOT,pathhash,sha

ARCHIVE=Path('C:/Program Files (x86)/Electronic Arts/BioWare/Star Wars - The Old Republic/Assets/swtor_main_gfx_assets_1.tor')
RECEIPT=ROOT/'manifests/spec-ui-installed-source.json'
ICONS={'role_dps':'symbols/icon_dps','role_heal':'symbols/icon_heal','role_tank':'symbols/icon_tank',
       'lightning':'icons/sorc_lightning','madness':'icons/sorc_madness','corruption':'icons/sorc_corruption',
       'darkness':'icons/ass_darkness','deception':'icons/ass_deception','hatred':'icons/ass_hatred'}

def index():
    wanted={pathhash('/resources/gfx/'+v+'.dds'):(k,'/resources/gfx/'+v+'.dds') for k,v in ICONS.items()};records=[]
    with ARCHIVE.open('rb') as f:
        magic,version,endian,table,cap,n=struct.unpack('<IIIQII',f.read(28));assert magic==0x50594d and version==6
        visited=set()
        while table:
            assert table not in visited;visited.add(table);f.seek(table);count,following=struct.unpack('<IQ',f.read(12))
            for i in range(count):
                offset,adjust,packed,size,h,crc,compression=struct.unpack('<QIIIQIH',f.read(34))
                if h not in wanted:continue
                assert 0<size<1024*1024 and compression in (0,1)
                name,path=wanted.pop(h);source=ROOT/'source-assets/installed-swtor'/path.lstrip('/');source.parent.mkdir(parents=True,exist_ok=True)
                pos=f.tell();f.seek(offset+adjust);body=f.read(packed);f.seek(pos);source.with_suffix('.packed').write_bytes(body)
                records.append(dict(name=name,path=path,archiveHash=f'0x{h:016X}',dataOffset=offset+adjust,compressedSize=packed,length=size,compression=compression,packedPath=str(source.with_suffix('.packed')),sourcePath=str(source)))
            table=following
    assert not wanted,wanted
    RECEIPT.write_text(json.dumps(dict(profile='Installed SWTOR client UI artwork; not April2012 gameplay data',archive=str(ARCHIVE),archiveBytes=ARCHIVE.stat().st_size,archiveMtimeNs=str(ARCHIVE.stat().st_mtime_ns),resources=records),indent=2))

def convert():
    receipt=json.loads(RECEIPT.read_text());out=ROOT/'godot/Assets/UI/specs';out.mkdir(parents=True,exist_ok=True)
    for row in receipt['resources']:
        source=Path(row['sourcePath']);assert source.stat().st_size==row['length'];image=Image.open(source).convert('RGBA');dest=out/(row['name']+'.png');image.save(dest)
        row.update(sourceSha256=sha(source),output=str(dest.relative_to(ROOT)),outputSha256=sha(dest),dimensions=list(image.size))
    receipt['conversion']='Original DDS to native-resolution RGBA PNG; no redraw or recolor'
    RECEIPT.write_text(json.dumps(receipt,indent=2));print('Recovered nine original SWTOR role/spec icons.')

if __name__=='__main__':
    index() if sys.argv[1]=='index' else convert()
