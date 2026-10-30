using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class AssassinPresentationSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool ok){if(!ok)throw new Exception(name);_checks.Add(name);}
    public async void Run(GameWorld w)
    {
        string? failure=null;bool capture=OS.GetCmdlineUserArgs().Contains("--assassin-presentation-captures");
        try {
            w.Waves!.Stop();w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Targeting.EnvironmentMask=0;
            foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
            var actor=w.Player.Presentation;actor.SetProcess(false);
            foreach(var a in w.Abilities.Abilities)((Node)a.Presentation).SetProcess(false);
            var targets=new List<Combatant>();
            for(int i=0;i<3;i++){
                var t=new Combatant{Name="PresentationTarget"+i,Team=CombatTeam.Hostile};t.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(t);
                t.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3((i-1)*.065f,0,-.17f);targets.Add(t);
                t.AddChild(new MeshInstance3D{Mesh=new CapsuleMesh{Radius=.025f,Height=.16f},Position=Vector3.Up*.08f,MaterialOverride=new StandardMaterial3D{AlbedoColor=new Color(.4f,.44f,.48f)}});
            }
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(capture){
                foreach(var layer in w.GetChildren().OfType<CanvasLayer>())layer.Visible=false;
                w.Player.Rig.SetProcess(false);w.Player.Rig.SetPhysicsProcess(false);
                var camera=new Camera3D{Fov=38};w.AddChild(camera);camera.GlobalPosition=w.Player.GlobalPosition+new Vector3(.5f,.35f,.65f);camera.LookAt(w.Player.GlobalPosition+new Vector3(0,.09f,-.05f));camera.Current=true;
            }
            async System.Threading.Tasks.Task Capture(string name){
                if(!capture)return;
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../manifests/assassin-presentation-{name}.png"));
            }
            Check("pole directional locomotion loaded",actor.DirectionalLocomotionEnabled);
            int left=actor.Skeleton.FindBone("LeftAnkle"),right=actor.Skeleton.FindBone("RightAnkle"),root=actor.Skeleton.FindBone("Root");
            var trails=actor.GetChildren().OfType<MarauderBladeTrails>().Single(t=>t.Assassin);trails.SetProcess(false);
            foreach(var a in w.Abilities.Abilities.Where(a=>a.Definition.StartClip!=""))foreach(var velocity in new[]{Vector3.Forward,Vector3.Back,Vector3.Left,Vector3.Right,new Vector3(1,0,-1).Normalized()}){
                actor.ResetPresentation();actor.Rotation=Vector3.Zero;var d=a.Definition;
                Check(d.Id+" moving gesture starts "+velocity,actor.TryBeginGesture(a,d.StartClip,d.GesturePriority,d.FullBodyGesture,d.KeepFullBodyWhileMoving));actor.FitGesture(a,d.GestureSeconds);
                actor.FacingTarget=actor.GlobalPosition+Vector3.Forward;
                Vector3? pl=null,pr=null;float ld=0,rd=0,turn=0;Quaternion? facing=null;Vector2 min=new(float.PositiveInfinity,float.PositiveInfinity),max=new(float.NegativeInfinity,float.NegativeInfinity);
                for(int frame=0;frame<55;frame++){
                    actor.Present(velocity*w.Player.MoveSpeed,.02);actor._Process(.02);
                    if(d.Id=="lacerate" && velocity==Vector3.Right)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    trails._Process(.02);
                    var l=actor.Skeleton.GetBoneGlobalPose(left).Origin;var r=actor.Skeleton.GetBoneGlobalPose(right).Origin;
                    if(frame>12){ld+=l.DistanceTo(pl!.Value);rd+=r.DistanceTo(pr!.Value);var pose=actor.Skeleton.GetBoneGlobalPose(root);var p=new Vector2(pose.Origin.X,pose.Origin.Z);min=min.Min(p);max=max.Max(p);facing??=pose.Basis.GetRotationQuaternion();turn=Math.Max(turn,facing.Value.AngleTo(pose.Basis.GetRotationQuaternion()));}
                    pl=l;pr=r;
                    if(frame==20 && d.Id=="lacerate" && velocity==Vector3.Right){Check("pole sweep produces blade ribbons",trails.RibbonCount>0);await Capture("moving-lacerate");}
                }
                Check(d.Id+" feet keep running "+velocity,ld>.015f && rd>.015f);
                Check(d.Id+" facing and legs match movement "+velocity,actor.LocomotionDirection.DistanceTo(new Vector2(velocity.X,velocity.Z))<.03f && (-actor.GlobalBasis.Z).Dot(Vector3.Forward)>.98f);
                Check(d.Id+" root stays in place "+velocity,(max-min).Length()<.015f && turn<.6f);
                Check(d.Id+" moving legs remain free "+velocity,(float)actor.Tree.Get("parameters/StandingCast/blend_amount")<.001f);
            }
            if(capture)foreach(string id in new[]{"lacerate","maul","voltaicslash","lowslash","wither","discharge"}){
                var sample=w.Abilities.Find(id)!;actor.ResetPresentation();actor.Rotation=Vector3.Zero;
                actor.TryBeginGesture(sample,sample.Definition.StartClip);actor.FitGesture(sample,sample.Definition.GestureSeconds);actor.FacingTarget=actor.GlobalPosition+Vector3.Forward;
                for(int frame=1;frame<=30;frame++){actor.Present(Vector3.Zero,.02);actor._Process(.02);if(frame%10==0)await Capture(id+"-pose-"+frame);}
            }
            void Reset(){w.Abilities.ResetCombat();actor.ResetPresentation();w.Build.Pregame.Reset();w.Periodic.Clear();foreach(var a in w.Abilities.Abilities)a.SetUnlocked(true);}
            void Cast(AutomaticAbility a,double time=-1){if(time<0)time=a.Definition.WindupSeconds+.05;actor.ResetPresentation();a.ResetCombat();a.SetUnlocked(true);Check(a.Definition.Id+" actual cast",a.TryBegin());a.Advance(time);actor._Process(time);((Node)a.Presentation).SetProcess(false);((Node)a.Presentation)._Process(Math.Max(0,time-a.Definition.WindupSeconds));}
            foreach(string id in new[]{"lacerate","maul","voltaicslash","lowslash","wither","discharge"}){
                Reset();var attack=w.Abilities.Find(id)!;actor.ResetPresentation();Check(id+" starts contact timing check",attack.TryBegin());
                float health=targets.Sum(t=>t.Health.Current);attack.Advance(attack.Definition.WindupSeconds-.02);
                Check(id+" waits for its visible strike",targets.Sum(t=>t.Health.Current)==health && attack.DamageEvents==0);
                attack.Advance(.03);Check(id+" applies damage at contact",targets.Sum(t=>t.Health.Current)<health && attack.DamageEvents>0);
                attack.Advance(attack.Definition.GestureSeconds-attack.Definition.WindupSeconds+.05);Check(id+" retains its total action length",!attack.Casting);
            }
            Reset();var lacerate=w.Abilities.Find("lacerate")!;int contacts=((D3AbilityPresentation)lacerate.Presentation).AssassinTargetImpacts;Cast(lacerate);Check("Lacerate contacts every victim",((D3AbilityPresentation)lacerate.Presentation).AssassinTargetImpacts==contacts+3);
            Reset();var discharge=w.Abilities.Find("discharge")!;contacts=((D3AbilityPresentation)discharge.Presentation).AssassinTargetImpacts;Cast(discharge);Check("Discharge contacts every victim",((D3AbilityPresentation)discharge.Presentation).AssassinTargetImpacts==contacts+3);
            Check("Discharge has one persistent status on every victim",targets.All(t=>t.GetChildren().OfType<PresentationStatusVisual>().Count(v=>v.IsInGroup("DischargeStatus") && !v.IsQueuedForDeletion())==1));
            await Capture("discharge");
            w.Periodic.Apply(discharge,targets[0]);Check("Discharge refresh replaces status rather than stacking",targets[0].GetChildren().OfType<PresentationStatusVisual>().Count(v=>v.IsInGroup("DischargeStatus") && !v.IsQueuedForDeletion())==1);
            w.Periodic.Clear();w.Periodic.Apply(discharge,targets[0]);Cast(lacerate);
            Check("spread Discharge carries its visual",targets.All(t=>w.Periodic.Has(discharge,t) && t.GetChildren().OfType<PresentationStatusVisual>().Any(v=>v.IsInGroup("DischargeStatus") && !v.IsQueuedForDeletion())));
            w.Periodic.Advance(w.Build.Pregame.PeriodicDuration(discharge)+.1);foreach(var v in targets.SelectMany(t=>t.GetChildren().OfType<PresentationStatusVisual>()))if(!v.IsQueuedForDeletion())v._Process(.01);
            Check("Discharge visual ends with the effect",targets.All(t=>t.GetChildren().OfType<PresentationStatusVisual>().All(v=>v.IsQueuedForDeletion())));
            Reset();var shock=w.Abilities.Find("shock")!;Check("Chain Shock talent applied",shock.Definition.Area==AbilityArea.Chain);Cast(shock,.15);
            var lightning=(OriginalAbilityPresentation)shock.Presentation;Check("Shock renders both secondary links",shock.LastDistinctHits==3 && lightning.ChainArcCount==2 && lightning.BoltCount>=2);await Capture("chain-shock");
            lightning._Process(.05);Check("chain links survive redraw",lightning.BoltCount>=4);lightning._Process(.3);Check("chain lightning expires",lightning.ChainArcCount==0 && lightning.BoltCount==0);
            Reset();var inv=UI.EquipmentWindows.Instance.Inventory;inv.State.Owned["voltaic_fury"]=0;inv.Equip("assassin","relic1","voltaic_fury");
            var slash=w.Abilities.Find("voltaicslash")!;w.Build.AssassinRelics.Damaged(slash,targets[0]);
            lightning.Begin(targets[0],targets[0].GlobalPosition,.1f);lightning.Impact(targets[0].AimPosition);w.Build.AssassinRelics.Resolved(shock,new[]{targets[0]});Check("Voltaic Fury renders secondary arcs",lightning.ChainArcCount==2);inv.Unequip("assassin","relic1");
            Reset();var deflect=w.Abilities.Find("deflection")!;Cast(deflect);var defense=(AssassinAbilityPresentation)deflect.Presentation;
            Check("Deflection talent extends duration",w.Build.Pregame.AssassinDeflectionDuration>4);
            w.Build.Pregame.Advance(4.1);defense._Process(4.1);Check("Deflection stays visible beyond four seconds",w.Build.Pregame.AssassinDeflecting && defense.DefensiveLoopVisible);
            w.Build.Pregame.Advance(1);defense._Process(1);Check("Deflection ends with protection",!w.Build.Pregame.AssassinDeflecting && !defense.DefensiveLoopVisible);
            Reset();Cast(deflect);w.Build.Pregame.Reset();defense._Process(.01);Check("Deflection clears on reset",!defense.DefensiveLoopVisible);
            Reset();var shroud=w.Abilities.Find("forceshroud")!;Cast(shroud);shroud.Advance(.45);Check("Shroud gesture survives short recovery",actor.GestureOwner==shroud);shroud.Advance(.5);Check("Shroud eventually releases gesture",actor.GestureOwner!=shroud);
            Reset();var wither=w.Abilities.Find("wither")!;Cast(wither);var vfx=(AssassinAbilityPresentation)wither.Presentation;
            var sprites=vfx.GetChildren().OfType<Sprite3D>().Where(s=>!s.IsQueuedForDeletion()).ToArray();var old=sprites.Select(s=>s.GlobalPosition).ToArray();var origin=actor.GlobalPosition;actor.GlobalPosition+=Vector3.Right*.2f;vfx._Process(.05);
            Check("Wither cast layers follow hand and retain offsets",sprites.Length==4 && sprites.Select((s,i)=>(s.GlobalPosition-old[i]).DistanceTo(Vector3.Right*.2f)<.001f).All(x=>x));actor.GlobalPosition=origin;vfx._Process(.001);
            Check("Wither still attaches target wisps",vfx.GetChildren().OfType<AssassinWitherVisual>().Count(e=>!e.GroundWave && !e.IsQueuedForDeletion())==3);
            await Capture("wither");
            Reset();var maul=w.Abilities.Find("maul")!;var mv=(AssassinAbilityPresentation)maul.Presentation;mv.TargetImpact(targets[0]);Check("Maul retains all eight recovered layers",mv.EffectCount==8);
            var contactSprites=mv.GetChildren().OfType<Sprite3D>().Where(s=>!s.IsQueuedForDeletion()).ToArray();var beforeMove=contactSprites.Select(s=>s.GlobalPosition).ToArray();
            targets[0].GlobalPosition+=Vector3.Right*.08f;mv._Process(.05);
            Check("Maul contact follows a moving victim",contactSprites.Select((s,i)=>(s.GlobalPosition-beforeMove[i]).DistanceTo(Vector3.Right*.08f)<.001f).All(x=>x));
            var lastContact=contactSprites.Select(s=>s.GlobalPosition).ToArray();targets[0].Health.ApplyDamage(1000000,DamageType.Internal);mv._Process(.05);
            Check("lethal contact remains at its last target position",contactSprites.Select((s,i)=>s.GlobalPosition.DistanceTo(lastContact[i])<.001f).All(x=>x));
            Reset();foreach(var target in targets){target.Health.Maximum=1;target.Health.Restore();}
            contacts=((D3AbilityPresentation)lacerate.Presentation).AssassinTargetImpacts;Cast(lacerate);
            Check("lethal Lacerate hits retain contact effects",targets.All(t=>!t.Alive) && ((D3AbilityPresentation)lacerate.Presentation).AssassinTargetImpacts==contacts+3);
            foreach(var a in w.Abilities.Abilities){a.Interrupt();a.ResetCombat();}
            Check("Assassin particle cleanup is complete",w.Abilities.Abilities.Select(a=>a.Presentation).OfType<AssassinAbilityPresentation>().All(v=>v.EffectCount==0 && !v.Vanishing));
        } catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/assassin-presentation-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,failure,checks=_checks},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
}
