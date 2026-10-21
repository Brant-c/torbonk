"""Read selected native SWTOR PE cursor resources as data; never load/execute the client."""
import argparse
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CLIENT = Path('C:/Program Files (x86)/Electronic Arts/BioWare/Star Wars - The Old Republic/swtor/retailclient/swtor.exe')


def resources(data):
    assert data[:2] == b'MZ'
    pe = struct.unpack_from('<I', data, 60)[0]
    assert data[pe:pe+4] == b'PE\0\0'
    count = struct.unpack_from('<H', data, pe+6)[0]
    optional_size = struct.unpack_from('<H', data, pe+20)[0]
    optional = pe+24
    magic = struct.unpack_from('<H', data, optional)[0]
    assert magic in (0x10b, 0x20b)
    directories = optional + (96 if magic == 0x10b else 112)
    rva, size = struct.unpack_from('<II', data, directories+16)
    sections = [struct.unpack_from('<IIII', data, optional+optional_size+40*i+8) for i in range(count)]

    def offset(address):
        for virtual_size, virtual, raw_size, raw in sections:
            if virtual <= address < virtual+max(virtual_size, raw_size):
                return raw+address-virtual
        raise ValueError('Unmapped resource RVA')

    base = offset(rva)
    result = {}

    def walk(at=0, path=()):
        assert len(path) <= 3 and 0 <= at < size
        count = sum(struct.unpack_from('<HH', data, base+at+12))
        for i in range(count):
            name, target = struct.unpack_from('<II', data, base+at+16+8*i)
            if name & 0x80000000:
                start = base+(name & 0x7fffffff)
                length = struct.unpack_from('<H', data, start)[0]
                name = data[start+2:start+2+length*2].decode('utf-16-le')
            if target & 0x80000000:
                walk(target & 0x7fffffff, path+(name,))
            else:
                address, length, _, _ = struct.unpack_from('<IIII', data, base+target)
                at_data = offset(address)
                assert at_data+length <= len(data)
                result[path+(name,)] = (at_data, data[at_data:at_data+length])

    walk()
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--client', type=Path, default=DEFAULT_CLIENT)
    args = parser.parse_args()
    data = args.client.read_bytes()
    assert len(data) < 128*1024*1024
    found = resources(data)
    original = ROOT/'source-assets/native-client-cursors'
    output = ROOT/'godot/Assets/UI/Cursor'
    original.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    rows = []
    # Numeric PE identities are original; their UI roles below are an explicit Godot adaptation.
    for role, group_id in [('default', 201), ('hover', 204), ('disabled', 205)]:
        group_offset, group = found[(12, group_id, 1033)]
        assert struct.unpack_from('<HHH', group) == (0, 2, 1)
        cursor_id = struct.unpack_from('<H', group, 18)[0]
        cursor_offset, cursor = found[(1, cursor_id, 1033)]
        hotspot = struct.unpack_from('<HH', cursor)
        dib = cursor[4:]
        header, width, doubled_height, planes, bpp = struct.unpack_from('<IiiHH', dib)
        assert header == 40 and planes == 1 and bpp == 32 and doubled_height > 0
        height = doubled_height//2
        assert 0 < width <= 256 and 0 < height <= 256
        image = Image.frombytes('RGBA', (width, height), dib[header:header+width*height*4], 'raw', 'BGRA', 0, -1)
        # Preserve native alpha; these resources contain genuine 32-bit alpha pixels.
        assert image.getchannel('A').getextrema()[1] == 255
        png = output/f'gold_{role}.png'
        image.save(png)
        (original/f'group_{group_id}.bin').write_bytes(group)
        (original/f'cursor_{cursor_id}.bin').write_bytes(cursor)
        cur = struct.pack('<HHHBBBBHHII', 0, 2, 1, width % 256, height % 256, 0, 0,
                          *hotspot, len(dib), 22)+dib
        (original/f'{role}.cur').write_bytes(cur)
        rows.append(dict(role=role, language=1033, groupId=group_id, cursorId=cursor_id,
                         groupOffset=group_offset, groupSha256=hashlib.sha256(group).hexdigest(),
                         cursorOffset=cursor_offset, cursorBytes=len(cursor), cursorSha256=hashlib.sha256(cursor).hexdigest(),
                         dimensions=[width, height], hotspot=list(hotspot),
                         output=str(png.relative_to(ROOT)), sha256=hashlib.sha256(png.read_bytes()).hexdigest()))
    manifest = dict(source=str(args.client), sourceSha256=hashlib.sha256(data).hexdigest(), sourceBytes=len(data),
                    sourceKind='Installed SWTOR client PE resources, read-only; NOT April TOR archive provenance',
                    conversion='RT_CURSOR hotspot + original 32-bit BGRA DIB to RGBA PNG, no resize/crop/recolor',
                    binding='Gold arrow for default; gold hand for enabled buttons; blue hand for disabled buttons. Godot UI mapping is adapted.',
                    pressed='No separately identified original pressed artwork; retain the current hover/default cursor on press.',
                    resources=rows)
    (ROOT/'manifests/b9-native-cursor.json').write_text(json.dumps(manifest, indent=2))
    print(json.dumps(dict(cursors=[dict(role=r['role'], cursorId=r['cursorId'], hotspot=r['hotspot']) for r in rows])))


if __name__ == '__main__':
    main()
