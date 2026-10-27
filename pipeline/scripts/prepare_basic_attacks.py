"""Bounded recovery of original weapon attacks, using the established April reader."""
import json
import character_source
from source_pipeline import ROOT

CLIPS={
    'Saber':'cb_saber_attack_right_1','DualSaber':'cb_2saber_attack_right_1',
    'Pole':'cb_pole_attack_right_1','Rifle':'cb_rifle_fire_1',
    'Pistol':'cb_pistol_fire_1','DualPistol':'cb_2pistol_fire_1',
}

if __name__=='__main__':
    character_source.MANIFEST=ROOT/'manifests/basic-attacks-source.json'
    source=character_source.SelectedSource()
    for name,stem in CLIPS.items():
        source.get(f'/resources/anim/humanoid/bmnnew/{stem}.jba',name,'Original weapon attack clip; semantic Godot mapping, not native action-graph parity')
    (ROOT/'godot/Assets/BasicAttacks').mkdir(parents=True,exist_ok=True)
    (ROOT/'manifests/basic-attacks-conversion.json').write_text(json.dumps({'clips':CLIPS},indent=2))
    print(f'Recovered {len(CLIPS)} original basic attack animations.')
