"""Idempotent additive collectible staging. Run after wardrobe generators."""
import json,copy,shutil,sys
from pathlib import Path
import character_source
from character_source import ROOT
sys.path.insert(0,str(ROOT/'tools/python-ui'))
from PIL import Image
character_source.MANIFEST=ROOT/'manifests/head-weapon-source.json';src=character_source.SelectedSource()
read=lambda p:json.loads((ROOT/p).read_text())
c=read('godot/Gameplay/Equipment/catalog.json');u=read('godot/Gameplay/Equipment/cosmetic-unlocks.json')
out=ROOT/'godot/Assets/Equipment/Arsenal';out.mkdir(exist_ok=True)
for key in ['Items','Appearances']:c[key]=[a for a in c[key] if not a['Id'].startswith('collectible.')]
u['Milestones']=[m for m in u['Milestones'] if not m['Id'].startswith('collectible.')]
def icon(identity,p):
 src.get(p,identity,'matching April inventory icon')
 with Image.open(ROOT/'source-assets/april2012'/p.lstrip('/')) as im:im.convert('RGBA').save(out/(identity+'.png'))
 return 'res://Assets/Equipment/Arsenal/'+identity+'.png'
def reward(identity,label,wave,base,ids):
 u['Milestones'].append(dict(Id=identity,DisplayName=label,Kind='Wave',Value=wave,BaseClasses=base,Appearances=ids))
for f,base in [('war','sith_warrior'),('inq','sith_inquisitor'),('spy','imperial_agent'),('bh','bounty_hunter')]:
 r=read(f'wardrobe-head-{f}-recipe.json');p=r['parts'][0];ident=f'collectible.{f}.head'
 shutil.copyfile(ROOT/f'generated/wardrobe-head-{f}-sorcerer.glb',out/(f'head-{f}.glb'))
 report=read(f'manifests/wardrobe-head-{f}-character-probe.json')
 a=copy.deepcopy(next(a for a in c['Appearances'] if a['Id']==f'wardrobe.{f}.chest'))
 a.update(Id=ident,Slot='head',Scene=f'res://Assets/Equipment/Arsenal/head-{f}.glb',MeshNames=[m['name'] for m in report['meshes']],CoversSlots=[],HidesBaseGroups=['hair'] if f in ['war','bh'] else [])
 c['Appearances'].append(a)
 item=copy.deepcopy(next(i for i in c['Items'] if i['Id']==f'wardrobe.{f}.chest'))
 item.update(Id=ident,Slot='head',Appearance=ident,DisplayName={'war':'Warrior Helmet','inq':'Inquisitor Circlet','spy':'Agent Mask','bh':'Hunter Helmet'}[f],Icon=icon(ident,p['iconSource']),Source=dict(GomId=p['sourceAppearance']['id'],Name=p['sourceAppearance']['name'],Evidence='Client-derived April definition and face registry; local unlock'))
 c['Items'].append(item);reward(ident,'Reach wave 11 as '+base.replace('_',' ').title(),11,[base],[ident])
# Share starters under the same rules as collectible weapons.
for a in c['Appearances']:
 if a['Id'] in ['red_saber','d3.saber.main_hand']:a['Compatibility']['AdvancedClasses']=['sorcerer','juggernaut','marauder']
 if a['Id']=='d3.saber.off_hand':a['Compatibility']['AdvancedClasses']=['marauder']
 if a['Id'] in ['d3.rifle.main_hand','d3.sniper.main_hand']:a['Compatibility']['AdvancedClasses']=['operative','sniper']
 if a['Id']=='d3.pistol.off_hand':a['Compatibility']['AdvancedClasses']=['mercenary']
for name,w in read('head-weapons-recipe.json')['weapons'].items():
 kind=name[:-1];tier=int(name[-1]);template={'saber':'d3.saber.main_hand','staff':'d3.staff.main_hand','pistol':'d3.pistol.main_hand','rifle':'d3.rifle.main_hand'}[kind]
 ids=[]
 for slot in (['main_hand','off_hand'] if kind in ['saber','pistol'] else ['main_hand']):
  ident=f'collectible.{name}.{slot}';a=copy.deepcopy(next(a for a in c['Appearances'] if a['Id']==template))
  a.update(Id=ident,Slot=slot,Scene=f'res://Assets/Equipment/Arsenal/{name}.glb',Locomotion=None)
  if slot=='off_hand':a['Compatibility']['AdvancedClasses']=['marauder' if kind=='saber' else 'mercenary'];a['Attachment']['Bone']='LeftWeapon';a['RequiredBones']=['LeftWeapon']
  c['Appearances'].append(a);ids.append(ident)
  item=copy.deepcopy(next(i for i in c['Items'] if i['Appearance']==template))
  item.update(Id=ident,Slot=slot,Appearance=ident,Compatibility=a['Compatibility'],CosmeticOnly=True,StatModifiers={},DisplayName={'saber':'Lightsaber','staff':'Double Saber','pistol':'Blaster Pistol','rifle':'Blaster Rifle'}[kind]+f' High {tier:02}',Icon=icon(ident,'/resources/gfx/icons/'+Path(w['model']).stem+'.dds'),Source=dict(GomId='',Name=w['model'],Evidence='Client-derived unique April TOR model/material/icon bytes; local collectible'))
  c['Items'].append(item)
 base={'saber':['sith_warrior','sith_inquisitor'],'staff':['sith_inquisitor'],'pistol':['bounty_hunter'],'rifle':['imperial_agent']}[kind]
 reward('collectible.'+name,f'Reach wave {11+tier} as '+('Sith' if kind=='saber' else base[0].replace('_',' ').title()),11+tier,base,ids)
# Hide original hair only for enclosed helmets, retaining the face underneath.
report=read('manifests/character-probe.json')
for rig in c['Rigs']:rig['BaseGroups']['hair']=[m['name'] for m in report['meshes'] if m['slot']=='hair']
for p,data in [('godot/Gameplay/Equipment/catalog.json',c),('godot/Gameplay/Equipment/cosmetic-unlocks.json',u)]: (ROOT/p).write_text(json.dumps(data,indent=2))
print('Staged four headpieces and eight weapon models, with paired left sockets.')
