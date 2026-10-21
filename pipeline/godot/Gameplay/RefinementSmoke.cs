using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>Bounded B5.1 smoke: animation masks, overlapping effects, GCD and reset. No archive work.</summary>
public partial class RefinementSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool passed)
    {
        if(!passed) throw new InvalidOperationException(name);
        _checks.Add(name);GD.Print("B5.1 smoke: "+name);
    }
    private async Task Frames(int count)
    { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }

    private CharacterPresentation AnimationFixture(GameWorld world)
    {
        var actor=new CharacterPresentation();
        actor.AddChild(GD.Load<PackedScene>("res://Assets/sorcerer.glb").Instantiate());
        world.AddChild(actor);actor.SetProcess(false);actor.Visible=false;
        return actor;
    }
    private async Task CheckLayers(GameWorld world)
    {
        var cast=AnimationFixture(world);var run=AnimationFixture(world);
        cast.LoadAbilityAnimations();
        // AnimationTree synchronizes its AnimationPlayer libraries through deferred signals.
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Check("cast libraries synchronized with AnimationTree",cast.Tree.HasAnimation("B5/Shock"));
        var owner=new object();
        foreach(string clip in new[]{"LightningLoop","B5/Shock","B5/StormLoop"})
        {
            cast.ReleaseGesture(owner);cast._Process(0.2);run._Process(0.2);
            Check(clip+" gesture admitted",cast.TryBeginGesture(owner,clip));
            Quaternion before=default;
            for(int frame=0;frame<5;frame++)
            {
                cast.Present(Vector3.Right*0.6f,0.1);run.Present(Vector3.Right*0.6f,0.1);
                cast._Process(0.1);run._Process(0.1);
                if(frame<2)continue; // Let the locomotion/gesture boundary crossfade finish.
                foreach(string bone in new[]{"Root","Pelvis","LeftHip","LeftKnee","RightKnee","LeftAnkle"})
                {
                    int a=cast.Skeleton.FindBone(bone),b=run.Skeleton.FindBone(bone);
                    if(!cast.Skeleton.GetBonePoseRotation(a).IsEqualApprox(run.Skeleton.GetBonePoseRotation(b)) ||
                        !cast.Skeleton.GetBonePosePosition(a).IsEqualApprox(run.Skeleton.GetBonePosePosition(b)))
                        throw new InvalidOperationException(clip+" changed running leg bone "+bone);
                }
                int knee=cast.Skeleton.FindBone("LeftKnee");
                var rotation=cast.Skeleton.GetBonePoseRotation(knee);
                if(frame==2)before=rotation;
                if(frame==4)Check(clip+" keeps original animated running legs",!before.IsEqualApprox(rotation));
            }
            int wrist=cast.Skeleton.FindBone("LeftWrist");
            Check(clip+" retains a distinct upper-body pose and hand sockets",
                !cast.Skeleton.GetBoneGlobalPose(wrist).IsEqualApprox(run.Skeleton.GetBoneGlobalPose(wrist)) &&
                cast.Skeleton.FindBone("vfx_hand_left")>=0 && cast.Skeleton.FindBone("RightIndexFinger")>=0);
        }
        cast.PlayReaction("Death");cast._Process(0.2);
        Check("death overrides and releases the gesture",cast.OverrideActive && cast.GestureOwner==null && cast.State=="Death");
        cast.ResetPresentation();Check("presentation resets after death",!cast.OverrideActive && cast.State=="Idle");
        cast.QueueFree();run.QueueFree();
    }

    public async void Run(GameWorld world)
    {
        ProcessMode=ProcessModeEnum.Always;string? failure=null;
        try
        {
            var controller=world.Abilities;var progression=world.Progression!;
            world.Waves!.Stop();world.Spawner!.Running=false;
            controller.SetPhysicsProcess(false);controller.ResetCombat();
            await CheckLayers(world);
            var clock=new ChannelClock();int ticks=0;
            clock.Reset(0.9f,0.3f);clock.Advance(1,()=>{ticks++;return true;});
            Check("short float-authored bursts keep their final tick",ticks==3);
            Check("Lightning-only start and one twelve-slot bar",
                controller.Abilities.Count(a=>a.Unlocked)==1 && world.Combat.Unlocked && world.Hud!.ActionSlotCount==12);
            var enemies=new List<StationaryEnemy>();
            for(int i=0;i<3;i++)
            {
                var enemy=GD.Load<PackedScene>("res://Gameplay/StationaryEnemy.tscn").Instantiate<StationaryEnemy>();
                enemy.TargetId="B51Fixture"+i;enemy.GetNode<Health>("Health").Maximum=10000;
                world.AddChild(enemy);enemy.GlobalPosition=world.PlayerActor.GlobalPosition+new Vector3(0.16f+i*0.035f,0,0);
                enemies.Add(enemy);
            }
            foreach(var ability in controller.Abilities)ability.SetUnlocked(true);
            await Frames(2);
            Check("fixture targets valid",enemies.All(e=>world.Targeting.IsValidTarget(e)));
            controller.SetPhysicsProcess(true);
            var storm=controller.Find("storm")!;var shock=controller.Find("shock")!;
            int[] casts=controller.Abilities.Select(a=>a.Casts).ToArray();
            double previousGcd=0;bool overlapping=false,persistentWithoutGesture=false,independentCooldown=false;
            double shockCooldown=0;bool shockCdSurvivedStorm=false;int previousStormEvents=0;
            ulong deadline=Time.GetTicksMsec()+11500;
            while(Time.GetTicksMsec()<deadline)
            {
                await Frames(1);
                var now=controller.Abilities.Select(a=>a.Casts).ToArray();
                int activations=now.Zip(casts,(a,b)=>a-b).Sum();
                if(activations>1 || (activations>0 && previousGcd>0.04))
                    throw new InvalidOperationException("GCD allowed overlapping new activations");
                if(storm.Casts>casts[2] && shockCooldown>0)
                    shockCdSurvivedStorm=shock.CooldownRemaining>0 && shock.CooldownRemaining<=shockCooldown;
                casts=now;previousGcd=controller.GlobalCooldownRemaining;shockCooldown=shock.CooldownRemaining;
                overlapping|=storm.State==LightningState.Channeling && controller.Abilities.Count(a=>a.Casting)>1;
                persistentWithoutGesture|=storm.State==LightningState.Channeling && storm.DamageEvents>previousStormEvents &&
                    world.Player.Presentation.GestureOwner!=storm && controller.GlobalCooldownRemaining>0;
                previousStormEvents=storm.DamageEvents;
                independentCooldown|=shock.CooldownRemaining>0 && world.Combat.CooldownRemaining>0 &&
                    Math.Abs(shock.CooldownRemaining-world.Combat.CooldownRemaining)>0.1;
                if(overlapping && persistentWithoutGesture && shockCdSurvivedStorm && controller.Abilities.All(a=>a.DamageEvents>0))break;
            }
            Check("all three independent damage paths",controller.Abilities.All(a=>a.DamageEvents>0));
            Check("GCD regulates activations while active effects overlap",overlapping);
            Check("Storm damages during GCD after releasing its gesture",persistentWithoutGesture);
            Check("individual cooldowns do not reset each other",independentCooldown && shockCdSurvivedStorm);
            Check("persistent area hits several targets",storm.DamageEvents>=3);
            controller.SetPhysicsProcess(false);controller.ResetCombat();
            // Isolate one persistent cast, then walk the owner outside acquisition range.
            Check("Storm can activate independently",storm.TryBegin());
            storm.Advance(storm.Definition.WindupSeconds+0.001);
            int events=storm.DamageEvents;var position=world.Player.GlobalPosition;
            world.Player.SetPhysicsProcess(false);world.Player.GlobalPosition+=Vector3.Right*2;
            storm.Advance(1.0);
            Check("placed Storm survives caster leaving range",storm.State==LightningState.Channeling && storm.DamageEvents>events);
            world.Player.GlobalPosition=position;controller.Abort();
            progression.AddExperience(progression.RequiredExperience);
            Check("upgrade selection still pauses",progression.Selecting && GetTree().Paused && world.Hud!.VisibleChoiceCount==3);
            world.ResetCombatWorld();world.Waves.Stop();world.Spawner.Running=false;
            world.PlayerActor.Health.ApplyDamage(10000);await Frames(2);
            Check("death stops every effect and gesture",controller.Abilities.All(a=>a.State==LightningState.Disabled) &&
                world.Player.Presentation.State=="Death" && world.Player.Presentation.GestureOwner==null);
            world.ResetCombatWorld();world.Waves.Stop();world.Spawner.Running=false;await Frames(2);
            Check("restart clears timers, ranks and unlocks",!GetTree().Paused && progression.Ranks.Count==0 &&
                controller.Abilities.Count(a=>a.Unlocked)==1 && controller.GlobalCooldownRemaining==0 &&
                controller.Abilities.All(a=>a.CooldownRemaining==0) && world.Hud!.UnlockedAbilityCount==1);
            foreach(var enemy in enemies)enemy.QueueFree();
            Check("rendered runtime alive",Engine.GetFramesDrawn()>0);
        }
        catch(Exception ex){failure=ex.ToString();GD.PushError("B5.1 smoke: "+failure);}
        GetTree().Paused=false;
        using(var file=FileAccess.Open("res://../manifests/refinement-smoke.json",FileAccess.ModeFlags.Write))
            file.StoreString(JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,failure,renderedFrames=Engine.GetFramesDrawn()},
                new JsonSerializerOptions{WriteIndented=true}));
        GD.Print(failure==null?"REFINEMENT_SMOKE_PASSED":"REFINEMENT_SMOKE_FAILED");GetTree().Quit(failure==null?0:1);
    }
}
