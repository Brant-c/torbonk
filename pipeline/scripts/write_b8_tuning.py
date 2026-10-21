"""B8 authored Survivors tuning; source facts live in separate manifests."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]/'godot/Gameplay'
def resource(folder,script,name,fields):
    text=f'[gd_resource type="Resource" script_class="{script}" load_steps=2 format=3]\n\n[ext_resource type="Script" path="res://Gameplay/{script}.cs" id="1"]\n\n[resource]\nscript = ExtResource("1")\n'
    for key,value in fields.items():
        if isinstance(value,str):value='"'+value+'"'
        elif isinstance(value,bool):value=str(value).lower()
        text+=f'{key} = {value}\n'
    (ROOT/folder/(name+'.tres')).write_text(text)
for id,title,icon,fields in [
    ('deathfield','Death Field','deathfield',dict(Damage=35,DamageType=2,Healing=3,HealingCap=15,CooldownSeconds=15,Range=1.1,Radius=0.34,
        TargetRule=1,Area=1,WindupSeconds=0.3,RecoverySeconds=0.75,GestureSeconds=0.65,StartClip='B8/DeathField',
        SourceFx='si_death_field_aoe',ImpactAudioEvent='Play_ability_sithinquisitor_deathfield_impact')),
    ('barrier','Static Barrier','electricshield',dict(Effect=1,Damage=0,ShieldCapacity=30,ShieldDuration=12,CooldownSeconds=18,Range=0.8,
        UsesGlobalCooldown=False,TriggersGlobalCooldown=False,WindupSeconds=0,RecoverySeconds=0.45,GestureSeconds=0.45,
        StartClip='B8/Barrier',SourceFx='si_backlash_caster_shield',CastAudioEvent='Play_ability_sithinquisitor_backlash_chargeup')),
    ('darkheal','Dark Heal','darkgift',dict(Effect=2,Damage=0,Healing=25,HealthThreshold=0.5,CooldownSeconds=25,
        UsesGlobalCooldown=False,TriggersGlobalCooldown=False,WindupSeconds=0,RecoverySeconds=0.65,GestureSeconds=0.65,
        StartClip='B8/DarkHeal',SourceFx='si_darkgift_target_01',ImpactAudioEvent='Play_ability_sithinquisitor_darkgift_cast'))]:
    resource('Abilities','AbilityDefinition',id,dict(Id=id,Title=title,Icon=icon,StartsUnlocked=False,ChannelSeconds=0,TickSeconds=1,**fields))
    resource('Upgrades','UpgradeDefinition','unlock_'+id,dict(Id='unlock_'+id,Title=title,Icon=icon,Effect=7,AbilityId=id,MaximumRank=1,AmountPerRank=0))
for id,title,icon,rarity,fields in [
    ('deathfield','Devouring Field','deathfield',2,dict(DamagePerRank=0.15,RadiusPerRank=0.08,HealingPerRank=0.15)),
    ('barrier','Enduring Barrier','electricshield',1,dict(ShieldPerRank=0.2,CooldownPerRank=0.1,DurationPerRank=0.15)),
    ('darkheal','Dark Restoration','darkgift',0,dict(HealingPerRank=0.2,CooldownPerRank=0.1,ThresholdPerRank=0.03))]:
    resource('Upgrades','UpgradeDefinition',id+'_power',dict(Id=id+'_power',Title=title,Icon=icon,Rarity=rarity,Effect=8,AbilityId=id,MaximumRank=5,**fields))
