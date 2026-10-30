"""Preserve source terrain tiling without rebaking or replacing scene geometry.

Run after the existing terrain converters. Uses their UV scale convention and
retains their baked normal/roughness maps. Existing operation detail stays intact.
"""
import json
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'godot/Assets'
OUT = ASSETS / 'TerrainClarity'
SOURCE = ROOT / 'source-assets/april2012'


def build():
    OUT.mkdir(exist_ok=True)
    registry = {}
    inputs = [('korriban', ROOT/'work/korriban/selection.json', ASSETS/'Korriban'),
              ('korriban', ROOT/'work/d4/environment/selection.json', ASSETS/'Korriban/D4')]
    inputs += [(m, ROOT/f'work/maps/{m}/selection.json', ASSETS/f'Maps/{m}')
               for m in ('hutta', 'dromund', 'balmorra')]
    for map_id, selection, asset_dir in inputs:
        s = json.loads(selection.read_text())
        e = json.loads((asset_dir/'environment.json').read_text())
        for patch in s['terrain']:
            p = next(p for p in e['terrain'] if p['id'] == patch['id'])
            used = [k for k, v in patch['weights'].items() if max(v) > 0 and k in s['terrainLayers']]
            w, h = patch['splatWidth'], patch['splatDepth']
            weights = np.array([patch['weights'][k] for k in used], dtype=np.float32).reshape(len(used), h, w)
            gap = weights.sum(axis=0) < 5.1
            weights[np.argmax(weights.sum(axis=(1, 2)))][gap] = 255
            weights /= np.maximum(weights.sum(axis=0), 1e-8)
            prefix = map_id+'_'+patch['id']
            lines = ['shader_type spatial;', 'render_mode cull_disabled;',
                     'varying vec3 source_pos;',
                     'void vertex(){source_pos=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;}']
            textures = []

            def texture(name, path, hints):
                lines.append(f'uniform sampler2D {name} : {hints};')
                textures.append((name, path))

            for i in range(0, len(used), 4):
                packed = np.zeros((h, w, 4), dtype=np.uint8)
                for j in range(min(4, len(used)-i)):
                    packed[:, :, j] = (weights[i+j]*255+.5).astype(np.uint8)
                name = f'{prefix}_weights{i//4}.png'
                Image.fromarray(packed).save(OUT/name)
                # Alpha is a fourth data channel, not transparency. Border fixing
                # would overwrite valid RGB weights wherever that layer is zero.
                imp = (OUT/name).with_suffix('.png.import')
                if imp.exists():
                    imp.write_text(imp.read_text().replace('process/fix_alpha_border=true', 'process/fix_alpha_border=false'))
                else:
                    imp.write_text('[remap]\nimporter="texture"\ntype="CompressedTexture2D"\n\n[params]\ncompress/mode=0\nmipmaps/generate=false\nprocess/fix_alpha_border=false\n')
                texture(f'weights{i//4}', 'res://Assets/TerrainClarity/'+name, 'filter_linear, repeat_disable')
            terms, sums = [], []
            for i, k in enumerate(used):
                layer = s['terrainLayers'][k]
                material = layer.get('material')
                source = material['textures']['DiffuseMap'] if material else layer['texture']
                name = Path(source).stem+'.png'
                Image.open(SOURCE/source.lstrip('/')).convert('RGBA').save(OUT/name)
                texture(f'layer{i}', 'res://Assets/TerrainClarity/'+name,
                        'source_color, filter_linear_mipmap_anisotropic, repeat_enable')
                scale = (material['values'].get('UVScaling', material['values'].get('UvScaling', '2,2')) if material else '2,2')
                scale = ','.join(str(float(x)) for x in scale.split(','))
                weight = f'texture(weights{i//4},UV).{"rgba"[i%4]}'
                sums.append(weight)
                terms.append(f'texture(layer{i},source_pos.xz*vec2({scale})).rgb*{weight}')
            tint = asset_dir/f'{patch["id"]}_source_color.png'
            tint_expr = ''
            if tint.exists():
                texture('tint', 'res://'+tint.relative_to(ROOT/'godot').as_posix(), 'source_color, filter_linear, repeat_disable')
                tint_expr = '*texture(tint,UV).rgb'
            texture('normal_map', p['maps']['normal'], 'hint_normal, filter_linear_mipmap_anisotropic, repeat_disable')
            texture('roughness_map', p['maps']['roughness'], 'filter_linear_mipmap_anisotropic, repeat_disable')
            # Renormalize after 8-bit packing to avoid faint seams/brightness steps.
            lines.append('void fragment(){ALBEDO=('+ '+'.join(terms)+')/max('+ '+'.join(sums)+',0.001)'+tint_expr+
                         ';NORMAL_MAP=texture(normal_map,UV).rgb;ROUGHNESS=texture(roughness_map,UV).r;SPECULAR=0.15;}')
            assert len(textures) <= 16, (prefix, len(textures))
            shader = prefix+'.gdshader'
            (OUT/shader).write_text('\n'.join(lines)+'\n')
            text = [f'[gd_resource type="ShaderMaterial" load_steps={len(textures)+2} format=3]',
                    f'[ext_resource type="Shader" path="res://Assets/TerrainClarity/{shader}" id="shader"]']
            text += [f'[ext_resource type="Texture2D" path="{path}" id="t{i}"]' for i, (_, path) in enumerate(textures)]
            text += ['[resource]', 'shader = ExtResource("shader")']
            text += [f'shader_parameter/{name} = ExtResource("t{i}")' for i, (name, _) in enumerate(textures)]
            (OUT/(prefix+'.tres')).write_text('\n'.join(text)+'\n')
            registry.setdefault(map_id, {})[patch['id']] = 'res://Assets/TerrainClarity/'+prefix+'.tres'
    (OUT/'materials.json').write_text(json.dumps(registry, indent=2)+'\n')
    # Mipmaps are essential for anisotropic filtering and stable oblique terrain.
    for png in OUT.glob('*.png'):
        if '_weights' in png.name:
            continue
        imp = png.with_suffix('.png.import')
        if imp.exists():
            imp.write_text(imp.read_text().replace('mipmaps/generate=false', 'mipmaps/generate=true'))
        else:
            imp.write_text('[remap]\nimporter="texture"\ntype="CompressedTexture2D"\n\n[params]\ncompress/mode=0\nmipmaps/generate=true\nprocess/size_limit=0\n')
    print('Terrain clarity:', {m: len(p) for m, p in registry.items()})


if __name__ == '__main__':
    build()
