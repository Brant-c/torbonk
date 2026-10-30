using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;

public partial class SniperRunPowersSmoke : Node
{
    public async void Run(GameWorld w)
    {
        var checks=new List<string>();var budgets=new List<object>();string? failure=null;
        void Check(string name,bool ok){if(!ok)throw new Exception(name);checks.Add(name);}
        try {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            w.Player.TestInput=Vector2.Zero;for(int i=0;i<30;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            w.Player.SetPhysicsProcess(false);Check("fixture grounded",w.Player.IsOnFloor());
            foreach(var e in w.Spawner.Enemies){e.SetPhysicsProcess(false);e.CombatEnabled=false;}
            var p=w.Progression!;var rt=w.Build.Pregame;
            AutomaticAbility A(string id)=>w.Abilities.Find(id)!;
            UpgradeDefinition U(string id)=>p.Upgrades.Single(u=>u.Id=="sniper_"+id);
            void Own(params string[] ids){foreach(string id in ids)A(id).SetUnlocked(true);p.Recalculate();rt.Advance(.61);}
            bool Grant(string id)=>p.Grant("sniper_"+id);
            void Reset(){p.ResetRun();w.Player.Presentation.ResetPresentation();rt.Advance(.61);}
            Check("14 scoped packages and twelve build interactions",p.Upgrades.Count(u=>u.Id.StartsWith("sniper_power_"))==14 && p.Upgrades.Count(u=>u.Mechanic.StartsWith("sn_"))==12);
            Check("no Snipe training or global damage dependency",!p.Upgrades.Any(u=>u.Effect is UpgradeEffect.Damage or UpgradeEffect.Range or UpgradeEffect.Cooldown) && !p.Upgrades.Any(u=>u.Effect!=UpgradeEffect.UnlockAbility && u.AbilityId=="snipershot"));
            Check("incomplete combinations excluded",!p.IsEligible(U("crossfire")) && !p.IsEligible(U("overload")) && !p.IsEligible(U("decay")));
            foreach(var power in p.Upgrades.Where(u=>u.Id.StartsWith("sniper_power_")).ToArray()) {
                Reset();Own(power.AbilityId);var a=A(power.AbilityId);float damage=a.DamagePerTick,snipe=w.Combat.DamagePerTick;
                Check(power.Id+" meaningful damage increase",p.Grant(power.Id) && a.DamagePerTick>=damage*1.59f);
                p.Recalculate();Check(power.Id+" stable and starter independent",Math.Abs(a.DamagePerTick-damage*(1+power.DamagePerRank))<.01f && w.Combat.DamagePerTick==snipe);
            }
            Reset();Own("ambush");float ambushBase=A("ambush").DamagePerTick;
            for(int i=0;i<20;i++)Check("uncapped late power "+i,Grant("power_ambush"));
            Check("late damage adds from base",Math.Abs(A("ambush").DamagePerTick-ambushBase*17)<.05f);
            var generate=typeof(RunProgression).GetMethod("GenerateOffers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
            foreach(var kit in new[]{Array.Empty<string>(),new[]{"ambush","seriesofshots","followthrough"},new[]{"interrogationprobe","explosiveprobe","plasmaprobe","empdischarge"},new[]{"dart","corrosivegrenade","agentcull","weakeningblast"}}) {
                Reset();Own(kit);
                for(ulong seed=1;seed<=60;seed++) {
                    p.SeedOffers(seed);int level=8+(int)(seed%8);
                    var offers=(List<UpgradeDefinition>)generate.Invoke(p,new object[]{level,Array.Empty<UpgradeDefinition>(),3})!;
                    Check("eligible three-card offer "+kit.Length+"/"+seed,offers.Count==3 && offers.Select(u=>u.Id).Distinct().Count()==3 && offers.All(p.IsEligible));
                    Check("survival remains occasional "+kit.Length+"/"+seed,offers.Count(u=>u.Effect is UpgradeEffect.Health or UpgradeEffect.Speed)<=1 && (level%4==0 || offers.All(u=>u.Effect is not (UpgradeEffect.Health or UpgradeEffect.Speed))));
                    if(kit.Length>0)Check("owned abilities get damage and interactions "+kit[0]+seed,offers.Any(u=>u.Id.StartsWith("sniper_power_")) && offers.Any(u=>u.Mechanic.StartsWith("sn_")));
                }
            }
            Reset();p.SeedOffers(14);p.AddExperience(p.RequiredExperience);p._Process(3);
            Check("early offense and offer controls preserved",p.Offers.Count(u=>u.Effect==UpgradeEffect.UnlockAbility)>=2 && p.Banish(0) && p.Reroll() && p.Skip());Reset();
            var targets=new List<Combatant>();
            for(int i=0;i<6;i++){var t=new Combatant{Name="SniperPowerTarget"+i,Team=CombatTeam.Hostile};t.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*(.16f+.05f*i);targets.Add(t);}
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var t0=targets[0];var t1=targets[1];
            void Cast(string id){w.Player.Presentation.ResetPresentation();var a=A(id);a.SetUnlocked(true);a.ResetCombat();rt.Advance(.61);w.Targeting.Update(0,true);Check(id+" actual cast",a.TryBegin());for(int i=0;i<600 && a.Casting;i++){a.Advance(.025);w.Player.Presentation._Process(.025);}}
            Own("ambush","followthrough","seriesofshots");Check("crossfire purchase",Grant("crossfire"));Cast("ambush");double cd=A("ambush").CooldownRemaining;Cast("followthrough");Check("Followthrough refunds Ambush once",Math.Abs(cd-A("ambush").CooldownRemaining-3)<.01);
            Check("ricochet purchase",Grant("ricochet"));Cast("ambush");Check("six distinct ricochet victims",A("ambush").LastDistinctHits==6 && A("ambush").ExtraBounces==2 && A("ambush").Definition.BounceFalloff>=.9f);
            Reset();Own("interrogationprobe","explosiveprobe","seriesofshots","suppressivefire","plasmaprobe");
            Check("probe synergy purchases",Grant("overload") && Grant("targeting") && Grant("suppression"));w.Periodic.Apply(A("interrogationprobe"),t0);w.Periodic.Advance(2);
            Check("probe mark buffs the actual core abilities",rt.DirectBonus(A("seriesofshots"),t0)>=.64f && rt.DirectBonus(A("plasmaprobe"),t0)>=.74f);
            float hp=t0.Health.Current,neighborHp=t1.Health.Current;rt.DirectHit(A("explosiveprobe"),t0);float burst=hp-t0.Health.Current;
            Check("overload detonates remaining damage and reapplies full probe",burst>50 && Math.Abs(w.Periodic.Remaining(A("interrogationprobe"),t0)-8)<.01);
            Check("overload only damages the marked victim",t1.Health.Current==neighborHp);
            var foreign=new Combatant{Name="ForeignProbeCaster",Team=CombatTeam.Player};foreign.AddChild(new Health{Name="Health",Maximum=100});w.AddChild(foreign);
            var foreignProbe=new AutomaticAbility{Caster=foreign};foreignProbe.Configure(A("interrogationprobe").Definition);
            w.Periodic.RemoveSource(A("interrogationprobe"));w.Periodic.Apply(foreignProbe,t0,relicApplied:true);hp=t0.Health.Current;rt.DirectHit(A("explosiveprobe"),t0);
            Check("foreign probe is neither detonated nor consumed",t0.Health.Current==hp && w.Periodic.Has(foreignProbe,t0) && w.Periodic.Has(A("interrogationprobe"),t0));
            w.Periodic.RemoveSource(foreignProbe);foreignProbe.Free();foreign.QueueFree();
            foreach(var t in targets)w.Periodic.Apply(A("interrogationprobe"),t);hp=targets.Sum(t=>t.Health.Current);Cast("explosiveprobe");
            Check("actual group explosion detonates and reapplies each owned probe",hp-targets.Sum(t=>t.Health.Current)>1000 && targets.All(t=>w.Periodic.Has(A("interrogationprobe"),t)));
            Reset();Own("orbitalstrike","suppressivefire","empdischarge","explosiveprobe");Check("artillery synergy purchases",Grant("bombardment") && Grant("emp"));
            Check("artillery gains coverage",A("orbitalstrike").Radius>=A("orbitalstrike").Definition.Radius*1.29f);Cast("orbitalstrike");cd=A("orbitalstrike").CooldownRemaining;rt.Activated(A("suppressivefire"));Check("suppression recovers artillery once per cast",Math.Abs(cd-A("orbitalstrike").CooldownRemaining-4)<.01);
            cd=A("orbitalstrike").CooldownRemaining;rt.Activated(A("empdischarge"));Check("EMP adds artillery recovery",Math.Abs(cd-A("orbitalstrike").CooldownRemaining-6)<.01);
            Reset();Own("dart","corrosivegrenade","agentcull","weakeningblast");Check("poison synergy purchases",Grant("cull") && Grant("decay") && Grant("weakening"));
            w.Periodic.Apply(A("dart"),t0);w.Periodic.Apply(A("corrosivegrenade"),t0);
            int ticks=w.Periodic.Ticks;w.Periodic.Advance(3);Check("existing poisons gain faster ticks",w.Periodic.Ticks-ticks==8);
            hp=t0.Health.Current;double remaining=w.Periodic.Remaining(A("dart"),t0);rt.DirectHit(A("weakeningblast"),t0);Check("weakening echoes both poisons without consumption",t0.Health.Current<hp-14 && w.Periodic.Remaining(A("dart"),t0)==remaining);
            rt.Activated(A("agentcull"));rt.Activated(A("weakeningblast"));Check("Cull improves splash and empowers the next Weakening Blast",rt.DirectBonus(A("weakeningblast"),t0)>=1.14f && A("agentcull").Definition.SplashMultiplier==1 && A("agentcull").Radius>A("agentcull").Definition.Radius*1.49f);
            rt.Activated(A("weakeningblast"));Check("volley empowerment is consumed once",rt.DirectBonus(A("weakeningblast"),t0)<1);
            Reset();Own("ambush","dart");Check("contaminated rounds purchase",Grant("contamination"));Cast("ambush");Check("Ambush ricochets apply owned Dart",targets.Count(t=>w.Periodic.Has(A("dart"),t))>=4);
            hp=t0.Health.Current;remaining=w.Periodic.Remaining(A("dart"),t0);rt.DirectHit(A("ambush"),t0);Check("poisoned Ambush victims echo without refresh",t0.Health.Current<hp-4 && w.Periodic.Remaining(A("dart"),t0)==remaining);
            Reset();Own("entrench","seriesofshots","suppressivefire");Check("Entrench synergy purchase",Grant("entrench"));float normal=rt.DirectBonus(A("seriesofshots"),t0);Cast("seriesofshots");cd=A("seriesofshots").CooldownRemaining;rt.Activated(A("entrench"));Check("Entrench recovers attacks and creates damage window",Math.Abs(Math.Max(0,cd-3)-A("seriesofshots").CooldownRemaining)<.01 && rt.DirectBonus(A("seriesofshots"),t0)>=normal+.39f);rt.Advance(7);Check("Entrench attack window expires",rt.DirectBonus(A("seriesofshots"),t0)<=normal+.01f);
            Reset();Check("restart clears interactions and mutated stats",p.Ranks.Count==0 && A("ambush").ExtraBounces==0 && A("ambush").Definition.BounceFalloff<.9f && A("agentcull").Definition.SplashMultiplier==.8f);
            Own("ambush","followthrough");Grant("crossfire");A("followthrough").SetUnlocked(false);Check("traded combo ability disables eligibility",!p.IsEligible(U("crossfire")));
            // Equal three-pick budget against the global +20% ranks used to tune difficulty.
            foreach(var kit in new[]{new[]{"ambush","seriesofshots","suppressivefire"},new[]{"interrogationprobe","explosiveprobe","plasmaprobe"},new[]{"dart","corrosivegrenade","agentcull"}}) {
                var dps=new List<float>();
                foreach(bool scoped in new[]{false,true}) {
                    Reset();Own(kit);foreach(var t in targets)t.Health.Restore();
                    var old=new UpgradeDefinition{Id="benchmark_global",Effect=UpgradeEffect.Damage,AmountPerRank=.2f};
                    if(scoped)foreach(string id in kit)Grant("power_"+id);else {p.Upgrades.Add(old);for(int i=0;i<3;i++)p.Grant(old.Id);}
                    w.Abilities.ResetCombat();w.Player.Presentation.ResetPresentation();
                    for(int step=0;step<1200;step++){w.Abilities._PhysicsProcess(.05);w.Periodic.Advance(.05);w.Build._PhysicsProcess(.05);w.Player.Presentation._Process(.05);}
                    dps.Add(targets.Sum(t=>t.Health.Maximum-t.Health.Current)/60);p.Upgrades.Remove(old);Reset();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                }
                budgets.Add(new{kit,oldGlobalDps=dps[0],newScopedDps=dps[1],ratio=dps[1]/dps[0]});Check(kit[0]+" meets damage budget",dps[0]>1 && dps[1]>=dps[0]*.99f);
            }
            Reset();
            await SniperLethalitySmoke.Check(w,Check);
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/sniper-run-powers-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,checks,budgets,failure},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
}
