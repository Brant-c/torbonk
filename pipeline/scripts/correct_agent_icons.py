"""Use exact April ablIconSpec bindings; preserve labelled art reuse for adapted talents."""
import json,re
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE,sha
G=ROOT/'godot/Gameplay';character_source.MANIFEST=ROOT/'manifests/agent-icon-source.json';src=character_source.SelectedSource();receipt=[]
def decoded(row):
 try:return json.loads(row['result']['content'][0]['text'])
 except (KeyError,ValueError):return {}
def extract(icon,owner,definition=None):
 path=f'/resources/gfx/icons/{icon.lower()}.dds'
 if not src.has(path):receipt.append(dict(owner=owner,path=path,status='unresolved unique April entry'));return None
 src.get(path,owner,'exact ablIconSpec' if definition else 'recovered art reuse for adapted talent')
 p=ROOT/f'godot/Assets/UI/icons/{icon.lower()}.png';Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(p)
 receipt.append(dict(owner=owner,icon=icon.lower(),definition=definition,path=path,output=str(p.relative_to(ROOT)),sha256=sha(p),evidence='Client-derived exact icon binding' if definition else 'Adapted talent using recovered ability art'))
 return icon.lower()
def bind(id,icon):
 for p in [G/f'Abilities/{id}.tres',G/f'Upgrades/unlock_{id}.tres']:
  if p.exists():p.write_text(re.sub(r'^Icon = .*$',f'Icon = "{icon}"',p.read_text(),flags=re.M))
rows=json.loads((ROOT/'manifests/agent-icon-definitions.json').read_text())
rows.append(dict(id='basicrifle',result=json.loads((ROOT/'manifests/agent-rifle-icon-definition.json').read_text())))
rows+=json.loads((ROOT/'manifests/agent-ability-definitions.json').read_text())
advanced={'abl.agent.shiv':'shiv','abl.agent.backstab':'backstab','abl.agent.hidden_strike':'hiddenstrike','abl.agent.skill.concealment.laceration':'laceration','abl.agent.skill.lethality.cull':'agentcull','abl.agent.skill.medic.kolto_probe':'koltoprobe','abl.agent.skill.medic.surgical_probe':'surgicalprobe','abl.agent.skill.marksmanship.followthrough':'followthrough','abl.agent.skill.engineering.interrogation_probe':'interrogationprobe','abl.agent.skill.engineering.plasma_probe':'plasmaprobe','abl.agent.skill.engineering.detonate_probes':'empdischarge','abl.agent.skill.lethality.weakening_blast':'weakeningblast'}
for row in rows:
 d=decoded(row);icon=next((x['value'] for x in d.get('children',[]) if x['key']=='ablIconSpec'),None);id=row.get('id') or advanced.get(row.get('name'))
 if id and icon:
  recovered=extract(icon,id,d.get('identity'))
  if recovered:bind(id,recovered)
p=G/'Talents/agent.json';catalog=json.loads(p.read_text());defs={row['id']:decoded(row) for row in json.loads((ROOT/'manifests/agent-talent-icon-definitions.json').read_text())}
fallback={'medicine.enduring_kolto':'koltoprobe','medicine.medical_screen':'shieldprobe','medicine.emergency_response':'koltoinjection','marksmanship.penetrating_blasts':'ambush','marksmanship.imperial_assassin':'ambush','marksmanship.deployed_cover':'entrench','engineering.interrogation_tactics':'interrogationprobe','agent_lethality.lethal_training':'agentcull','agent_lethality.toxic_resilience':'dart'}
for t in catalog['Talents']:
 d=defs.get(t['Id'],{});icon=next((x['value'] for x in d.get('children',[]) if x['key']=='ablIconSpec'),None)
 definition=d.get('identity') if icon else None
 if not icon:
  ability=fallback.get(t['Id']) or next((e['Ability'] for e in t['Effects'] if e.get('Ability')),'dart')
  icon=re.search(r'^Icon = "(.*?)"',(G/f'Abilities/{ability}.tres').read_text(),re.M)[1]
 recovered=extract(icon,t['Id'],definition)
 if recovered:t['Icon']='res://Assets/UI/icons/'+recovered+'.png';t['IconSource']='Exact April ablIconSpec: '+definition['name'] if definition else 'Adapted talent reuses recovered ability icon'
p.write_text(json.dumps(catalog,indent=2)+'\n')
(ROOT/'manifests/agent-icon-corrections.json').write_text(json.dumps(receipt,indent=2))
print(json.dumps(dict(bound=sum(r.get('definition') is not None for r in receipt),adapted=sum(r.get('evidence','').startswith('Adapted') for r in receipt),unresolved=[r for r in receipt if r.get('status')]),indent=2))
