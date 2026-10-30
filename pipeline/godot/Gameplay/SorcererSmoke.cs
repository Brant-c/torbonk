using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors;

public partial class SorcererSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool passed){if(!passed)throw new Exception(name);_checks.Add(name);GD.Print("SORCERER: "+name);}
    public async void Run(GameWorld w)
    {
        string? failure=null;
        try
        {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
            foreach(var enemy in w.Spawner.Enemies){enemy.CombatEnabled=false;enemy.SetPhysicsProcess(false);}
            var target=new Combatant {Name="SorcFixture",Team=CombatTeam.Hostile};target.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(target);
            target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.2f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            bool assassin=w.RunConfiguration.AdvancedClassId=="assassin";
            var p=w.Progression!;
            int startingCount=assassin?1:2;
            Check("correct starting attacks begin unlocked",w.Abilities.Abilities.Count(a=>a.Unlocked)==startingCount && w.Abilities.Equipped(AbilityCategory.Offensive)==(assassin?0:1));
            var ritual=w.Artifacts!.Catalog.Single(a=>a.Id=="run.ritual");
            Check("locked Death Field relic excluded",!w.Artifacts.Eligible(ritual,2));
            if(!assassin)
            {
                // Talent rework: one axis per card, no rank caps, no Static Barrier, no recovery on single-target DoTs.
                int Axes(UpgradeDefinition u)=>new[]{u.DamagePerRank,u.RadiusPerRank,u.CooldownPerRank,u.HealingPerRank,u.ShieldPerRank,u.DurationPerRank,u.ThresholdPerRank}.Count(x=>x!=0);
                var powers=p.Upgrades.Where(u=>u.Id.StartsWith("sorcerer_") && u.Effect==UpgradeEffect.AbilityPower).ToArray();
                Check("every Sorcerer power card moves exactly one axis",powers.Length>=18 && powers.All(u=>Axes(u)==1));
                Check("Sorcerer scalable cards are never rank-capped",p.Upgrades.Where(u=>u.Effect is UpgradeEffect.AbilityPower or UpgradeEffect.Damage or UpgradeEffect.Speed or UpgradeEffect.Radius or UpgradeEffect.Cooldown or UpgradeEffect.TickRate).All(u=>u.MaximumRank==0));
                bool Recovery(UpgradeDefinition u)=>u.Effect==UpgradeEffect.Cooldown || u.CooldownPerRank>0;
                Check("single-target DoTs have no recovery card",!p.Upgrades.Any(u=>Recovery(u) && u.AbilityId is "affliction" or "creepingterror" or "crushingdarkness"));
                Check("Force Storm, Death Field and Thundering Blast have recovery cards",new[]{"storm","deathfield","thunderingblast"}.All(id=>p.Upgrades.Any(u=>Recovery(u) && u.AbilityId==id)));
                Check("range and tick cards exist for Force Lightning and Force Storm",new[]{"sorcerer_range_lightning","sorcerer_range_storm","sorcerer_tick_lightning","sorcerer_tick_storm"}.All(id=>p.Upgrades.Any(u=>u.Id==id)));
                Check("Static Barrier cards and Dark Heal bundle are gone",!p.Upgrades.Any(u=>u.AbilityId=="barrier" || u.Id is "backlash" or "darkheal_power" or "deathfield_power" or "barrier_power") &&
                    !p.Upgrades.Any(u=>u.Mechanic is "so_pulse" or "so_heal_barrier" or "so_barrier_field" or "barrier_heal" or "barrier_mending" or "shield_recovery" or "emergency"));
                var heal=p.Upgrades.Single(u=>u.Id=="sorcerer_healing_darkheal");
                Check("Dark Heal healing card is +5% per rank and healing-only",Math.Abs(heal.HealingPerRank-.05f)<1e-6f && Axes(heal)==1);
                Check("flat Recklessness, Channel Mastery and Lightning Reach are gone",!p.Upgrades.Any(u=>u.Id is "cooldown" or "tick" or "range"));
                Check("Affliction cards are not offered to a Lightning-only run",!p.IsEligible(p.Upgrades.Single(u=>u.Id=="sorcerer_power_affliction")));
            }
            void Cast(AutomaticAbility a)
            {
                w.Player.Presentation.ResetPresentation();
                a.ResetCombat();Check(a.Definition.Id+" starts",a.TryBegin());
                for(double t=0;t<a.Definition.WindupSeconds+a.ChannelSeconds+a.Definition.RecoverySeconds+.2;t+=.05)a.Advance(.05);
            }
            if(assassin)
            {
                var field=w.Abilities.Find("deathfield")!;
                Check("Assassin owns field unlock and power",field!=null && p.Upgrades.Any(u=>u.AbilityId=="deathfield" && u.Effect==UpgradeEffect.AbilityPower));
                Check("Assassin cannot learn blast",w.Abilities.Find("thunderingblast")==null && !p.Upgrades.Any(u=>u.AbilityId=="thunderingblast"));
                Check("Assassin learns Death Field",p.Grant("unlock_deathfield"));
                Check("Assassin can also learn Affliction",p.Grant("unlock_affliction"));
                Check("original field clip available",w.Player.Presentation.Animator.HasAnimation("B8/DeathField") && field!.Presentation is SurvivalAbilityPresentation);
                var before=target.Health.Current;Cast(field!);
                Check("Death Field deals internal area damage",target.Health.Current<before && target.Health.LastDamageType==DamageType.Internal);
                Check("Death Field relic follows Assassin",w.Artifacts.Eligible(ritual,2));
            }
            else
            {
                Check("Sorcerer shares Death Field and Creeping Terror",w.Abilities.Find("deathfield")?.Unlocked==false && w.Abilities.Find("creepingterror")?.Unlocked==false);
                Check("blast is learnable alongside baseline Affliction",p.Grant("unlock_thunderingblast") && w.Abilities.Find("affliction")!.Unlocked);
                Check("blast cannot be learned twice",!p.Grant("unlock_thunderingblast"));
                var blast=w.Abilities.Find("thunderingblast")!;var dot=w.Abilities.Find("affliction")!;
                Check("April cast and cooldown",blast.Definition.WindupSeconds==2 && blast.CooldownSeconds==9 && blast.ChannelSeconds==0);
                Check("original blast clips",w.Player.Presentation.Animator.HasAnimation("Sorc/BlastCharge") && w.Player.Presentation.Animator.HasAnimation("Sorc/BlastRelease"));
                bool crit=false;target.Health.Resolved+=change=>{if(change.Kind==CombatEventKind.Damage)crit=change.IsCritical;};
                blast.ResetCombat();Check("blast begins windup",blast.TryBegin());var hp=target.Health.Current;blast.Advance(1.9);
                Check("no damage before two seconds",target.Health.Current==hp);
                var buildup=(SorcererAbilityPresentation)blast.Presentation;
                Check("blast charge persists on both hands through windup",buildup.ChargeVisible && buildup.ChargeHandCount==2);
                async System.Threading.Tasks.Task Capture(string name)
                {
                    if(!OS.GetCmdlineUserArgs().Contains("--sorcerer-review"))return;
                    if(name=="charge")for(int frame=0;frame<24;frame++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../manifests/sorcerer-{name}.png"));
                }
                await Capture("charge");blast.Advance(.11);
                Check("blast has no damage during outbound flight",target.Health.Current==hp && buildup.ProjectileVisible);
                blast.Advance(blast.Definition.ProjectileSeconds);
                Check("one internal hit without Affliction",Math.Abs(hp-target.Health.Current-60)<.01 && !crit && target.Health.LastDamageType==DamageType.Internal);
                Check("blast releases held charge on impact",!buildup.ChargeVisible);
                await Capture("blast");
                blast.Abort();Cast(dot);Check("Affliction applied",w.Periodic.Has(dot,target));
                hp=target.Health.Current;Cast(blast);
                Check("owned Affliction guarantees critical hit",Math.Abs(hp-target.Health.Current-90)<.01 && crit);
                Check("blast does not consume Affliction",w.Periodic.Has(dot,target));
                var visual=(SorcererAbilityPresentation)blast.Presentation;
                Check("original audio and impact visible",visual.AudioVoicesStarted>=4 && visual.EffectCount>0);
                blast.Abort();Check("abort clears blast effects",visual.EffectCount==0);
                w.Periodic.Clear();var foreign=new AutomaticAbility {Caster=target};foreign.Configure(dot.Definition);foreign.SetUnlocked(true);w.AddChild(foreign);w.Periodic.Apply(foreign,target);
                hp=target.Health.Current;Cast(blast);Check("foreign Affliction cannot guarantee critical",Math.Abs(hp-target.Health.Current-60)<.01 && !crit);
                w.Periodic.Clear();Cast(dot);hp=target.Health.Current;w.Periodic.Advance(15);
                Check("Affliction ticks five more times then expires",Math.Abs(hp-target.Health.Current-40)<.01 && w.Periodic.Count==0);
                hp=target.Health.Current;Cast(blast);Check("expired Affliction cannot guarantee critical",Math.Abs(hp-target.Health.Current-60)<.01 && !crit);
                blast.ResetCombat();Check("cancellable blast starts",blast.TryBegin());hp=target.Health.Current;target.GlobalPosition+=Vector3.Right*3;blast.Advance(2.1);
                Check("lost target cancels without damage",target.Health.Current==hp && !blast.Casting || target.Health.Current==hp && blast.State==LightningState.Ending);
                target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.2f;
                Check("four offensive slot limit preserved",w.Abilities.Unlock("chain") && w.Abilities.Unlock("shock") && !w.Abilities.Unlock("storm"));
            }
            w.Periodic.Clear();w.Abilities.Abort();p.ResetRun();
            Check("restart restores starting attacks and clears effects",w.Abilities.Abilities.Count(a=>a.Unlocked)==startingCount && w.Periodic.Count==0 && p.Ranks.Count==0);
        }
        catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        var report=new {passed=failure==null,checks=_checks,failure};
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/sorcerer-{w.RunConfiguration.AdvancedClassId}-smoke.json"),System.Text.Json.JsonSerializer.Serialize(report,new System.Text.Json.JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
