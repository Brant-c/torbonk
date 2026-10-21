"""Stage existing converted assets and reusable content data. No shared writes."""
from pathlib import Path
import json,shutil,hashlib,struct
R=Path(__file__).resolve().parents[1];P=R.parent/'pipeline';A=R/'Frontend/Assets'
records=[]
def copy(src,dest,evidence):
    dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,dest)
    records.append({'source':str(src),'output':str(dest.relative_to(R)).replace('\\','/'),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'evidence':evidence})
for name in ['Eurostile.ttf','EurostileBold.ttf']:
    copy(P/'godot/Assets/UI'/name,A/'UI'/name,'Original swtor_fonts.gfx DefineFont3 outlines; existing B4 ui-conversion.json')
for name in ['bwaui_standard_window_1','bwaui_standard_window_24','bwaui_standard_window_9','bwaui_standard_window_18','bwaui_tooltip_1']:
    copy(P/'godot/Assets/UI/slices'/(name+'.png'),A/'UI'/(name+'.png'),'Original GFX tag 1008 crop; existing B4 ui-conversion.json')
for p in (P/'godot/Assets').glob('sorcerer*'):
    if p.suffix in ('.png','.glb'):copy(p,A/'Character'/p.name,'Existing April modular Sorcerer conversion; character-source.json + character-recipe.json + character-conversion.json')
for name in ['ui-source.json','ui-conversion.json','character-source.json','character-conversion.json','character-artifacts.json']:
    if (P/'manifests'/name).exists():copy(P/'manifests'/name,R/'manifests/prior'/name,'Frozen existing conversion evidence; referenced output paths are historical')
copy(P/'character-recipe.json',R/'manifests/prior/character-recipe.json','Exact prior model assembly prescription')
(R/'manifests/staged.json').write_text(json.dumps(records,indent=2))
asset='res://Frontend/Assets/'
raw=asset+'Recovered/'
def art(name):return raw+name+'.png'
classes=[]
specs=[
('empire','sith_warrior','Sith Warrior','sithwarrior','sith_warrior','A relentless champion of the dark side. The Warrior dominates the front line through strength and lightsaber mastery.',[
('juggernaut','Juggernaut','juggernaut','A single lightsaber and heavy armor turn the Warrior into an unyielding force.'),('marauder','Marauder','marauder','Twin lightsabers and aggressive assaults overwhelm opponents.')]),
('empire','sith_inquisitor','Sith Inquisitor','sithsorcerer','sith_inquisitor','Ambition and forbidden knowledge fuel the Inquisitor. Manipulate the Force, channel lightning, and pursue the mysteries of the Sith.',[
('sorcerer','Sorcerer','sith_sorcerer','Command Force lightning from range. The existing Survivors combat kit is available for future integration.'),('assassin','Assassin','sith_assassin','A double-bladed lightsaber, stealth, and dark-side techniques define the Assassin.')]),
('empire','imperial_agent','Imperial Agent','imperialagent','agent','The Empireâ€™s unseen operative. Precision marksmanship, advanced technology, and deception serve Imperial Intelligence.',[
('sniper','Sniper','sniper','Strike from cover with a sniper rifle and carefully placed long-range shots.'),('operative','Operative','operative','Use concealment, a vibroknife, and field medicine at close quarters.')]),
('empire','bounty_hunter','Bounty Hunter','bountyhunter','bounty_hunter','A resourceful professional who tracks dangerous targets. Armor, blasters, and specialized gadgets bring every contract within reach.',[
('mercenary','Mercenary','mercenary','Twin blasters, missiles, and support technology provide ranged firepower.'),('powertech','Powertech','powertech','Heavy armor, a flamethrower, and close-range tools keep the pressure on.')]),
('republic','jedi_knight','Jedi Knight','jediknight','jedi_knight','A defender of the Republic and a master of lightsaber combat. Courage and disciplined use of the Force guide the Knight.',[
('guardian','Guardian','guardian','A single lightsaber and protective techniques defend allies on the front line.'),('sentinel','Sentinel','sentinel','Two lightsabers deliver a swift, sustained offensive.')]),
('republic','jedi_consular','Jedi Consular','jediconsular','jedi_consular','A scholar and diplomat guided by the Force. The Consular protects the Republic through wisdom, insight, and powerful Force techniques.',[
('sage','Sage','jedi_sage','Channel the Force at range to defend, heal, and project telekinetic power.'),('shadow','Shadow','jedi_shadow','Combine stealth, a double-bladed lightsaber, and precise Force attacks.')]),
('republic','smuggler','Smuggler','smuggler','smuggler','Quick thinking and a faster blaster keep the Smuggler alive. Luck, cover, and dirty tricks can turn any confrontation.',[
('gunslinger','Gunslinger','gunslinger','Dual blasters and cover deliver precise ranged attacks.'),('scoundrel','Scoundrel','scoundrel','A scattergun, stealth, and improvised medicine reward close-range ingenuity.')]),
('republic','trooper','Trooper','trooper','trooper','An elite Republic soldier. Military training, heavy weapons, and advanced armor face the Empire head-on.',[
('commando','Commando','commando','An assault cannon and battlefield support bring heavy ranged power.'),('vanguard','Vanguard','vanguard','A blaster rifle and defensive technology lead the assault at close range.')])]
for faction,cid,name,icon,painting,description,advanced in specs:
    aa=[]
    for aid,an,ap,ad in advanced:
        a={'Id':aid,'Name':an,'Artwork':art('cdx.advanced_classes.'+ap),'Description':ad,'GameplayImplemented':aid=='sorcerer'}
        if aid=='sorcerer':a.update(PreviewScene=asset+'Character/sorcerer.glb',IdleAnimation='Idle',FacingDegrees=0)
        aa.append(a)
    iconpath=asset+'Icons/'+icon+'.png' if (A/'Icons'/(icon+'.png')).exists() else art(icon)
    classes.append({'Id':cid,'FactionId':faction,'Name':name,'Icon':iconpath,'Artwork':art('cdx.game_rules.classes.'+painting),'Description':description,'AdvancedClasses':aa})
