"""Bounded April presentation dependency recovery; never replaces existing source evidence."""
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
code = (ROOT/'scripts/prepare_d2.py').read_text()
code = code.replace('Assets/D2', 'Assets/PresentationRepairs').replace('work/d2', 'work/presentation-repairs').replace('d2-', 'presentation-repairs-')
code = code.replace("CLIPS={'Chain':'fp_2h_elect_instant_right_1','Overload':'fp_force_slam_1'}", 'CLIPS={}')
start = code.index('    for icon in [')
end = code.index('    seen=set();', start)
paths = [
 'sith_warrior/sw_force_scream_caster', 'sith_warrior/sw_force_scream_projectile', 'sith_warrior/sw_force_scream_impact',
 'sith_inquisitor/si_creepingterror_cast_01', 'sith_inquisitor/si_creepingterror_target_01', 'sith_inquisitor/si_force_shroud_cast',
 'spy/sp_shieldprobe_01', 'spy/sp_shieldprobe_hit_01', 'spy/sp_ambush_laser', 'spy/sp_ambush_target_01',
 'bounty_hunter/bh_explosive_fuel', 'bounty_hunter/bh_explosive_fuel_cast_01',
 'bounty_hunter/bh_heatseekermissiles_fake_projectile', 'bounty_hunter/bh_flamethrower']
queue = ['/resources/art/fx/fxspec/abilities/'+p+'.fxspec' for p in paths]
queue += ['/resources/art/fx/fxspec/abilities/jedi_consular/jc_resilience_cast.fxspec', '/resources/art/fx/fxspec/abilities/jedi_consular/jc_resilience_buff.fxspec', '/resources/art/fx/fxspec/cinematics/force_ghost_projector.fxspec']
code = code[:start] + '    queue='+repr(queue)+'\n' + code[end:]
code = code.replace('maximumFiles=140', 'maximumFiles=260').replace('60*1024*1024', '100*1024*1024')
exec(compile(code, __file__, 'exec'))
