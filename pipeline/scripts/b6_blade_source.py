exec(open(__file__.replace('b6_blade_source.py','b6_discover.py')).read().split("for slot in")[0])
for p in ['/resources/art/fx/particles/lightsaber_red_base_p.prt','/resources/art/fx/particles/lightsaber_red_base.prt','/resources/art/fx/geometry/all/texture/fx_saber_blade_01_d.dds','/resources/art/fx/fxspec/cinematics/lightsaber_functions/red_turn_on_and_loop.fxspec']:
 print(p,s.has(p))
 if s.has(p):
  data=s.get(p,'B6 red blade investigation')
  if not p.endswith('.dds'):print(data.decode(errors='replace')[:5000])
for stem in ['dynamic_saber_core_white','dynamic_saber_glow_white','dynamic_saber_tip_core_white']:
 p=f'/resources/art/fx/texture/weapon_melee/{stem}.dds'
 print(p,s.has(p))
 if s.has(p):s.get(p,'B6 original saber textures')
for stem in ['saber_blade_alpha_blended_mwp_red_p','saber_core_alpha_blended_mw_red_p','lightsaber_red_static_prt']:
 p=f'/resources/art/fx/particles/{stem}.prt'
 if s.has(p):
  text=s.get(p,'B6 blade investigation').decode(errors='replace')
  print(stem,'\n'.join(line for line in text.splitlines() if any(k in line.lower() for k in ['texture','color','size','scale','length'])))
