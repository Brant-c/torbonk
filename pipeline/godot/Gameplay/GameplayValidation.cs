using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>Opt-in B1 acceptance runner; exercises the real controller and existing static bodies.</summary>
public partial class GameplayValidation : Node
{
    private readonly List<object> _checks = new();
    private readonly List<string> _failures = new();
    private GameWorld _world = null!;
    private PlayerController P => _world.Player;
    private ArenaBounds A => _world.Arena;
    private bool _capture;
    private void Check(string name, bool ok, object? details = null)
    { _checks.Add(new { name, passed=ok, details }); if (!ok) { _failures.Add(name); GD.PushWarning("B1: " + name); } }
    private static float[] V(Vector3 v) => new[] { v.X,v.Y,v.Z };
    private async Task Frames(int count)
    {
        long target=P.PhysicsSteps+count;
        while(P.PhysicsSteps < target) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }
    private async Task Drive(Vector2 input, float seconds)
    { P.TestInput = input; await Frames((int)Math.Round(seconds*Engine.PhysicsTicksPerSecond)); }
    private async Task Shot(string name)
    {
        if (!_capture) return;
        // Allow visibility/camera changes to reach the renderer before reading its texture.
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if(name=="06-architecture") CheckVisibleMeshSightline();
        Check("capture " + name, GetViewport().GetTexture().GetImage().SavePng("res://../generated/gameplay/"+name+".png") == Error.Ok,
            new{player=V(P.GlobalPosition),focus=V(_world.Rig.Focus),camera=V(_world.Rig.Camera.GlobalPosition),hiddenMeshes=_world.Rig.HiddenPropMeshes,clearFraction=_world.Rig.ClearFraction});
    }

