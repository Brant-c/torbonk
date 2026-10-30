using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors;
public partial class PowertechSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string s,bool pass){if(!pass)throw new Exception(s);_checks.Add(s);GD.Print("POWERTECH: "+s);}
    public async void Run(GameWorld w)
    {
        string? failure=null;
        try{
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
            // Advanced Prototype restores Rocket Punch and Flame Burst to the kit (Bounty Hunter completion pass).
            Check("Rocket Punch is in the kit and can drop",w.Abilities.Find("rocketpunch") is {Unlocked:false} && w.Progression!.Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId=="rocketpunch"));
            var targets=new List<Combatant>();
            for(int i=0;i<5;i++){var t=new Combatant {Name="PTFixture"+i,Team=CombatTeam.Hostile};t.AddChild(new Health {Name="Health",Maximum=1000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3(.12f+i*.03f,0,0);targets.Add(t);}
            targets[4].GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Left*.7f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var burst=w.Abilities.Find("flameburst");var sweep=w.Abilities.Find("flamesweep")!;var missile=w.Abilities.Find("incendiarymissile")!;
            Check("Flame Sweep and basic weapon attack start unlocked",sweep.Unlocked && burst is {Unlocked:false} && w.Abilities.Abilities.Count(a=>a.Unlocked)==2 && w.Abilities.Equipped(AbilityCategory.Offensive)==1);
            Check("Mercenary excludes Explosive Fuel",!ClassProfile.Find("mercenary")!.Kit.Contains("reactivearmor") && !ClassProfile.Find("mercenary")!.TalentPool.Contains("unlock_reactivearmor"));
            Check("Spread requires both owned abilities",!w.Progression!.IsEligible(w.Progression.Upgrades.Single(u=>u.Mechanic=="combustible_spread")));
            w.Progression.Grant("unlock_incendiarymissile");
            void Cast(AutomaticAbility a){a.ResetCombat();Check(a.Definition.Id+" activates",a.TryBegin());for(double t=0;t<a.ChannelSeconds+a.Definition.WindupSeconds+a.Definition.RecoverySeconds+.2;t+=.05)a.Advance(.05);}
            targets[1].Health.SetMaximum(1200,200);
            var directBefore=targets.Select(t=>t.Health.Current).ToArray();
            Cast(missile);Check("Incendiary Missile is single-target",w.Periodic.Count==1 && targets.Where((t,i)=>t.Health.Current<directBefore[i]).Count()==1);
            Check("IM selects highest current health rather than nearest",w.Periodic.Has(missile,targets[1]) && !w.Periodic.Has(missile,targets[0]));
            Check("IM only deals 3 upfront damage",Math.Abs(directBefore[1]-targets[1].Health.Current-3)<.001f);
            Check("burn visual attaches to target",targets[1].GetChildren().OfType<PowertechStatusVisual>().Any());
            w.Progression.Grant("powertech_combustible_spread");Cast(sweep);
            Check("spread is bounded to activation hit set",targets.Take(4).All(t=>w.Periodic.Has(missile,t)) && !w.Periodic.Has(missile,targets[4]) && w.Periodic.Count==4);
            var before=targets.Take(4).Select(t=>t.Health.Current).ToArray();w.Periodic.Advance(1);
            Check("each target has independent periodic damage",targets.Take(4).Select((t,i)=>t.Health.Current<before[i]).All(b=>b));
            w.Periodic.Advance(5);Check("all burns expire at configured duration",w.Periodic.Count==0);
            w.Progression.Grant("unlock_ionshield");w.Progression.Grant("powertech_pyro_shield");var shield=w.Abilities.Find("ionshield")!;Cast(shield);
            float hp=w.PlayerActor.Health.Current;w.PlayerActor.Health.ApplyDamage(5);Check("Energy Shield absorbs damage independently of visuals",w.PlayerActor.Health.Current==hp && w.PlayerActor.Health.ShieldRemaining==25);
            // A real NPC exercises animation/AI stop and recovery plus elite/boss policy.
            var body=GD.Load<PackedScene>("res://Gameplay/Enemies/Broodling.tscn").Instantiate<CharacterBody3D>();var enemy=body.GetNode<EnemyAgent>("Actor");enemy.Target=w.PlayerActor;enemy.Navigation=w.Spawner.Navigation;w.AddChild(body);body.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.2f;enemy.SetPhysicsProcess(false);
            w.Progression.Grant("unlock_carbonize");var carbon=w.Abilities.Find("carbonize")!;
            carbon.ResetCombat();Check("Carbonize activates with its spin",carbon.TryBegin() && w.Player.Presentation.GestureOwner==carbon && w.Player.Presentation.FullBodyGesture && carbon.Definition.StartClip=="D3/Flame");
            carbon.Advance(.26);var carbonFx=(D3AbilityPresentation)carbon.Presentation;
            Check("Carbonize plays original audio and emits spray",carbonFx.CarbonizeAudioPlaying && carbonFx.CarbonizeEmitting);
            carbon.Advance(1.8);Check("Carbonize releases spin safely",w.Player.Presentation.GestureOwner!=carbon);enemy._PhysicsProcess(3.1);
            float eh=enemy.Health.Current;w.Build.Attacked(enemy,true);w.Build.Attacked(enemy,true);Check("Pyro Shield retaliates once under its trigger cooldown",enemy.Health.Current==eh-8);
            Check("Carbonize stuns ordinary NPC",enemy.Stun(.2f));Check("Carbonite coats NPC meshes without a billboard",enemy.GetChildren().OfType<PowertechCarbonite>().Single().CoatedMeshes>0 && !enemy.GetChildren().OfType<PowertechStatusVisual>().Any(n=>n.GetChildren().OfType<Sprite3D>().Any(sp=>sp.Texture.ResourcePath.EndsWith("proj_carbonite_01.png"))));enemy._PhysicsProcess(.1);Check("stun freezes movement and attacks",enemy.StunRemaining>0 && enemy.Body.Velocity==Vector3.Zero);enemy._PhysicsProcess(.11);Check("stun restores animation processing",enemy.StunRemaining==0 && enemy.Visual?.ProcessMode!=ProcessModeEnum.Disabled);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            w.Periodic.Apply(missile,enemy);var coat=enemy.GetChildren().OfType<BountyStatusOverlay>().SingleOrDefault();coat?._Process(.3);
            Check("burns coat the NPC's own meshes instead of a billboard",coat!=null && coat.CoatedMeshes>0 && coat.Burn>0);w.Periodic.Clear();
            enemy.RewardTier=2;Check("boss stun resistance",!enemy.Stun(3));enemy.RewardTier=1;enemy.Stun(3);Check("elite stun duration is reduced",enemy.StunRemaining<3);
            // Electro Dart: few targets, long stun, shocks while it holds.
            enemy.RewardTier=0;body.GlobalPosition=targets[0].GlobalPosition+Vector3.Right*.004f;
            enemy.Health.SetMaximum(5000,5000);enemy.Health.Restore();w.Progression.Grant("unlock_electrodart");var electro=w.Abilities.Find("electrodart")!;Cast(electro);
            Check("Electro Dart stuns up to three enemies for 4 seconds",enemy.StunRemaining>3.5 && w.Build.Pregame.PowertechElectrified(enemy) && electro.Definition.MaximumTargets==3);
            float shocked=enemy.Health.Current;w.Build.Pregame.Advance(1.01);
            Check("Electro Dart shocks its stunned target every half second",enemy.Health.Current<=shocked-electro.DamagePerTick*.6f*1.9f);
            body.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.2f;
            enemy.RewardTier=0;w.Progression.Grant("unlock_oilslick");Cast(w.Abilities.Find("oilslick")!);var oil=w.GetChildren().OfType<PowertechOilSlick>().Single();oil._PhysicsProcess(.05);Check("Oil Slick projects onto terrain",oil.ProjectedVertexCount>0 && !oil.GetChildren().OfType<Sprite3D>().Any());Check("Oil Slick applies temporary slow and damage reduction",enemy.OilRemaining>0 && enemy.SlowRemaining>0);
            float soaked=enemy.Health.Current;w.Build.Pregame.DirectHit(w.Abilities.Find("flameburst")!,enemy);oil._PhysicsProcess(.05);
            Check("a fire hit ignites the Oil Slick into burning ground",oil.Burning && enemy.Health.Current<soaked && w.Build.Pregame.PowertechBurning(enemy));enemy.Stun(.3f);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(!DisplayServer.GetName().Equals("headless",StringComparison.OrdinalIgnoreCase))GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/powertech-projection-review.png"));
            body.GlobalPosition+=Vector3.Right;enemy._PhysicsProcess(.3);Check("Oil Slick modifiers expire after leaving",enemy.OilRemaining==0);oil._PhysicsProcess(8);w.PlayerActor.Health.ClearShield();Check("shield cleanup",!w.PlayerActor.Health.Shielded);
            Check("Powertech has three utility slots",w.Abilities.UtilityLimit==3);
            w.Periodic.Clear();missile.ResetCombat();
            var foreign=new AutomaticAbility {Caster=targets[4]};foreign.Configure(missile.Definition);w.AddChild(foreign);w.Periodic.Apply(foreign,targets[0]);Cast(sweep);
            Check("foreign caster burn cannot seed spread",!w.Periodic.Has(missile,targets[1]));w.Periodic.Clear();
            w.Progression.Grant("unlock_immolate");var immolate=w.Abilities.Find("immolate")!;Cast(immolate);
            Check("Immolate leaves its own burn",w.Periodic.Has(immolate,targets[0]));immolate.ResetCombat();Cast(sweep);
            Check("Flame Sweep spreads Immolate's burn",targets.Take(4).All(t=>w.Periodic.Has(immolate,t)));w.Periodic.Clear();
            // Free an offensive slot for the channel fixture.
            sweep.SetUnlocked(false);w.Progression.Grant("unlock_flamethrower");var flame=w.Abilities.Find("flamethrower")!;
            targets[4].GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Left*.15f;
            var ahead=targets[0].Health.Current;var behind=targets[4].Health.Current;
            w.Targeting.Update(0,false);Cast(flame);
            Check("Flame Thrower ticks repeatedly in front",targets[0].Health.Current<=ahead-flame.DamagePerTick*5);
            Check("Flame Thrower excludes rear targets",targets[4].Health.Current==behind);

        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        var report=new {passed=failure==null,checks=_checks,failure};System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/powertech-smoke.json"),System.Text.Json.JsonSerializer.Serialize(report,new System.Text.Json.JsonSerializerOptions {WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
}

