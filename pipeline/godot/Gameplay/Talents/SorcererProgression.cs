using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

public sealed partial class TalentRuntime
{
    // Force Lightning stays the starter, but no power card scales it: growth goes to the rest of the kit.
    // Every card below moves exactly one axis, nothing is rank-capped, and rarity scales that one number.
    private static readonly (string id,string title,float amount)[] SorcererDamageCards={
        ("lightningstrike","Striking Current",.95f),("chain","Arc Conductor",.95f),("storm","Tempest Surge",.95f),("thunderingblast","Thunderhead",1f),
        ("affliction","Festering Torment",.9f),("creepingterror","Spreading Dread",.9f),("crushingdarkness","Crushing Despair",.9f),("deathfield","Devouring Field",.9f)};
    // Force Storm and Death Field have their own radius/cooldown cards; Death Field cooldown is the Gathering Darkness resource.
    private static readonly (string id,string title,float amount)[] SorcererRadiusCards={
        ("thunderingblast","Wider Blast",.2f),("deathfield","Wider Field",.2f),("lightningstrike","Arc Reach",.18f),("shock","Forked Reach",.18f)};
    // Cast range: how far away each ability starts working.
    private static readonly (string id,string title,float amount)[] SorcererRangeCards={
        ("lightning","Lightning Reach",.2f),("lightningstrike","Long Current",.2f),("chain","Long Arc",.2f),("thunderingblast","Long Thunder",.2f),
        ("shock","Long Shock",.2f),("storm","Distant Storm",.2f)};
    // Single-target DoTs (Affliction, Creeping Terror, Crushing Darkness) deliberately have no recovery card.
    private static readonly (string id,string title,float amount)[] SorcererRecoveryCards={
        ("lightningstrike","Quickened Current",.18f),("chain","Quickened Arc",.18f),("storm","Quickened Storm",.18f),("thunderingblast","Quickened Thunder",.18f),
        ("shock","Quickened Shock",.18f),("overload","Quickened Overload",.18f)};
    private static readonly (string id,string title,float amount)[] SorcererTickCards={
        ("affliction","Festering Pulse",.15f),("creepingterror","Creeping Pulse",.15f),("crushingdarkness","Crushing Pulse",.15f),
        ("lightning","Rapid Lightning",.15f),("storm","Rapid Storm",.15f)};

