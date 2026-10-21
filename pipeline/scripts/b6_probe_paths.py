exec(open(__file__.replace('b6_probe_paths.py','b6_discover.py')).read().split("for slot in")[0])
for family in ['klorslug','dragonbat']:
 for p in [f'/resources/art/dynamic/spec/{family}_skeleton.gr2',f'/resources/art/dynamic/spec/{family}.xml',f'/resources/anim/creature/{family}/anim.xml',f'/resources/anim/creature/{family}/animation.xml']:
  if s.has(p):print(p,s.get(p)[:1500])
 for group in ['creature','creatures',family]:
  for stem in ['idle','run','walk','cb_idle','mv_run','dg_death_1','death','cb_attack','cb_melee_1','cb_unarmed_idle','mv_cb_run_forward','mv_run_forward']:
   p=f'/resources/anim/{group}/{family}/{stem}.jba'
   if s.has(p):print('ANIM',p);s.get(p)
for slot in ['weapon','melee','ranged','lightsaber','electroblade']:
 p=f'/resources/art/dynamic/{slot}/index.xml'
 print('registry',p,s.has(p))
 if s.has(p):
  root=ET.fromstring(s.get(p))
  for a in root:
   if any(t in ET.tostring(a,encoding='unicode').lower() for t in ['low03','lightsaber']):print(ET.tostring(a,encoding='unicode')[:2000])
