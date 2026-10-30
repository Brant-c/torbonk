"""Bounty Hunter advanced-class assets: exact April ablIconSpec icons, FXSpec textures, JBA stems and Wwise events.

Inputs are April appearance (epp.bounty_hunter.*) and ability (abl.bounty_hunter.*) values recorded in
manifests/bounty-hunter-appearances.json and work/bh_icons_april.json (research MCP reads). Outputs are additive:
Assets/UI/icons, Assets/BountyHunter textures/audio and manifests/bountyhunter-conversion.json clip stems.
"""
import json,re,sys
from pathlib import Path
from PIL import Image
import character_source
from source_pipeline import ROOT,SOURCE,sha
from lightning_source import decode_xml
from prepare_abilities import unpack

OUT=ROOT/'godot/Assets/BountyHunter'
ICONS=ROOT/'godot/Assets/UI/icons'
# Survivors ability id -> April abl node whose ablIconSpec is bound.
ABILITIES={
 'retractableblade':'abl.bounty_hunter.skill.advanced_prototype.retractable_blade','railshot':'abl.bounty_hunter.rail_shot',
 'explosivedart':'abl.bounty_hunter.explosive_dart','electrodart':'abl.bounty_hunter.electro_dart',
 'immolate':'abl.bounty_hunter.skill.advanced_prototype.immolate','jetcharge':'abl.bounty_hunter.skill.shield_tech.jet_charge',
 'deathfromabove':'abl.bounty_hunter.death_from_above','sweepingblasters':'abl.bounty_hunter.sweeping_blasters',
 'fusionmissile':'abl.bounty_hunter.fusion_missile','powershot':'abl.bounty_hunter.power_shot',
 'thermalsensoroverride':'abl.bounty_hunter.thermal_sensor_override','koltomissile':'abl.bounty_hunter.skill.bodyguard.kolto_missile',
 'koltoshell':'abl.bounty_hunter.skill.bodyguard.kolto_shell','concussionmissile':'abl.bounty_hunter.concussion_missile',
 'jetboost':'abl.bounty_hunter.jet_boost','missileblast':'abl.bounty_hunter.missile_blast',
 # No April player Thermal Detonator exists; the Bounty Hunter boss Sorno's April ability supplies the icon binding.
 'thermaldetonator':'abl.qtr.1x1.raid.karaggas_palace.enemy.boss.boss_2_bh.sorno.difficulty_1.thermal_detonator','basicrapid':'abl.bounty_hunter.rapid_shots','combustiblegas':'abl.bounty_hunter.combustible_gas_cylinder',
 # Run cards: Reinforced Plating and Jet Thrusters reuse their April passive-skill icons.
 'powertech_plating':'abl.bounty_hunter.skill.advanced_prototype.power_armor','mercenary_plating':'abl.bounty_hunter.skill.bodyguard.reactive_armor',
 'bh_thrusters':'abl.bounty_hunter.skill.shield_tech.jet_speed'}
FXSPECS=['bh_retractable_blade_caster','bh_retractable_blade_target','bh_explosive_dart_explosion','bh_explosive_dart_projectile',
 'bh_electro_dart_01','bh_electro_dart_target','bh_electro_dart_projectile','bh_immolate_flame_ignite_v02','bh_jet_charge_01',
 'bh_fusion_missile_cast','bh_fusion_missile_fire','bh_tactical_nuke_explosion','bh_powershot_projectile','bh_thermal_override',
 'bh_thermal_overload','bh_kolto_missile_aoe','bh_kolto_missile_projectile','bh_kolto_shell_cast','bh_kolto_shell_proc_heal',
 'bh_concussion_missile_impact','bh_concussion_missile_projectile','bh_jetboost_rev1','bh_marktarget_target_01','bh_armormelt_01',
 'bh_rocketblast_muzzleflash','bh_missileblast_fakeprojectile','bh_missile_blast_splash_damage','bh_sonic_missile_muzzleflash_01','bh_missletracking_redoverlay_01','jetpack_standard_looping']
# Animation-network names in the epp records resolve to these bmnnew JBA stems.
CLIPS={'RetractableBlade':'cb_bounty_retractable_blade','ImmolateStart':'cb_immolate_start','ImmolateLoop':'cb_immolate_loop',
 'ImmolateEnd':'cb_immolate_end','JetCharge':'cb_jet_charge','DeathFromAbove':'cb_2pistol_death_from_above',
 'PowerShotStart':'cb_2pistol_fire_special_start_2','PowerShotLoop':'cb_2pistol_fire_special_loop_2','PowerShotEnd':'cb_2pistol_fire_special_end_2',
 'SweepLoop':'cb_2pistol_sweep_loop','MissileStrikeStart':'cb_bounty_missile_strike_start','MissileStrikeLoop':'cb_bounty_missile_strike_loop',
 'MissileStrikeEnd':'cb_bounty_missile_strike_end','JetBoost':'cb_buff_determination'}
