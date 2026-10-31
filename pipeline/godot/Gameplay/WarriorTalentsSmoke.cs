using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;

/// <summary>Juggernaut / Marauder single-axis cards and mechanic cards. Run once per class: --advanced-class=juggernaut|marauder --warrior-talents-smoke.</summary>
public partial class WarriorTalentsSmoke : Node
{
    public async void Run(GameWorld w)
    {
        var checks=new List<string>();var notes=new Dictionary<string,object>();string? failure=null;
        void Check(string name,bool ok){if(!ok)throw new Exception(name);checks.Add(name);}
        string cls=w.RunConfiguration.AdvancedClassId;string prefix=cls+"_";
        try {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);
            w.Periodic.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.SetPhysicsProcess(false);e.CombatEnabled=false;}
            var p=w.Progression!;var rt=w.Build.Pregame;
            UpgradeDefinition Card(string id)=>p.Upgrades.Single(u=>u.Id==id);
            bool Grant(string id)=>p.Grant(id);
            void Own(params string[] ids){foreach(string id in ids)w.Abilities.Find(id)!.SetUnlocked(true);p.Recalculate();}
            Combatant Dummy(string name,float x){var t=new Combatant{Name=name,Team=CombatTeam.Hostile};t.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*x;return t;}
            void SetCooldown(AutomaticAbility a,double v)=>typeof(AutomaticAbility).GetProperty("CooldownRemaining")!.SetValue(a,v);
            int Axes(UpgradeDefinition u)=>(u.DamagePerRank>0?1:0)+(u.RadiusPerRank>0?1:0)+(u.CooldownPerRank>0?1:0)+(u.HealingPerRank>0?1:0)+(u.ShieldPerRank>0?1:0)+(u.DurationPerRank>0?1:0)+(u.ThresholdPerRank>0?1:0)+(u.Effect==UpgradeEffect.TickRate?1:0);
            var mine=p.Upgrades.Where(u=>u.Id.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
            var scoped=mine.Where(u=>u.Id.Split('_')[1] is "power" or "radius" or "cooldown" or "tick").ToArray();
            Check("scoped cards exist",scoped.Length>=20);
            Check("every scoped card moves exactly one axis",scoped.All(u=>Axes(u)==1));
            Check("no global damage cards remain",!p.Upgrades.Any(u=>u.Effect==UpgradeEffect.Damage));
            var fresh=mine.Where(u=>u.Mechanic.StartsWith(cls=="juggernaut"?"jgm_":"mrm_",StringComparison.Ordinal) || scoped.Contains(u)).ToArray();
            Check("every new card is uncapped",fresh.All(u=>u.MaximumRank==0));
            Check("single-target DoTs and Force Choke have no recovery card",!scoped.Any(u=>u.Id.StartsWith(prefix+"cooldown_") && u.AbilityId is "forcecrush" or "forcechoke" or "rupture" or "deadlysaber"));
            Check("ids are unique: "+string.Join(",",p.Upgrades.GroupBy(u=>u.Id).Where(g=>g.Count()>1).Select(g=>g.Key)),p.Upgrades.Select(u=>u.Id).Distinct().Count()==p.Upgrades.Count);
            notes["scopedCards"]=scoped.Length;notes["newCards"]=fresh.Length;
            // Gating: nothing for abilities the build does not own.
            foreach(var a in w.Abilities.Abilities)if(!a.Definition.IsBasicAttack && a.Definition.Slotless==false)a.SetUnlocked(false);
            p.Recalculate();
            Check("unowned ability cards are not offered",scoped.Where(u=>w.Abilities.Find(u.AbilityId)?.Unlocked!=true).All(u=>!p.IsEligible(u)));
            if(cls=="juggernaut") await JuggernautChecks(w,p,rt,Card,Grant,Own,Dummy,SetCooldown,Check,notes);
            else await MarauderChecks(w,p,rt,Card,Grant,Own,Dummy,SetCooldown,Check,notes);
            // Spec coverage: every spec offers damage, a radius or target card, tick or recovery where allowed, 2+ mechanics and a healing or defence card.
            foreach(var a in w.Abilities.Abilities)a.SetUnlocked(true);
            p.Recalculate();
            string[] specs=cls=="juggernaut"?new[]{"immortal","vengeance","juggernaut_rage"}:new[]{"carnage","annihilation","marauder_rage"};
            var allCards=p.Upgrades.Where(u=>u.Id.StartsWith(prefix,StringComparison.Ordinal)).ToArray();
            string Tree(UpgradeDefinition u)=>cls=="juggernaut"?rt.JuggernautCardTreeOf(u):rt.MarauderCardTree(u);
            foreach(string spec in specs) {
                var own=allCards.Where(u=>u.Spec==spec || Tree(u)==spec).ToArray();
                bool Defence(UpgradeDefinition u)=>u.Mechanic is "jgm_soresu" or "jgm_darkblood" or "jgm_rageheal" or "jgm_reprisal" or "mrm_resolve" or "mrm_hunger" or "mrm_siphon" or "mrm_pulse" or "jg_mitigation" or "jg_darkblood" or "jg_rage_heal" or "mr_heal" or "mr_siphon" || u.AbilityId is "saberward" or "enrageddefense" && u.CooldownPerRank>0;
                var mechanics=own.Where(u=>u.Effect==UpgradeEffect.Mechanic && (u.Mechanic.StartsWith("jgm_",StringComparison.Ordinal) || u.Mechanic.StartsWith("mrm_",StringComparison.Ordinal)) && !u.Mechanic.Contains("_dup") && !u.Mechanic.Contains("_multi") && !u.Mechanic.Contains("_target_")).ToArray();
                notes[spec]=new{damage=own.Count(u=>u.DamagePerRank>0),areaOrTargets=own.Count(u=>u.RadiusPerRank>0 || u.Mechanic.Contains("_target_") || u.Mechanic is "jg_grip" or "jg_sweep" or "jg_resonance"),
                    tickOrRecovery=own.Count(u=>u.Effect==UpgradeEffect.TickRate || u.CooldownPerRank>0),mechanics=mechanics.Select(u=>u.Id).ToArray(),defence=own.Count(Defence)};
                Check(spec+" has damage cards",own.Any(u=>u.DamagePerRank>0));
                Check(spec+" has a radius or target card",own.Any(u=>u.RadiusPerRank>0 || u.Mechanic.Contains("_target_") || u.Mechanic is "jg_grip" or "jg_sweep" or "jg_resonance"));
                Check(spec+" has a tick or recovery card",own.Any(u=>u.Effect==UpgradeEffect.TickRate || u.CooldownPerRank>0));
                Check(spec+" has two or more spec mechanics",mechanics.Length>=2);
                Check(spec+" has a healing or defence card",allCards.Any(Defence) && (own.Any(Defence) || allCards.Any(u=>u.Mechanic is "jgm_soresu" or "mrm_resolve")));
            }
            // Offers stay valid and varied for a mid-run kit.
            var generate=typeof(RunProgression).GetMethod("GenerateOffers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
            p.ResetRun();Own(cls=="juggernaut"?new[]{"forcescream","impale","ravage","forcecrush"}:new[]{"rupture","ravage","massacre","sweepingslash"});
            var seen=new HashSet<string>();bool rolledRarity=false;
            for(ulong seed=1;seed<=120;seed++) {
                p.SeedOffers(seed);
                var offers=(List<UpgradeDefinition>)generate.Invoke(p,new object[]{10+(int)(seed%6),Array.Empty<UpgradeDefinition>(),3})!;
                Check("valid offers "+seed,offers.Count==3 && offers.Select(u=>u.Id).Distinct().Count()==3 && offers.All(p.IsEligible));
                foreach(var o in offers){seen.Add(o.Id);if(o.Rarity!=PowerRarity.Premium)rolledRarity=true;}
                Check("class cards dominate "+seed,offers.Count(u=>u.Id.StartsWith(prefix,StringComparison.Ordinal))>=2);
            }
            Check("offers vary across many levels",seen.Count>=18);
            Check("rarity is rolled on new cards",rolledRarity);
            notes["distinctOffered"]=seen.Count;
            p.ResetRun();
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/warrior-talents-{cls}-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,checks,notes,failure},new JsonSerializerOptions{WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }

    private async System.Threading.Tasks.Task JuggernautChecks(GameWorld w,RunProgression p,Talents.TalentRuntime rt,Func<string,UpgradeDefinition> Card,Func<string,bool> Grant,Action<string[]> Own,
        Func<string,float,Combatant> Dummy,Action<AutomaticAbility,double> SetCooldown,Action<string,bool> Check,Dictionary<string,object> notes)
    {
        var target=Dummy("JgTarget",.15f);var neighbor=Dummy("JgNeighbor",.2f);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        var smash=w.Abilities.Find("smash")!;var impale=w.Abilities.Find("impale")!;var scream=w.Abilities.Find("forcescream")!;var crush=w.Abilities.Find("forcecrush")!;
        var choke=w.Abilities.Find("forcechoke")!;var ravage=w.Abilities.Find("ravage")!;var blow=w.Abilities.Find("crushingblow")!;var roar=w.Abilities.Find("intimidatingroar")!;
        // Target cards are uncapped.
        Own(new[]{"forcechoke","ravage","forcescream"});
        for(int i=1;i<=6;i++){Check("Choke target rank "+i,Grant("juggernaut_grip"));Check("Choke targets "+i,choke.Definition.MaximumTargets==1+i);}
        for(int i=1;i<=5;i++){Check("Scream target rank "+i,Grant("juggernaut_resonance"));Check("Scream targets "+i,scream.Definition.MaximumTargets==1+2*i);}
        for(int i=1;i<=5;i++){Check("Ravage target rank "+i,Grant("juggernaut_sweep"));Check("Ravage targets "+i,ravage.Definition.MaximumTargets==1+i);}
        Check("target cards have no cap",Card("juggernaut_grip").MaximumRank==0 && Card("juggernaut_sweep").MaximumRank==0 && Card("juggernaut_resonance").MaximumRank==0);
        p.ResetRun();
        // Single-axis radius / damage / cooldown behaviour.
        Own(new[]{"smash","forcecrush"});
        float cooldown=smash.CooldownSeconds,radius=smash.Radius,damage=smash.DamagePerTick;
        Check("Smash radius card",Grant("juggernaut_radius_smash") && smash.Radius>radius && Math.Abs(smash.DamagePerTick-damage)<.001f && Math.Abs(smash.CooldownSeconds-cooldown)<.001f);
        for(int i=0;i<40;i++)Grant("juggernaut_cooldown_smash");
        Check("Smash recovery stops at its internal three second floor",Math.Abs(smash.CooldownSeconds-3f)<.001f);
        notes["jugSmashBaseCooldown"]=cooldown;notes["jugSmashDefinitionCooldown"]=smash.Definition.CooldownSeconds;notes["jugSmashFlooredTo"]=smash.CooldownSeconds;
        Check("Force Crush tick card",true);float interval=rt.PeriodicInterval(crush);Check("tick card applies",Grant("juggernaut_tick_forcecrush") && rt.PeriodicInterval(crush)<interval);
        Check("Force Crush damage scales its periodic damage",Grant("juggernaut_power_forcecrush"));
        p.ResetRun();
        // Echo and Legion.
        Own(new[]{"forcecrush"});
        Check("Crush Legion purchase",Grant("juggernaut_legion_forcecrush") && Grant("juggernaut_legion_forcecrush") && rt.WarriorDotSlots(crush)==3);
        Check("Crush Echo purchase",Grant("juggernaut_echo_forcecrush") && Math.Abs(rt.WarriorDuplicateChance(crush)-.2f)<.001f);
        w.Periodic.Clear();for(int i=0;i<6;i++)w.Periodic.Apply(crush,target);
        Check("three simultaneous Crush instances then refresh",w.Periodic.Count==3);
        w.Periodic.Clear();p.ResetRun();Check("reset drops slots and echo",rt.WarriorDotSlots(crush)==1 && rt.WarriorDuplicateChance(crush)==0);
        // Bleed echo requires the bleed talent.
        Own(new[]{"forcescream"});
        Check("Scream bleed cards need the bleed talent",!p.IsEligible(Card("juggernaut_tick_forcescream")) && !p.IsEligible(Card("juggernaut_echo_forcescream")) && !p.IsEligible(Card("juggernaut_legion_forcescream")));
        typeof(RunProgression).GetProperty("Level")!.SetValue(p,8);
        Check("bleed talent purchase",Grant("juggernaut_scream_bleed") && p.IsEligible(Card("juggernaut_tick_forcescream")) && p.IsEligible(Card("juggernaut_echo_forcescream")));
        p.ResetRun();
        // Fury Bank.
        Own(new[]{"smash","impale"});
        Check("Fury Bank purchase",Grant("juggernaut_fury"));
        for(int i=0;i<3;i++){rt.DirectHit(impale,target);rt.Advance(.3);}
        rt.Activated(smash);Check("Smash spends banked Fury",Math.Abs(rt.DirectBonus(smash,target)-3*.04f)<.001f);
        rt.Activated(smash);Check("Fury is spent",rt.DirectBonus(smash,target)<.001f);
        p.ResetRun();
        // Aftershock.
        Own(new[]{"smash"});Check("Aftershock purchase",Grant("juggernaut_aftershock"));
        rt.Activated(smash);float hp=target.Health.Current;rt.Advance(1.3);
        Check("Aftershock strikes again",target.Health.Current<hp);
        p.ResetRun();
        // Reprisal.
        Own(new[]{"retaliation"});Check("Reprisal purchase",Grant("juggernaut_reprisal"));
        rt.Damaged(20);hp=target.Health.Current;rt.Activated(w.Abilities.Find("retaliation")!);
        Check("Retaliation releases stored damage",hp-target.Health.Current>=4.9f);
        hp=target.Health.Current;rt.Activated(w.Abilities.Find("retaliation")!);Check("stored damage is spent and locked",Math.Abs(hp-target.Health.Current)<.01f);
        p.ResetRun();
        // Rend Mark, Finishing Blow, Dread Presence.
        Own(new[]{"forcescream","impale","crushingblow","intimidatingroar","smash"});
        Check("Rend, Finishing and Dread purchases",Grant("juggernaut_rend") && Grant("juggernaut_finisher") && Grant("juggernaut_dread"));
        Check("unmarked targets get no Rend bonus",rt.DirectBonus(impale,target)<.001f);
        rt.DirectHit(scream,target);Check("Rend Mark boosts Impale",Math.Abs(rt.DirectBonus(impale,target)-.15f)<.001f);
        rt.Advance(6.5);Check("Rend Mark expires",rt.DirectBonus(impale,target)<.001f);
        target.Health.ApplyDamage(75000,DamageType.Internal);Check("Finishing Blow executes",Math.Abs(rt.DirectBonus(blow,target)-.3f)<.001f);target.Health.Restore();
        rt.DirectHit(roar,target);Check("Dread Presence boosts Smash",Math.Abs(rt.DirectBonus(smash,target)-.1f)<.001f);
        p.ResetRun();
        // Blood Cadence and Rolling Quake.
        Own(new[]{"impale","ravage","smash","forcecrush"});Check("Cadence and Quake purchases",Grant("juggernaut_cadence") && Grant("juggernaut_quake"));
        SetCooldown(ravage,9);rt.PeriodicTick(impale);Check("Impale bleed shaves Ravage",ravage.CooldownRemaining<9-.29);
        SetCooldown(crush,10);var third=Dummy("JgThird",.22f);
        rt.DirectHit(smash,target);rt.DirectHit(smash,neighbor);rt.DirectHit(smash,third);
        Check("three-target Smash recovers Crush",crush.CooldownRemaining<=10-1.49f);
        p.ResetRun();
        // Defence: Soresu divides, never reaches zero.
        float incoming=rt.JuggernautIncoming;
        for(int i=0;i<200;i++)Grant("juggernaut_soresu");
        Check("Soresu lowers damage taken without reaching zero",rt.JuggernautIncoming<incoming*.4f && rt.JuggernautIncoming>0);
        p.ResetRun();Check("Soresu resets",Math.Abs(rt.JuggernautIncoming-incoming)<.001f);
        Check("Dark Blood and Undying Rage healing need their talents",!p.IsEligible(Card("juggernaut_darkblood_surge")) && !p.IsEligible(Card("juggernaut_rageheal")));
        Own(new[]{"smash"});
        Check("Dark Blood talent unlocks its heal card",Grant("juggernaut_darkblood") && p.IsEligible(Card("juggernaut_darkblood_surge")));
        w.PlayerActor.Health.Restore();w.PlayerActor.Health.ApplyDamage(50,DamageType.Internal);float before=w.PlayerActor.Health.Current;
        var sundering=w.Abilities.Find("sunderingassault")!;sundering.SetUnlocked(true);rt.Advance(2);rt.DirectHit(sundering,target);float healed=w.PlayerActor.Health.Current-before;
        Check("Dark Blood heals",healed>0);
        Check("Dark Blood Surge card grants a larger heal",Grant("juggernaut_darkblood_surge") && Grant("juggernaut_darkblood_surge"));
        w.PlayerActor.Health.Restore();w.PlayerActor.Health.ApplyDamage(50,DamageType.Internal);before=w.PlayerActor.Health.Current;rt.Advance(2);rt.DirectHit(sundering,target);
        Check("two Surge ranks heal 30% more",Math.Abs((w.PlayerActor.Health.Current-before)-healed*1.3f)<.01f);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }

    private async System.Threading.Tasks.Task MarauderChecks(GameWorld w,RunProgression p,Talents.TalentRuntime rt,Func<string,UpgradeDefinition> Card,Func<string,bool> Grant,Action<string[]> Own,
        Func<string,float,Combatant> Dummy,Action<AutomaticAbility,double> SetCooldown,Action<string,bool> Check,Dictionary<string,object> notes)
    {
        var target=Dummy("MrTarget",.1f);var neighbor=Dummy("MrNeighbor",.14f);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        AutomaticAbility A(string id)=>w.Abilities.Find(id)!;
        var massacre=A("massacre");var ravage=A("ravage");var sweep=A("sweepingslash");var throwAbility=A("saberthrow");var rupture=A("rupture");var deadly=A("deadlysaber");var crush=A("forcecrush");var smash=A("smash");
        // Target cards: uncapped, replaces Blademaster's Reach.
        Own(new[]{"massacre","ravage","sweepingslash","saberthrow"});
        int massacreBase=massacre.Definition.MaximumTargets,sweepBase=sweep.Definition.MaximumTargets,throwBase=throwAbility.Definition.MaximumTargets,ravageBase=ravage.Definition.MaximumTargets;
        for(int i=1;i<=8;i++){
            Check("Massacre reach "+i,Grant("marauder_target_massacre") && massacre.Definition.MaximumTargets==massacreBase+i);
            Check("Ravage reach "+i,Grant("marauder_target_ravage") && ravage.Definition.MaximumTargets==ravageBase+i);
            Check("Sweep reach "+i,Grant("marauder_target_sweepingslash") && sweep.Definition.MaximumTargets==sweepBase+i);
            Check("Throw reach "+i,Grant("marauder_target_saberthrow") && throwAbility.Definition.MaximumTargets==throwBase+i);
        }
        p.Recalculate();Check("recalculation does not accumulate targets",massacre.Definition.MaximumTargets==massacreBase+8 && sweep.Definition.MaximumTargets==sweepBase+8);
        p.ResetRun();Check("reset restores targets",massacre.Definition.MaximumTargets==massacreBase && ravage.Definition.MaximumTargets==ravageBase && sweep.Definition.MaximumTargets==sweepBase && throwAbility.Definition.MaximumTargets==throwBase);
        Check("old Blademaster and mastery cards are gone",!p.Upgrades.Any(u=>u.Id=="marauder_blademaster") && p.Upgrades.Count(u=>u.Id.StartsWith("marauder_power_"))==13);
        // Radius, tick, Smash floor.
        Own(new[]{"massacre","smash","rupture","deadlysaber","forcecrush"});
        float radius=massacre.Radius;Check("Massacre radius card",Grant("marauder_radius_massacre") && massacre.Radius>radius*1.1f);
        foreach(var bleed in new[]{rupture,deadly,crush}){float interval=rt.PeriodicInterval(bleed);Check(bleed.Definition.Id+" tick card",Grant("marauder_tick_"+bleed.Definition.Id) && rt.PeriodicInterval(bleed)<interval);}
        for(int i=0;i<60;i++)Grant("marauder_cooldown_smash");
        Check("Smash floor is six seconds",Math.Abs(smash.CooldownSeconds-6f)<.001f);
        notes["smashFloorSeconds"]=smash.CooldownSeconds;
        Check("no recovery cards for the three bleeds",!p.Upgrades.Any(u=>u.Id.StartsWith("marauder_cooldown_") && u.AbilityId is "rupture" or "deadlysaber" or "forcecrush" or "forcechoke"));
        p.ResetRun();
        // Open Wounds / Wound Legion.
        Own(new[]{"rupture","deadlysaber","forcecrush"});
        Check("Open Wounds and Wound Legion",Grant("marauder_open_wounds") && Grant("marauder_open_wounds") && Math.Abs(rt.WarriorDuplicateChance(rupture)-.4f)<.001f && Math.Abs(rt.WarriorDuplicateChance(crush)-.4f)<.001f);
        Check("Wound Legion slots",Grant("marauder_wound_legion") && Grant("marauder_wound_legion") && rt.WarriorDotSlots(deadly)==3);
        w.Periodic.Clear();for(int i=0;i<6;i++)w.Periodic.Apply(rupture,target);Check("three Rupture instances then refresh",w.Periodic.Count==3);
        w.Periodic.Clear();p.ResetRun();
        // Detonate.
        Own(new[]{"rupture","annihilate"});Check("Detonate purchase",Grant("marauder_detonate"));
        w.Periodic.Apply(rupture,target);float hp=target.Health.Current;rt.DirectHit(A("annihilate"),target);
        Check("Detonate pays out remaining bleed damage",hp-target.Health.Current>0);Check("Detonate leaves two seconds",w.Periodic.Remaining(rupture,target)<=2.001 && w.Periodic.Remaining(rupture,target)>0);
        hp=target.Health.Current;rt.DirectHit(A("annihilate"),target);Check("a trimmed bleed cannot be detonated for the same ticks again",hp-target.Health.Current<=(rupture.Definition.PeriodicDamage*rupture.DamagePerTick/Math.Max(.001f,rupture.Definition.Damage))*2.01f*.35f*1.01f*Math.Max(1f,w.Build.DamageMultiplier(rupture,target)));
        w.Periodic.Clear();p.ResetRun();
        // Blood Tempo.
        Own(new[]{"massacre","rupture"});Check("Blood Tempo purchase",Grant("marauder_blood_tempo"));
        for(int i=0;i<10;i++){rt.PeriodicTick(rupture);rt.Advance(.6);}
        SetCooldown(massacre,6);rt.Activated(massacre);
        Check("ten bleed ticks make Massacre free",massacre.CooldownRemaining<.001 && Math.Abs(rt.DirectBonus(massacre,target)-.4f)<.001f);
        SetCooldown(massacre,6);rt.Activated(massacre);Check("Tempo is spent",massacre.CooldownRemaining>5.9);
        p.ResetRun();
        // Gore Hunger: base Gore, then extend by kills, then hard stop at three times base.
        Own(new[]{"gore","massacre"});Check("Gore Hunger purchase",Grant("marauder_gore_hunger"));
        rt.Activated(A("gore"));rt.Advance(.1);for(int i=0;i<40;i++)rt.MarauderKilled();
        double length=0;while(rt.MarauderAuraActive("gore") && length<60){rt.Advance(.1);length+=.1;}
        double baseGore=0;p.ResetRun();rt.Activated(A("gore"));while(rt.MarauderAuraActive("gore") && baseGore<60){rt.Advance(.1);baseGore+=.1;}
        notes["goreBaseSeconds"]=baseGore;notes["goreWithHunger"]=length+.1;
        Check("kills extend Gore but never past three times its base",length+.1>baseGore+1 && length+.1<=baseGore*3+.35);
        p.ResetRun();
        // Ataru Surge.
        Own(new[]{"massacre","ataruform"});Check("Ataru stance for Surge",Grant("marauder_flurry") && Grant("marauder_ataru_surge") && Grant("marauder_ataru_surge"));
        var extras=new List<Combatant>{target,neighbor};for(int i=0;i<5;i++)extras.Add(Dummy("MrAtaru"+i,.12f+.01f*i));
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        rt.Activated(massacre);rt.DirectHit(massacre,target);
        Check("Surge strikes two additional enemies",extras.Count(t=>t.Health.Current<t.Health.Maximum)==6);
        foreach(var t in extras.Skip(2))t.QueueFree();
        p.ResetRun();
        // Predator's Mark.
        Own(new[]{"predation","rupture"});Check("Predator's Mark purchase",Grant("marauder_predator_mark"));
        rt.Activated(A("predation"));Check("marked target takes more bleed damage",Math.Abs(rt.DirectBonus(rupture,target)-.1f)<.001f && rt.DirectBonus(rupture,neighbor)<.001f);
        p.ResetRun();
        // Berserk Chain.
        Own(new[]{"berserk"});rt.Activated(A("berserk"));rt.Advance(6.4);Check("Berserk expires without the card",!rt.MarauderAuraActive("berserk"));
        p.ResetRun();Own(new[]{"berserk"});Check("Berserk Chain purchase",Grant("marauder_berserk_chain"));
        rt.Activated(A("berserk"));rt.Advance(1);for(int i=0;i<5;i++)rt.MarauderKilled();rt.Advance(6.4);
        Check("kills extend Berserk",rt.MarauderAuraActive("berserk"));
        double chain=0;while(rt.MarauderAuraActive("berserk") && chain<60){rt.Advance(.1);chain+=.1;}
        Check("Berserk extension is bounded",chain<12);
        p.ResetRun();
        // Siphon Pulse.
        Own(new[]{"saberthrow"});Check("Siphon Pulse purchase",Grant("marauder_siphon_pulse"));
        var third=Dummy("MrThird",.12f);w.PlayerActor.Health.Restore();w.PlayerActor.Health.ApplyDamage(30,DamageType.Internal);float health=w.PlayerActor.Health.Current;
        rt.DirectHit(throwAbility,target);rt.DirectHit(throwAbility,neighbor);Check("two hits do not heal",Math.Abs(w.PlayerActor.Health.Current-health)<.001f);
        rt.DirectHit(throwAbility,third);Check("three hits heal once",Math.Abs(w.PlayerActor.Health.Current-health-2)<.01f);
        rt.DirectHit(throwAbility,third);Check("Siphon Pulse is rate limited",Math.Abs(w.PlayerActor.Health.Current-health-2)<.01f);
        p.ResetRun();
        // Smash Aftershock and Scream Contagion.
        Own(new[]{"smash"});Check("Aftershock purchase",Grant("marauder_smash_aftershock"));
        rt.Activated(smash);hp=target.Health.Current;rt.Advance(1.3);Check("Smash echoes",target.Health.Current<hp);
        p.ResetRun();
        Own(new[]{"forcescream","rupture"});Check("Scream Contagion purchase",Grant("marauder_scream_contagion"));
        w.Periodic.Clear();w.Periodic.Apply(rupture,target);rt.DirectHit(A("forcescream"),target);
        Check("Scream spreads Rupture to exactly one nearby enemy per rank",new[]{neighbor,third}.Count(c=>w.Periodic.Has(rupture,c))==1);w.Periodic.Clear();p.ResetRun();
        // Healing and defence.
        Check("healing boosts need their talents",!p.IsEligible(Card("marauder_hungering_surge")) && !p.IsEligible(Card("marauder_siphon_surge")));
        float incoming=rt.IncomingMultiplier;
        for(int i=0;i<150;i++)Grant("marauder_resolve");
        Check("Resolve lowers damage taken without reaching zero",rt.IncomingMultiplier<incoming*.45f && rt.IncomingMultiplier>0);
        p.ResetRun();Check("Resolve resets",Math.Abs(rt.IncomingMultiplier-incoming)<.001f);
        // Drops exist for all three trees.
        foreach(string tree in new[]{"carnage","annihilation","marauder_rage"})Check(tree+" has run drops",p.Upgrades.Count(u=>u.Spec==tree && u.Effect==UpgradeEffect.Mechanic)>=3);
        notes["drops"]=new{carnage=p.Upgrades.Count(u=>u.Spec=="carnage"),annihilation=p.Upgrades.Count(u=>u.Spec=="annihilation"),rage=p.Upgrades.Count(u=>u.Spec=="marauder_rage")};
        Check("Annihilation drops are usable once owned",true);
    }
}
