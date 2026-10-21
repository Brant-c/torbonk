"""UI-only resource views over preserved April atlases; existing converters stay unchanged."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'godot/Assets/UI/hud'
# Regions are native pixels, not resized/recolored replacement artwork.
REGIONS = {
    'cooldown_wipe': (10, [929, 308, 50, 100], 'bwaui_slot.gfx subimage 117 -> shape 118 -> sprite 119; mc_cooldown in sprite 140, shared by mc_cooldownStates and mc_gcdCooldownStates in ConversionIconButton (155)'),
    'player_slot': (1, [660, 126, 56, 56], 'bwaui_slot.gfx subimage 1; defaultFrame in sprite 9, imported by ConversionSlotQuickslot (156)'),
    'unit_frame': (15, [308, 414, 342, 108], 'Visual selection: joined portrait, level socket and two unit strips; GFX symbol binding unresolved'),
    'rarity_premium': (1, [329, 184, 54, 54], 'bwaui_slot.gfx subimage 63; original green outline, used cosmetically'),
    'rarity_prototype': (1, [0, 242, 54, 54], 'bwaui_slot.gfx subimage 72; original blue outline, used cosmetically'),
    'rarity_artifact': (1, [161, 184, 54, 54], 'bwaui_slot.gfx subimage 49; original purple outline, used cosmetically'),
}

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    for name, (atlas, region, evidence) in REGIONS.items():
        file = OUT / (name + '.tres')
        file.write_text('[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n'
            f'[ext_resource type="Texture2D" path="res://Assets/UI/atlases/bwa_uitexture_{atlas}.png" id="1"]\n\n'
            '[resource]\natlas = ExtResource("1")\n'
            f'region = Rect2({", ".join(map(str, region))})\nfilter_clip = true\n', encoding='utf-8')
        records.append(dict(name=name, source=f'/resources/gfx/gfx_production/bwa_uitexture_{atlas}.dds',
            regionXYWH=region, output=str(file.relative_to(ROOT)), evidence=evidence))
    (ROOT / 'manifests/ui-player-hud.json').write_text(json.dumps({
        'profile': 'operator-labelled April2012', 'resources': records,
        'sourceIdentities': 'ui-source.json; ui-conversion.json (existing native PNG atlases)',
        'quickbar': '12 default player slots; bwaui_quickslots_1/3 end controls and _6 page control; no companion module',
        'adaptation': 'Godot layout, horizontally repeated slots and thin connecting rules; not a reconstruction of the Scaleform timeline',
        'conversion': 'AtlasTexture regions only; no modified source pixels, archive hashes or converter changes',
    }, indent=2), encoding='utf-8')

if __name__ == '__main__':
    main()
