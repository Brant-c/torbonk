using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;
public partial class AssassinRefinementSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool passed){if(!passed)throw new Exception(name);_checks.Add(name);}
    public async void Run(GameWorld w)
    {
        string? failure=null;
        bool review=OS.GetCmdlineUserArgs().Contains("--assassin-review");
        string build=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--assassin-build="))?.Split('=')[1]??"baseline";
        try {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
            var p=w.Progression!;
            Check("starts with Lacerate only",w.Abilities.Abilities.Where(a=>a.Unlocked).Select(a=>a.Definition.Id).SequenceEqual(new[]{"lacerate"}) && !w.Abilities.Abilities.Any(a=>a.Definition.IsBasicAttack));
            Check("one bounded global power track",p.Upgrades.Count(u=>u.Effect==UpgradeEffect.Damage)==1 && p.Upgrades.Single(u=>u.Effect==UpgradeEffect.Damage).MaximumRank==5 && p.Upgrades.Single(u=>u.Effect==UpgradeEffect.Damage).AmountPerRank==.1f);
            Check("Lacerate baseline is twelve and free",w.Combat.Definition.Damage==12 && w.Combat.Definition.Slotless && w.Abilities.Equipped(AbilityCategory.Offensive)==0);
            Check("Spike starts with modest burst and area",w.Abilities.Find("spike")!.Definition.Damage==24 && w.Abilities.Find("spike")!.Radius==.22f && w.Abilities.Find("spike")!.CooldownSeconds==16);
            var cat=TalentCatalog.Current;
            var maps=JsonDocument.Parse(FileAccess.GetFileAsString("res://../manifests/assassin-refinement-icon-mappings.json")).RootElement;
            Check("all twenty-four talent icons match decoded April records",cat.ForClass("assassin").All(t=>maps.EnumerateArray().Any(m=>t.Source.StartsWith(m.GetProperty("id").GetString()!) && t.Icon.EndsWith("/"+m.GetProperty("icon").GetString()!.ToLowerInvariant()+".png"))));
            Check("all talent artwork loads",cat.ForClass("assassin").All(t=>ResourceLoader.Exists(t.Icon)));
            var expectedIcons=new Dictionary<string,string>{{"spike","spike"},{"voltaicslash","vampiricslash"},{"maul","sunderingassault"},{"wither","crushingdarkness"},{"lowslash","lowslash"},{"forcecloak","force"},{"deflection","lightsaberreflection"}};
            Check("new ability and offer icons agree",expectedIcons.All(x=>w.Abilities.Find(x.Key)!.Definition.Icon==x.Value && p.Upgrades.Single(u=>u.Id=="unlock_"+x.Key).Icon==x.Value));
            string[] order=w.Build.Pregame.EarlyAbilityOrder;
            if(build=="madness")Check("Madness core priority",order[0]=="creepingterror" && order[1]=="deathfield");
            if(build=="deception")Check("Deception core priority",order[0]=="discharge" && order[1]=="spike");
            p.AddExperience(p.RequiredExperience);p._Process(3);
            Check("level two guarantees two core unlock choices",p.Offers.Count(u=>u.Effect==UpgradeEffect.UnlockAbility)==2 && p.Offers.Any(u=>u.AbilityId==order[0]) && p.Offers.Any(u=>u.AbilityId==order[1]));
            Check("first core learned at two",p.Choose(p.Offers.ToList().FindIndex(u=>u.AbilityId==order[0])));p._Process(3);
            p.AddExperience(p.RequiredExperience);p._Process(3);
            Check("other defining core offered at three",p.Offers.Any(u=>u.AbilityId==order[1] && u.Effect==UpgradeEffect.UnlockAbility));
            Check("Banish replaces without resurrecting the ability",p.Banish(p.Offers.ToList().FindIndex(u=>u.AbilityId==order[1])) && !p.Offers.Any(u=>u.AbilityId==order[1]));
            int rerolls=p.RerollsRemaining;Check("Reroll respects banished core",p.Reroll() && p.RerollsRemaining==rerolls-1 && !p.Offers.Any(u=>u.AbilityId==order[1]));
            Check("Skip remains available",p.Skip());p._Process(3);p.ResetRun();
            var targets=new List<Combatant>();
            for(int i=0;i<3;i++){var t=new Combatant {Name="RefinementFixture"+i,Team=CombatTeam.Hostile};t.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3(.08f+i*.04f,0,.01f);targets.Add(t);}
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            async System.Threading.Tasks.Task Capture(string name){if(!review)return;await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/assassin-"+name+".png"));}
            var spike=w.Abilities.Find("spike")!;spike.SetUnlocked(true);w.Player.Presentation.ResetPresentation();
            float before=targets[0].Health.Current;
            Check("Spike begins without Force Cloak",!w.Abilities.Find("forcecloak")!.Unlocked && spike.TryBegin());
            Check("Spike immediately enters stealth",w.Build.Pregame.AssassinCloaked && ((AssassinAbilityPresentation)spike.Presentation).Vanishing);
            Check("character and attached weapon are actually hidden during stealth",!w.Player.Presentation.Visible && !w.Player.Presentation.IsVisibleInTree());await Capture("spike-stealth");
            spike.Advance(.7);Check("no premature Spike damage",targets.All(t=>t.Health.Current==before));spike.Advance(.31);
            Check("reveal precedes area damage",!w.Build.Pregame.AssassinCloaked && !((AssassinAbilityPresentation)spike.Presentation).Vanishing && targets.All(t=>t.Health.Current<before) && spike.LastDistinctHits>=3);
            Check("opacity restores after reveal",w.Player.Presentation.Visible && w.Player.Presentation.IsVisibleInTree());
            var visual=(AssassinAbilityPresentation)spike.Presentation;Check("Spike has original particle and audio output",visual.EffectCount>0 && visual.AudioVoicesStarted>0);await Capture("spike-impact");
            spike.Interrupt();visual.Release();Check("Spike interruption clears particles",visual.EffectCount==0);w.Build.Pregame.Advance(5);
            spike.ResetCombat();w.Player.Presentation.ResetPresentation();Check("Spike can restart for interruption check",spike.TryBegin());spike.Interrupt();Check("interrupting vanish restores visibility immediately",!w.Build.Pregame.AssassinCloaked && w.Player.Presentation.Visible && w.Player.Presentation.IsVisibleInTree());
            foreach(string id in new[]{"maul","voltaicslash","wither","lowslash","deflection","forcecloak"}){
                w.Player.Presentation.ResetPresentation();var a=w.Abilities.Find(id)!;a.SetUnlocked(true);a.ResetCombat();Check(id+" casts",a.TryBegin());a.Advance(a.Definition.WindupSeconds+.05);
                var v=(AssassinAbilityPresentation)a.Presentation;Check(id+" original effects/audio resolve",v.AudioVoicesStarted>0 || v.EffectCount>0);
                if(id=="wither") {
                    var emitters=v.GetChildren().OfType<AssassinWitherVisual>().ToArray();
                    Check("Wither has a ground wave and attached target wisps",emitters.Any(e=>e.GroundWave) && emitters.Count(e=>!e.GroundWave)>=3);
                    foreach(var e in emitters)e._Process(.3);await Capture("wither-ground-and-wisps");
                    foreach(var e in emitters.Where(e=>!e.GroundWave))e._Process(.8);
                    Check("Wither wisps remain after the impact flash",emitters.Where(e=>!e.GroundWave).All(e=>e.WispCount>0 && !e.IsQueuedForDeletion()));
                    foreach(var e in emitters)e._Process(4.1);
                    Check("Wither emitters expire",emitters.All(e=>e.IsQueuedForDeletion()));
                }
                if(review){await Capture(id);}
                a.Interrupt();v.Release();w.Build.Pregame.Advance(6);
            }
            void Cast(string id) {
                w.Player.Presentation.ResetPresentation();var a=w.Abilities.Find(id)!;a.SetUnlocked(true);a.ResetCombat();Check(id+" synergy cast",a.TryBegin());for(int i=0;i<90;i++)a.Advance(.05);
            }
            w.Periodic.Clear();
            var discharge=w.Abilities.Find("discharge")!;discharge.SetUnlocked(true);
            var terror=w.Abilities.Find("creepingterror")!;terror.SetUnlocked(true);
            w.Periodic.Apply(discharge,targets[0]);w.Periodic.Apply(terror,targets[0]);
            Cast("lacerate");Check("baseline Lacerate spreads Discharge",targets.All(t=>w.Periodic.Has(discharge,t)));
            Check("other DoTs still need spreading talent",!w.Periodic.Has(terror,targets[1]));
            double duration=w.Periodic.Remaining(terror,targets[0]);float hp=targets[0].Health.Current;Cast("shock");
            Check("Shock echoes existing DoTs without consuming them",hp-targets[0].Health.Current>w.Abilities.Find("shock")!.DamagePerTick && w.Periodic.Has(terror,targets[0]) && Math.Abs(w.Periodic.Remaining(terror,targets[0])-duration)<.001);
            w.Build.Pregame.Advance(.01);float normal=w.Build.DamageMultiplier(discharge,targets[0]);Cast("voltaicslash");Check("Voltaic melee charges also empower Discharge",w.Build.DamageMultiplier(discharge,targets[0])>normal);
            float periodic=w.Build.Pregame.PeriodicMultiplier(targets[0]);Cast("deathfield");Check("Death Field supports subsequent DoT damage",w.Build.Pregame.PeriodicMultiplier(targets[0])>periodic);
            p.ResetRun();
            foreach(string id in new[]{"creepingterror","deathfield","discharge","voltaicslash","shock","crushingdarkness"})Check("core fits: "+id,p.Grant("unlock_"+id));
            Check("six learned damage abilities fit beside baseline",w.Abilities.Equipped(AbilityCategory.Offensive)==6 && w.Abilities.Abilities.Count(a=>a.Unlocked && a.Definition.Category==AbilityCategory.Offensive)==7);
            foreach(string id in new[]{"forceshroud","forcespeed","forcecloak","deflection","lowslash"})Check("utility fits without consuming damage space: "+id,p.Grant("unlock_"+id));
            Check("utility and control stay outside damage budget",w.Abilities.Equipped(AbilityCategory.Offensive)==6 && w.Abilities.Abilities.Count(a=>a.Unlocked)==12);
            p.AddExperience(p.RequiredExperience);p._Process(3);int spikeOffer=p.Offers.ToList().FindIndex(u=>u.AbilityId=="spike");
            Check("full damage budget offers replacement",spikeOffer>=0 && p.Choose(spikeOffer) && p.Replacing);
            int starter=p.ReplacementOptions.ToList().FindIndex(a=>a.Definition.Id=="lacerate");
            Check("Lacerate can be swapped while retaining its free position",starter>=0 && p.Replace(starter) && !w.Combat.Unlocked && w.Abilities.Find("spike")!.InheritsFreeSlot && w.Abilities.Equipped(AbilityCategory.Offensive)==6);
            Check("relearning Lacerate cannot create another free slot",!w.Combat.Definition.Slotless && !p.Grant("unlock_lacerate"));
            p._Process(3);
            Check("all new original clips resolve",w.Abilities.Abilities.Where(a=>a.Definition.StartClip.StartsWith("Assassin/")).All(a=>w.Player.Presentation.Animator.HasAnimation(a.Definition.StartClip)));
            p.ResetRun();Check("reset restores Lacerate-only build",w.Abilities.Abilities.Count(a=>a.Unlocked)==1 && w.Combat.Unlocked);
            w.Abilities.ResetCombat();w.Player.Presentation.ResetPresentation();
            var cloak=w.Abilities.Find("forcecloak")!;var maul=w.Abilities.Find("maul")!;cloak.SetUnlocked(true);maul.SetUnlocked(true);
            Check("Force Cloak opens an automatic Maul opportunity",cloak.TryBegin());w.Abilities._PhysicsProcess(.2);
            Check("automatic scheduler uses Maul before filler in its opening window",maul.Casts==1 && w.Combat.Casts==0 && w.Player.Presentation.Visible);
            p.ResetRun();
            var power=p.Upgrades.Single(u=>u.Effect==UpgradeEffect.Damage);
            for(int i=0;i<5;i++)Check("power rank "+i,p.Grant(power.Id));Check("power track cannot grow indefinitely",!p.Grant(power.Id));
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/assassin-refinement-{build}-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,failure,checks=_checks},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
}