EVENTS=['Play_ability_bountyhunter_retractableblade_preswing','Play_ability_bountyhunter_retractableblade_swing','Play_ability_bountyhunter_retractableblade_impact',
 'Play_ability_bountyhunter_railshot_shotcharge','Play_ability_bountyhunter_railshot_shot','Play_ability_bountyhunter_electrodart_fire',
 'Play_ability_bountyhunter_electrodart_staticburst','Play_ability_bountyhunter_electrodart_hit','Play_ability_bountyhunter_immolate_activate',
 'Play_ability_bountyhunter_jetcharge_jetpackactivate','Play_ability_bountyhunter_jetcharge_jetpackend','Play_ability_bountyhunter_jetcharge_hit',
 'Play_ability_bountyhunter_rocketboost_buttonpress','Play_ability_bountyhunter_deathfromabove_activate','Play_ability_bountyhunter_deathfromabove_jetpacksmallburst',
 'Play_ability_bountyhunter_powershot_shot','Play_ability_bountyhunter_koltoshell_cast','Play_ability_bountyhunter_concussionmissiles_bracerfire',
 'Play_ability_bountyhunter_thermalsensoroverride_braceractivate','Play_ability_bountyhunter_thermalsensoroverride_alarm',
 'Play_ability_bountyhunter_thermalsensoroverride_discharge_primary','Play_ability_bountyhunter_missilefan_getoverhere','Play_ability_bountyhunter_missileblast_muzzleblast']

def icon_spec(node):
    rows=json.loads((ROOT/'work/bh_icons_april.json').read_text())
    return rows[node]['icon'],rows[node]['id']

def extract_icon(src,icon,owner,node):
    path=f'/resources/gfx/icons/{icon.lower()}.dds'
    if not src.has(path):return dict(owner=owner,node=node,path=path,status='unresolved unique April entry')
    src.get(path,owner,'exact ablIconSpec')
    out=ICONS/f'{icon.lower()}.png'
    if not out.exists():Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(out)
    return dict(owner=owner,node=node,icon=icon.lower(),path=path,output=str(out.relative_to(ROOT)),sha256=sha(out),evidence='Client-derived exact ablIconSpec')

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    character_source.MANIFEST=ROOT/'manifests/bounty-hunter-source.json'
    if not character_source.MANIFEST.exists():
        character_source.MANIFEST.write_text(json.dumps(dict(profile='April2012 (operator labelled)',maximumFiles=600,
            maximumDecodedBytes=200*1024*1024,resources=[],dependencies=[],unresolved=[],selection='Bounty Hunter advanced-class kits')))
    src=character_source.SelectedSource();icons=[]
    for ability,node in ABILITIES.items():
        icon,_=icon_spec(node);icons.append(extract_icon(src,icon,ability,node))
    talents=json.loads((ROOT/'godot/Gameplay/Talents/bounty_hunter.json').read_text())
    for t in talents['Talents']:
        node=t['Source'].split(' ')[0];icon,_=icon_spec(node);icons.append(extract_icon(src,icon,t['Id'],node))
    (ROOT/'manifests/bounty-hunter-icons.json').write_text(json.dumps(icons,indent=2))
    clips={}
    for name,stem in CLIPS.items():
        path=f'/resources/anim/humanoid/bmnnew/{stem}.jba'
        if src.has(path):src.get(path,name,'April epp animation-network stem');clips[name]=stem
        else:src.manifest['unresolved'].append(dict(path=path,status='No unique April entry'))
    (ROOT/'manifests/bountyhunter-conversion.json').write_text(json.dumps(dict(clips=clips),indent=2))
    queue=['/resources/art/fx/fxspec/abilities/bounty_hunter/'+p+'.fxspec' for p in FXSPECS]
    # epp.npc.ability.tech.thermal_detonator (used by Sorno) throws the shared smuggler detonator FX.
    queue+=['/resources/art/fx/fxspec/abilities/smuggler/'+p+'.fxspec' for p in ['sm_thermaldetonator_01','sm_thermaldetonator_explosion_01']]
    seen=set();configs={};fx={};textures=[]
    while queue:
        path=queue.pop(0)
        if path in seen:continue
        seen.add(path)
        if not src.has(path):
            src.manifest['unresolved'].append(dict(path=path,status='No unique April entry'));continue
        data=src.get(path,'Bounty Hunter dependency traversal','FXSpec / particle / material dependency')
        if path.endswith('.dds'):
            dest=OUT/(Path(path).stem+'.png')
            if not dest.exists():Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(dest)
            textures.append(dict(source=path,output=str(dest.relative_to(ROOT)),sha256=sha(dest)));continue
        if path.endswith(('.gr2','.jba')):continue
        if path.endswith('.prt'):fields=re.findall(r'^\s*\.(\w+)=(.*)$',data.decode(errors='replace').replace('\r',''),re.M)
        else:
            xml=decode_xml(data);fields=[(f.get('name',f.tag),f.text or '') for f in xml.iter() if len(f)==0]
            if path.endswith('.mat'):
                fields+=[(n.findtext('semantic'),n.findtext('value').replace('\\','/')+'.dds') for n in xml.findall('input') if n.findtext('type')=='texture']
            if path.endswith('.fxspec'):fx[Path(path).stem]=unpack(xml.find('.//marshalData/node'))
        configs[path]=fields
        for field,value in fields:
            value=value.strip().strip('"').replace('\\','/').lower()
            if value.endswith(('.dds','.prt','.mat','.fxspec')):
                target='/resources/'+value.lstrip('/')
                if '/' not in value and value.endswith('.prt'):target='/resources/art/fx/particles/'+value
                if '/' not in value and value.endswith('.fxspec'):target='/resources/art/fx/fxspec/'+value
                if src.has(target):queue.append(target)
                else:
                    row=dict(source=path,field=field,target=target,status='Engine primitive' if '/engine/' in target else 'Missing unique April entry')
                    if row not in src.manifest['unresolved']:src.manifest['unresolved'].append(row)
    character_source.MANIFEST.write_text(json.dumps(src.manifest,indent=2))
    (OUT/'source-parameters.json').write_text(json.dumps(configs,indent=2));(OUT/'fx.json').write_text(json.dumps(fx,indent=2))
    (ROOT/'manifests/bounty-hunter-fx.json').write_text(json.dumps(dict(textures=textures),indent=2))
    print(json.dumps(dict(icons=len(icons),unresolvedIcons=[i for i in icons if i.get('status')],clips=clips,textures=len(textures),unresolved=len(src.manifest['unresolved']))))

