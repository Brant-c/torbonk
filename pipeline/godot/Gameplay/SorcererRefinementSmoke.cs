using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class SorcererRefinementSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool ok){if(!ok)throw new InvalidOperationException(name);_checks.Add(name);}
    public async void Run(GameWorld w)
    {
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        string? failure=null;
        async System.Threading.Tasks.Task Capture(string name)
        {
            if(!OS.GetCmdlineUserArgs().Contains("--t22-review"))return;
            for(int frame=0;frame<3;frame++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../manifests/t22-{name}.png"));
        }
        try
        {
            if(OS.GetCmdlineUserArgs().Contains("--t22-polarity-start"))
            {
                Check("saved Lightning Barrage talent grants Polarity",w.Build.Pregame.Ranks.GetValueOrDefault("lightning.lightning_barrage")>0 && w.Build.Pregame.PolarityReady);
                Check("talented Polarity appears on starting action bar",w.Abilities.Find("polarityshift")!.Unlocked && w.Hud!.UnlockedAbilityCount==3);
                Check("talented Polarity does not offer redundant unlock training",!w.Progression!.IsEligible(w.Progression.Upgrades.First(u=>u.Id=="polarity_training")));
                w.Progression!.ResetRun();Check("restart preserves talent-granted Polarity",w.Abilities.Find("polarityshift")!.Unlocked && w.Hud!.UnlockedAbilityCount==3);
            }
            else
            {
            var clock=new ChannelClock();int clockTicks=0;clock.Reset(4,1);clock.Advance(1,()=>{clockTicks++;return true;});clock.SetInterval(.5);clock.Advance(3,()=>{clockTicks++;return true;});
            Check("in-flight channel accelerates without replaying ticks or changing duration",clock.Complete && clockTicks==7 && clock.Duration==4);
            var p=w.Progression!;p.SeedOffers(20261027);
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            foreach(var enemy in w.Spawner.Enemies){enemy.CombatEnabled=false;enemy.SetPhysicsProcess(false);}
            Combatant Target(string name,float x,float z=0){var t=new Combatant {Name=name,Team=CombatTeam.Hostile};t.AddChild(new Health {Name="Health",Maximum=100000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3(x,0,z);return t;}
            var a=Target("T22A",.2f);var b=Target("T22B",.25f,.05f);var c=Target("T22C",.28f,-.05f);
            var lightningVisual=(LightningPresentation)w.Combat.Presentation;
            var originalPosition=a.GlobalPosition;float originalAimHeight=a.AimHeight;a.AimHeight=.145f;
            Check("Force Lightning configures a target without an attachment rig",lightningVisual.Start(a));
            Check("Force Lightning ends at torso height",lightningVisual.Effect.Call("aim_point").AsVector3().IsEqualApprox(a.AimPosition));
            a.GlobalPosition+=new Vector3(.03f,.02f,0);
            Check("Force Lightning torso endpoint follows moving targets",lightningVisual.Effect.Call("aim_point").AsVector3().IsEqualApprox(a.AimPosition));
            lightningVisual.Release();a.GlobalPosition=originalPosition;a.AimHeight=originalAimHeight;
            var aff=w.Abilities.Find("affliction")!;var storm=w.Abilities.Find("storm")!;
            Check("Affliction starts unlocked and slotless",aff.Unlocked && aff.Definition.Slotless && w.Abilities.Equipped(AbilityCategory.Offensive)==1);
            Check("untalented Polarity is locked and absent from starting bar",w.Abilities.Find("polarityshift")?.Unlocked==false && w.Hud!.UnlockedAbilityCount==2);
            Check("Dark Escape uses movement artwork",p.Upgrades.First(u=>u.Id=="haste").Icon=="forcespeed");
            Check("DoT cooldowns increased",aff.CooldownSeconds==12 && w.Abilities.Find("creepingterror")!.CooldownSeconds==14 && w.Abilities.Find("crushingdarkness")!.CooldownSeconds==18);
            Check("new original clips loaded",w.Player.Presentation.Animator.HasAnimation("T22/Strike") && w.Player.Presentation.Animator.HasAnimation("T22/Darkness") && w.Player.Presentation.Animator.HasAnimation("T22/Polarity"));
            void Cast(AutomaticAbility ability){ability.ResetCombat();w.Player.Presentation.ResetPresentation();Check("cast "+ability.Definition.Id,ability.TryBegin());ability.Advance(ability.Definition.WindupSeconds+.001);}
            Cast(aff);Check("Affliction chooses nearest clean target",w.Periodic.Has(aff,a));
            Check("Affliction cast starts original audio and visible impact",((SorcererAbilityPresentation)aff.Presentation).AudioVoicesStarted>0 && ((SorcererAbilityPresentation)aff.Presentation).EffectCount>0);
            Check("Affliction owns visible target status and original audio loop",a.GetChildren().OfType<PresentationStatusVisual>().Any(v=>v.GetNodeOrNull<AudioStreamPlayer3D>("OriginalAfflictionLoop")?.Playing==true));
            await Capture("affliction");aff.Abort();Cast(aff);Check("Affliction moves to next uninfected target",w.Periodic.Has(aff,b));aff.Abort();Cast(aff);aff.Abort();w.Player.Presentation.ResetPresentation();aff.ResetCombat();Check("all afflicted defers DoT for filler",!aff.TryBegin());
            w.Periodic.Clear();w.Periodic.Apply(aff,a);w.Periodic.Advance(4);double remaining=w.Periodic.Remaining(aff,a);
            w.Build.PassOn(a);Check("death transfer retains remaining duration",Math.Abs(w.Periodic.Remaining(aff,b)-remaining)<.001 && !w.Periodic.Has(aff,c));
            w.Periodic.Clear();Check("Crushing Darkness unlock",p.Grant("unlock_crushingdarkness"));var crush=w.Abilities.Find("crushingdarkness")!;Cast(crush);crush.Abort();
            Check("Crushing Darkness applies owned DoT",w.Periodic.Has(crush,a));
            await Capture("darkness");
            Check("Death Field hybrid unlock",p.Grant("unlock_deathfield"));var field=w.Abilities.Find("deathfield")!;Cast(field);field.Advance(1);field.Abort();Check("Death Field spreads Crushing Darkness to multiple enemies",w.Periodic.Has(crush,b) && w.Periodic.Has(crush,c));
            w.Periodic.Clear();w.Periodic.Apply(crush,a);float hp=a.Health.Current;w.Periodic.Advance(9);float normal=hp-a.Health.Current;
            Check("tick mastery learnable",p.Grant("tick"));w.Periodic.Apply(crush,a);hp=a.Health.Current;w.Periodic.Advance(8.9);Check("tick mastery increases damage without shorter duration",hp-a.Health.Current>normal && w.Periodic.Remaining(crush,a)>.09);w.Periodic.Advance(.1);Check("DoT duration unchanged",!w.Periodic.Has(crush,a));
            p.ResetRun();Check("reset restores slotless Affliction",aff.Unlocked && p.Ranks.Count==0 && w.Abilities.Equipped(AbilityCategory.Offensive)==1);
            Check("Storm and Lightning Strike learnable",p.Grant("unlock_storm") && p.Grant("unlock_lightningstrike"));
            Cast(storm);storm.Advance(.85);Check("Storm no longer blocks casting after gesture",storm.Casting && !storm.BlocksCasting && w.Player.Presentation.GestureOwner!=storm);
            hp=a.Health.Current;var strike=w.Abilities.Find("lightningstrike")!;Cast(strike);Check("another attack hits while Storm persists",a.Health.Current<hp && storm.Casting);
            await Capture("storm-strike");
            strike.Abort();w.Player.Presentation.ResetPresentation();hp=a.Health.Current;storm.Advance(1);Check("Storm keeps dealing ground damage",a.Health.Current<hp);
            storm.Abort();Check("Storm abort clears original area",((OriginalAbilityPresentation)storm.Presentation).AreaIndicator==null);
            strike.ResetCombat();Cast(strike);Check("Strike starts single target",strike.LastDistinctHits==1);strike.Abort();Check("Strike cleave training",p.Grant("strike_cleave"));Cast(strike);Check("Strike evolves to cleave",strike.LastDistinctHits==3);strike.Abort();
            Check("Shock unlock fills fourth offensive slot",p.Grant("unlock_shock"));var shock=w.Abilities.Find("shock")!;Cast(shock);Check("Shock starts weaker and single target",shock.DamagePerTick==22 && shock.LastDistinctHits==1);shock.Abort();Check("Shock cleave training",p.Grant("shock_cleave"));Cast(shock);Check("Shock cleave adds targets",shock.LastDistinctHits==3);shock.Abort();
            Check("offensive limit blocks direct grant",!p.Grant("unlock_deathfield") && w.Abilities.Equipped(AbilityCategory.Offensive)==w.Abilities.OffensiveLimit);
            Check("full slots still allow replacement offer",p.IsEligible(p.Upgrades.First(u=>u.Id=="unlock_deathfield")));
            Check("Force Speed utility learnable",p.Grant("unlock_forcespeed"));var speed=w.Abilities.Find("forcespeed")!;var pos=w.Player.GlobalPosition;float move=w.Player.MoveSpeed;Cast(speed);await Capture("forcespeed");Check("Force Speed raises speed without moving player",w.Player.MoveSpeed>move*1.9f && w.Player.GlobalPosition==pos);speed.Advance(1);w.Build._PhysicsProcess(2.1);Check("Force Speed expires",Math.Abs(w.Player.MoveSpeed-move)<.001);speed.Abort();
            var polarity=w.Abilities.Find("polarityshift")!;w.Player.Presentation.ResetPresentation();polarity.ResetCombat();Check("Polarity requires talent",!polarity.TryBegin());Check("Polarity training available",p.Grant("polarity_training"));Check("learned Polarity unlocks and enters the bar",polarity.Unlocked && w.Hud!.UnlockedAbilityCount==w.Abilities.Abilities.Count(x=>x.Unlocked));
            float interval=w.Combat.TickSeconds;Cast(polarity);await Capture("polarity");Check("Polarity accelerates combat without slot",w.Combat.TickSeconds<interval && polarity.Definition.Slotless && w.Abilities.Equipped(AbilityCategory.Utility)==1);polarity.Advance(1);w.Build.Pregame.Advance(6.1);Check("Polarity expires cleanly",Math.Abs(w.Combat.TickSeconds-interval)<.001);polarity.Abort();
            p.ResetRun();a.CombatEnabled=b.CombatEnabled=c.CombatEnabled=false;
            Check("Overload utility learnable",p.Grant("unlock_overload"));var overload=w.Abilities.Find("overload")!;
            var mobs=new List<EnemyAgent>();
            for(int i=0;i<3;i++)
            {
                w.Spawner.Running=true;Check("spawn knockback fixture",w.Spawner.TrySpawn(ignoreLimit:true));w.Spawner.Running=false;
                var mob=w.Spawner.Enemies.Last();mob.SetPhysicsProcess(false);mob.Health.SetMaximum(1000,1000);
                Check("supported enemy placement",mob.Navigation.TryStandingPoint(w.PlayerActor.GlobalPosition+new Vector3(.13f+i*.035f,0,.045f),out var feet));
                mob.Body.GlobalPosition=feet;mobs.Add(mob);
            }
            mobs[2].KnockbackResistance=1;var positions=mobs.Select(e=>e.Body.GlobalPosition).ToArray();Cast(overload);
            Check("Overload physically displaces nearby enemies",mobs.Take(2).Select((e,i)=>e.Body.GlobalPosition.DistanceTo(positions[i])>.02f).All(ok=>ok));
            Check("boss knockback resistance retained",mobs[2].Body.GlobalPosition==positions[2]);
            var wallStart=new Vector3(33.600002f,-16.996994f,13.199996f);var wallDirection=new Vector3(.92387956f,0,-.38268343f);
            Check("supported pillar approach",mobs[0].Navigation.TryStandingPoint(wallStart,out var wallFeet));mobs[0].Body.GlobalPosition=wallFeet;
            float pushed=mobs[0].DisplaceFrom(wallFeet-wallDirection*.1f,.3f);Check("knockback respects original wall collision",pushed<.1624f && mobs[0].Navigation.CanWalk(wallFeet,mobs[0].Body.GlobalPosition));
            mobs[0].Body.GlobalPosition=positions[0];mobs[1].Body.GlobalPosition=positions[1];w.Periodic.Apply(aff,mobs[0]);w.Periodic.Advance(4);remaining=w.Periodic.Remaining(aff,mobs[0]);mobs[0].Health.ApplyDamage(10000);
            Check("actual enemy death transfers remaining owned DoT",Math.Abs(w.Periodic.Remaining(aff,mobs[1])-remaining)<.001);
            foreach(var mob in mobs)mob.CombatEnabled=false;
            a.CombatEnabled=b.CombatEnabled=c.CombatEnabled=true;
            p.ResetRun();Check("pity chance grows and stays bounded",p.AbilityOfferChance(2)<p.AbilityOfferChance(6) && p.AbilityOfferChance(30)==.65f);
            int unlockLevels=0;bool offCadence=false;var banned=new HashSet<string>();
            for(int level=2;level<=30;level++)
            {
                p.AddExperience(p.RequiredExperience);p._Process(10);
                Check("three mixed offers at level "+level,p.Level==level && p.Offers.Count==3 && p.Offers.Count(u=>u.Effect==UpgradeEffect.UnlockAbility)<=1 && p.Offers.All(u=>!banned.Contains(u.Id)));
                if(p.Offers.Any(u=>u.Effect==UpgradeEffect.UnlockAbility)){unlockLevels++;offCadence|=level%5!=0;}
                if(level==2){var old=p.Offers[1];Check("Banish refills",p.Banish(1) && p.BanishesRemaining==2);banned.Add(old.Id);Check("Reroll preserves three mixed choices",p.Reroll() && p.RerollsRemaining==2 && p.Offers.Count==3 && p.Offers.Count(u=>u.Effect==UpgradeEffect.UnlockAbility)<=1);Check("Skip preserved",p.Skip() && p.SkipsRemaining==1);continue;}
                int index=p.Offers.ToList().FindIndex(u=>u.Effect!=UpgradeEffect.UnlockAbility);Check("ordinary training remains selectable",p.Choose(index));p._Process(10);
            }
            Check("weighted ability offers occur off cadence without flooding",offCadence && unlockLevels>2 && unlockLevels<29);
            p.ResetRun();Check("replacement fixture fills offensive category",p.Grant("unlock_shock") && p.Grant("unlock_storm") && p.Grant("unlock_crushingdarkness"));
            var fullCatalog=p.Upgrades;p.Upgrades=new Godot.Collections.Array<UpgradeDefinition>(fullCatalog.Where(u=>u.Effect!=UpgradeEffect.UnlockAbility || w.Abilities.Find(u.AbilityId)?.Definition.Category==AbilityCategory.Offensive));
            bool replaced=false;
            for(int level=2;level<=12 && !replaced;level++)
            {
                p.AddExperience(p.RequiredExperience);p._Process(10);int index=p.Offers.ToList().FindIndex(u=>u.Effect==UpgradeEffect.UnlockAbility);
                if(index<0){Check("replacement fixture can continue progression",p.Choose(0));p._Process(10);continue;}
                var incoming=w.Abilities.Find(p.Offers[index].AbilityId)!;
                Check("full-category offered unlock opens replacement",p.Choose(index) && p.Replacing);
                Check("replacement includes Affliction but excludes other slotless effects",p.ReplacementOptions.Contains(aff) && p.ReplacementOptions.All(x=>!x.Definition.Slotless || x==aff));
                var old=p.ReplacementOptions[0];Check("replacement applies within limit",p.Replace(0) && incoming.Unlocked && !old.Unlocked && aff.Unlocked && w.Abilities.Equipped(AbilityCategory.Offensive)==4);
                p._Process(10);replaced=true;
            }
            Check("replacement choice completes",replaced);
            AutomaticAbility free=aff;
            for(int swap=0;swap<2;swap++)
            {
                bool traded=false;
                for(int attempt=0;attempt<12 && !traded;attempt++)
                {
                    p.AddExperience(p.RequiredExperience);p._Process(10);
                    int index=p.Offers.ToList().FindIndex(u=>u.Effect==UpgradeEffect.UnlockAbility);
                    if(index<0){Check("free replacement can continue progression",p.Choose(0));p._Process(10);continue;}
                    var incoming=w.Abilities.Find(p.Offers[index].AbilityId)!;
                    Check("free replacement opens capped choices",p.Choose(index) && p.Replacing);
                    int oldIndex=p.ReplacementOptions.ToList().IndexOf(free);
                    Check("free replacement option is available",oldIndex>=0);
                    if(swap==0)w.Periodic.Apply(aff,a);
                    Check("free replacement preserves slot count",p.Replace(oldIndex) && incoming.Unlocked && incoming.InheritsFreeSlot && !free.Unlocked && !free.InheritsFreeSlot && w.Abilities.Equipped(AbilityCategory.Offensive)==4);
                    Check("only one inherited free position exists",w.Abilities.Abilities.Count(x=>x.InheritsFreeSlot)==1);
                    if(swap==0)Check("traded Affliction clears owned DoTs and cannot create duplicate free slots",!w.Periodic.Has(aff,a) && !p.IsEligible(p.Upgrades.First(u=>u.Id=="unlock_affliction")));
                    free=incoming;p._Process(10);traded=true;
                }
                Check("free replacement choice completes",traded);
            }
            p.Upgrades=fullCatalog;
            p.ResetRun();Check("restart restores Affliction and clears inherited free positions",aff.Unlocked && aff.Definition.Slotless && w.Abilities.Abilities.All(x=>!x.InheritsFreeSlot) && w.Abilities.Equipped(AbilityCategory.Offensive)==1);Check("restart replenishes controls",p.BanishesRemaining==3 && p.RerollsRemaining==3 && p.SkipsRemaining==2);
            }
        }
        catch(Exception e){failure=e.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath(OS.GetCmdlineUserArgs().Contains("--t22-polarity-start")?"res://../manifests/t22-polarity-start.json":"res://../manifests/t22-sorcerer-smoke.json"),JsonSerializer.Serialize(new {passed=failure==null,failure,checks=_checks},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
