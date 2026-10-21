"""Idempotent corrections after the D3 starter content stage."""
import json,re,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
audio=json.loads((ROOT/'godot/Assets/D3/audio.json').read_text())
def sound(event):
    ids=audio['graphs'][audio['events'][event]]['media'];assert ids,event
    return f'res://Assets/D3/Audio/{ids[0]}.wav'
# A state present in the saber-only awesomeness event also identifies the saber branch
# of the generic melee swing. Preserve the original numeric state, not an invented label.
receipt=json.loads((ROOT/'manifests/d3-weapon-audio.json').read_text())
branches={}
for event,index in [('right','3001'),('left','3003')]:
    candidates=list((ROOT/'work/d3-weapon/events').glob(f'weapons_data-{index}-event*1359942228*.txtp'))
    if not candidates:
        eid=receipt['events'][f'play_weapon_melee_{"right" if event=="right" else "left"}hand_swing_normal']
        candidates=[p for p in (ROOT/'work/d3-weapon/events').glob('*1359942228*.txtp') if f'] {eid}' in p.read_text()]
    if not candidates and event=='left':
        branches[event]=dict(branches['right'],mapping='wwiser deduplicated symmetric event; share the same saber swing media');continue
    assert candidates,event
    body=candidates[0].read_text();ids=[int(i) for i in re.findall(r'[?#](\d+)\.wem',body)]
    branches[event]=dict(state='1359942228',graph=str(candidates[0].relative_to(ROOT)),media=ids)
(ROOT/'manifests/d3-saber-branch.json').write_text(json.dumps(branches,indent=2))
def swing(hand):return f'res://Assets/D3/Audio/{branches[hand]["media"][0]}.wav'
changes={
 'smash':dict(Icon='forceslam',FullBodyGesture=True,PresentationKind='slam',WeaponAudio=swing('right')),
 'rupture':dict(ImpactTexture='',PresentationKind='saber',WeaponAudio=swing('right'),OffhandAudio=swing('left')),
 'lacerate':dict(ImpactTexture='',PresentationKind='saber',WeaponAudio=swing('right')),
 'snipershot':dict(PresentationKind='rifle',MuzzleBone='RightWeapon',CastAudio=sound('Play_ability_spy_snipe_fire'),ImpactAudio='',ImpactTexture=''),
 'dart':dict(PresentationKind='bracer',MuzzleBone='LeftWrist',CastAudio=sound('Play_ability_spy_poisondart_fire'),ImpactAudio=sound('Play_ability_spy_poisondart_impact'),ImpactTexture='',EffectTint='Color(0.58, 0.88, 0.12, 1)'),
 'flamesweep':dict(FullBodyGesture=True,PresentationKind='flame',MuzzleBone='LeftWrist',WindupSeconds=.25,ChannelSeconds=1.2,TickSeconds=.3,Damage=4.25,RecoverySeconds=.5,GestureSeconds=2.,Description='360-degree moving Flame Sweep. Four radial damage pulses during the spin; 17 total base damage to enemies caught throughout.')}
for id,fields in changes.items():
    p=ROOT/f'godot/Gameplay/Abilities/{id}.tres';lines=p.read_text().splitlines();values={};order=[];prefix=[];active=False
    for line in lines:
        if active and ' = ' in line:
            k,v=line.split(' = ',1)
            if k not in values:order.append(k)
            values[k]=v
        else:
            prefix.append(line)
            if line=='[resource]':active=True
    fields['ImpactTailsOnTargetDeath']=True
    for k,v in fields.items():
        if k not in values:order.append(k)
        values[k]=v if isinstance(v,str) and v.startswith('Color(') else json.dumps(v)
    p.write_text('\n'.join(prefix)+'\n'+'\n'.join(f'{k} = {values[k]}' for k in order)+'\n')
shutil.copyfile(ROOT/'work/d3/forceslam.png',ROOT/'godot/Assets/UI/icons/forceslam.png')
p=ROOT/'godot/Gameplay/classes.json';profiles=json.loads(p.read_text())
next(p for p in profiles if p['Id']=='sniper')['Idle']='SniperIdle';p.write_text(json.dumps(profiles,indent=2))
p=ROOT/'godot/Gameplay/Equipment/catalog.json';catalog=json.loads(p.read_text())
humgraph=next((ROOT/'work/d3-weapon/events').glob('weapons_data-3010-event*1359942228*.txtp')).read_text()
humids=[int(i) for i in re.findall(r'[?#](\d+)\.wem',humgraph)]
for a in catalog['Appearances']:
    if a.get('Attachment') and a['Attachment']['RedBlade']:a['Attachment']['HumAudio']=f'res://Assets/D3/Audio/{humids[0]}.wav'
p.write_text(json.dumps(catalog,indent=2))
p=ROOT/'godot/Gameplay/Upgrades/d3_smash.tres';p.write_text(p.read_text().replace('Icon = "smash"','Icon = "forceslam"'))
print('Updated Empire presentation bindings; saber branch',branches)
