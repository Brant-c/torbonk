from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/prepare_d2.py').read_text().replace('Assets/D2','Assets/Agent').replace('work/d2','work/agent').replace('d2-','agent-fx-').replace("CLIPS={'Chain':'fp_2h_elect_instant_right_1','Overload':'fp_force_slam_1'}",'CLIPS={}')
start=code.index('    for icon in [');end=code.index('    seen=set();',start)
paths=['sp_backstab_01','sp_lacerate_knife_cast','sp_fragmentationgrenade_explosion_01','sp_explosiveprobe_explode','sp_interrogationprobe_01','sp_plasma_probe_01','sp_orbital_child_singlestrike_01','sp_orbitalstrike_laser_01','sp_kolto_probe_caster_01','sp_kolto_injection_body','sp_ballistic_field_shield','sp_debilitate_impact_01','sp_flash_bang_target','sp_stealth_cast_01','sp_toxic_dart_projectile','sp_toxic_haze_aoe_loop','sp_kolto_injection_charge','sp_shiv_01','sp_shiv_impact_01','sp_shiv_weapon_01','sp_shiv_weapon_timeout_01','sp_lacerate_impact','sp_backstab_knife_only','sp_portablecover_dynamiclength_01','sp_portablecover_dynamiclength_attached']
code=code[:start]+'    queue='+repr(['/resources/art/fx/fxspec/abilities/spy/'+p+'.fxspec' for p in paths]+['/resources/art/shaders/materials/'+p+'.mat' for p in ['fx_sp_portableshield_device_main','fx_sp_portableshield_field','fx_sp_portableshield_projection_01']])+'\n'+code[end:]
code=code.replace('maximumFiles=140','maximumFiles=350')
exec(compile(code,__file__,'exec'))
