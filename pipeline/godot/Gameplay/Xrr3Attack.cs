using Godot;
using System;
using System.Collections.Generic;

namespace SwtorSurvivors;

/// <summary>Authored solo-arena XRR-3 mechanics, using the original boss presentation.</summary>
public partial class Xrr3Attack : Node
{
    public BossPhase Phase { get; private set; }=BossPhase.Pursuit;
    public string Instruction { get; private set; }="";
    public int PhaseNumber => _actor.Health.Current/_actor.Health.Maximum<=.35f?3:_actor.Health.Current/_actor.Health.Maximum<=.65f?2:1;
    public int Strikes {get;private set;}
    public int Warnings {get;private set;}
    public int Dodged {get;private set;}
    private EnemyAgent _actor=null!;
    public float WarningSeconds => _windup;
    private OperationDifficultyTuning Mode=>_actor.Crowd?.OperationMode ?? OperationDifficultyTuning.Baseline;
    private double _remaining=4;
    private int _turn,_count;
    private float _windup,_radius;
    private readonly Vector3[] _centers=new Vector3[3],_directions=new Vector3[3];
    private readonly List<MeshInstance3D> _markers=new();
    private readonly List<ShaderMaterial> _materials=new();
    private readonly List<GroundTelegraphSurface> _surfaces=new();
    private int _shape,_step;
    private double _vulnerability,_fireRemaining,_fireTick;
    private readonly Vector3[] _fireCenters=new Vector3[3];
    private readonly List<MeshInstance3D> _fires=new();
    public int ActiveHazards=>_fireRemaining>0?_fires.Count:0;
    public float MechanicRadius=>_radius;
    private void ClearFire(){foreach(var m in _fires)if(GodotObject.IsInstanceValid(m))m.QueueFree();_fires.Clear();_fireRemaining=0;}
    private void TickFire(double delta){
        _vulnerability=Math.Max(0,_vulnerability-delta);
        if(_fireRemaining<=0)return;
        _fireRemaining-=delta;if(_fireRemaining<=0){ClearFire();return;}
        if((_fireTick-=delta)>0)return;_fireTick=.65;
        for(int i=0;i<_fires.Count;i++){
            var d=_actor.Target.GlobalPosition-_fireCenters[i];
            if(Math.Abs(d.Y)<.35f && new Vector2(d.X,d.Z).Length()<=.19f*Mode.MechanicRadius
                && _actor.Navigation.HasLineOfSight(_fireCenters[i]+Vector3.Up*.1f,_actor.Target.AimPosition)){
                _actor.DealAttack(10,DamageType.Kinetic);break;
            }
        }
    }
    private void LeaveFire(){
        ClearFire();_fireRemaining=Mode.MechanicsTier==2?9:7;_fireTick=.65;
        for(int i=0;i<_count;i++){
            var mat=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/TombTelegraph.gdshader")};mat.SetShaderParameter("impact",1f);
            var surface=new GroundTelegraphSurface();var m=new MeshInstance3D {Mesh=surface.Mesh,MaterialOverride=mat,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
            _actor.GetTree().CurrentScene.AddChild(m);m.GlobalPosition=_centers[i]+Vector3.Up*.008f;m.Scale=Vector3.One*(.38f*Mode.MechanicRadius);
            surface.Project(m,_actor.Navigation.Arena);_fireCenters[i]=_centers[i];_fires.Add(m);
        }
    }
    public override void _Ready() {
        _actor=GetParent<EnemyAgent>();_actor.GetNode<Health>("Health").Died+=End;
        _remaining*=Mode.AttackInterval;
        for(int i=0;i<3;i++) {
            var mat=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/TombTelegraph.gdshader")};
            var surface=new GroundTelegraphSurface();
            var marker=new MeshInstance3D {Visible=false,Mesh=surface.Mesh,MaterialOverride=mat,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
            _actor.GetTree().CurrentScene.AddChild(marker);_materials.Add(mat);_surfaces.Add(surface);_markers.Add(marker);
        }
    }
    public bool Tick(double delta) {
        if(Phase==BossPhase.Ended || !_actor.Alive || !_actor.Target.Alive){End();return false;}
        TickFire(delta);
        _remaining-=delta;
        if(Phase==BossPhase.Pursuit) {
            if(_remaining>0 || _actor.GlobalPosition.DistanceTo(_actor.Target.GlobalPosition)>6
                || !_actor.Navigation.HasLineOfSight(_actor.AimPosition,_actor.Target.AimPosition))return false;
            Lock();Phase=BossPhase.Telegraph;_remaining=_windup;return true;
        }
        if(Phase==BossPhase.Telegraph) {
            for(int i=0;i<_count;i++)_materials[i].SetShaderParameter("progress",Mathf.Clamp(1-(float)(_remaining/_windup),0,1));
            if(_remaining>0)return true;
            Strike();if(Phase==BossPhase.Ended)return true;
            if(_shape==0 && Mode.MechanicsTier>0)LeaveFire();
            if(_shape==1 && Mode.MechanicsTier>0 && _step<(_count-1)+(Mode.MechanicsTier==2?1:0)){
                _step++;_remaining=_windup*.65f;
                Instruction=_step>=_count?"CANNON RETURN · KEEP CLEAR OF THE FIRST LANE":"CANNON SWEEP · MOVE TO A CLEARED LANE";
                for(int i=0;i<_count;i++)_materials[i].SetShaderParameter("impact",0f);return true;
            }
            Phase=BossPhase.Recovery;_remaining=(PhaseNumber==3?1.35:1.8)*Mode.RecoveryTime;return true;
        }
        if(_remaining>0)return true;
        Hide();Phase=BossPhase.Pursuit;_remaining=(PhaseNumber==3?2.5:3.8)*Mode.AttackInterval;Instruction="";return false;
    }
    private void Lock() {
        Hide();_turn++;_count=1;_step=0;
        var actor=_actor.GlobalPosition;var target=_actor.Target.GlobalPosition;
        var direction=target-actor;direction.Y=0;direction=direction.LengthSquared()>.00001f?direction.Normalized():Vector3.Forward;
        int pattern=(_turn-1)%3;
        // A distant player triggers missiles rather than making the boss wait forever for a melee swipe.
        if(pattern==2 && actor.DistanceTo(target)>1.1f)pattern=0;
        _shape=pattern==0?0:pattern==1?1:2;
        _windup=pattern==0?1.65f:pattern==1?1.8f:1.5f;
        if(PhaseNumber==3)_windup-=.2f;
        _windup*=Mode.WarningTime;
        _radius=pattern==0?.19f:pattern==1?Math.Max(1.6f,actor.DistanceTo(target)+.3f):.65f;
        _radius*=Mode.MechanicRadius;
        _centers[0]=pattern==0?target:actor;_directions[0]=direction;
        if(pattern==0) {
            _count=PhaseNumber>=2?3:2;
            // Lock every impact before the warning begins. Keep a broad escape route.
            var side=direction.Rotated(Vector3.Up,Mathf.Pi/2);
            _centers[1]=target+side*.48f;_centers[2]=target-side*.48f;
            for(int i=0;i<_count;i++)_directions[i]=direction;
            Instruction="MISSILE SALVO · LEAVE THE MARKED CIRCLES";
        } else if(pattern==1) {
            _count=PhaseNumber>=2 || Mode.MechanicsTier>0?3:1;
            for(int i=1;i<_count;i++){_centers[i]=actor;_directions[i]=direction.Rotated(Vector3.Up,(i==1?1:-1)*Mathf.DegToRad(28));}
            Instruction="CANNON BURST · SIDESTEP THE LOCKED FIRING LANES";
        } else Instruction="OVERWHELMING SWIPE · MOVE BEHIND XRR-3";
        Warnings++;
        for(int i=0;i<_count;i++) {
            var p=_centers[i];if(_actor.Navigation.Arena.GroundAt(p,out var ground,out _,.8f,1))p=ground;
            _centers[i]=p;var marker=_markers[i];marker.GlobalPosition=p+Vector3.Up*.006f;
            marker.Rotation=new Vector3(0,Mathf.Atan2(-_directions[i].X,-_directions[i].Z),0);
            marker.Scale=Vector3.One*(_radius*2);marker.Visible=true;_surfaces[i].Project(marker,_actor.Navigation.Arena);
            _materials[i].SetShaderParameter("shape",_shape);_materials[i].SetShaderParameter("progress",0f);_materials[i].SetShaderParameter("impact",0f);
        }
    }
    private void Strike() {
        Strikes++;_actor.Visual?.Attack();bool hit=false;
        for(int i=0;i<_count;i++) {
            if(_shape==1 && Mode.MechanicsTier>0 && i!=(_step<_count?_step:0))continue;
            _materials[i].SetShaderParameter("impact",1f);
            var offset=_actor.Target.GlobalPosition-_centers[i];offset.Y=0;float forward=offset.Dot(_directions[i]);
            bool inside=_shape switch {
                1=>Math.Abs(offset.Dot(_directions[i].Rotated(Vector3.Up,Mathf.Pi/2)))<=_radius*.079f && Math.Abs(forward)<=_radius,
                2=>offset.Length()<=_radius && (offset.Length()<.03f || forward/offset.Length()>=.55f),
                _=>offset.Length()<=_radius
            };
            hit|=inside && Math.Abs(_actor.Target.GlobalPosition.Y-_centers[i].Y)<.35f;
        }
        if(hit && _actor.Navigation.HasLineOfSight(_actor.AimPosition,_actor.Target.AimPosition))
            {_actor.DealAttack((PhaseNumber==3?38:30)*(_vulnerability>0?1.35f:1),DamageType.Kinetic);if(Mode.MechanicsTier>0)_vulnerability=Mode.MechanicsTier==2?14:9;}
        else Dodged++;
    }
    private void Hide(){foreach(var marker in _markers)marker.Visible=false;}
    public void End(){Phase=BossPhase.Ended;Instruction="";Hide();ClearFire();}
    public override void _ExitTree(){ClearFire();foreach(var marker in _markers)if(GodotObject.IsInstanceValid(marker))marker.QueueFree();}
}
