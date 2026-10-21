from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/prepare_d2.py').read_text().replace("Assets/D2","Assets/D3").replace("d2-","d3-fx-").replace("work/d2","work/d3").replace("CLIPS={'Chain':'fp_2h_elect_instant_right_1','Overload':'fp_force_slam_1'}","CLIPS={}")
start=code.index("    for icon in [");end=code.index("    seen=set();",start)
code=code[:start]+'''    queue=['/resources/art/fx/fxspec/'+p+'.fxspec' for p in ['sw_smash','sw_impale_impact_01','abilities/all/all_dynamic_saber_trail_alphagrad_01','abilities/bounty_hunter/bh_flamesweep_bracer_flames_01','abilities/bounty_hunter/bh_fusion_missile_fire','abilities/bounty_hunter/bh_tactical_nuke_explosion','abilities/spy/sp_toxic_dart_muzzle_flash']]
'''+code[end:]
code=code.replace('maximumFiles=140','maximumFiles=240').replace('maximumDecodedBytes=60*1024*1024','maximumDecodedBytes=100*1024*1024')
exec(compile(code,'d3-fx','exec'))
