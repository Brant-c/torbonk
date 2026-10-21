import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
audio=json.loads((ROOT/'godot/Assets/D3/audio.json').read_text())
def media(event):
    graph=audio['graphs'].get(audio['events'].get(event,''),{});ids=graph.get('media',[])
    return f'res://Assets/D3/Audio/{ids[0]}.wav' if ids else ''
rows={
    'smash':('radial_trails_out','Play_ability_sithwarrior_smash_quickcharge','Play_ability_sithwarrior_smash_wave'),
    'rupture':('lightsaber_critblade','Play_ability_sithwarrior_impale_mediumgrip','Play_ability_sithwarrior_impale_impact'),
    'lacerate':('lightsaber02_trail_red_nomips','Play_ability_jediconsular_hiddenstrike_mediumgrip',''),
    'dart':('trail_glob','Play_ability_spy_painsupressiondart_prep',''),
    'snipershot':('','Play_ability_spy_snipe_activate',''),
    'tracer':('fx_2x2_roundflames_01_desat','Play_ability_bountyhunter_fusionmissile_launch','Play_ability_bountyhunter_tacticalnuke_explosion'),
    'flamesweep':('fx_2x2_flames_emmissive_bw_01_d','Play_ability_bountyhunter_flamesweep_bracerfire','Play_ability_bountyhunter_flamesweep_fireburst')}
for id,(texture,cast,impact) in rows.items():
    p=ROOT/f'godot/Gameplay/Abilities/{id}.tres';s=p.read_text();s=s.split('ImpactTexture =')[0]
    s+=f'ImpactTexture = "res://Assets/D3/{texture}.png"\n' if texture else ''
    s+=f'CastAudio = "{media(cast)}"\nImpactAudio = "{media(impact)}"\n';p.write_text(s)
    lengths={'smash':1.6,'rupture':1.6,'lacerate':1.4,'dart':1.066667,'snipershot':1.066667,'tracer':2.266667,'flamesweep':2.0}
    s=p.read_text().replace('RecoverySeconds = 0.1',f'RecoverySeconds = {lengths[id]}').replace('GestureSeconds = 0.9',f'GestureSeconds = {lengths[id]}');p.write_text(s)
    print(id,media(cast),media(impact))
# Keep wardrobe appearances restricted to their original base class.
p=ROOT/'godot/Gameplay/Equipment/catalog.json';c=json.loads(p.read_text())
for a in c['Appearances']:
    if a['Id'].startswith('d3.'):
        family=a['Id'].split('.')[1]
        if family in ['war','spy','bh']:a['Compatibility']['BaseClasses']=[dict(war='sith_warrior',spy='imperial_agent',bh='bounty_hunter')[family]]
        else:a['Compatibility']['AdvancedClasses']=dict(saber=['juggernaut','marauder'],staff=['assassin'],rifle=['operative'],sniper=['sniper'],pistol=['mercenary','powertech'])[family]
    elif a['Id'].startswith(('level1.','ss_a04.')):a['Compatibility']['BaseClasses']=['sith_inquisitor']
p.write_text(json.dumps(c,indent=2))
for a in c['Appearances']:
    if a['Id']=='d3.staff.main_hand':a['Attachment']['BladeStart']=.0296
    if a['Id'].startswith('d3.saber.'):
        a['Locomotion']=dict(LibraryId='EquipmentSaber',Scene='res://Assets/Enemies/looter.glb',Idle='Idle',Run='Run')
    if a['Id']=='red_saber':a['Compatibility']['AdvancedClasses']=['sorcerer']
p.write_text(json.dumps(c,indent=2))
profiles=json.loads((ROOT/'godot/Gameplay/classes.json').read_text())
for p in profiles:
    if p['StartingAbility'] in ['smash','lacerate','flamesweep']:
        ability=p['StartingAbility'];p['TalentPool']=[('d3_area_'+ability if v=='range' else v) for v in p['TalentPool']]
        (ROOT/f'godot/Gameplay/Upgrades/d3_area_{ability}.tres').write_text('[gd_resource type="Resource" script_class="UpgradeDefinition" load_steps=2 format=3]\n[ext_resource type="Script" path="res://Gameplay/UpgradeDefinition.cs" id="1"]\n[resource]\nscript = ExtResource("1")\n'+f'Id = "d3_area_{ability}"\nTitle = "Wider Sweep"\nEffect = 6\nAbilityId = "{ability}"\nAmountPerRank = 0.12\n')
(ROOT/'godot/Gameplay/classes.json').write_text(json.dumps(profiles,indent=2))
