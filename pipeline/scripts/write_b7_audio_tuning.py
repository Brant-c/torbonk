import json
from source_pipeline import ROOT
base='res://Assets/Presentation/Audio/'
cues={}
def cue(name,category,mid,gain,cooldown=0,duration=0,pitch=1,priority=False):cues[name]=dict(category=category,path=base+str(mid)+'.wav',gainDb=gain,cooldown=cooldown,duration=duration,pitch=pitch,priority=priority)
cue('hover','UI',163129423,2,.08,.075,1.35)
cue('click','UI',163129423,8,.06,.16,1.1)
cue('pickup','Reward','xp_17892505_quiet',0,.12,0,1)
cue('level','Reward',787228180,-3,.1,2.1,1,True)
cue('upgrade_premium','Reward',163129423,12,.1,0,1)
cue('upgrade_prototype','Reward',130814347,-4,.1,.7,1.12)
cue('upgrade_artifact','Reward',576226339,3,.1,.85,1,True)
cue('boss_start','Boss',130814347,-3,.1,1.15,.8,True)
cue('boss_victory','Boss',576226339,-2,.1,2.1,1,True)
cue('burrow_warning','Boss',24757886,9,.4,1.35,1,True)
cue('burrow_impact','Boss',113287997,-5,.4,.95,1)
def layer(media,gain):return dict(media=media,gainDb=gain)
events={
 'Play_ability_sithinquisitor_shock_buildup':dict(layers=[layer([50181172,38107453],-8),layer([13800022,67837780,70458762,23364535],-8)]),
 'Play_ability_sithinquisitor_shock_cast':dict(layers=[layer([35854337,41158987],-5)]),
 'Play_ability_sithinquisitor_shock_explode':dict(priority=True,layers=[layer([294434565,133966111,186175857],-3),layer([18124,17414480,72225944],-9)]),
 'Play_ability_sithinquisitor_forcestorm_cast_caster':dict(layers=[layer([43766601,60467976,676198],-5),layer([61761001,129620424,21323851],-9)]),
 'Play_ability_sithinquisitor_forcestorm_channelloop':dict(priority=True,layers=[layer([31429439],1),layer([7737937,48136250,142805332,34158220,68384798,44696493,15050748,13840107],2)]),
 'Play_ability_sithinquisitor_forcestorm_aoe_bolt':dict(priority=True,layers=[layer([403988737,227842474,332179516,186569051],-3)])}
result=dict(maximumPresentationVoices=12,maximumCombatVoices=16,identicalCombatMinimumSeconds=.075,combatDuckDb=-9,musicDuckDb=-5,worldDuckDb=-4,categories={n:dict(gainDb=g,voices=v) for n,g,v in [('Music',-12,2),('World',-10,4),('Combat',-6,16),('UI',-2,3),('Reward',-3,4),('Boss',-3,3)]},cues=cues,abilityEvents=events)
(ROOT/'godot/Gameplay/presentation-audio.json').write_text(json.dumps(result,indent=2))
