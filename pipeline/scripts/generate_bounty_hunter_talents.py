"""Writes Gameplay/Talents/bounty_hunter.json. Icons are the exact April ablIconSpec of each named talent node
(work/bh_icons_april.json, read through the research MCP); behaviour is a Survivors adaptation."""
import json
from source_pipeline import ROOT
AP='abl.bounty_hunter.skill.advanced_prototype.';FB='abl.bounty_hunter.skill.firebug.';ST='abl.bounty_hunter.skill.shield_tech.'
AR='abl.bounty_hunter.skill.arsenal.';BG='abl.bounty_hunter.skill.bodyguard.'
def E(kind,ability='',key='',amount=1):return dict(Kind=kind,Ability=ability,Key=key,Amount=amount)
TREES={
'powertech':[('advanced_prototype','Advanced Prototype',[
 ('prototype_blade_vents',AP+'prototype_blade_vents','Prototype Blade Vents','Retractable Blade and its bleed deal 30% more damage.',[E('damage','retractableblade',amount=.3)]),
 ('prototype_weapon_systems',AP+'prototype_weapon_systems','Prototype Weapon Systems','Thermal Detonator gains 25% instead of 15% per bleeding enemy in its blast, and its blast refreshes your Retractable Blade bleed on everything it hits.',[E('hook',key='pt_td_systems',amount=.25)]),
 ('retractable_blade',AP+'retractable_blade','Retractable Blade','Your starting Flame Sweep becomes Retractable Blade, so Advanced Prototype opens on bleeds instead of burns. Explosive Dart is offered early to spread them. Applies when the run starts.',[E('hook',key='pt_dart_conversion')]),
 ('puncture',AP+'puncture','Puncture','Rail Shot deals 40% more damage to enemies bleeding from your Retractable Blade.',[E('hook',key='pt_puncture',amount=.4)]),
 ('prototype_electro_surge',AP+'prototype_electro_surge','Prototype Electro Surge','Electro Dart stuns one more enemy, and its stun lasts 1 second longer.',[E('hook',key='pt_electro_surge')]),
 ('iron_fist',AP+'iron_fist','Iron Fist','Rocket Punch cleaves enemies around its target for 60% damage and applies your Retractable Blade bleed to every enemy it hits.',[E('hook',key='pt_iron_fist')]),
 ('prototype_cylinders',AP+'prototype_cylinders','Prototype Cylinders','Explosive Dart spreads only your Retractable Blade bleed, never burns, but its blast deals 40% more damage, covers 30% more ground, and each newly bled enemy immediately takes 50% of Retractable Blade damage.',[E('hook',key='pt_blade_focus',amount=.4)]),
 ('flame_barrage',AP+'flame_barrage','Flame Barrage','Rocket Punch hits recover 3 seconds of Flame Thrower cooldown, and Flame Thrower deals 25% more damage to bleeding enemies.',[E('hook',key='pt_flame_barrage',amount=3)]),
 ('advanced_tools',AP+'advanced_tools','Advanced Tools','Enemies stunned by Electro Dart take 25% more damage from all your abilities.',[E('hook',key='pt_advanced_tools',amount=.25)])]),
 ('pyrotech','Pyrotech',[
 ('combustion',FB+'combustion','Combustion','Incendiary Missile and its burn deal 25% more damage.',[E('damage','incendiarymissile',amount=.25)]),
 ('bursting_flame',FB+'bursting_flame','Bursting Flame','Flame Burst deals 25% more damage and Immolate 15% more.',[E('damage','flameburst',amount=.25),E('damage','immolate',amount=.15)]),
 ('sweltering_heat',FB+'sweltering_heat','Sweltering Heat','Incendiary Missile and Immolate burn 2 seconds longer.',[E('duration','incendiarymissile',amount=2),E('duration','immolate',amount=2)]),
 ('fanned_flames',FB+'fanned_flames','Fanned Flames','Flame Sweep spreads your burns to 3 more enemies per activation.',[E('hook',key='pt_fanned',amount=3)]),
 ('superheated_rail',FB+'superheated_rail','Superheated Rail','Rail Shot on a burning enemy spreads your burns to up to 4 nearby enemies.',[E('hook',key='pt_superheated_rail',amount=4)]),
 ('rain_of_fire',FB+'rain_of_fire_powertech','Rain of Fire','Ignited Oil Slicks deal 50% more damage and burn 3 seconds longer.',[E('hook',key='pt_rain_of_fire',amount=.5)]),
 ('prototype_particle_accelerator',FB+'prototype_particle_accelerator_powertech','Prototype Particle Accelerator','Flame Burst hits on burning enemies recover 2 seconds of Rail Shot and Immolate cooldown.',[E('hook',key='pt_accelerator',amount=2)]),
 ('infrared_detection',FB+'infrared_detection','Infrared Detection','Immolate and Flame Burst deal an extra 25% damage to burning enemies.',[E('hook',key='pt_infrared',amount=.25)]),
 ('incendiary_payload',FB+'explosive_dart','Incendiary Payload','Explosive Dart applies your Incendiary Missile burn to every enemy it hits.',[E('hook',key='pt_incendiary_dart')])]),
 ('shield_tech','Shield Tech',[
 ('ion_overload',ST+'ion_overload','Ion Overload','Energy Shield absorbs 25% more damage.',[E('shield','ionshield',amount=.25)]),
 ('supercooled_carbonite',ST+'supercooled_carbonite','Supercooled Carbonite','Carbonize reaches 25% farther, and carbonized enemies take 20% more damage from you.',[E('radius','carbonize',amount=.25),E('hook',key='pt_supercooled',amount=.2)]),
 ('jet_speed',ST+'jet_speed','Jet Speed','Move 3% faster.',[E('hook',key='pt_jet_speed',amount=.03)]),
 ('flame_shield',ST+'flame_shield','Flame Shield','While Energy Shield holds, close attackers are scorched for 8 damage and catch your Incendiary Missile burn. Feeds Pyrotech.',[E('hook',key='pt_flame_shield')]),
 ('jet_charge',ST+'jet_charge','Jet Charge','Jet Charge refreshes your bleeds and burns on every enemy it hits. Feeds both damage specs.',[E('hook',key='pt_jet_refresh')]),
 ('rail_loaders',ST+'rail_loaders','Rail Loaders','Rail Shot recovers 25% faster and Rocket Punch 15% faster. Feeds Advanced Prototype.',[E('cooldown','railshot',amount=.25),E('cooldown','rocketpunch',amount=.15)]),
 ('heat_screen',ST+'heat_screen','Heat Screen','Take 10% less damage while Energy Shield or Explosive Fuel is active.',[E('hook',key='pt_heat_screen',amount=.1)]),
 ('oil_slick',ST+'oil_slick','Oil Slick','Oil Slick covers 25% more ground and lasts 4 seconds longer.',[E('radius','oilslick',amount=.25),E('hook',key='pt_oil_duration',amount=4)]),
 ('heat_blast',ST+'heat_blast','Heat Blast','When Energy Shield ends it detonates for 24 damage around you, applying your Incendiary Missile burn and Retractable Blade bleed to enemies hit if you own them.',[E('hook',key='pt_heat_blast',amount=24)])])],
'mercenary':[('arsenal','Arsenal',[
 ('tracer_lock',AR+'tracer_lock','Tracer Lock','Heat Signature stacks up to 7 instead of 5.',[E('hook',key='mc_heat_max',amount=2)]),
 ('tracer_missile',AR+'tracer_missile','Tracer Missile','Your starting Power Shot becomes Tracer Missile: a splash missile that marks its target with Heat Signature. Applies when the run starts.',[E('hook',key='mc_tracer_conversion')]),
 ('riddle',AR+'riddle','Riddle','Unload deals 25% more damage and Missile Blast 20% more.',[E('damage','unload',amount=.25),E('damage','missileblast',amount=.2)]),
 ('barrage',AR+'barrage','Barrage','Every 2nd Power Shot or Tracer Missile cast, instead of every 4th, readies Unload.',[E('hook',key='mc_barrage')]),
 ('mandalorian_iron_warheads',AR+'mandalorian_iron_warheads','Mandalorian Iron Warheads','Heatseeker Missiles deal 25% more damage and Death From Above 20% more.',[E('damage','heatseeker',amount=.25),E('damage','deathfromabove',amount=.2)]),
 ('light_em_up',AR+'light_em_up',"Light 'em Up","Heatseeker Missiles consume only half of the target's Heat Signature.",[E('hook',key='mc_light_em_up')]),
 ('terminal_velocity',AR+'terminal_velocity','Terminal Velocity','Death From Above deals 10% more damage per Heat Signature stack on each enemy it hits.',[E('hook',key='mc_terminal',amount=.1)]),
 ('upgraded_arsenal',AR+'upgraded_arsenal','Upgraded Arsenal','Tracer Missile and Power Shot apply 2 Heat Signature stacks to their primary target, and Tracer Missile splashes 25% wider.',[E('hook',key='mc_upgraded'),E('radius','tracer',amount=.25)]),
 ('pinning_fire',AR+'pinning_fire','Pinning Fire','Unload and Missile Blast slow enemies by 40% for 2 seconds, and Unload adds 1 Heat Signature per hit, once per enemy per cast.',[E('hook',key='mc_pinning')])]),
 ('innovative_ordnance','Innovative Ordnance',[
 ('pyrotech',FB+'pyrotech','Pyrotech','Incendiary Missile deals 25% more damage and Fusion Missile 15% more, burns included.',[E('damage','incendiarymissile',amount=.25),E('damage','fusionmissile',amount=.15)]),
 ('advanced_targeting',FB+'advanced_targeting','Advanced Targeting','Power Shot and Rail Shot deal 20% more damage.',[E('damage','powershot',amount=.2),E('damage','railshot',amount=.2)]),
 ('sweltering_heat',FB+'sweltering_heat','Sweltering Heat','Incendiary Missile and Fusion Missile burn 2 seconds longer.',[E('duration','incendiarymissile',amount=2),E('duration','fusionmissile',amount=2)]),
 ('combustible_gas_cylinder','abl.bounty_hunter.combustible_gas_cylinder','Combustible Gas Cylinder','Power Shot and Rail Shot ignite enemies that are not already burning with a half-strength Incendiary Missile burn.',[E('hook',key='mc_gas')]),
 ('superheated_rail',FB+'superheated_rail','Superheated Rail','Rail Shot on a burning enemy spreads your burns to up to 4 nearby enemies.',[E('hook',key='mc_superheated_rail',amount=4)]),
 ('rain_of_fire',FB+'rain_of_fire','Rain of Fire','Fusion Missile covers 30% more ground and Sweeping Blasters reaches 20% farther.',[E('radius','fusionmissile',amount=.3),E('radius','sweepingblasters',amount=.2)]),
 ('prototype_particle_accelerator',FB+'prototype_particle_accelerator','Prototype Particle Accelerator','Power Shot hits recover 2 seconds of Rail Shot cooldown.',[E('hook',key='mc_accelerator',amount=2)]),
 ('prototype_heat_dampers',FB+'prototype_heat_dampers','Prototype Heat Dampers','Thermal Sensor Override recovers 30% faster and also fully readies Fusion Missile.',[E('cooldown','thermalsensoroverride',amount=.3),E('hook',key='mc_tso_fusion')]),
 ('bursting_flame',FB+'bursting_flame','Bursting Flame','Power Shot and Rail Shot deal an extra 15% damage per distinct damage-over-time effect you own on the target.',[E('hook',key='mc_dot_bonus',amount=.15)])]),
 ('bodyguard','Bodyguard',[
 ('med_tech',BG+'med_tech','Med Tech','Kolto Missile recovers 20% faster. Its healing cap is unchanged.',[E('cooldown','koltomissile',amount=.2)]),
 ('energy_overrides',BG+'energy_overrides','Energy Overrides','Energy Shield absorbs 25% more damage and recovers 15% faster.',[E('shield','ionshield',amount=.25),E('cooldown','ionshield',amount=.15)]),
 ('critical_efficiency',BG+'critical_efficiency','Critical Efficiency','Concussion Missile and Jet Boost recover 20% faster.',[E('cooldown','concussionmissile',amount=.2),E('cooldown','jetboost',amount=.2)]),
 ('kolto_shell',BG+'kolto_shell','Kolto Shell','Kolto Shell can mend you 2 more times per activation. Each mend stays 1 HP.',[E('hook',key='mc_shell_charges',amount=2)]),
 ('proactive_medicine',BG+'proactive_medicine','Proactive Medicine','While you stand in a Kolto Missile field your abilities deal 12% more damage. Feeds Arsenal and Innovative Ordnance.',[E('hook',key='mc_field_damage',amount=.12)]),
 ('warden',BG+'warden','Warden','Concussion Missile gives stunned enemies 2 Heat Signature stacks and, if you own it, your Incendiary Missile burn.',[E('hook',key='mc_warden')]),
 ('supercharge',BG+'supercharge','Supercharge','Activating a utility ability grants 15% more damage for 5 seconds.',[E('hook',key='mc_supercharge',amount=.15)]),
 ('reactive_armor',BG+'reactive_armor','Reactive Armor','Take 8% less damage while Kolto Shell or Energy Shield is active.',[E('hook',key='mc_reactive',amount=.08)]),
 ('empowered_scans',BG+'empowered_scans','Empowered Scans','Jet Boost ignites the enemies it throws back with your Incendiary Missile burn and gives them 1 Heat Signature stack.',[E('hook',key='mc_jet_burn')])])]}

def main():
    icons=json.loads((ROOT/'work/bh_icons_april.json').read_text());talents=[];trees=[]
    for cls,rows in TREES.items():
        for tree,name,entries in rows:
            trees.append(dict(Id=tree,Name=name))
            for i,(key,node,title,text,effects) in enumerate(entries):
                spec=icons[node]
                talents.append(dict(Id=f'{tree}.{key}',Classes=[cls],Tree=tree,Tier=i//3,Column=i%3,Icon=f"res://Assets/UI/icons/{spec['icon'].lower()}.png",
                    Name=title,Description=text,MaxRank=1,Cost=1,RequiredPoints=[0,2,5][i//3],Effects=effects,
                    Source=f"{node} (April ablIconSpec {spec['id']}; survivor adaptation)"))
    (ROOT/'godot/Gameplay/Talents/bounty_hunter.json').write_text(json.dumps(dict(Version=1,Budget=12,Trees=trees,Talents=talents),indent=2)+'\n')
    print(len(talents),sorted({t['Icon'].rsplit('/',1)[1] for t in talents}))
if __name__=='__main__':main()
