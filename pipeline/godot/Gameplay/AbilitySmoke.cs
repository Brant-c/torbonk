using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>One brief opt-in B5 runtime; no historical suites or archive checks.</summary>
public partial class AbilitySmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool passed)
    {if(!passed)throw new InvalidOperationException(name);_checks.Add(name);GD.Print("B5 smoke: "+name);}
    private async Task Frames(int count)
    {for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
    public async void Run(GameWorld world)
    {
        ProcessMode=ProcessModeEnum.Always;string? failure=null;
        try
        {
            var controller=world.Abilities;var progression=world.Progression!;
            world.Waves!.Stop();world.Spawner!.Running=false;
            world.ResetCombatWorld();world.Waves.Stop();world.Spawner.Running=false;
            Check("Lightning starts unlocked in one twelve-slot bar; original clips remain available",
                controller.Abilities.Count==3 && controller.Abilities.Count(a=>a.Unlocked)==1 &&
                world.Combat.Unlocked && world.Hud!.ActionSlotCount==12 && world.Hud.UnlockedAbilityCount==1 &&
                new[]{"B5/Shock","B5/StormStart","B5/StormLoop","B5/StormEnd"}.All(clip=>world.Player.Presentation.Animator.HasAnimation(clip)));
            var enemies=new List<StationaryEnemy>();
            for(int i=0;i<3;i++)
            {
                var enemy=GD.Load<PackedScene>("res://Gameplay/StationaryEnemy.tscn").Instantiate<StationaryEnemy>();
                enemy.TargetId="B5Fixture"+i;enemy.GetNode<Health>("Health").Maximum=10000;
                world.AddChild(enemy);enemy.GlobalPosition=world.PlayerActor.GlobalPosition+new Vector3(0.16f+i*0.035f,0,0);
                enemies.Add(enemy);
            }
            await Frames(2);
            Check("nearby cluster is targetable",enemies.All(e=>world.Targeting.IsValidTarget(e)));
            var storm=controller.Abilities.Single(a=>a.Definition.Id=="storm");
            var shock=controller.Abilities.Single(a=>a.Definition.Id=="shock");
            Check("locked abilities cannot cast or offer their specific upgrades",
                !shock.TryBegin() && !storm.TryBegin() && shock.Casts==0 && storm.Casts==0 &&
                !progression.IsEligible(progression.Upgrades.Single(u=>u.Id=="shock_damage")) &&
                !progression.IsEligible(progression.Upgrades.Single(u=>u.Id=="storm_radius")));
            var catalog=progression.Upgrades;
            foreach(string id in new[]{"shock","storm"})
            {
                var unlock=catalog.Single(u=>u.Id=="unlock_"+id);
                progression.Upgrades=new Godot.Collections.Array<UpgradeDefinition> {
                    unlock,catalog.Single(u=>u.Id=="damage"),catalog.Single(u=>u.Id=="health") };
                progression.AddExperience(progression.RequiredExperience);
                Check(id+" unlock appears as a paused level-up choice",progression.Selecting && GetTree().Paused &&
                    progression.Offers.Contains(unlock) && world.Hud!.VisibleChoiceCount==3);
                Check(id+" selection unlocks the ability once",progression.Choose(progression.Offers.ToList().IndexOf(unlock)) &&
                    controller.Find(id)!.Unlocked && !progression.IsEligible(unlock));
                await Frames(2);
                Check(id+" appears on the existing bar",world.Hud!.UnlockedAbilityCount==controller.Abilities.Count(a=>a.Unlocked) &&
                    world.Hud.ActionSlotCount==12);
            }
            progression.Upgrades=catalog;
            Check("ability-specific upgrades become eligible after unlock",
                progression.IsEligible(catalog.Single(u=>u.Id=="shock_damage")) &&
                progression.IsEligible(catalog.Single(u=>u.Id=="storm_radius")));
            ulong deadline=Time.GetTicksMsec()+22000;
            while(controller.Abilities.Any(a=>a.DamageEvents==0) && Time.GetTicksMsec()<deadline)
            {
                CheckLease(controller);
                await Frames(1);
            }
            Check("Lightning, Shock and Storm deal damage through Health",
                controller.Abilities.All(a=>a.Casts>0 && a.DamageEvents>0) && enemies.All(e=>e.Health.LastDamageType==DamageType.Energy));
            Check("Storm hits several hostiles at its fixed center",storm.DamageEvents>=3);
            Check("Shock has an independent cooldown while Storm is active",
                shock.CooldownRemaining>0 && storm.Casting);
            controller.SetPhysicsProcess(false);controller.Abort();
            // Exercise one actual rarity-scaled offer and the existing pause/reset flow.
            var radius=progression.Upgrades.Single(u=>u.Id=="storm_radius");
            float before=storm.Radius;
            progression.Upgrades=new Godot.Collections.Array<UpgradeDefinition> {
                radius,progression.Upgrades.Single(u=>u.Id=="damage"),progression.Upgrades.Single(u=>u.Id=="health") };
            progression.AddExperience(progression.RequiredExperience);
            Check("level pauses combat and shows three offers",GetTree().Paused && world.Hud!.VisibleChoiceCount==3);
            int index=progression.Offers.ToList().IndexOf(radius);progression.Choose(index);
            Check("Artifact radius modifier updates gameplay and upgrade state",
                Mathf.IsEqualApprox(storm.Radius,before*(1+radius.EffectiveAmount)) &&
                progression.AbilityRarity("storm")==PowerRarity.Artifact && progression.AbilityRanks("storm")==1);
            world.PlayerActor.Health.ApplyDamage(10000);
            Check("death stops every ability",controller.Abilities.All(a=>a.State==LightningState.Disabled));
            world.ResetCombatWorld();await Frames(2);
            Check("restart relocks new abilities and clears ranks, cooldowns and radius",!GetTree().Paused && progression.Ranks.Count==0 &&
                controller.Abilities.Count(a=>a.Unlocked)==1 && world.Combat.Unlocked && world.Hud!.UnlockedAbilityCount==1 &&
                Mathf.IsEqualApprox(storm.Radius,before) && controller.GlobalCooldownRemaining==0 &&
                controller.Abilities.All(a=>a.CooldownRemaining==0));
            foreach(var enemy in enemies)enemy.QueueFree();
            Check("rendered runtime is alive",Engine.GetFramesDrawn()>0);
        }
        catch(Exception ex) {failure=ex.ToString();GD.PushError("B5 smoke: "+failure);}
        GetTree().Paused=false;
        using(var file=FileAccess.Open("res://../manifests/abilities-smoke.json",FileAccess.ModeFlags.Write))
            file.StoreString(JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,failure,renderedFrames=Engine.GetFramesDrawn(),
                scope="One brief B5 rendered smoke; no historical regressions, archive rehash or benchmarks."},new JsonSerializerOptions{WriteIndented=true}));
        GD.Print(failure==null?"ABILITIES_SMOKE_PASSED":"ABILITIES_SMOKE_FAILED");
        GetTree().Quit(failure==null?0:1);
    }
    private static void CheckLease(AbilityController controller)
    {
        if(controller.Abilities.Count(a=>a.Casting)>1)throw new InvalidOperationException("Overlapping full-body cast leases");
    }
}
