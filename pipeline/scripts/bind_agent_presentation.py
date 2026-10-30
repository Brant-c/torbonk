import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];G=ROOT/'godot/Gameplay'
def set_fields(id,**fields):
 p=G/f'Abilities/{id}.tres';s=p.read_text()
 for k,v in fields.items():
  line=k+' = '+(json.dumps(v) if isinstance(v,str) else str(v).lower())
  s=re.sub('^'+k+r' = .*$',lambda m:line,s,flags=re.M) if re.search('^'+k+' = ',s,re.M) else s+'\n'+line+'\n'
 p.write_text(s)
def gesture(ids,start,loop=None,**extra):
 for id in ids:set_fields(id,StartClip=start,LoopClip=loop or start,**extra)
gesture(['hiddenstrike'],'Agent/HiddenStrike',GestureSeconds=1.2,WindupSeconds=.25)
gesture(['fragmentationgrenade','flashbang','corrosivegrenade'],'Agent/RifleGrenade',GestureSeconds=1.3)
gesture(['explosiveprobe','interrogationprobe','plasmaprobe','empdischarge','orbitalstrike'],'Agent/Detonator',GestureSeconds=1.2,PresentationKind='agent',CastAudio='',ImpactAudio='')
# Ground effects outlive the brief call-in gesture; the caster never fires a rifle into the sky.
set_fields('orbitalstrike',MaintainChannelGesture=False)
gesture(['suppressivefire','agentcull'],'Agent/RifleAutoStart','Agent/RifleAutoLoop',MaintainChannelGesture=True,EndClip='Agent/RifleAutoEnd',GestureSeconds=.65)
gesture(['seriesofshots'],'Agent/SniperSeries',MaintainChannelGesture=True)
gesture(['koltoprobe','koltoinjection','surgicalprobe'],'Agent/Medical',CastAudio='',ImpactAudio='',GestureSeconds=1.2)
gesture(['agentstealth','cloakingscreen'],'Agent/Detonator',CastAudio='',ImpactAudio='')
gesture(['coverpulse'],'D3/EnergyShield',PresentationKind='agent',CastAudio='',ImpactAudio='')
gesture(['overloadshot'],'D3/RifleFire3')
set_fields('entrench',CastAudio='')
audio=json.loads((ROOT/'godot/Assets/Agent/audio.json').read_text())
bindings={'debilitate':('ImpactAudio','Play_ability_agent_debilitate_impact'),'flashbang':('ImpactAudio','Play_ability_spy_flashbang_owmyeyes'),'ballisticshield':('CastAudio','Play_ability_spy_ballisticshield_activateshield'),'interrogationprobe':('CastAudio','Play_ability_general_interrogationprobe_hoverloop'),'koltoprobe':('CastAudio','Play_ability_spy_surgicalprobe_flyin')}
for id,(field,event) in bindings.items():
 eid=audio['events'].get(event);media=audio.get('graphs',{}).get(eid,{}).get('media',[])
 if media:set_fields(id,**{field:f'res://Assets/Agent/Audio/{media[0]}.wav'})
rows=[];profiles=json.loads((G/'classes.json').read_text())
for id in dict.fromkeys(x for p in profiles if p['Id'] in ['operative','sniper'] for x in [p['StartingAbility']]+p['Kit']):
 s=(G/f'Abilities/{id}.tres').read_text();rows.append(dict(id=id,bindings={k:re.search('^'+k+r' = (.*)$',s,re.M)[1] for k in ['StartClip','LoopClip','PresentationKind']}))
(ROOT/'manifests/agent-presentation-bindings.json').write_text(json.dumps(rows,indent=2))
