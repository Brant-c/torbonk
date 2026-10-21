import sys,json,subprocess
from pathlib import Path
from source_pipeline import ROOT
import character_source
from lightning_source import decode_xml
character_source.MANIFEST=ROOT/'manifests/d3-fx-source.json';src=character_source.SelectedSource()
for bank in ['abilities_imperialranged_data','abilities_imperialranged_media']:
    p=f'/resources/bnk2/{bank}.bnk';print(bank,src.has(p))
    if bank.endswith('_data') and src.has(p):src.get(p,'D3 audio','original ranged HIRC')
# Reuse exact melee HIRC/DIDX decoder for the three melee attacks first.
import prepare_ability_audio
prepare_ability_audio.main('d3-fx','D3',['Play_ability_sithwarrior_smash_quickcharge','Play_ability_sithwarrior_impale_mediumgrip','Play_ability_jediconsular_hiddenstrike_mediumgrip'])
