using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;

public partial class AssassinRunPowersSmoke : Node
{
    public async void Run(GameWorld w)
    {
        var checks=new List<string>();var budgets=new List<object>();string? failure=null;
        void Check(string name,bool ok){if(!ok)throw new Exception(name);checks.Add(name);}
        try {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);
            w.Periodic.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.SetPhysicsProcess(false);e.CombatEnabled=false;}
            var p=w.Progression!;
            UpgradeDefinition Power(string id)=>p.Upgrades.Single(u=>u.Id==id);
            bool Grant(string id)=>p.Grant("assassin_"+id);
            void Own(params string[] ids){foreach(string id in ids)w.Abilities.Find(id)!.SetUnlocked(true);p.Recalculate();}
            Check("11 damage, 5 radius, 4 cooldown, 3 tick, 3 duplicate, 3 multi and nine interactions",p.Upgrades.Count(u=>u.Id.StartsWith("assassin_power_"))==11 && p.Upgrades.Count(u=>u.Id.StartsWith("assassin_radius_"))==5 && p.Upgrades.Count(u=>u.Id.StartsWith("assassin_cooldown_"))==4 && p.Upgrades.Count(u=>u.Id.StartsWith("assassin_tick_"))==3 && p.Upgrades.Count(u=>u.Mechanic.StartsWith("as_dup_"))==3 && p.Upgrades.Count(u=>u.Mechanic.StartsWith("as_multi_"))==3 && p.Upgrades.Count(u=>u.Mechanic is "as_conduit" or "as_ambush" or "as_chain" or "as_echo" or "as_spread" or "as_harvest" or "as_wither" or "as_guard" or "as_decay")==9);
            Check("no global damage dependency",!p.Upgrades.Any(u=>u.Effect==UpgradeEffect.Damage));
            Check("unowned abilities and incomplete combos excluded",!p.IsEligible(Power("assassin_power_maul")) && !p.IsEligible(Power("assassin_conduit")) && !p.IsEligible(Power("assassin_spread")));
            foreach(string id in new[]{"lacerate","voltaicslash","discharge","maul","spike","shock","creepingterror","affliction","crushingdarkness","deathfield","wither"}) {
                p.ResetRun();Own(id);var a=w.Abilities.Find(id)!;float damage=a.DamagePerTick;
                Check(id+" gains a substantial scoped rank",Grant("power_"+id) && a.DamagePerTick>=damage*1.59f);
                Check(id+" recalculation does not accumulate",Math.Abs(a.DamagePerTick-damage*(1+Power("assassin_power_"+id).DamagePerRank))<.01f);
                p.Recalculate();Check(id+" remains stable",Math.Abs(a.DamagePerTick-damage*(1+Power("assassin_power_"+id).DamagePerRank))<.01f);
            }
            p.ResetRun();Own("affliction");
            Check("uncapped duplicate and slot cards",Power("assassin_duplicate_affliction").MaximumRank==0 && Power("assassin_multi_affliction").MaximumRank==0 && Grant("multi_affliction") && Grant("multi_affliction") && Grant("multi_affliction") && Grant("multi_affliction"));
            var aff=w.Abilities.Find("affliction")!;Check("slots follow ranks",w.Build.Pregame.AssassinDotSlots(aff)==5);
            var tgt=Dummy("SlotTarget",.2f);w.Periodic.Clear();for(int i=0;i<7;i++)w.Periodic.Apply(aff,tgt);
            Check("five distinct instances then refresh",w.Periodic.Count==5);
            p.ResetRun();Own("affliction");Check("reset drops slots",w.Build.Pregame.AssassinDotSlots(aff)==1);
            Grant("duplicate_affliction");Check("duplicate chance scales",w.Build.Pregame.AssassinDuplicateChance(aff)>=.2f-.001f);
            p.ResetRun();float baseline=w.Combat.DamagePerTick;
            for(int i=0;i<20;i++)Check("late run attack growth "+i,Grant("power_lacerate"));
            Check("damage remains additive through twenty ranks",Math.Abs(w.Combat.DamagePerTick-baseline*(1+20*Power("assassin_power_lacerate").DamagePerRank))<.02f);
            // Equal-pick comparison: distribute three ranks across three owned damage sources.
            foreach(var kit in new[]{new[]{"voltaicslash","shock","maul"},new[]{"discharge","creepingterror","deathfield"},new[]{"lacerate","wither","spike"}}) {
                p.ResetRun();Own(kit);var original=kit.Select(id=>w.Abilities.Find(id)!.DamagePerTick).ToArray();
                foreach(string id in kit)Grant("power_"+id);
                var ratios=kit.Select((id,i)=>w.Abilities.Find(id)!.DamagePerTick/original[i]).ToArray();
                Check(string.Join("/",kit)+" three picks meet old 3x20% damage budget",ratios.Min()>=1.6f);
                budgets.Add(new{kit,damageMultipliers=ratios,oldThreeGlobalRanks=1.6f});
            }
            var generate=typeof(RunProgression).GetMethod("GenerateOffers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
            foreach(string[] kit in new[]{new[]{"lacerate"},new[]{"voltaicslash","shock","maul","spike"},new[]{"discharge","creepingterror","deathfield"},new[]{"wither","deflection"}}) {
                p.ResetRun();Own(kit);
                for(ulong seed=1;seed<=80;seed++) {
                    p.SeedOffers(seed);int level=8+(int)(seed%8);
                    var offers=(List<UpgradeDefinition>)generate.Invoke(p,new object[]{level,Array.Empty<UpgradeDefinition>(),3})!;
                    Check("valid varied offers "+string.Join("/",kit)+seed,offers.Count==3 && offers.Select(u=>u.Id).Distinct().Count()==3 && offers.All(p.IsEligible) && offers.Any(u=>u.Effect==UpgradeEffect.AbilityPower));
                    Check("survival is occasional "+string.Join("/",kit)+seed,offers.Count(u=>u.Effect is UpgradeEffect.Health or UpgradeEffect.Speed)<=1 && (level%4==0 || offers.All(u=>u.Effect is not (UpgradeEffect.Health or UpgradeEffect.Speed))));
                    if(kit.Length>=3)Check("two class choices "+seed,offers.Count(u=>u.Id.StartsWith("assassin_"))>=2);
                }
            }
            p.ResetRun();p.SeedOffers(14);p.AddExperience(p.RequiredExperience);p._Process(3);
            Check("early core offers preserved",p.Offers.Count(u=>u.Effect==UpgradeEffect.UnlockAbility)==2);
            Check("banish, reroll and skip work",p.Banish(0) && p.Reroll() && p.Skip());p.ResetRun();
            Combatant Dummy(string name,float x){var t=new Combatant{Name=name,Team=CombatTeam.Hostile};t.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*x;return t;}
            var target=Dummy("PowerTarget",.1f);var neighbor=Dummy("PowerNeighbor",.14f);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            void Cast(string id){w.Player.Presentation.ResetPresentation();var a=w.Abilities.Find(id)!;a.SetUnlocked(true);a.ResetCombat();Check(id+" casts",a.TryBegin());for(int i=0;i<160 && a.Casting;i++)a.Advance(.025);}
            Own("voltaicslash","shock","discharge");Check("conduit requires both ends",Grant("conduit"));
            Cast("shock");var shock=w.Abilities.Find("shock")!;float cd=(float)shock.CooldownRemaining;
            w.Build.Pregame.DirectHit(w.Abilities.Find("voltaicslash")!,target);float reduced=(float)shock.CooldownRemaining;
            w.Build.Pregame.DirectHit(w.Abilities.Find("voltaicslash")!,neighbor);
            Check($"conduit cooldown refund is bounded ({cd}, {reduced}, {shock.CooldownRemaining})",Math.Abs(cd-reduced-1)<.01 && Math.Abs(shock.CooldownRemaining-reduced)<.001);
            Check("fork purchase works",Grant("chain"));Cast("shock");
            Check("fork actually hits unique neighboring enemies",shock.Definition.Area==AbilityArea.Chain && shock.LastDistinctHits>=2 && shock.Definition.BounceCount==2);
            p.ResetRun();Own("maul","forcecloak");Check("ambush purchase works",Grant("ambush"));
            var maul=w.Abilities.Find("maul")!;float normal=w.Build.DamageMultiplier(maul,target);Cast("maul");float before=(float)maul.CooldownRemaining;Cast("forcecloak");
            Check("cloak recovers and empowers Maul",Math.Abs(before-(float)maul.CooldownRemaining-4)<.01 && w.Build.DamageMultiplier(maul,target)>=normal+1);
            p.ResetRun();Own("shock","creepingterror");var terror=w.Abilities.Find("creepingterror")!;
            w.Periodic.Apply(terror,target);float hp=target.Health.Current;w.Build.Pregame.DirectHit(shock,target);float echo=hp-target.Health.Current;
            Check("echo purchase works",Grant("echo"));hp=target.Health.Current;w.Build.Pregame.DirectHit(shock,target);
            Check("resonance quadruples baseline echo without removing DoT",Math.Abs(hp-target.Health.Current-echo*4)<.1 && w.Periodic.Has(terror,target));
            Check("decay purchase works",Grant("decay"));float interval=w.Build.Pregame.PeriodicInterval(terror);Check("decay accelerates existing ticks",interval<.75f);
            int ticks=w.Periodic.Ticks;w.Periodic.Advance(3);Check("accelerated existing effect ticks four times",w.Periodic.Ticks-ticks==4);
            p.ResetRun();Own("creepingterror");Check("spread purchase works",Grant("spread"));w.Periodic.Apply(terror,target);
            hp=neighbor.Health.Current;Cast("lacerate");Check("Razor Wake spreads and bursts",w.Periodic.Has(terror,neighbor) && hp-neighbor.Health.Current>w.Combat.DamagePerTick*1.7f);
            hp=neighbor.Health.Current;Cast("lacerate");Check("existing DoTs cannot repeatedly trigger fresh spread burst",Math.Abs(hp-neighbor.Health.Current-w.Combat.DamagePerTick)<.1);
            p.ResetRun();Own("deathfield","creepingterror");Check("harvest purchase works",Grant("harvest"));w.Periodic.Apply(terror,target);
            var field=w.Abilities.Find("deathfield")!;Check("harvest counts owned effects",Math.Abs(w.Build.Pregame.DirectBonus(field,target)-.3f)<.01);
            p.ResetRun();Own("wither","maul","voltaicslash","deflection");Check("Wither and guard purchases work",Grant("wither") && Grant("guard"));Cast("maul");before=(float)maul.CooldownRemaining;
            w.Build.Pregame.DirectHit(w.Abilities.Find("wither")!,target);w.Build.Pregame.DirectHit(w.Abilities.Find("wither")!,neighbor);
            Check("Wither refund is once for a group",Math.Abs(before-(float)maul.CooldownRemaining-2)<.01);Cast("deflection");
            Check("guard creates attack window",w.Build.Pregame.DirectBonus(w.Combat,target)>=.6f);w.Build.Pregame.Advance(6);Check("guard window expires",w.Build.Pregame.DirectBonus(w.Combat,target)==0);
            p.ResetRun();Check("restart restores stats and removes learned interactions",p.Ranks.Count==0 && Math.Abs(w.Combat.DamagePerTick-baseline)<.01 && !w.Build.Pregame.AssassinSpread && shock.Definition.Area==AbilityArea.TargetSplash);
            Own("voltaicslash","shock");Grant("conduit");w.Abilities.Find("voltaicslash")!.SetUnlocked(false);
            Check("replaced combo ability makes power ineligible",!p.IsEligible(Power("assassin_conduit")));
            // Stationary two-target, 60-second automatic rotations: equal three-pick budgets,
            // no relics or interaction powers. These measure combat output, not just stat ratios.
            foreach(string slot in new[]{"relic1","relic2"})UI.EquipmentWindows.Instance.Inventory.Unequip("assassin",slot);
            foreach(var kit in new[]{new[]{"voltaicslash","shock","maul"},new[]{"discharge","creepingterror","deathfield"},new[]{"lacerate","wither","spike"}}) {
                var results=new List<float>();
                foreach(bool scoped in new[]{false,true}) {
                    p.ResetRun();Own(kit);target.Health.Restore();neighbor.Health.Restore();w.PlayerActor.Health.Restore();
                    var old=new UpgradeDefinition {Id="benchmark_global",Effect=UpgradeEffect.Damage,AmountPerRank=.2f};
                    if(scoped)foreach(string id in kit)Grant("power_"+id);
                    else {p.Upgrades.Add(old);for(int i=0;i<3;i++)p.Grant(old.Id);}
                    w.Abilities.ResetCombat();w.Player.Presentation.ResetPresentation();
                    for(int step=0;step<1200;step++) {
                        w.Abilities._PhysicsProcess(.05);w.Periodic.Advance(.05);w.Build._PhysicsProcess(.05);w.Player.Presentation._Process(.05);
                    }
                    float damage=200000-target.Health.Current-neighbor.Health.Current;results.Add(damage/60);
                    p.Upgrades.Remove(old);p.ResetRun();
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                }
                budgets.Add(new{kit,oldGlobalDps=results[0],newScopedDps=results[1],ratio=results[1]/results[0]});
                Check(string.Join("/",kit)+" automatic damage meets difficulty budget",results[0]>1 && results[1]>=results[0]*.85f);
            }
            p.ResetRun();
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/assassin-run-powers-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,checks,budgets,failure},new JsonSerializerOptions{WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