    public IEnumerable<UpgradeDefinition> SorcererRunPowers()
    {
        foreach(var (id,title,amount) in SorcererDamageCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="sorcerer_power_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,DamagePerRank=amount};
        }
        foreach(var (id,title,amount) in SorcererRadiusCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="sorcerer_radius_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,RadiusPerRank=amount,
                RequiresUpgrade=id=="lightningstrike"?"strike_cleave":id=="shock"?"shock_cleave":""};
        }
        foreach(var (id,title,amount) in SorcererRangeCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="sorcerer_range_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.Range,AmountPerRank=amount};
        }
        foreach(var (id,title,amount) in SorcererRecoveryCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="sorcerer_cooldown_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,CooldownPerRank=amount};
        }
        if(_world.Abilities.Find("darkheal") is {} heal)
            yield return new UpgradeDefinition {Id="sorcerer_healing_darkheal",Title="Dark Restoration",Icon=heal.Definition.Icon,AbilityId="darkheal",Effect=UpgradeEffect.AbilityPower,HealingPerRank=.05f};
        foreach(var (id,title,amount) in SorcererTickCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="sorcerer_tick_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.TickRate,AmountPerRank=amount};
        }
    }

    // Run-only mechanics (no pregame talent twin). Spread cards stack without a cap; the rest fire once with an internal cooldown.
    public IEnumerable<UpgradeDefinition> SorcererMechanics()
    {
        UpgradeDefinition Card(string id,string title,string icon,string spec,string mechanic,string text,int maximum=1,string requires="")=>
            new UpgradeDefinition {Id="sorcerer_"+id,Title=title,Icon=icon,Spec=spec,Effect=UpgradeEffect.Mechanic,Mechanic=mechanic,MaximumRank=maximum,MechanicalDescription=text,RequiresUpgrade=requires};
        yield return Card("static_reserve","Static Reserve","forcestorm","lightning","so_static_reserve","Defeating an enemy recovers 1 second of Force Storm cooldown. At most once every 0.5 seconds.");
        yield return Card("overcharge","Overcharge","thundering_blast","lightning","so_overcharge","Each enemy beyond the first struck by one Thundering Blast recovers 0.5 seconds of Chain Lightning cooldown, up to 2 seconds per cast.");
        yield return Card("arc_charge","Arc Charge","chainlightning","lightning","so_chain_storm","Chain Lightning hits recover 0.4 seconds of Force Storm cooldown, at most once every 0.5 seconds.");
        yield return Card("rot_bloom","Rot Bloom","creepingterror","madness","so_rot_bloom","Death Field hits add 2 seconds to Creeping Terror on enemies that already carry it (never beyond its full duration). At most once every 0.5 seconds.");
        yield return Card("spread_count","Contagion","creepingterror","madness","so_spread_count","Each rank: when an enemy carrying your damage over time dies, your effects spread to one more nearby enemy, keeping their remaining duration.",0);
        yield return Card("spread_reach","Wider Contagion","creepingterror","madness","so_spread_reach","Each rank: the extra Contagion spreads reach 0.06 farther. Requires Contagion.",0,"sorcerer_spread_count");
    }

    // Talent-tree mechanics double as run drops, like Marauder's Carnage tree; ranks never stack with the pregame talent.
    public IEnumerable<UpgradeDefinition> SorcererDrops()
    {
        foreach(var t in TalentCatalog.Current.ForClass("sorcerer").Where(t=>t.Effects.Any(e=>e.Kind=="hook" && e.Key.StartsWith("so_",StringComparison.Ordinal)))) {
            var e=t.Effects.Single(x=>x.Kind=="hook");
            yield return new UpgradeDefinition {Id="sorcerer_"+e.Key[3..],Title=t.Name,Icon=System.IO.Path.GetFileNameWithoutExtension(t.Icon),
                Spec=t.Tree,Effect=UpgradeEffect.Mechanic,Mechanic=e.Key,MaximumRank=1,MechanicalDescription=t.Description};
        }
    }
    public int SorcererTalentRank(string key)=>Hook(key)>0?1:0;
    private bool So(string key)=>Hook(key)>0 || SoRanks(key)>0;
    private int SoRanks(string key)=>_world.Progression is {} p?p.Upgrades.Where(u=>u.Mechanic==key).Sum(p.Rank):0;
    private bool SoDot(Combatant target,int minimum)=>new[]{"affliction","creepingterror","crushingdarkness","deathfield"}
        .Count(id=>_world.Abilities.Find(id) is {} a && _world.Periodic.Has(a,target))>=minimum;

    public bool SorcererUpgradeUsable(UpgradeDefinition u)
    {
        bool Has(string id)=>_world.Abilities.Find(id)?.Unlocked==true;
        bool Dot()=>new[]{"affliction","creepingterror","crushingdarkness"}.Any(Has);
        // Baseline Affliction alone is not a Madness build, so Lightning runs are not offered its cards.
        bool Madness()=>Has("creepingterror") || Has("crushingdarkness") || Has("deathfield") || TalentCatalog.Current.ForClass("sorcerer").Where(t=>t.Tree=="madness").Sum(t=>Ranks.GetValueOrDefault(t.Id))>0;
        if(u.Id is "sorcerer_power_affliction" or "sorcerer_tick_affliction")return Madness();
        return u.Mechanic switch {
            "so_heal_field"=>Has("darkheal") && Has("deathfield"),
            "so_strike_heal"=>Has("darkheal") && Has("lightningstrike"),
            "so_field_slow"=>Has("deathfield"),
            "so_siphon_heal"=>Has("darkheal") && Dot(),
            "so_surge"=>Has("darkheal"),
            "so_siphon" or "so_calcify" or "so_spread_count" or "so_spread_reach"=>Dot(),
            "so_shock_blast" or "so_blast_shock"=>Has("shock") && Has("thunderingblast"),
            "so_storm_strike"=>Has("storm") && Has("lightningstrike"),
            "so_field_afflict"=>Has("deathfield") && Has("affliction"),
            "so_dark_focus"=>Has("crushingdarkness"),
            "so_static_reserve"=>Has("storm"),
            "so_overcharge"=>Has("thunderingblast") && Has("chain"),
            "so_chain_storm"=>Has("chain") && Has("storm"),
            "so_rot_bloom"=>Has("deathfield") && Has("creepingterror"),
            _=>true
        };
    }

    private int _blastTargets;
    private void SorcererActivated(AutomaticAbility a)
    {
        string id=a.Definition.Id;
        if(id=="thunderingblast")_blastTargets=0;
        if(id=="darkheal" && So("so_heal_field"))_world.Abilities.Find("deathfield")?.ReduceCooldown(3);
    }
    private void SorcererDamaged()
    {
        var health=_world.PlayerActor.Health;
        if(!health.IsAlive)return;
        if(So("so_reprisal") && health.Current<health.Maximum*.35f && Ready("so_reprisal",20))
            foreach(var enemy in _world.Targeting.InArea(_world.PlayerActor.GlobalPosition,.35f,.35f).ToArray())
                enemy.Health.ApplyDamage(8,DamageType.Energy);
        if(So("so_surge") && health.Current<health.Maximum*.25f && Ready("so_surge",30))_world.Abilities.Find("darkheal")?.ReduceCooldown(4);
    }
    private void SorcererHit(AutomaticAbility a)
    {
        string id=a.Definition.Id;
        if(id=="shock" && Hook("barrage")>0 && Ready("barrage",10)){_barrage=4;_world.Progression?.Recalculate();}
        if(id=="shock" && So("so_shock_blast") && Ready("so_shock_blast",.5))_world.Abilities.Find("thunderingblast")?.ReduceCooldown(.5f);
        if(id=="storm" && So("so_storm_strike") && Ready("so_storm_strike",.5))_world.Abilities.Find("lightningstrike")?.ReduceCooldown(.3f);
        if(id=="lightningstrike" && So("so_strike_heal") && Ready("so_strike_heal",2))_world.Abilities.Find("darkheal")?.ReduceCooldown(.5f);
        if(id=="chain" && So("so_chain_storm") && Ready("so_chain_storm",.5))_world.Abilities.Find("storm")?.ReduceCooldown(.4f);
        if(id=="crushingdarkness" && So("so_dark_focus") && Ready("so_dark_focus",2))_world.PlayerActor.Health.ApplyHealing(1);
    }
    private void SorcererDirectHit(AutomaticAbility a,Combatant target)
    {
        string id=a.Definition.Id;
        // Madness: Force Lightning drives your damage over time harder while it channels.
        if(id=="lightning" && target.Alive && _world.RunConfiguration.AdvancedClassId=="sorcerer")
        {
            float echo=OwnedTickDamage(target,a.Caster)*.3f;
            if(echo>0)target.Health.ApplyDamage(echo,DamageType.Internal,abilityId:"lightning");
        }
        // Wrath: instead of recovering DoT cooldowns, lightning pushes the target's existing DoTs for one extra tick.
        if(id is "shock" or "chain" or "thunderingblast" && target.Alive && Hook("wrath")>0 && Every("wrath",6) && Ready("wrath",1))
        {
            float tick=OwnedTickDamage(target,a.Caster)*Hook("wrath");
            if(tick>0)target.Health.ApplyDamage(tick,DamageType.Internal,abilityId:id);
        }
        if(id=="thunderingblast" && So("so_blast_shock") && Ready("so_blast_shock",2))_world.Abilities.Find("shock")?.ReduceCooldown(2);
        if(id=="thunderingblast" && So("so_overcharge") && ++_blastTargets>1 && _blastTargets<=5)_world.Abilities.Find("chain")?.ReduceCooldown(.5f);
        if(id=="deathfield" && target.Alive && So("so_field_afflict") && _world.Abilities.Find("affliction") is {Unlocked:true} aff && !_world.Periodic.Has(aff,target))
            _world.Periodic.Apply(aff,target);
        if(id=="deathfield" && target.Alive && So("so_field_slow") && target is EnemyAgent enemy)enemy.Slow(.75f,1.5f);
        if(id=="deathfield" && target.Alive && So("so_rot_bloom") && _world.Abilities.Find("creepingterror") is {Unlocked:true} terror && _world.Periodic.Has(terror,target) && Ready("so_rot_bloom",.5))
            _world.Periodic.Extend(terror,target,2,terror.Definition.PeriodicDuration);
    }
    // One tick of every damage-over-time effect this caster has on the target, as Force Lightning's echo already measures it.
    private float OwnedTickDamage(Combatant target,Combatant caster)=>_world.Periodic.On(target).Where(x=>x.Caster==caster)
        .Sum(x=>x.Definition.PeriodicDamage*x.DamagePerTick/Math.Max(.001f,x.Definition.Damage));
    private float SorcererBonus(Combatant target)=>So("so_calcify") && SoDot(target,2)?.12f:0;
    public void SorcererKilled(Combatant dead)
    {
        bool dotted=_world.Periodic.On(dead).Any();
        if(So("so_siphon") && dotted && Ready("so_siphon",1))_world.PlayerActor.Health.ApplyHealing(2);
        if(So("so_siphon_heal") && dotted && Ready("so_siphon_heal",1))_world.Abilities.Find("darkheal")?.ReduceCooldown(1);
        var health=_world.PlayerActor.Health;
        if(So("so_kill_mend") && health.IsAlive && health.Current<health.Maximum*.5f && Ready("so_kill_mend",3))health.ApplyHealing(1);
        if(So("so_static_reserve") && Ready("so_static_reserve",.5))_world.Abilities.Find("storm")?.ReduceCooldown(1);
        // Contagion: the base transfer reaches one enemy; each rank adds another, and Wider Contagion lengthens the extra reach.
        int extra=SoRanks("so_spread_count");
        if(dotted && extra>0 && _world.RunConfiguration.AdvancedClassId=="sorcerer")
        {
            float reach=(_world.Build.Talent("last_gasp")>0?.45f:.3f)+.06f*SoRanks("so_spread_reach");
            for(int i=0;i<extra;i++)_world.Periodic.TransferOwned(_world.PlayerActor,dead,reach:reach);
        }
    }
}
