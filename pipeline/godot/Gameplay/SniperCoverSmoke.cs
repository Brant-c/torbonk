using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class SniperCoverSmoke : Node
{
    public async void Run(GameWorld w)
    {
        var checks=new List<string>();string? failure=null;
        void Check(string name,bool ok){if(!ok)throw new Exception(name);checks.Add(name);}
        try{
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
            var controller=w.GetNode<SniperCoverController>("SniperCover");controller.SetProcess(false);
            w.Player.TestInput=Vector2.Zero;
            for(int i=0;i<30;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            Check("fixture grounded",w.Player.IsOnFloor());
            var actor=w.Player.Presentation;actor.ResetPresentation();w.Build.Pregame.Reset();
            w.Build.Pregame.Advance(.59);controller._Process(.1);Check("cover waits for positioning delay",!controller.Active);
            w.Build.Pregame.Advance(.02);controller._Process(.2);actor._Process(.2);
            Check("standing still enters cover",controller.Active && actor.SniperCoverActive);
            Check("original barrier deploys ahead of player",controller.BarrierVisible && controller.GetNode<Node3D>("OriginalPortableCover").GlobalPosition.DistanceTo(actor.GlobalPosition)>.1f);
            var barrier=controller.GetNode<Node3D>("OriginalPortableCover");
            Check("original left right and base devices exist",new[]{"DeviceMain","DeviceLeft","DeviceRight"}.All(n=>barrier.HasNode(n)));
            Check("both original projection beam meshes exist",barrier.HasNode("FieldBeams1") && barrier.HasNode("FieldBeams2"));
            Check("source edge attachment positions retained",barrier.GetNode<Node3D>("DeviceLeft").Position==new Vector3(.065f,.11f,0) && barrier.GetNode<Node3D>("DeviceRight").Position==new Vector3(-.065f,.11f,0));
            Check("one original shield surface",barrier.GetNode<Node3D>("ShieldField").FindChildren("*","MeshInstance3D",true,false).Count==1);
            Check("field uses original anchor and parent scale",barrier.GetNode<Node3D>("ShieldField").Position==new Vector3(0,0,.025f) && barrier.GetNode<Node3D>("ShieldField").Scale==new Vector3(1.2f,1.1f,1.1f));
            Check("projectors retain native model units",new[]{"DeviceMain","DeviceLeft","DeviceRight","FieldBeams1","FieldBeams2"}.All(n=>barrier.GetNode<Node3D>(n).Scale==Vector3.One));
            Check("source facing converted once at root",barrier.GlobalBasis.IsEqualApprox(actor.GlobalBasis.Orthonormalized()*new Basis(Vector3.Up,Mathf.Pi)));
            var fieldMesh=barrier.GetNode<Node3D>("ShieldField").FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().Single();
            Check("field uses recovered native projector layers",fieldMesh.MaterialOverride is ShaderMaterial fm && fm.Shader.ResourcePath=="res://Gameplay/SniperCoverField.gdshader" && fm.GetShaderParameter("hex_grid").AsGodotObject() is Texture2D hex && hex.ResourcePath=="res://Assets/Agent/proj_hexgrid_tile.png" && fm.GetShaderParameter("glow_bar").AsGodotObject() is Texture2D glow && glow.ResourcePath=="res://Assets/Agent/bw_projector_glowbar_blue_shield.png");
            Check("projector beam tips reach shield surface",new[]{"FieldBeams1","FieldBeams2"}.All(n=>fieldMesh.GetAabb().Grow(.002f).HasPoint(fieldMesh.GlobalTransform.AffineInverse()*(barrier.GetNode<Node3D>(n).GlobalTransform*new Vector3(0,.09f,0)))));
            Check("crouch blends fully",actor.SniperCoverBlend>.99);
            object owner=new object();Check("cover firing gesture starts",actor.TryBeginGesture(owner,"D3/Sniper",0));actor._Process(.1);
            Check("cover uses native firing animation",actor.State=="Agent/CoverFire");actor.ReleaseGesture(owner);
            var ballistic=w.Abilities.Find("ballisticshield")!;ballistic.SetUnlocked(true);ballistic.CombatEnabled=true;ballistic.ResetCombat();actor.ResetPresentation();
            controller.SetCover(false,.01);
            Check("Ballistic Shield requires cover",!ballistic.TryBegin());
            controller.SetCover(true,.01);
            Check("Ballistic Shield deploys in cover: "+ballistic.LastEvent+" / "+ballistic.State,ballistic.TryBegin());
            var dome=ballistic.BallisticField!;
            Check("Ballistic Shield uses a ground hemisphere",dome.GetNode<MeshInstance3D>("BallisticDome").Mesh is SphereMesh sphere && sphere.IsHemisphere && Mathf.IsEqualApprox(sphere.Radius,1));
            var origin=dome.GlobalPosition;
            w.PlayerActor.Health.Restore();
            Check("Ballistic Shield reduces damage by twenty percent",Mathf.IsEqualApprox(w.PlayerActor.Health.ApplyDamage(10),8));
            Check("Ballistic Shield does not grant absorb capacity",!w.PlayerActor.Health.Shielded);
            var ally=new Combatant {Team=CombatTeam.Player};ally.AddChild(new Health {Name="Health"});w.AddChild(ally);ally.GlobalPosition=origin;
            Check("allies inside dome protected",Mathf.IsEqualApprox(ally.Health.ApplyDamage(10),8));
            ally.GlobalPosition=origin+Vector3.Right*1.1f;
            Check("allies outside dome unprotected",Mathf.IsEqualApprox(ally.Health.ApplyDamage(10),10));
            ally.Team=CombatTeam.Hostile;ally.GlobalPosition=origin;
            Check("hostiles inside dome unprotected",Mathf.IsEqualApprox(ally.Health.ApplyDamage(10),10));ally.QueueFree();
            actor.GlobalPosition+=Vector3.Right*.01f;
            Check("dome remains at deployment position",dome.GlobalPosition==origin);actor.GlobalPosition-=Vector3.Right*.01f;
            dome._PhysicsProcess(10);
            Check("dome expires after duration",!dome.Active);
            ballistic.ResetCombat();controller.SetCover(true,.01);Check("dome can deploy again after reset",ballistic.TryBegin());
            dome=ballistic.BallisticField!;controller.SetCover(false,.01);
            Check("cover exit immediately ends mitigation",!dome.Active && Mathf.IsEqualApprox(w.PlayerActor.Health.ApplyDamage(10),10));
            dome._PhysicsProcess(.01);controller.SetCover(true,.01);
            Check("returning to cover does not restore cancelled dome",!dome.Active);
            ballistic.ResetCombat();w.PlayerActor.Health.Restore();
            if(OS.GetCmdlineUserArgs().Contains("--agent-capture")){
                foreach(var layer in GetTree().Root.FindChildren("*","CanvasLayer",true,false).OfType<CanvasLayer>())layer.Visible=false;
                var camera=new Camera3D();w.AddChild(camera);camera.GlobalPosition=actor.GlobalPosition+new Vector3(.3f,.2f,.38f);camera.LookAt(actor.GlobalPosition+Vector3.Up*.08f);camera.MakeCurrent();
                foreach(var view in new[]{("",new Vector3(.3f,.2f,.38f)),("-side",new Vector3(.42f,.16f,.02f)),("-front",new Vector3(-.2f,.15f,-.32f))}){
                    camera.GlobalPosition=actor.GlobalPosition+view.Item2;camera.LookAt(actor.GlobalPosition+Vector3.Up*.08f);
                    for(int i=0;i<15;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/sniper-portable-cover"+view.Item1+".png"));
                }
            }
            var strikeTarget=new Combatant{Name="CoverStrikeFixture",Team=CombatTeam.Hostile};strikeTarget.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(strikeTarget);strikeTarget.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*.2f;
            var orbital=w.Abilities.Find("orbitalstrike")!;orbital.SetUnlocked(true);orbital.ResetCombat();actor.ResetPresentation();w.Build.Pregame.Advance(.61);
            Check("Orbital Strike begins from cover",orbital.TryBegin());float strikeHealth=strikeTarget.Health.Current;
            w.Player.TestInput=Vector2.Right;
            for(int i=0;i<8;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            w.Build.Pregame.Advance(.02);controller._Process(.02);
            Check("moving exits cover without interrupting movement",w.Player.ActualVelocity.Length()>.03f && !controller.Active && !actor.SniperCoverActive && !controller.BarrierVisible);
            orbital.Advance(3.5);Check("active Orbital Strike finishes after cover exit",strikeTarget.Health.Current<strikeHealth);orbital.ResetCombat();
            foreach(string id in new[]{"ambush","orbitalstrike","explosiveprobe","agentcull"}) {
                var a=w.Abilities.Find(id)!;a.SetUnlocked(true);a.ResetCombat();actor.ResetPresentation();
                Check(id+" cannot begin while moving or spend a cooldown",!a.TryBegin() && a.Casts==0 && a.CooldownRemaining==0);
                Check(id+" tooltip explains cover",UI.AbilityTooltips.Describe(w,a).Contains("Requires cover to begin"));
            }
            w.Player.TestInput=Vector2.Zero;
            for(int i=0;i<20;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            w.Build.Pregame.Advance(.7);controller._Process(.2);Check("cover reenters after stopping",controller.Active);
            w.Player.Rig.SetControlsEnabled(true);w.Player._Input(new InputEventAction {Action="b9_jump",Pressed=true});
            for(int i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            w.Build.Pregame.Advance(.02);controller._Process(.02);Check("jumping retracts cover",!w.Player.IsOnFloor() && !controller.Active && !controller.BarrierVisible);
            for(int i=0;i<70;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            w.Build.Pregame.Advance(.7);controller._Process(.2);
            actor.ResetPresentation();w.Build.Pregame.Reset();controller._Process(.01);Check("restart retracts cover",!controller.Active && !controller.BarrierVisible && !actor.SniperCoverActive);
            controller.SetCover(true,.2);w.PlayerActor.Health.ApplyDamage(10000);controller._Process(.01);Check("death retracts cover",!controller.Active && !controller.BarrierVisible);
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/sniper-cover-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,failure,checks},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
}
