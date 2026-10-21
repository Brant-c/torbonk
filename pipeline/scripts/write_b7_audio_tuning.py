import json
from source_pipeline import ROOT
base='res://Assets/Presentation/Audio/'
cues={}
def cue(name,category,mid,gain,cooldown=0,duration=0,pitch=1,priority=False):cues[name]=dict(category=category,path=base+str(mid)+'.wav',gainDb=gain,cooldown=cooldown,duration=duration,pitch=pitch,priority=priority)
cue('hover','UI',163129423,2,.08,.075,1.35)
cue('click','UI',163129423,8,.06,.16,1.1)
cue('pickup','Reward',163129423,8,.085,0,1.18)
cue('level','Reward',787228180,-3,.1,2.1,1,True)
cue('upgrade_premium','Reward',163129423,12,.1,0,1)
cue('upgrade_prototype','Reward',130814347,-4,.1,.7,1.12)
cue('upgrade_artifact','Reward',576226339,3,.1,.85,1,True)
cue('boss_start','Boss',130814347,-3,.1,1.15,.8,True)
cue('boss_victory','Boss',576226339,-2,.1,2.1,1,True)
cue('burrow_warning','Boss',24757886,9,.4,1.35,1,True)
cue('burrow_impact','Boss',113287997,-5,.4,.95,1)
result=dict(maximumPresentationVoices=12,maximumCombatVoices=16,identicalCombatMinimumSeconds=.075,combatDuckDb=-9,musicDuckDb=-5,worldDuckDb=-4,categories={n:dict(gainDb=g,voices=v) for n,g,v in [('Music',-12,2),('World',-10,4),('Combat',-6,16),('UI',-2,3),('Reward',-3,4),('Boss',-3,3)]},cues=cues)
(ROOT/'godot/Gameplay/presentation-audio.json').write_text(json.dumps(result,indent=2))
