exec(open(__file__.replace('b6_find_weapons.py','b6_discover.py')).read().split("for slot in")[0])
names=Path('D:/SWTORClassic/extracTOR/extracTOR/bin/Debug/net8.0-windows/filenames-all.txt').read_text().splitlines()
for p in names:
 if any(t in p for t in ['electroblade','saber_sithlow03','am_klorslug','am_dragonbat']) and p.endswith(('.gr2','.xml','.asb')):
  if s.has(p):print(p);s.get(p)
