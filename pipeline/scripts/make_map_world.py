"""Write Gameplay/GameWorld_<Map>.tscn from the Korriban world template and the map's source footprint."""
import json, sys, re
from source_pipeline import ROOT

NAMES = {'hutta': 'Hutta', 'dromund': 'Dromund', 'balmorra': 'Balmorra', 'eternity_vault': 'EternityVault'}

def main(ids):
    template = (ROOT/'godot/Gameplay/GameWorld.tscn').read_text(encoding='utf-8')
    for m in ids:
        if m == 'eternity_vault':
            sel = json.loads((ROOT/f'work/maps/{m}/selection.json').read_text())
            r = json.loads((ROOT/f'maps/{m}.json').read_text())
            x0,x1,z0,z1 = sel['footprintXZ']; x,y,z = r['spawnSourceXYZ']
            t = (ROOT/'godot/Gameplay/GameWorld_Balmorra.tscn').read_text()
            t = t.replace('Maps/balmorra/', f'Maps/{m}/').replace('script = ExtResource("1")', 'script = ExtResource("1")\nPlanetRunPath = "res://Gameplay/Planets/EternityVault/run.json"')
            t = t.replace('script = ExtResource("3")','script = ExtResource("3")\nGroundCollisionMask = 3')
            for key,value in [('Minimum',f'Vector2({x0}, {z0})'),('Maximum',f'Vector2({x1}, {z1})'),('SpawnSeed',f'Vector2({x}, {z})')]:
                t = re.sub(key+r' = Vector2\([^\n]+\)',key+' = '+value,t)
            t = re.sub(r'position = Vector3\([^\n]+\)\nscript = ExtResource\("4"\)',f'position = Vector3({x}, {y}, {z})\nscript = ExtResource("4")',t)
            (ROOT/f'godot/Gameplay/GameWorld_{NAMES[m]}.tscn').write_text(t,encoding='utf-8')
            continue
        sel = json.loads((ROOT/f'work/maps/{m}/selection.json').read_text())
        x0, x1, z0, z1 = sel['footprintXZ']
        cx, cz = round((x0+x1)/2, 3), round((z0+z1)/2, 3)
        t = template.replace('res://Assets/Korriban/KorribanEnvironment.tscn', f'res://Assets/Maps/{m}/MapEnvironment.tscn')
        t = t.replace('[node name="KorribanEnvironment" parent="."', '[node name="MapEnvironment" parent="."')
        t = t.replace('[node name="ArenaBounds" type="Node3D" parent="."]\nscript = ExtResource("3")\n',
            f'[node name="ArenaBounds" type="Node3D" parent="."]\nscript = ExtResource("3")\nMinimum = Vector2({x0:.3f}, {z0:.3f})\nMaximum = Vector2({x1:.3f}, {z1:.3f})\nSpawnSeed = Vector2({cx}, {cz})\n')
        t = t.replace('position = Vector3(36, -17.13, 12.6)', f'position = Vector3({cx}, 0, {cz})')
        assert 'MapEnvironment.tscn' in t and 'SpawnSeed' in t and f'Vector3({cx}, 0, {cz})' in t
        (ROOT/f'godot/Gameplay/GameWorld_{NAMES[m]}.tscn').write_text(t, encoding='utf-8', newline='\n')
        print(m, 'world written; footprint', sel['footprintXZ'])

if __name__ == '__main__': main(sys.argv[1:] or list(NAMES))