def audio():
    code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/d3/ranged.xml'").replace('abilities_imperialmelee','abilities_imperialranged').replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
    n={'__name__':'bountyhunteraudio'};exec(compile(code,'bounty-hunter-audio','exec'),n);n['main']('bountyhunter','BountyHunter',EVENTS)

def republic_audio():
    """Kolto Missile and Explosive Dart epp records call shared Trooper events from the Republic ranged bank."""
    import subprocess
    character_source.MANIFEST=ROOT/'manifests/bounty-hunter-source.json';src=character_source.SelectedSource()
    for bank in ['data','media']:src.get(f'/resources/bnk2/abilities_republicranged_{bank}.bnk','Kolto Missile / Explosive Dart','April shared Trooper epp audio graph')
    work=ROOT/'work/bountyhunter-republic';work.mkdir(parents=True,exist_ok=True)
    subprocess.run([sys.executable,str(ROOT/'tools/wwiser.pyz'),str(SOURCE/'resources/bnk2/abilities_republicranged_data.bnk'),'-d','xml','-dn',str(work/'bank')],check=True,stdout=subprocess.DEVNULL)
    assets=work/'assets';assets.mkdir(exist_ok=True);(assets/'source-parameters.json').write_text('{}')
    code=(ROOT/'scripts/prepare_ability_audio.py').read_text().replace("ROOT/'work/lightning/bank.xml'","ROOT/'work/bountyhunter-republic/bank.xml'").replace('abilities_imperialmelee','abilities_republicranged').replace('##(\\d+)\\.wem','[?#](\\d+)\\.wem')
    code=code.replace("ROOT/f'godot/Assets/{asset_dir}/source-parameters.json'","ROOT/'work/bountyhunter-republic/assets/source-parameters.json'").replace("ROOT/f'godot/Assets/{asset_dir}/Audio'","ROOT/'work/bountyhunter-republic/assets/Audio'")
    n={'__name__':'bountyhunterrepublic'};exec(compile(code,'bounty-hunter-republic-audio','exec'),n)
    n['main']('bountyhunter-republic','BountyHunterRepublic',['Play_ability_trooper_koltobomb_explode','Play_ability_trooper_stickygrenade_attach','Play_ability_smuggler_thermaldetonator_buttonpress',
        'Play_ability_smuggler_thermaldetonator_toss','Play_ability_smuggler_thermaldetonator_projectile','Play_ability_smuggler_thermaldetonator_explosion','Play_ability_smuggler_thermaldetonator_explosioncore'])
    merged=json.loads((OUT/'audio.json').read_text());extra=json.loads((assets/'audio.json').read_text())
    merged['events'].update(extra['events']);merged['graphs'].update(extra['graphs']);merged['media']+=extra['media']
    merged['missingEvents']=[e for e in merged['missingEvents'] if extra['events'].get(e) not in extra['graphs']]
    for f in (assets/'Audio').glob('*.wav'):(OUT/'Audio'/f.name).write_bytes(f.read_bytes())
    (OUT/'audio.json').write_text(json.dumps(merged,indent=2));(ROOT/'manifests/bountyhunter-audio.json').write_text(json.dumps(merged,indent=2))
    print(json.dumps(dict(events=extra['events'],graphs={k:v['media'] for k,v in extra['graphs'].items()},missing=merged['missingEvents'])))

if __name__=='__main__':
    audio() if '--audio' in sys.argv else republic_audio() if '--republic-audio' in sys.argv else main()