    private void CheckVisibleMeshSightline()
    {
        float nearest=float.MaxValue; string blocker=""; Vector3 point=Vector3.Zero;
        foreach(var node in _world.EnvironmentRoot.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
        {
            if(!node.IsVisibleInTree() || node.Mesh==null) continue;
            var inverse=node.GlobalTransform.AffineInverse();
            var from=inverse*_world.Rig.Focus; var to=inverse*_world.Rig.Camera.GlobalPosition;
            if(!node.GetAabb().IntersectsSegment(from,to)) continue;
            for(int s=0;s<node.Mesh.GetSurfaceCount();s++)
            {
                var arrays=node.Mesh.SurfaceGetArrays(s); var vertices=arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                var indices=arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                int count=indices.Length>0?indices.Length:vertices.Length;
                for(int i=0;i<count;i+=3)
                {
                    var hit=Geometry3D.SegmentIntersectsTriangle(from,to,vertices[indices.Length>0?indices[i]:i],vertices[indices.Length>0?indices[i+1]:i+1],vertices[indices.Length>0?indices[i+2]:i+2]);
                    if(hit.VariantType==Variant.Type.Nil) continue;
                    var p=node.GlobalTransform*hit.AsVector3(); float d=p.DistanceTo(_world.Rig.Focus);
                    if(d<nearest) { nearest=d; blocker=node.GetPath().ToString(); point=p; }
                }
            }
        }
        Check("render mesh sightline clear",blocker=="",new{blocker,point=V(point),distance=nearest});
    }

    public async void Run(GameWorld world, bool capture)
    {
        _world=world; _capture=capture;
        try { await Validate(); }
        catch (Exception e) { Check("validation exception",false,e.ToString()); }
        var result = new { passed=_failures.Count==0, failures=_failures, checks=_checks, captures=capture,
            engine=Engine.GetVersionInfo()["string"].AsString(), renderer=RenderingServer.GetCurrentRenderingMethod(),
            physicsEngine=ProjectSettings.GetSetting("physics/3d/physics_engine").AsString(),
            spawn=V(P.SpawnPosition), recoveries=P.Recoveries };
        using var file=Godot.FileAccess.Open("res://../manifests/gameplay-validation"+(capture?"-visual":"")+".json",Godot.FileAccess.ModeFlags.Write);
        file.StoreString(JsonSerializer.Serialize(result,new JsonSerializerOptions { WriteIndented=true }));
        GD.Print(_failures.Count==0?"GAMEPLAY_VALIDATION_PASSED":"GAMEPLAY_VALIDATION_FAILED");
        GetTree().Quit(_failures.Count==0?0:1);
    }

    private async Task Validate()
    {
        var originalSpawn=P.SpawnPosition;
        await Frames(10);
        Check("grounded spawn",P.IsOnFloor(),V(P.GlobalPosition));
        Check("original scale",P.Presentation.GetNode<Node3D>("OriginalSorcerer").Scale.IsEqualApprox(Vector3.One));
        Check("original skeleton",P.Presentation.Skeleton.GetBoneCount()==211);
        Check("seven original clips",new[]{"Idle","Run","LightningStart","LightningLoop","Hit","Death","LightningEnd"}.All(x => P.Presentation.Animator.HasAnimation(x)));
        Check("idle at rest",P.Presentation.State=="Idle");
        await Shot("01-idle");
        // Probe data is also retained to make terrain/prop limitations reviewable.
        var cells=new List<object>();
        for(float x=21;x<45;x+=1) for(float z=7;z<18;z+=1)
        {
            if(A.GroundAt(new Vector3(x,0,z),out var floor,out var normal,40,70))
                cells.Add(new{point=V(floor),normal=V(normal),supported=A.Supported(floor,P.CapsuleRadius)});
        }
        using(var f=Godot.FileAccess.Open("res://../manifests/gameplay-terrain-probes.json",Godot.FileAccess.ModeFlags.Write)) f.StoreString(JsonSerializer.Serialize(cells));
        if(OS.GetCmdlineUserArgs().Contains("--gameplay-probe")) return;
        // Physical input events use the same action map as a keyboard, including diagonal normalization.
        P.TestInput=null; var start=P.GlobalPosition;
        Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.W,Pressed=true});
        await Frames(30);
        Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.W,Pressed=false});
        Check("physical W moves camera forward",P.GlobalPosition.Z < start.Z-0.1f,V(P.GlobalPosition-start));
        Check("run after movement",P.Presentation.State=="Run"); await Shot("02-running");
        Check("character faces actual movement",(-P.Presentation.GlobalBasis.Z).Dot(new Vector3(P.ActualVelocity.X,0,P.ActualVelocity.Z).Normalized())>0.95f);
        Check("root motion does not move model wrapper",P.Presentation.GetNode<Node3D>("OriginalSorcerer").Position.Length()<0.00001f &&
            P.Presentation.Skeleton.GetBoneGlobalPose(P.Presentation.Skeleton.FindBone("Bip01")).Origin.Length()<0.0001f);
        float followLag=_world.Rig.Focus.DistanceTo(P.GlobalPosition+Vector3.Up*_world.Rig.TargetHeight);
        Check("smooth follow stays near moving character",followLag>0.001f && followLag<0.12f,followLag);
        await Drive(Vector2.Zero,0.4f); Check("decelerates to idle",P.Presentation.State=="Idle" && P.Velocity.Length()<0.04f);
        foreach(var input in new[]{Vector2.Left,Vector2.Right,Vector2.Up,Vector2.Down})
        {
            P.ResetPlayer(); await Frames(5); start=P.GlobalPosition;
            await Drive(input,0.5f);
            var moved=P.GlobalPosition-start; var expected=PlayerController.CameraDirection(input,0);
            Check("camera input "+input,moved.Dot(expected)>0.08f,V(moved));
        }
        _world.Rig.YawDegrees=90; P.ResetPlayer(); await Frames(5); start=P.GlobalPosition;
        await Drive(Vector2.Up,0.5f);
        Check("camera-relative rotated yaw",(P.GlobalPosition-start).X < -0.08f,V(P.GlobalPosition-start));
        _world.Rig.YawDegrees=0;
        var rates=new List<object>(); var distances=new List<float>();
        foreach(var tickRate in new[]{30,60,120})
        {
            Engine.PhysicsTicksPerSecond=tickRate; P.TestInput=Vector2.Zero; P.ResetPlayer(); await Frames(5); start=P.GlobalPosition;
            await Drive(Vector2.Up,1); var d=(P.GlobalPosition-start); distances.Add(new Vector2(d.X,d.Z).Length());
            rates.Add(new{tickRate,distance=distances.Last(),grounded=P.IsOnFloor()});
            Check("grounded at physics rate "+tickRate,P.IsOnFloor());
        }
        Check("physics rate movement agreement",distances.Max()-distances.Min()<0.025f,rates);
        Engine.PhysicsTicksPerSecond=60;
        var renderRates=new List<object>(); distances.Clear();
        foreach(var fps in new[]{30,60,144})
        {
            Engine.MaxFps=fps; P.TestInput=Vector2.Zero; P.ResetPlayer(); await Frames(5); start=P.GlobalPosition;
            ulong wallStart=Time.GetTicksUsec(); int renderedStart=Engine.GetFramesDrawn();
            await Drive(Vector2.Up,1); var d=P.GlobalPosition-start;
            double elapsedSeconds=(Time.GetTicksUsec()-wallStart)/1_000_000.0;
            distances.Add(new Vector2(d.X,d.Z).Length()); renderRates.Add(new{renderFpsCap=fps,distance=distances.Last(),
                measuredRenderFps=(Engine.GetFramesDrawn()-renderedStart)/elapsedSeconds});
        }
        Engine.MaxFps=0;
        Check("render rate movement agreement",distances.Max()-distances.Min()<0.025f,renderRates);
        // Integration traversal over source terrain; no test floors are introduced.
        P.TestInput=Vector2.Zero; P.ResetPlayer(); await Frames(5);
        foreach(var input in new[]{Vector2.Up,Vector2.Left,Vector2.Down,Vector2.Right})
        {
            await Drive(input,2);
            Check("grounded traversal "+input,P.IsOnFloor() && A.Contains(P.GlobalPosition,P.CapsuleRadius),V(P.GlobalPosition));
        }
        Check("no recovery during ordinary traversal",P.Recoveries==0,P.Recoveries);
        await Drive(Vector2.Zero,0.3f); await Shot("03-traversal");
        Check("outside rectangle rejected",!A.CanTraverse(P.GlobalPosition,new Vector3(46,P.GlobalPosition.Y,12),P.CapsuleRadius));
        Check("crop boundary rejected",!A.Supported(new Vector3(A.Minimum.X+0.01f,P.GlobalPosition.Y,12),P.CapsuleRadius));
        Check("missing ground rejected",!A.Supported(new Vector3(36,-50,12),P.CapsuleRadius));
        // Find a real missing source triangle, rather than simulating a hole with a test floor.
        using(var source=JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://Assets/Korriban/environment.json")))
        {
            bool found=false; Vector3 hole=Vector3.Zero;
            foreach(var patch in source.RootElement.GetProperty("terrain").EnumerateArray())
            {
                var vertices=patch.GetProperty("vertices").EnumerateArray().Select(v=>new Vector3(v[0].GetSingle(),v[1].GetSingle(),v[2].GetSingle())).ToArray();
                var pos=patch.GetProperty("position"); var offset=new Vector3(pos[0].GetSingle(),pos[1].GetSingle(),pos[2].GetSingle());
                var indices=patch.GetProperty("indices").EnumerateArray().Select(v=>v.GetInt32()).ToArray();
                var faces=new HashSet<string>();
                for(int i=0;i<indices.Length;i+=3) faces.Add(string.Join(",",indices.Skip(i).Take(3).OrderBy(v=>v)));
                for(int z=1;z<62 && !found;z++) for(int x=1;x<62 && !found;x++)
                {
                    int a=z*64+x;
                    foreach(var tri in new[]{new[]{a,a+1,a+64},new[]{a+1,a+64,a+65}})
                    {
                        if(faces.Contains(string.Join(",",tri.OrderBy(v=>v)))) continue;
                        hole=offset+(vertices[tri[0]]+vertices[tri[1]]+vertices[tri[2]])/3;
                        if(!A.GroundAt(hole,out _,out _,0.02f,0.02f) && !A.Supported(hole,P.CapsuleRadius)) { found=true; break; }
                    }
                }
                if(found) break;
            }
            Check("actual source hole rejected",found,V(hole));
        }
        await TestArchitecture();
        await TestBoundary();
        P.SpawnPosition=originalSpawn; P.TestInput=Vector2.Zero; P.ResetPlayer(); await Frames(5);
        // Assert actual camera boom is obstruction-free; test camera at geometry during prop exercise below.
        _world.Rig.Snap();
        var hit=_world.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(_world.Rig.Focus,_world.Rig.Camera.GlobalPosition,3));
        Check("camera sightline clear",hit.Count==0,new{fraction=_world.Rig.ClearFraction});
        P.GlobalPosition+=Vector3.Down*3; await Frames(2);
        Check("fall recovery",P.Recoveries==1 && A.Contains(P.GlobalPosition));
        P.ResetPlayer(); await Drive(Vector2.Zero,0.3f);
        Check("reset returns to grounded spawn",P.GlobalPosition.DistanceTo(P.SpawnPosition)<0.01f && P.IsOnFloor());
        _world.ToggleDebug(); await Shot("04-collision-debug"); _world.ToggleDebug();
        await Shot("05-reset");
    }

    private bool FreeCapsule(Vector3 feet)
    {
        var shape=P.GetNode<CollisionShape3D>("CollisionShape3D");
        var query=new PhysicsShapeQueryParameters3D { Shape=shape.Shape,
            Transform=new Transform3D(Basis.Identity,feet+shape.Position+Vector3.Up*0.002f), CollisionMask=2, Margin=0.001f };
        return _world.GetWorld3D().DirectSpaceState.IntersectShape(query).Count==0;
    }

    private async Task TestArchitecture()
    {
        var savedSpawn=P.SpawnPosition;
        Vector3 candidate=Vector3.Zero, direction=Vector3.Zero; float distance=0; string source="";
        for(float x=33;x<41 && source=="";x+=0.2f) for(float z=9;z<16 && source=="";z+=0.2f)
        {
            if(!A.GroundAt(new Vector3(x,0,z),out var floor,out _,40,70) || !A.Supported(floor,P.CapsuleRadius) || !FreeCapsule(floor)) continue;
            for(int i=0;i<16;i++)
            {
                var d=new Vector3(Mathf.Cos(i*Mathf.Tau/16),0,Mathf.Sin(i*Mathf.Tau/16));
                var rayOrigin=floor+Vector3.Up*0.09f;
                var hit=_world.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(rayOrigin,rayOrigin+d*0.4f,2));
                if(hit.Count==0) continue;
                var n=(Vector3)hit["normal"]; if(Mathf.Abs(n.Y)>0.25f || n.Dot(d)>-0.95f) continue;
                var node=(Node)hit["collider"];
                while(node!=null && !node.HasMeta("source_model")) node=node.GetParent();
                string model=node?.GetMeta("source_model").AsString()??"";
                if(!model.Contains("ruins_") && !model.Contains("greatstatue")) continue;
                float dist=rayOrigin.DistanceTo((Vector3)hit["position"]);
                if(dist<0.12f || !A.CanTraverse(floor,floor+d*0.4f,P.CapsuleRadius)) continue;
                candidate=floor; direction=d; distance=dist; source=model; break;
            }
        }
        Check("architectural collision candidate found",source!="",new{source,point=V(candidate),direction=V(direction),distance});
        if(source=="") return;
        P.SpawnPosition=candidate+Vector3.Up*0.002f; P.TestInput=Vector2.Zero; P.ResetPlayer(); await Frames(5);
        var start=P.GlobalPosition; int contacts=P.WallContacts; int rejected=A.RejectedMoves;
        var traces=new List<object>(); P.TestInput=new Vector2(direction.X,direction.Z);
        for(int step=0;step<12;step++)
        {
            await Frames(1);
            var collisions=new List<object>();
            for(int c=0;c<P.GetSlideCollisionCount();c++)
            { var hit=P.GetSlideCollision(c); collisions.Add(new{normal=V(hit.GetNormal()),collider=((Node)hit.GetCollider()).GetPath().ToString()}); }
            traces.Add(new{step,position=V(P.GlobalPosition),velocity=V(P.Velocity),collisions});
        }
        await Drive(new Vector2(direction.X,direction.Z),1);
        float moved=(P.GlobalPosition-start).Dot(direction);
        Check("original architecture blocks controller",P.WallContacts>contacts && moved<distance+0.015f,
            new{source,wallContacts=P.WallContacts-contacts,moved,rayDistance=distance,end=V(P.GlobalPosition),rejected=A.RejectedMoves-rejected,lastRejection=A.LastRejection,traces});
        await Drive(Vector2.Zero,0.3f);
        Check("blocked character returns idle",P.Presentation.State=="Idle");
        // Sweep the follow camera deliberately through the same architecture; compression must activate.
        var rig=_world.Rig; var oldYaw=rig.YawDegrees; var oldDistance=rig.Distance; var oldHeight=rig.Height; var oldAngle=rig.AngleDegrees;
        rig.RaiseAboveObstructions=false;
        rig.HideCloseOccluders=false;
        rig.YawDegrees=Mathf.RadToDeg(Mathf.Atan2(direction.X,direction.Z)); rig.Distance=0.8f; rig.Height=0.1f; rig.AngleDegrees=5;
        rig.Snap();
        var blocked=_world.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(rig.Focus,rig.DesiredPosition,3));
        Check("camera detects architecture",blocked.Count>0 && rig.ClearFraction<0.9f,new{fraction=rig.ClearFraction});
        var clear=_world.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(rig.Focus,rig.Camera.GlobalPosition,3));
        Check("compressed camera stays before geometry",clear.Count==0);
        rig.RaiseAboveObstructions=true;
        rig.HideCloseOccluders=true;
        rig.YawDegrees=oldYaw; rig.Distance=oldDistance; rig.Height=oldHeight; rig.AngleDegrees=oldAngle; rig.Snap();
        Check("wall camera preserves subject distance",rig.Camera.GlobalPosition.DistanceTo(rig.Focus)>rig.MinimumSubjectDistance && rig.HiddenPropMeshes>0,
            new{angle=rig.EffectiveAngleDegrees,distance=rig.Camera.GlobalPosition.DistanceTo(rig.Focus),hiddenMeshes=rig.HiddenPropMeshes});
        await Shot("06-architecture");
        var hidden=_world.EnvironmentRoot.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().Where(x=>!x.Visible).ToArray();
        P.SpawnPosition=savedSpawn; P.TestInput=Vector2.Zero; P.ResetPlayer(); await Frames(5); rig.Snap();
        Check("occluding prop visibility restored",hidden.Length>0 && hidden.All(x=>x.Visible));
    }

    private async Task TestBoundary()
    {
        Vector3 candidate=Vector3.Zero, direction=Vector3.Zero; bool found=false;
        for(int edge=0;edge<4 && !found;edge++) for(float t=0.5f;t<11 && !found;t+=0.2f)
        {
            var point=edge switch { 0=>new Vector3(A.Minimum.X+0.15f,0,A.Minimum.Y+t),
                1=>new Vector3(A.Maximum.X-0.15f,0,A.Minimum.Y+t),
                2=>new Vector3(A.Minimum.X+t,0,A.Minimum.Y+0.15f),
                _=>new Vector3(A.Minimum.X+t,0,A.Maximum.Y-0.15f) };
            if(!A.GroundAt(point,out var floor,out _,40,70) || !A.Supported(floor,P.CapsuleRadius) || !FreeCapsule(floor)) continue;
            candidate=floor; direction=edge switch {0=>Vector3.Left,1=>Vector3.Right,2=>Vector3.Forward,_=>Vector3.Back}; found=true;
        }
        Check("traversable crop edge found",found,V(candidate));
        if(!found) return;
        P.SpawnPosition=candidate+Vector3.Up*0.002f; P.TestInput=Vector2.Zero; P.ResetPlayer(); await Frames(5);
        int blocks=A.RejectedMoves, recoveries=P.Recoveries;
        await Drive(new Vector2(direction.X,direction.Z),1);
        Check("controller stops at crop edge",A.RejectedMoves>blocks && A.Contains(P.GlobalPosition,P.CapsuleRadius) && P.Recoveries==recoveries,
            new{start=V(candidate),end=V(P.GlobalPosition),blocks=A.RejectedMoves-blocks});
    }
}
