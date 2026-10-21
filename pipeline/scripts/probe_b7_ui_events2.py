import json,itertools,xml.etree.ElementTree as ET
from source_pipeline import ROOT
from probe_lightning_audio import fnv
obs=ET.parse(ROOT/'work/b7/gui.xml').find('.//list[@name="listLoadedItem"]');ids={int(o.find('field[@name="ulID"]').get('value')) for o in obs if o.get('name')=='CAkEvent'}
f=json.loads((ROOT/'work/b7/gui-events.json').read_text())
for prefix in ['button','widget_button','button_default','button_standard','button_generic','widget','state','system','skilltree','inventory','mission','quest','reward','notification']:
 for suffix in ['over','hover','rollover','mouseover','press','pressed','click','clicked','down','select','accept','confirm','on','off','unlock','success','complete','completed','open','close','levelup','error','alert']:
  for n in [f'Play_gui_{prefix}_{suffix}',f'Play_gui_{prefix}_{suffix}_default',f'Play_gui_{prefix}_default_{suffix}',f'Play_gui_{suffix}_{prefix}']:
   if fnv(n) in ids:f[n]=fnv(n)
(ROOT/'work/b7/gui-events.json').write_text(json.dumps(f,indent=2));print(f)
