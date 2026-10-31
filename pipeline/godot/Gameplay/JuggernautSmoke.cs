using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;
public partial class JuggernautSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool ok){if(!ok)throw new Exception(name);_checks.Add(name);}
    public async void Run(GameWorld w)
    {
        string? failure=null;
        string savePath=TalentProfiles.SavePath;
        string? originalSave=System.IO.File.Exists(savePath)?System.IO.File.ReadAllText(savePath):null;
        var originalRuntime=w.Build.Pregame;
        TalentProfiles.Save("juggernaut",new Dictionary<string,int>());
        w.PlayerActor.Health.Damaged-=originalRuntime.Damaged;
        var cleanRuntime=new TalentRuntime(w);
        typeof(RunBuildEffects).GetProperty("Pregame")!.SetValue(w.Build,cleanRuntime);
        w.PlayerActor.Health.Damaged+=cleanRuntime.Damaged;
        w.Progression!.ResetRun();
        try {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
            var cat=TalentCatalog.Current;
            Check("27 talents, nine per tree",cat.ForClass("juggernaut").Length==27 && cat.ForClass("juggernaut").GroupBy(t=>t.Tree).All(t=>t.Count()==9));
            var talents=cat.ForClass("juggernaut");
            Check("nine one-point talents and 12 shared points",cat.Budget==12 && talents.All(t=>t.MaxRank==1 && t.Cost==1) && talents.GroupBy(t=>t.Tree).All(g=>g.Sum(t=>t.MaxRank*t.Cost)==9));
            Check("strong bottom row gated behind five points",talents.Where(t=>t.Tier==2).Count()==9 && talents.Where(t=>t.Tier==2).All(t=>t.RequiredPoints==5));
            Check("authentic distinct icon textures load",talents.Select(t=>t.Icon).Distinct().Count()>=23 && talents.All(t=>GD.Load<Texture2D>(t.Icon) is {} texture && texture.GetWidth()>0));
            foreach(var tree in talents.GroupBy(t=>t.Tree)) {
                var dip=tree.Where(t=>t.Tier==0).ToDictionary(t=>t.Id,t=>1);
                Check("off-spec top row "+tree.Key,cat.Validate("juggernaut",dip).Count==3);
                Check("capstones inaccessible from top alone "+tree.Key,tree.Where(t=>t.Tier==2).All(t=>cat.Locked("juggernaut",t,dip)!=null));
                var split=tree.Concat(talents.Where(t=>t.Tree!=tree.Key && t.Tier==0).Take(3)).ToDictionary(t=>t.Id,t=>1);
                Check("nine plus three budget "+tree.Key,cat.Validate("juggernaut",split).Count==12);
            }
            var drops=w.Progression!.Upgrades.Where(u=>talents.Any(t=>t.Effects.Any(e=>e.Key==u.Mechanic))).ToArray();
            Check("entire drop pool has unique identities",w.Progression!.Upgrades.Select(u=>u.Id).Distinct().Count()==w.Progression.Upgrades.Count);
            Check("27 matching distinct talent drops",drops.Length==27&&drops.Select(u=>u.Mechanic).Distinct().Count()==27);
            Check("capstone drops wait until level eight",drops.Where(u=>talents.Any(t=>t.Tier==2&&t.Effects.Any(e=>e.Key==u.Mechanic))).All(u=>!w.Progression.IsEligible(u)));
            Check("other talent catalogs retained",cat.ForClass("sorcerer").Length==27 && cat.ForClass("assassin").Length==27);
            Check("free replaceable starting Smash",w.Combat.Definition.Id=="smash" && w.Combat.Unlocked && w.Combat.Definition.Slotless && w.Abilities.Equipped(AbilityCategory.Offensive)==0);
            Check("free Vicious Slash occupies slot 1",w.Abilities.Find("basicassault")==null && w.Abilities.Find("viciousslash") is {Unlocked:true} slash && slash.Definition.IsBasicAttack && w.Abilities.QuickbarAbilities[0]==slash);
            Check("normal offensive and utility budgets",w.Abilities.OffensiveLimit==w.SlotRules!.OffensiveSlots && w.Abilities.UtilityLimit==w.SlotRules.UtilitySlots);
            Check("durable baseline",w.PlayerActor.Health.Maximum>=155 && w.Build.IncomingMultiplier<.9f);
            Check("Push removed and baseline Slash stays out of unlock offers",w.Abilities.Find("forcepush")==null && !w.Progression!.Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId is "forcepush" or "viciousslash"));
            var opening=w.Progression!;
            for(ulong seed=1;seed<=8;seed++) {
                opening.ResetRun();opening.SeedOffers(seed);opening.AddExperience(opening.RequiredExperience);opening._Process(3);
                Check("seed "+seed+": level two always offers Scream and Choke",opening.Level==2 && opening.Offers.Count==3 && opening.Offers[0].AbilityId=="forcescream" && opening.Offers[1].AbilityId=="forcechoke");
                Check("seed "+seed+": early build talent is usable",opening.Offers.Any(u=>u.Mechanic.StartsWith("jg_") && opening.IsEligible(u)));
                Check("seed "+seed+": reroll retains Scream",opening.Reroll() && opening.Offers.Any(u=>u.AbilityId=="forcescream"));
                int smashOffer=opening.Offers.ToList().FindIndex(u=>u.AbilityId=="forcescream");
                if(seed==1){Check("banishing Scream is respected",opening.Banish(smashOffer) && !opening.Offers.Any(u=>u.AbilityId=="forcescream" && u.Effect==UpgradeEffect.UnlockAbility));continue;}
                Check("Scream can be learned immediately",opening.Choose(smashOffer) && w.Abilities.Find("forcescream")!.Unlocked);opening._Process(3);
                opening.AddExperience(opening.RequiredExperience);opening._Process(3);
                Check("level three prioritizes Choke after Scream",opening.Offers[0].AbilityId=="forcechoke" && !opening.Offers.Any(u=>u.AbilityId=="forcescream" && u.Effect==UpgradeEffect.UnlockAbility));
                Check("Choke can be learned at level three",opening.Choose(0) && w.Abilities.Find("forcechoke")!.Unlocked);opening._Process(3);
                opening.AddExperience(opening.RequiredExperience);opening._Process(3);
                Check("level four has one ability and two usable AoE talents",opening.Offers.Count(u=>u.Effect==UpgradeEffect.UnlockAbility)==1 && opening.Offers.Count(u=>u.Mechanic.StartsWith("jg_"))==2);
            }
            opening.ResetRun();GetTree().Paused=false;
            var target=new Combatant {Name="JuggernautFixture",Team=CombatTeam.Hostile};target.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(target);target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.15f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            AutomaticAbility Cast(string id){var a=w.Abilities.Find(id)!;w.Player.Presentation.ResetPresentation();a.SetUnlocked(true);a.ResetCombat();Check(id+" activates",a.TryBegin());for(int i=0;i<90;i++)a.Advance(.05);return a;}
            Cast("impale");var ravage=Cast("ravage");Check("Ravage has three timed hits",ravage.DamageEvents==3);
            Cast("forcecrush");Check("Force Crush persists",w.Periodic.Has(w.Abilities.Find("forcecrush")!,target));var before=target.Health.Current;w.Periodic.Advance(2);Check("Force Crush ticks",target.Health.Current<before);
            var retaliate=w.Abilities.Find("retaliation")!;retaliate.SetUnlocked(true);retaliate.ResetCombat();Check("counterattack waits for damage",!retaliate.TryBegin());w.PlayerActor.Health.ApplyDamage(5,DamageType.Internal);Cast("retaliation");
            var execute=w.Abilities.Find("viciousthrow")!;execute.SetUnlocked(true);execute.ResetCombat();Check("execution waits for threshold",!execute.TryBegin());target.Health.ApplyDamage(8000,DamageType.Internal);Cast("viciousthrow");
            w.PlayerActor.Health.ApplyDamage(80,DamageType.Internal);Cast("enrageddefense");float h=w.PlayerActor.Health.Current;w.PlayerActor.Health.ApplyDamage(1,DamageType.Internal);Check("Enraged Defense reacts with bounded healing",w.PlayerActor.Health.Current>h && w.PlayerActor.Health.Current<h+5);w.Build.Pregame.Advance(7);
            Cast("saberward");Check("Saber Ward creates finite protection",w.PlayerActor.Health.Shielded);w.PlayerActor.Health.ClearShield();
            var smash=w.Abilities.Find("smash")!;Check("Smash leap preserved",smash.Definition.StartClip=="D3/SmashLeap" && smash.Definition.WindupSeconds>.9f);
            Cast("smash");Cast("forcescream");Cast("sunderingassault");Check("Sundering Assault weakens armor",target.Health.VulnerabilitySeconds>0);
            var cluster=new List<Combatant>{target};
            for(int i=1;i<7;i++){
                var f=new Combatant{Name="AoeFixture"+i,Team=CombatTeam.Hostile};f.AddChild(new Health{Name="Health",Maximum=10000});w.AddChild(f);f.GlobalPosition=target.GlobalPosition+Vector3.Right*(i*.015f);cluster.Add(f);
            }
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            int DamagedBy(string id){foreach(var f in cluster)f.Health.Restore();w.Periodic.Clear();Cast(id);return cluster.Count(f=>f.Health.Current<f.Health.Maximum);}
            var choke=w.Abilities.Find("forcechoke")!;choke.SetUnlocked(true);
            Check("base Choke is single target",DamagedBy("forcechoke")==1);
            for(int rank=1;rank<=3;rank++){
                Check("Choke target talent rank "+rank,w.Progression!.Grant("juggernaut_grip"));
                Check("Choke hits exactly "+(rank+1)+" targets",DamagedBy("forcechoke")==rank+1);
            }
            Check("Choke target card is no longer rank capped",w.Progression!.Upgrades.Single(u=>u.Id=="juggernaut_grip").MaximumRank==0);
            var rankedScream=w.Abilities.Find("forcescream")!;rankedScream.SetUnlocked(true);
            for(int rank=1;rank<=3;rank++){
                Check("Scream target talent rank "+rank,w.Progression.Grant("juggernaut_resonance"));
                Check("Scream hits exactly "+(1+2*rank)+" targets",DamagedBy("forcescream")==1+2*rank);
            }
            for(int rank=1;rank<=3;rank++){
                Check("Ravage cleave rank "+rank,w.Progression.Grant("juggernaut_sweep"));
                Check("Ravage cleaves exactly "+(1+rank)+" targets",DamagedBy("ravage")==1+rank);
            }
            var sundering=w.Abilities.Find("sunderingassault")!;sundering.SetUnlocked(true);
            Check("Sundering cleave learned",w.Progression.Grant("juggernaut_sundering"));
            Check("Sundering cleaves two targets",DamagedBy("sunderingassault")==2);
            Check("both cleave victims have weakened armor",cluster.Count(f=>f.Health.VulnerabilitySeconds>0)==2);
            typeof(RunProgression).GetProperty("Level")!.SetValue(w.Progression,8);
            Check("Smash wound learned",w.Progression.Grant("juggernaut_fissure"));DamagedBy("smash");Check("Smash wounds its victims",cluster.All(f=>w.Periodic.Has(smash,f)));
            Check("Choke Crush interaction learned",w.Progression.Grant("juggernaut_convergence"));DamagedBy("forcechoke");Check("Choke applies learned Crush to four victims",cluster.Count(f=>w.Periodic.Has(w.Abilities.Find("forcecrush")!,f))==4);
            w.Progression.ResetRun();Check("restart clears run AoE ranks",choke.Definition.Area==AbilityArea.SingleTarget && choke.Definition.MaximumTargets==1 && rankedScream.Definition.Area==AbilityArea.SingleTarget && smash.Definition.PeriodicDamage==0);
            foreach(var f in cluster.Skip(1)){f.CombatEnabled=false;f.QueueFree();}
            var ranks=talents.Where(t=>t.Tree=="immortal" || t.Tree=="vengeance" && t.Tier==0).ToDictionary(t=>t.Id,t=>1);
            Check("12-point full tree and off-spec dip validates",cat.Validate("juggernaut",ranks).Count==12);
            TalentRuntime Runtime(string tree){TalentProfiles.Save("juggernaut",talents.Where(t=>t.Tree==tree).ToDictionary(t=>t.Id,t=>1));return new TalentRuntime(w);}
            var runtime=Runtime("juggernaut_rage");var scream=w.Abilities.Find("forcescream")!;
            runtime.ApplyStats(scream);Check("one-point Scream talent reaches seven targets",scream.Definition.MaximumTargets==7 && scream.Radius>=.3f);
            float forceCd=smash.CooldownSeconds;runtime.ApplyStats(smash);Check("generic Force recovery benefits Smash",Math.Abs(smash.CooldownSeconds-forceCd/1.1)<.001);
            runtime.Activated(scream);runtime.Activated(smash);Check("Scream primes Smash without a leap",Math.Abs(runtime.DirectBonus(smash,target)-.33f)<.001f);
            runtime.PeriodicTick(w.Abilities.Find("forcecrush")!);runtime.PeriodicTick(w.Abilities.Find("forcecrush")!);runtime.Activated(smash);Check("Force Crush empowers Smash",Math.Abs(runtime.DirectBonus(smash,target)-.32f)<.001f);
            runtime=Runtime("vengeance");runtime.Activated(w.Abilities.Find("impale")!);runtime.Activated(scream);Check("Impale empowers Scream",runtime.DirectBonus(scream,target)>=.3f);
            Check("generic saber mastery works without bleeds",runtime.DirectBonus(w.Abilities.Find("impale")!,target)>=.08f);
            runtime.Damaged(2);runtime.Activated(w.Abilities.Find("impale")!);Check("taking damage empowers next saber attack",runtime.DirectBonus(w.Abilities.Find("impale")!,target)>=.279f);
            Check("generic saber protection activates",runtime.IncomingMultiplier<.8f);runtime.Advance(3);Check("generic saber protection expires",runtime.IncomingMultiplier>.85f);
            runtime=Runtime("immortal");runtime.Damaged(2);runtime.Activated(retaliate);Check("Retaliation grants temporary mitigation",runtime.IncomingMultiplier<.75f);runtime.Advance(5);Check("mitigation expires",runtime.IncomingMultiplier>.75f);
            foreach(string tree in new[]{"immortal","vengeance","juggernaut_rage"}) {
                runtime=Runtime(tree);typeof(RunBuildEffects).GetProperty("Pregame")!.SetValue(w.Build,runtime);
                foreach(var u in drops.Where(u=>u.Spec==tree))Check("pregame suppresses duplicate "+u.Id,!w.Progression.IsEligible(u));
            }
            typeof(RunBuildEffects).GetProperty("Pregame")!.SetValue(w.Build,cleanRuntime);
            Check("all equipped clips exist",w.Abilities.Abilities.All(a=>a.Definition.StartClip=="" || w.Player.Presentation.Animator.HasAnimation(a.Definition.StartClip)));
            foreach(var a in w.Abilities.Abilities)Check(a.Definition.Id+" icon exists",ResourceLoader.Exists("res://Assets/UI/icons/"+a.Definition.Icon+".png"));
            w.Abilities.ResetUnlocks();w.PlayerActor.Health.Restore();
            foreach(string id in new[]{"forcescream","impale","ravage","forcecrush"})Check(id+" fits normal slot budget",w.Abilities.Unlock(id));
            var incoming=w.Abilities.Find("crushingblow")!;Check("offensive slots are full",!w.Abilities.CanUnlock(incoming));
            var p=w.Progression!;p.Upgrades.Clear();p.Upgrades.Add(GD.Load<UpgradeDefinition>("res://Gameplay/Upgrades/unlock_crushingblow.tres"));p.AddExperience(p.RequiredExperience);p._Process(3);
            Check("full bar offers replacement",p.Offers.Count>0 && p.Choose(0) && p.Replacing);
            int free=p.ReplacementOptions.ToList().FindIndex(a=>a==w.Combat);Check("free Smash appears in replacement list",free>=0);
            Check("replacement preserves free position",p.Replace(free) && !w.Combat.Unlocked && incoming.Unlocked && incoming.InheritsFreeSlot && w.Abilities.Equipped(AbilityCategory.Offensive)==4);
            GetTree().Paused=false;
            TalentProfiles.Save("juggernaut",new Dictionary<string,int>());
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        w.PlayerActor.Health.Damaged-=cleanRuntime.Damaged;
        w.PlayerActor.Health.Damaged+=originalRuntime.Damaged;
        typeof(RunBuildEffects).GetProperty("Pregame")!.SetValue(w.Build,originalRuntime);
        if(originalSave!=null)System.IO.File.WriteAllText(savePath,originalSave);else if(System.IO.File.Exists(savePath))System.IO.File.Delete(savePath);
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/juggernaut-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,failure,checks=_checks},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
}