maps=[]
for mid,name,region,painting,surface,x,y,description in [
('korriban','Korriban','Sith Worlds','korriban','korriban',.8,.28,'Ancient tombs and red sands beneath the Sith Academy. The existing Survivors arena is available for future integration.'),
('tython','Tython','Deep Core','tython','tython',.2,.72,'Forested birthplace of the Jedi Order. Environment and gameplay are planned.'),
('dromund_kaas','Dromund Kaas','Sith Worlds','dromund_kaas','dromund_kass',.8,.72,'Storm-wreathed capital of the Sith Empire. Environment and gameplay are planned.'),
('coruscant','Coruscant','Core Worlds','coruscant','coruscant',.5,.28,'The vast city-world at the heart of the Republic. Environment and gameplay are planned.'),
('hutta','Hutta','Hutt Space','hutta','hutta',.5,.72,'Industrial swamps and Hutt domains. Environment and gameplay are planned.'),
('ord_mantell','Ord Mantell','Mid Rim','ordmantell','ord_mantell',.2,.28,'A contested world of smugglers and soldiers. Environment and gameplay are planned.')]:
    maps.append({'Id':mid,'Name':name,'Region':region,'Description':description,'Artwork':art('cdx.planets.'+painting),'Surface':art('planets_'+surface+'_d'),'X':x,'Y':y,'GameplayImplemented':mid=='korriban'})
catalog={'Factions':[{'Id':'republic','Name':'Galactic Republic','Description':'Stand with the Republic. Defend a civilization built on freedom, diplomacy, and the ideals of the Jedi Order.','Emblem':asset+'Slices/bwaui_character_create_window_93.png'},
{'Id':'empire','Name':'Sith Empire','Description':'Serve the Empire. Pursue power and order alongside the Sith, Imperial Intelligence, and the galaxyâ€™s most formidable hunters.','Emblem':asset+'Slices/bwaui_character_create_window_81.png'}], 'Classes':classes,'Maps':maps}
(R/'Frontend/Data/catalog.json').write_text(json.dumps(catalog,indent=2),encoding='utf-8')
b=(A/'Character/sorcerer.glb').read_bytes();n=struct.unpack_from('<I',b,12)[0];g=json.loads(b[20:20+n])
print('GLB animations:',[a.get('name') for a in g.get('animations',[])])
print('GLB external images:',[a.get('uri') for a in g.get('images',[]) if 'uri' in a])
positions=[g['accessors'][p['attributes']['POSITION']] for m in g['meshes'] for p in m['primitives']]
print('GLB mesh position bounds:',[(p.get('min'),p.get('max')) for p in positions[:2]])
print('Staged',len(records),'files; classes',len(classes),'advanced',sum(len(x['AdvancedClasses']) for x in classes))
