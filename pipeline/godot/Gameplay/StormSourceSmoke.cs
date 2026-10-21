using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>One brief source-Storm/moving-legs check; no archive work or animation conversion.</summary>
public partial class StormSourceSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string label,bool pass)
    {if(!pass)throw new InvalidOperationException(label);_checks.Add(label);GD.Print("Storm source: "+label);}
    private async Task Frames(int n) {for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private CharacterPresentation Fixture(GameWorld world)
    {
        var actor=new CharacterPresentation();actor.AddChild(GD.Load<PackedScene>("res://Assets/sorcerer.glb").Instantiate());
        world.AddChild(actor);actor.SetProcess(false);actor.Visible=false;return actor;
    }
    public async void Run(GameWorld world)
    {
        ProcessMode=ProcessModeEnum.Always;string? failure=null;
        try
        {
            world.ResetCombatWorld();world.Waves!.Stop();world.Spawner!.Running=false;
            world.Abilities.SetPhysicsProcess(false);world.Abilities.Abort();world.Player.SetPhysicsProcess(false);
            var cast=Fixture(world);var run=Fixture(world);cast.LoadAbilityAnimations();await Frames(2);
            Check("original Storm channel is loaded",cast.Animator.HasAnimation("B5/StormLoop"));
            Check("original upper-body Storm gesture begins",cast.TryBeginGesture(new object(),"B5/StormLoop"));
            Quaternion knee=default;
            for(int f=0;f<8;f++)
            {
                cast.Present(Vector3.Right*0.6f,0.1);run.Present(Vector3.Right*0.6f,0.1);cast._Process(0.1);run._Process(0.1);
                if(f<2)continue;
                foreach(string bone in new[]{"Root","Pelvis","LeftHip","LeftKnee","RightKnee","LeftAnkle","RightAnkle"})
                {
                    int a=cast.Skeleton.FindBone(bone),b=run.Skeleton.FindBone(bone);
                    if(!cast.Skeleton.GetBonePoseRotation(a).IsEqualApprox(run.Skeleton.GetBonePoseRotation(b)) ||
                        !cast.Skeleton.GetBonePosePosition(a).IsEqualApprox(run.Skeleton.GetBonePosePosition(b)))
                        throw new InvalidOperationException("Storm changed original running leg: "+bone);
                }
                var now=cast.Skeleton.GetBonePoseRotation(cast.Skeleton.FindBone("LeftKnee"));
                if(f==2)knee=now;if(f==7)Check("running legs match the original run animation and keep moving",!knee.IsEqualApprox(now));
            }
            Check("Storm retains its original upper-body pose and hand sockets",
                !cast.Skeleton.GetBoneGlobalPose(cast.Skeleton.FindBone("LeftWrist")).IsEqualApprox(run.Skeleton.GetBoneGlobalPose(run.Skeleton.FindBone("LeftWrist"))) &&
                cast.Skeleton.FindBone("vfx_hand_left")>=0 && cast.Skeleton.FindBone("RightIndexFinger")>=0);
            cast.QueueFree();run.QueueFree();
            var storm=world.Abilities.Find("storm")!;var shock=world.Abilities.Find("shock")!;
            storm.SetUnlocked(true);shock.SetUnlocked(true);
            Check("the real Storm target has terrain",world.Arena.GroundAt(world.Player.GlobalPosition+Vector3.Right*0.36f,out var point,out _,0.3f,0.5f));
            var target=GD.Load<PackedScene>("res://Gameplay/StationaryEnemy.tscn").Instantiate<StationaryEnemy>();target.TargetId="StormSourceFixture";
            target.GetNode<Health>("Health").Maximum=1000;world.AddChild(target);target.GlobalPosition=point;await Frames(2);
            Check("Storm starts with original phase clips and source-based lightning",storm.TryBegin() && storm.Definition.StartClip=="B5/StormStart" && storm.Definition.LoopClip=="B5/StormLoop");
            var presentation=(OriginalAbilityPresentation)storm.Presentation;var fx=presentation.StormLightning!;
            Check("all thirteen visible caster particle emitters and the source light initialize",fx.CasterParticleEmitterCount==13 && fx.SourceLightCount==1);
            world.Player.SetPhysicsProcess(true);Input.ActionPress("b1_right");var start=world.Player.GlobalPosition;
            int maxGround=0,maxCaster=0;
            for(int f=0;f<105;f++)
            {
                await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);storm.Advance(1.0/Engine.PhysicsTicksPerSecond);
                maxGround=Math.Max(maxGround,fx.GroundBeamCount);maxCaster=Math.Max(maxCaster,fx.CasterBeamCount);
                if(f==54)
                {
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    string review=ProjectSettings.GlobalizePath("res://../generated/storm-source");DirAccess.MakeDirRecursiveAbsolute(review);
                    GetViewport().GetTexture().GetImage().SavePng(review+"/moving-storm.png");
                }
            }
            Input.ActionRelease("b1_right");
            Check("movement continues while the sustained original Storm gesture remains active",world.Player.GlobalPosition.DistanceTo(start)>0.03f &&
                storm.State==LightningState.Channeling && world.Player.Presentation.GestureOwner==storm);
            Check("original layered strikes and caster arcs render together",fx.StrikesStarted>0 && maxGround>=5 && maxCaster>=4 && presentation.AreaIndicator!.Visible);
            var particleLayers=new[]{"LeftSmoke","RightSmoke","chest tatters","Right elbow tatters","left elbow tatters",
                "LeftGroundGlow","RightGroundGlows","leftbicep","rightbicep","lefthand","righthand","SparkLine","SparkLine1",
                "StrikeGroundFlash","StrikeGroundGlow"};
            Check("every source smoke, tatter, glow, hand, spark and strike-flash layer emits",particleLayers.All(n=>fx.EmittedParticleLayers.GetValueOrDefault(n)>0) && fx.SourceParticleCount>0);
            Check("Storm keeps its existing damage balance",storm.DamagePerTick==5 && storm.TickSeconds==0.8f && storm.DamageEvents>0);
            // Place this independent attack near the moving caster. The Storm gesture does not block gameplay.
            target.GlobalPosition=world.Player.GlobalPosition+Vector3.Right*0.2f;Check("another ability can activate during Storm",shock.TryBegin());shock.Advance(0.13);
            Check("other ability damage is independent of the casting gesture",shock.DamageEvents==1 && world.Player.Presentation.GestureOwner==storm);
            world.PlayerActor.Health.ApplyDamage(10000);await Frames(2);
            Check("death clears original Storm layers and gesture",fx.LiveVisuals==0 && world.Player.Presentation.GestureOwner==null && presentation.AreaIndicator==null);
            world.ResetCombatWorld();world.Waves.SetPhysicsProcess(false);world.Spawner.Running=false;await Frames(2);
            Check("restart cleans up and relocks Storm",!storm.Unlocked && fx.LiveVisuals==0 && storm.CooldownRemaining==0);target.QueueFree();
            Check("rendered session completed",Engine.GetFramesDrawn()>0);
        }
        catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        Input.ActionRelease("b1_right");GetTree().Paused=false;
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/storm-source-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,failure},new JsonSerializerOptions{WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
