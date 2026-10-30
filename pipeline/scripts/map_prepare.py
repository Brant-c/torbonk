"""Select terrain patches and placements from one room, resolve exact models/materials/maps."""
import json, struct, xml.etree.ElementTree as ET
from maps_common import recipe, source, work, records, sections
from map_terrain_decode import decode


def canonical(p): return '/resources/' + p.replace('\\', '/').lower().lstrip('/')
def vec(f, key, default): return [float(v) for v in f.get(key, default).strip('()').split(',')]


def main():
    R = recipe(); src = source(); AREA = R['areaId']
    sec = sections(src.get(f'/resources/world/areas/{AREA}/area.dat', 'area', 'area data').decode('utf-8', 'replace'))
    recs = records(src.get(f"/resources/world/areas/{AREA}/{R['room']}.dat", 'area', 'room data').decode('utf-8', 'replace'))
    assets = {l.strip().split('=', 1)[0]: canonical(l.strip().split('=', 1)[1]) for l in sec['ASSETS'] if '=' in l}
    x0, x1, z0, z1 = R['patchCenterWindowXZ']
    terrain = []
    for rec in recs.values():
        f = rec['fields']
        if 'VertexData' not in f: continue
        p = vec(f, 'Position', '(0,0,0)')
        if not (x0 <= p[0] <= x1 and z0 <= p[2] <= z1): continue
        assert f.get('ParentInstance', '0') == '0' and vec(f, 'Rotation', '(0,0,0)') == [0, 0, 0] and vec(f, 'Scale', '(1,1,1)') == [1, 1, 1], rec['id']
        data = decode(rec)
        if data['embeddedPosition'] is not None:
            # Copied source patches can retain their original embedded origin.
            # Vertices are local; the room placement remains authoritative.
            data['embeddedPositionDelta'] = [a - b for a, b in zip(data['embeddedPosition'], p)]
            assert R.get('allowCopiedTerrainOrigins', False) or max(abs(v) for v in data['embeddedPositionDelta']) < 1e-8
        terrain.append(dict(id=rec['id'], asset=rec['asset'], room=R['room'], sourceLine=rec['line'],
                            tessellation=f.get('tesselation', ''), position=p, **data))
    assert terrain, 'no terrain patches in window'
    minx = min(t['position'][0] + t['vertices'][0][0] for t in terrain)
    maxx = max(t['position'][0] + t['vertices'][-1][0] for t in terrain)
    minz = min(t['position'][2] + t['vertices'][0][2] for t in terrain)
    maxz = max(t['position'][2] + t['vertices'][-1][2] for t in terrain)
    m = R['propMarginXZ']; ylo, yhi = R['propYRange']
    candidates = []
    def visible_model(rec):
        path = assets.get(rec['asset'], '')
        return (path.endswith('.gr2') and not any(s in path for s in
                ['/dbo_', 'occluder', 'fadeportal', 'collision', 'blocker', 'trigger'])
                and rec['fields'].get('Hidden', 'false') == 'false')
    for rec in recs.values():
        path = assets.get(rec['asset'], '')
        if not path.endswith('.gr2'): continue
        f = rec['fields']; p = vec(f, 'Position', '(0,0,0)')
        if R.get('includeRoomArchitecture', False):
            if visible_model(rec) and '/art/static/area/' in path: candidates.append(rec['id'])
            continue
        if f.get('ParentInstance', '0') == '0' and minx - m <= p[0] <= maxx + m and minz - m <= p[2] <= maxz + m and ylo <= p[1] <= yhi:
            if any(s in path for s in ['/dbo_', 'occluder', 'fadeportal', 'collision', 'blocker', 'trigger']): continue
            if f.get('Hidden', 'false') == 'false': candidates.append(rec['id'])
    selected = set(candidates)
    for _ in range(len(recs)):
        add = set() if R.get('includeRoomArchitecture', False) else {r['id'] for r in recs.values() if r['fields'].get('ParentInstance', '0') in selected}
        add |= {recs[i]['fields']['ParentInstance'] for i in selected if recs[i]['fields'].get('ParentInstance', '0') != '0'}
        if add <= selected: break
        assert add <= recs.keys(), 'parent outside selected room'
        selected |= add
    placements = []
    for i in sorted(selected):
        rec = recs[i]; f = rec['fields']
        placements.append(dict(id=i, asset=rec['asset'], path=assets.get(rec['asset'], ''), room=R['room'], sourceLine=rec['line'],
                               parent=f.get('ParentInstance', '0'), position=vec(f, 'Position', '(0,0,0)'),
                               rotation=vec(f, 'Rotation', '(0,0,0)'), scale=vec(f, 'Scale', '(1,1,1)'), fields=f))
    # Utility parents carry transforms only; never render their helper meshes.
    if R.get('includeRoomArchitecture', False):
        for p in placements:
            if not visible_model(recs[p['id']]): p['path'] = ''
    modelpaths = sorted({p['path'] for p in placements if p['path'].endswith('.gr2') and p['fields'].get('Hidden', 'false') == 'false'})
    assert len(modelpaths) <= R['maximumUniqueModels'] and len(placements) <= R['maximumPlacements'], (len(modelpaths), len(placements))
    materials = {}

    def material(path, parent):
        if path in materials: return materials[path]
        xml = ET.fromstring(src.get(path, parent, 'source material dependency'))
        textures = {}; values = {}
        for inp in xml.findall('.//input'):
            sem = inp.findtext('semantic'); val = inp.findtext('value')
            if inp.findtext('type', '').lower() == 'texture' and (val or '').startswith('#'):
                values[sem + '__builtin'] = val
            elif inp.findtext('type', '').lower() == 'texture':
                tex = canonical(val)
                if not tex.endswith('.dds'): tex += '.dds'
                src.get(tex, path, sem); textures[sem] = tex
            else: values[sem] = val
        info = dict(path=path, derived=xml.findtext('Derived'), alpha=xml.findtext('AlphaMode'),
                    twoSided=xml.findtext('IsTwoSided'), textures=textures, values=values)
        materials[path] = info; return info

    models = []
    for path in modelpaths:
        b = src.get(path, 'room placements', 'ASSETS exact model path')
        assert b[:4] == b'GAWB' and struct.unpack_from('<I', b, 4)[0] == 4
        count = struct.unpack_from('<H', b, 26)[0]; offset = struct.unpack_from('<I', b, 88)[0]
        names = []
        for i in range(count):
            off = struct.unpack_from('<I', b, offset + 4 * i)[0]
            name = b[off:b.index(b'\0', off)].decode('ascii')
            mat = '/resources/art/shaders/materials/' + name + '.mat'
            material(mat, path); names.append(mat)
        models.append(dict(path=path, materials=names))
    layers = {}
    used = sorted({int(k) for p in terrain for k, v in p['weights'].items() if max(v) > 0})
    for line in sec['TERRAINTEXTURES']:
        i, raw = line.strip().split(':', 1)
        if int(i) not in used: continue
        if raw.startswith('bwa:'):
            mat = '/resources/art/shaders/materials/' + raw.split(':')[1] + '.mat'
            layers[i] = dict(source=raw, material=material(mat, 'area terrain layer ' + i))
        else:
            tex = canonical(raw.split(':')[0]); src.get(tex, 'area terrain layer ' + i, 'direct terrain diffuse')
            layers[i] = dict(source=raw, texture=tex)
    out = dict(area=AREA, areaLabel=R['areaLabel'], room=R['room'], selection=R['section'],
               patchIds=[t['id'] for t in terrain], footprintXZ=[minx, maxx, minz, maxz],
               terrain=terrain, placements=placements, models=models, materials=materials, terrainLayers=layers,
               transformPolicy='raw Y-up, source units 1:1; local T * Euler ZXY * S; compose complete ancestry')
    (work() / 'selection.json').write_text(json.dumps(out))
    print('SELECTION', R['id'], len(terrain), 'patches', len(placements), 'placements', len(models), 'models', len(materials), 'materials')
    print('FAMILIES', sorted({m['derived'] for m in materials.values()}))
    print('FOOTPRINT', [round(v, 2) for v in out['footprintXZ']])
    for t in terrain: print('PATCH', t['id'], t['position'], t['width'], t['depth'], 'layers', sorted(t['weights']))


if __name__ == '__main__': main()
