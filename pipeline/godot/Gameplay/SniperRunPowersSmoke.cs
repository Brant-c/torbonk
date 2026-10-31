using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using SwtorSurvivors.Talents;
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
            void Reset(){foreach(var card in p.Upgrades.Where(c=>c.Id.StartsWith("sniper_")))card.Rarity=PowerRarity.Premium;p.ResetRun();w.Player.Presentation.ResetPresentation();rt.Advance(.61);}
            if(w.RunConfiguration.AdvancedClassId=="operative") {OperativeChecks(w,p,rt,Check);await MechanicEffects(w,p,rt,false,Check);WriteReceipt("operative",checks,failure);return;}
            AgentCardChecks(p,"sniper","snm_",Check);
            Check("single-axis Sniper cards replace the bundled packages",p.Upgrades.Count(u=>u.Id.StartsWith("sniper_power_"))>=15 && !p.Upgrades.Any(u=>u.Id.StartsWith("sniper_") && u.Effect==UpgradeEffect.Mechanic && u.MaximumRank==1));
            Check("no Snipe training or global damage dependency",!p.Upgrades.Any(u=>u.Effect is UpgradeEffect.Damage or UpgradeEffect.Range or UpgradeEffect.Cooldown) && !p.Upgrades.Any(u=>u.Effect!=UpgradeEffect.UnlockAbility && u.AbilityId=="snipershot"));
            Check("incomplete combinations excluded",!p.IsEligible(U("crossfire")) && !p.IsEligible(U("overload")) && !p.IsEligible(U("decay")));
            foreach(var power in p.Upgrades.Where(u=>u.Id.StartsWith("sniper_power_")).ToArray()) {
                Reset();Own(power.AbilityId);var a=A(power.AbilityId);float damage=a.DamagePerTick,snipe=w.Combat.DamagePerTick;
                Check(power.Id+" meaningful damage increase",p.Grant(power.Id) && a.DamagePerTick>=damage*(1+power.DamagePerRank)-.01f);
                p.Recalculate();Check(power.Id+" stable and starter independent",Math.Abs(a.DamagePerTick-damage*(1+power.DamagePerRank))<.01f && w.Combat.DamagePerTick==snipe);
            }
            Reset();Own("ambush");float ambushBase=A("ambush").DamagePerTick;
            for(int i=0;i<20;i++)Check("uncapped late power "+i,Grant("power_ambush"));
            Check("late damage adds from base",Math.Abs(A("ambush").DamagePerTick-ambushBase*19)<.05f);
            var generate=typeof(RunProgression).GetMethod("GenerateOffers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
            foreach(var kit in new[]{Array.Empty<string>(),new[]{"ambush","seriesofshots","followthrough"},new[]{"interrogationprobe","explosiveprobe","plasmaprobe","empdischarge"},new[]{"dart","corrosivegrenade","agentcull","weakeningblast"}}) {
                Reset();Own(kit);
                for(ulong seed=1;seed<=60;seed++) {
                    p.SeedOffers(seed);int level=8+(int)(seed%8);
                    var offers=(List<UpgradeDefinition>)generate.Invoke(p,new object[]{level,Array.Empty<UpgradeDefinition>(),3})!;
                    Check("eligible three-card offer "+kit.Length+"/"+seed,offers.Count==3 && offers.Select(u=>u.Id).Distinct().Count()==3 && offers.All(p.IsEligible));
                    Check("survival remains occasional "+kit.Length+"/"+seed,offers.Count(u=>u.Effect is UpgradeEffect.Health or UpgradeEffect.Speed)<=1 && (level%4==0 || offers.All(u=>u.Effect is not (UpgradeEffect.Health or UpgradeEffect.Speed))));
                    if(kit.Length>0)Check("owned abilities get damage and interactions "+kit[0]+seed,offers.Any(u=>u.DamagePerRank>0) && offers.Any(u=>u.Mechanic.StartsWith("snm_")));
                }
            }
            Reset();p.SeedOffers(14);p.AddExperience(p.RequiredExperience);p._Process(3);
            Check("early offense and offer controls preserved",p.Offers.Count(u=>u.Effect==UpgradeEffect.UnlockAbility)>=2 && p.Banish(0) && p.Reroll() && p.Skip());Reset();
            var targets=new List<Combatant>();
            for(int i=0;i<6;i++){var t=new Combatant{Name="SniperPowerTarget"+i,Team=CombatTeam.Hostile};t.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*(.16f+.05f*i);targets.Add(t);}
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var t0=targets[0];var t1=targets[1];
            void Cast(string id){w.Player.Presentation.ResetPresentation();var a=A(id);a.SetUnlocked(true);a.ResetCombat();rt.Advance(.61);w.Targeting.Update(0,true);Check(id+" actual cast",a.TryBegin());for(int i=0;i<600 && a.Casting;i++){a.Advance(.025);w.Player.Presentation._Process(.025);}}
            Own("ambush","followthrough","seriesofshots");Check("crossfire purchase",Grant("crossfire"));Cast("ambush");double cd=A("ambush").CooldownRemaining;Cast("followthrough");Check("Followthrough refunds Ambush once",Math.Abs(cd-A("ambush").CooldownRemaining-2)<.01);
            Check("ricochet purchases",Grant("ricochet") && Grant("targets_ambush") && Grant("targets_ambush"));Cast("ambush");Check("six distinct ricochet victims",A("ambush").LastDistinctHits==6 && A("ambush").ExtraBounces==2 && A("ambush").Definition.BounceFalloff>.85f);
            Reset();Own("interrogationprobe","explosiveprobe","seriesofshots","suppressivefire","plasmaprobe");
            Check("probe synergy purchases",Grant("overload") && Grant("targeting") && Grant("suppression"));w.Periodic.Apply(A("interrogationprobe"),t0);w.Periodic.Advance(2);
            Check("probe mark buffs the actual core abilities",rt.DirectBonus(A("seriesofshots"),t0)>=.39f && rt.DirectBonus(A("plasmaprobe"),t0)>=.44f);
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
            Check("artillery gains coverage",A("orbitalstrike").Radius>=A("orbitalstrike").Definition.Radius*1.14f);Cast("orbitalstrike");cd=A("orbitalstrike").CooldownRemaining;rt.Activated(A("suppressivefire"));Check("suppression recovers artillery once per cast",Math.Abs(cd-A("orbitalstrike").CooldownRemaining-2)<.01);
            cd=A("orbitalstrike").CooldownRemaining;rt.Activated(A("empdischarge"));Check("EMP adds artillery recovery",Math.Abs(cd-A("orbitalstrike").CooldownRemaining-3)<.01);
            Reset();Own("dart","corrosivegrenade","agentcull","weakeningblast");Check("poison synergy purchases",Grant("cull") && Grant("decay") && Grant("weakening"));
            w.Periodic.Apply(A("dart"),t0);w.Periodic.Apply(A("corrosivegrenade"),t0);
            int ticks=w.Periodic.Ticks;w.Periodic.Advance(3);Check("existing poisons gain faster ticks",w.Periodic.Ticks-ticks==6);
            hp=t0.Health.Current;double remaining=w.Periodic.Remaining(A("dart"),t0);rt.DirectHit(A("weakeningblast"),t0);Check("weakening echoes both poisons without consumption",t0.Health.Current<hp-14 && w.Periodic.Remaining(A("dart"),t0)==remaining);
            rt.Activated(A("agentcull"));rt.Activated(A("weakeningblast"));Check("Cull improves splash and empowers the next Weakening Blast",rt.DirectBonus(A("weakeningblast"),t0)>=.64f && A("agentcull").Definition.SplashMultiplier==1 && A("agentcull").Radius>A("agentcull").Definition.Radius*1.49f);
            rt.Activated(A("weakeningblast"));Check("volley empowerment is consumed once",rt.DirectBonus(A("weakeningblast"),t0)<.4f);
            Reset();Own("ambush","dart");Check("contaminated rounds purchase",Grant("contamination"));Cast("ambush");Check("Ambush ricochets apply owned Dart",targets.Count(t=>w.Periodic.Has(A("dart"),t))>=4);
            hp=t0.Health.Current;remaining=w.Periodic.Remaining(A("dart"),t0);rt.DirectHit(A("ambush"),t0);Check("poisoned Ambush victims echo without refresh",t0.Health.Current<hp-2 && w.Periodic.Remaining(A("dart"),t0)==remaining);
            Reset();Own("entrench","seriesofshots","suppressivefire");Check("Entrench synergy purchase",Grant("entrench"));float normal=rt.DirectBonus(A("seriesofshots"),t0);Cast("seriesofshots");cd=A("seriesofshots").CooldownRemaining;rt.Activated(A("entrench"));Check("Entrench recovers attacks and creates damage window",Math.Abs(Math.Max(0,cd-2)-A("seriesofshots").CooldownRemaining)<.01 && rt.DirectBonus(A("seriesofshots"),t0)>=normal+.24f);rt.Advance(7);Check("Entrench attack window expires",rt.DirectBonus(A("seriesofshots"),t0)<=normal+.01f);
            Reset();Check("restart clears interactions and mutated stats",p.Ranks.Count==0 && A("ambush").ExtraBounces==0 && A("ambush").Definition.BounceFalloff<.9f && A("agentcull").Definition.SplashMultiplier==.8f);
            Own("ambush","followthrough");Grant("crossfire");A("followthrough").SetUnlocked(false);Check("traded combo ability disables eligibility",!p.IsEligible(U("crossfire")));
            // Equal three-pick budget against the global +20% ranks used to tune difficulty.
            foreach(var kit in new[]{new[]{"ambush","seriesofshots","suppressivefire"},new[]{"interrogationprobe","explosiveprobe","plasmaprobe"},new[]{"dart","corrosivegrenade","agentcull"}}) {
                var dps=new List<float>();
                foreach(bool scoped in new[]{false,true}) {
                    Reset();Own(kit);A("snipershot").SetUnlocked(false);foreach(var t in targets)t.Health.Restore();
                    var old=new UpgradeDefinition{Id="benchmark_global",Effect=UpgradeEffect.Damage,AmountPerRank=.2f};
                    if(scoped)foreach(string id in kit)Grant("power_"+id);else {p.Upgrades.Add(old);for(int i=0;i<3;i++)p.Grant(old.Id);}
                    w.Abilities.ResetCombat();w.Player.Presentation.ResetPresentation();
                    for(int step=0;step<1200;step++){w.Abilities._PhysicsProcess(.05);w.Periodic.Advance(.05);w.Build._PhysicsProcess(.05);w.Player.Presentation._Process(.05);}
                    dps.Add(targets.Sum(t=>t.Health.Maximum-t.Health.Current)/60);p.Upgrades.Remove(old);Reset();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                }
                budgets.Add(new{kit,oldGlobalDps=dps[0],newScopedDps=dps[1],ratio=dps[1]/dps[0]});Check(kit[0]+" meets damage budget "+dps[0]+" "+dps[1],dps[0]>1 && dps[1]>=dps[0]*.99f);
            }
            Reset();
            SniperMechanicChecks(w,p,rt,Check);
            await MechanicEffects(w,p,rt,true,Check);
            Reset();
            await SniperLethalitySmoke.Check(w,Check);
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/sniper-run-powers-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,checks,budgets,failure},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }

    // Behavioural checks for the new scaled mechanics on live abilities and stand-in enemies.
    internal static async Task MechanicEffects(GameWorld w,RunProgression p,TalentRuntime rt,bool sniper,Action<string,bool> Check)
    {
        string cls=sniper?"sniper":"operative";
        AutomaticAbility A(string id)=>w.Abilities.Find(id)!;
        foreach(var card in p.Upgrades.Where(c=>c.Id.StartsWith(cls+"_")))card.Rarity=PowerRarity.Premium;
        void Reset(params string[] own){p.ResetRun();w.Player.Presentation.ResetPresentation();foreach(string id in own)A(id).SetUnlocked(true);p.Recalculate();rt.Advance(.61);}
        bool Grant(string id)=>p.Grant(cls+"_"+id);
        var enemies=new List<Combatant>();
        for(int i=0;i<3;i++){var t=new Combatant{Name="EffectTarget"+i,Team=CombatTeam.Hostile};t.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*(.14f+.04f*i);enemies.Add(t);}
        await w.ToSignal(w.GetTree(),SceneTree.SignalName.ProcessFrame);
        var t0=enemies[0];var t1=enemies[1];
        void Heal(){foreach(var e in enemies)e.Health.Restore();w.PlayerActor.Health.Restore();}
        // Shared Lethality
        Reset("dart","corrosivegrenade","agentcull","weakeningblast");Heal();
        w.Periodic.Apply(A("dart"),t0);w.Periodic.Apply(A("corrosivegrenade"),t0);float hp=t0.Health.Current;
        Check(cls+" Septic Detonation grant",Grant("septic"));rt.DirectHit(A("agentcull"),t0);
        Check(cls+" Septic Detonation pays remaining poison and leaves 2s",hp-t0.Health.Current>5 && w.Periodic.Remaining(A("dart"),t0)<=2.01);
        Reset("dart","corrosivegrenade");Heal();w.Periodic.Apply(A("dart"),t0);
        Check(cls+" Pandemic grant",Grant("pandemic"));rt.Defeated(t0);
        Check(cls+" Pandemic jumps a poison to a nearby unpoisoned enemy",w.Periodic.Has(A("dart"),t1));
        w.Periodic.RemoveSource(A("dart"));
        Reset("dart","corrosivegrenade");Heal();
        Check(cls+" Dart Echo and Legion grants",Grant("echo_dart") && Grant("legion_dart") && rt.AgentDuplicateChance(A("dart"))>.19f && rt.AgentDotSlots(A("dart"))==2);
        w.Periodic.Apply(A("dart"),t0);int before=w.Periodic.Count;w.Periodic.Apply(A("dart"),t0);
        Check(cls+" Legion adds a simultaneous Dart instance",w.Periodic.Count==before+1);
        w.Periodic.RemoveSource(A("dart"));
        Reset("dart","corrosivegrenade","agentcull","weakeningblast");Heal();
        Check(cls+" Toxic Tempo and Mending grants",Grant("tempo") && Grant("mending"));
        w.Periodic.Apply(A("dart"),t0);w.PlayerActor.Health.ApplyDamage(10);A("agentcull").ResetCombat();w.Targeting.Update(0,true);Check(cls+" Cull begins",A("agentcull").TryBegin());A("agentcull").Advance(.1);double cd=A("agentcull").CooldownRemaining;float life=w.PlayerActor.Health.Current;rt.PeriodicTick(A("dart"));
        Check(cls+" poison ticks recover Cull and heal slightly",A("agentcull").CooldownRemaining<=cd-.14f && w.PlayerActor.Health.Current>=life+.99f);
        w.Periodic.RemoveSource(A("dart"));
        if(sniper) {
            Reset("ambush","followthrough","seriesofshots");Heal();
            float deadEye=rt.DirectBonus(A("ambush"),t0);Check("Dead Eye grant",Grant("deadeye"));
            Check("Dead Eye opens Ambush on healthy enemies",rt.DirectBonus(A("ambush"),t0)>=deadEye+.24f);
            t0.Health.ApplyDamage(80000);Check("Dead Eye finishes with Followthrough on weak enemies",rt.DirectBonus(A("followthrough"),t0)>=.24f);Heal();
            float chainBase=rt.DirectBonus(A("seriesofshots"),t1);Check("Firing Chain grant",Grant("chain"));rt.Activated(A("ambush"));rt.Activated(A("seriesofshots"));
            Check("Firing Chain adds a link bonus to the next shot",rt.DirectBonus(A("seriesofshots"),t1)>=chainBase+.059f);rt.Advance(7);
            Check("Firing Chain resets when the window breaks",rt.DirectBonus(A("seriesofshots"),t1)<=chainBase+.001f);
            Check("Echo Round grant",Grant("echoround"));hp=t0.Health.Current;rt.DirectHit(A("followthrough"),t0);
            Check("Echo Round waits for its delay",t0.Health.Current==hp);rt.Advance(1.1);
            Check("Echo Round fires at half damage",hp-t0.Health.Current>=A("followthrough").DamagePerTick*.4f);
            float incoming=rt.IncomingMultiplier;Check("Ballistic Dampers grant",Grant("dampers"));
            Check("Ballistic Dampers divide damage taken",rt.IncomingMultiplier<incoming && rt.IncomingMultiplier>incoming*.98f);
            Check("Snipe has no cards or mechanics",!p.Upgrades.Any(u=>u.AbilityId=="snipershot" && u.Effect!=UpgradeEffect.UnlockAbility) && w.Abilities.Find("snipershot")!.Definition.Damage>=60);
            Check("Holdline and old cover cards are gone",!p.Upgrades.Any(u=>u.Mechanic is "snm_holdline" or "snm_coverfire" or "snm_steadyaim" or "snm_opening"));
            Reset("ambush","seriesofshots","followthrough","suppressivefire");Heal();
            Check("Patience and Killing Window grants",Grant("patience") && Grant("window"));rt.Advance(6);
            float burstBase=rt.DirectBonus(A("ambush"),t1);rt.Activated(A("ambush"));
            Check("Sniper's Patience and Killing Window charge the first cover shot",rt.DirectBonus(A("ambush"),t1)>=burstBase+1.07f);
            rt.Activated(A("seriesofshots"));
            Check("Patience is spent and Killing Window is locked",rt.DirectBonus(A("seriesofshots"),t1)<=burstBase+.01f);
            Reset("ambush","suppressivefire");Heal();Check("Kill Zone grant",Grant("killzone"));hp=t1.Health.Current;rt.DirectHit(A("ambush"),t0);
            Check("Kill Zone splashes nearby enemies in cover",t1.Health.Current<hp);
            Reset("ambush");Heal();Check("Overwatch grant",Grant("overwatch"));w.Player.Presentation.ResetPresentation();w.Targeting.Update(0,true);
            Check("Ambush begins",A("ambush").TryBegin());A("ambush").Advance(.1);cd=A("ambush").CooldownRemaining;rt.Advance(5);
            Check("Overwatch recovers cooldowns faster in cover",A("ambush").CooldownRemaining<=cd-.29f);
            Reset("explosiveprobe","interrogationprobe","plasmaprobe","orbitalstrike");Heal();
            w.Periodic.Apply(A("interrogationprobe"),t0);w.Periodic.Advance(1);
            Check("Relay Network grant",Grant("relay"));rt.DirectHit(A("plasmaprobe"),t0);
            Check("Relay Network spreads the probe to a nearby enemy",w.Targeting.InArea(t0.GlobalPosition,.35f,2.2f).Any(e=>e!=t0 && w.Periodic.Has(A("interrogationprobe"),e)));
            w.Periodic.RemoveSource(A("interrogationprobe"));Heal();w.Periodic.Apply(A("interrogationprobe"),t0);w.Periodic.Advance(1);
            Check("Cascade Detonation grant",Grant("cascade"));hp=t0.Health.Current;rt.DirectHit(A("explosiveprobe"),t0);
            Check("Cascade Detonation pays remaining probe damage without consuming it",hp-t0.Health.Current>5 && w.Periodic.Has(A("interrogationprobe"),t0));
            w.PlayerActor.Health.ClearShield();Check("Probe Shielding grant",Grant("probeshield"));rt.DirectHit(A("interrogationprobe"),t0);
            Check("Probe Shielding grants a small shield",w.PlayerActor.Health.ShieldRemaining>=2.99f);
            float channel=A("plasmaprobe").ChannelSeconds;Check("Lingering Plasma grant",Grant("lingering"));
            Check("Lingering Plasma lengthens the field",Math.Abs(A("plasmaprobe").ChannelSeconds-channel-1)<.01f);
            w.Periodic.RemoveSource(A("interrogationprobe"));
            Reset("empdischarge");for(int i=0;i<12;i++)p.Grant("sniper_cooldown_empdischarge");
            Check("EMP Discharge recovery keeps an 8 second floor",A("empdischarge").CooldownSeconds>=7.99f);
            Reset("ambush","dart","agentcull","weakeningblast");Heal();Check("Contaminated Rounds and Concentrated Volley",Grant("contamination") && Grant("cull"));
        } else {
            Reset("hiddenstrike","backstab","laceration","severtendon");Heal();
            float veilBase=rt.IncomingMultiplier;Check("Shadow Veil grant",Grant("veil"));
            Check("Shadow Veil divides damage taken during the opening",rt.IncomingMultiplier<veilBase);
            float baseline=rt.DirectBonus(A("backstab"),t0);Check("Opening Gambit grant",Grant("gambit"));rt.Activated(A("hiddenstrike"));
            Check("Opening Gambit opens a Concealment window",rt.DirectBonus(A("backstab"),t0)>=baseline+.19f);rt.Advance(4.5);
            Check("Opening Gambit window expires",rt.DirectBonus(A("backstab"),t0)<=baseline+.01f);
            Check("Hamstring Mark grant",Grant("hamstring"));rt.DirectHit(A("severtendon"),t0);
            Check("Hamstring Mark boosts Backstab on marked enemies only",rt.DirectBonus(A("backstab"),t0)>=rt.DirectBonus(A("backstab"),t1)+.14f);
            rt.Defeated(t0);Check("Hamstring Mark spreads on death",rt.DirectBonus(A("backstab"),t1)>=baseline+.14f);
            Check("Twin Slash grant",Grant("twin"));Heal();hp=t0.Health.Current;rt.DirectHit(A("laceration"),t0);rt.Advance(.3);
            Check("Twin Slash repeats at half damage",hp-t0.Health.Current>=A("laceration").DamagePerTick*.4f);
            w.PlayerActor.Health.ApplyDamage(10);life=w.PlayerActor.Health.Current;Check("Close Quarters Recovery grant",Grant("recovery"));rt.Defeated(t1);
            Check("Close Quarters Recovery heals on a kill",w.PlayerActor.Health.Current>=life+1.99f);
        }
        Reset();
        foreach(var e in enemies)e.QueueFree();
    }
    void WriteReceipt(string cls,List<string> checks,string? failure)
    {
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/{cls}-run-powers-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,checks,failure},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
    static readonly string[] NoRecovery={"dart","interrogationprobe","debilitate","flashbang","severtendon"};
    // Shared structural rules for the single-axis Sniper and Operative card pools.
    internal static void AgentCardChecks(RunProgression p,string cls,string mechPrefix,Action<string,bool> Check)
    {
        var cards=p.Upgrades.Where(u=>u.Id.StartsWith(cls+"_")).ToArray();
        var axis=cards.Where(u=>u.Effect is UpgradeEffect.AbilityPower or UpgradeEffect.TickRate).ToArray();
        Check(cls+" axis cards exist for damage, radius, recovery and tick",axis.Any(u=>u.DamagePerRank>0) && axis.Any(u=>u.RadiusPerRank>0) && axis.Any(u=>u.CooldownPerRank>0) && axis.Any(u=>u.Effect==UpgradeEffect.TickRate));
        Check(cls+" every axis card moves exactly one number",axis.All(u=>new[]{u.DamagePerRank,u.RadiusPerRank,u.CooldownPerRank,u.HealingPerRank,u.ShieldPerRank,u.DurationPerRank,u.ThresholdPerRank,u.Effect==UpgradeEffect.TickRate?u.AmountPerRank:0}.Count(v=>v>0)==1));
        Check(cls+" no recovery card on single-target DoTs or stuns",!axis.Any(u=>u.CooldownPerRank>0 && NoRecovery.Contains(u.AbilityId)));
        Check(cls+" no rank caps on class cards, survival cards or mechanics",cards.All(u=>u.MaximumRank==0) && p.Upgrades.Where(u=>u.Effect is UpgradeEffect.Health or UpgradeEffect.Speed).All(u=>u.MaximumRank==0));
        Check(cls+" no generic damage, range or cooldown cards",!p.Upgrades.Any(u=>u.Effect is UpgradeEffect.Damage or UpgradeEffect.Range or UpgradeEffect.Cooldown));
        Check(cls+" survival cards are small",p.Upgrades.Where(u=>u.Effect==UpgradeEffect.Health).All(u=>u.AmountPerRank<=4) && p.Upgrades.Where(u=>u.Effect==UpgradeEffect.Speed).All(u=>u.AmountPerRank<=.0151f));
        var scalable=typeof(RunProgression).GetMethod("Scalable",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
        var mechanics=cards.Where(u=>u.Mechanic.StartsWith(mechPrefix)).ToArray();
        Check(cls+" mechanics use the scaled prefix",mechanics.Length>=15 && !cards.Any(u=>u.Effect==UpgradeEffect.Mechanic && !u.Mechanic.StartsWith(mechPrefix)));
        Check(cls+" every numeric mechanic rolls rarity",mechanics.Where(u=>u.AmountPerRank>0).All(u=>(bool)scalable.Invoke(null,new object[]{u})!));
    }
    // Union of 12 seeded offers must show each DPS spec its damage, area/target, tick-or-recovery, two mechanics and a defence card.
    internal static void SpecCoverage(GameWorld w,RunProgression p,string spec,string[] kit,Action<string,bool> Check)
    {
        p.ResetRun();
        foreach(string id in kit)w.Abilities.Find(id)!.SetUnlocked(true);
        p.Recalculate();w.Build.Pregame.Advance(.61);
        var generate=typeof(RunProgression).GetMethod("GenerateOffers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
        var seen=new List<UpgradeDefinition>();
        for(ulong seed=1;seed<=12;seed++){
            p.SeedOffers(seed);var offers=(List<UpgradeDefinition>)generate.Invoke(p,new object[]{8+(int)(seed%8),Array.Empty<UpgradeDefinition>(),3})!;
            Check(spec+" offer is eligible "+seed,offers.Count==3 && offers.All(p.IsEligible) && offers.Select(u=>u.Id).Distinct().Count()==3);
            seen.AddRange(offers);
        }
        bool Defence(UpgradeDefinition u)=>w.Build.Pregame.AgentDefenceCard(u) || u.Effect is UpgradeEffect.Health;
        Check(spec+" sees a damage card",seen.Any(u=>u.DamagePerRank>0));
        Check(spec+" sees a radius or target card",seen.Any(u=>u.RadiusPerRank>0 || u.Mechanic.Contains("target_")));
        Check(spec+" sees a tick or recovery card",seen.Any(u=>u.Effect==UpgradeEffect.TickRate || u.CooldownPerRank>0));
        Check(spec+" sees two mechanics",seen.Where(u=>u.Effect==UpgradeEffect.Mechanic).Select(u=>u.Id).Distinct().Count()>=2);
        Check(spec+" sees a defence card",seen.Any(Defence));
        p.ResetRun();
    }
    static void SniperMechanicChecks(GameWorld w,RunProgression p,TalentRuntime rt,Action<string,bool> Check)
    {
        var scaled=p.Upgrades.First(u=>u.Id=="sniper_chain");var original=scaled.Rarity;
        p.ResetRun();foreach(string id in new[]{"ambush","seriesofshots","followthrough"})w.Abilities.Find(id)!.SetUnlocked(true);p.Recalculate();rt.Advance(.61);
        scaled.Rarity=PowerRarity.Artifact;Check("Firing Chain grant",p.Grant("sniper_chain"));
        Check("mechanic power scales with rarity",Math.Abs(p.RankPower(scaled)-scaled.RarityMultiplier)<.001f && scaled.RarityMultiplier>1.9f);scaled.Rarity=original;
        p.ResetRun();
        SpecCoverage(w,p,"marksmanship",new[]{"ambush","seriesofshots","followthrough","suppressivefire","entrench","shieldprobe"},Check);
        SpecCoverage(w,p,"engineering",new[]{"interrogationprobe","explosiveprobe","plasmaprobe","empdischarge","orbitalstrike","fragmentationgrenade","shieldprobe"},Check);
        SpecCoverage(w,p,"sniper lethality",new[]{"dart","corrosivegrenade","agentcull","weakeningblast","shieldprobe"},Check);
    }
    static void OperativeChecks(GameWorld w,RunProgression p,TalentRuntime rt,Action<string,bool> Check)
    {
        AgentCardChecks(p,"operative","opm_",Check);
        Check("Operative has Venomous Echo, Twin Slash, Hamstring Mark and shared Lethality cards",new[]{"operative_venomous_echo","operative_twin","operative_hamstring","operative_pandemic","operative_septic","operative_tempo","operative_echo_dart","operative_legion_corrosivegrenade"}.All(id=>p.Upgrades.Any(u=>u.Id==id)));
        Check("Operative has no Medicine cards",!p.Upgrades.Any(u=>u.Id.StartsWith("operative_") && u.AbilityId is "koltoprobe" or "koltoinjection" or "surgicalprobe" or "stimboost"));
        Check("Operative cards exist for unowned abilities only as ineligible",p.Upgrades.Where(u=>u.Id=="operative_twin").All(u=>!p.IsEligible(u)));
        SpecCoverage(w,p,"concealment",new[]{"backstab","hiddenstrike","laceration","severtendon","debilitate","shiv","shieldprobe"},Check);
        SpecCoverage(w,p,"operative lethality",new[]{"dart","corrosivegrenade","agentcull","weakeningblast","backstab","laceration","shieldprobe"},Check);
    }
}
