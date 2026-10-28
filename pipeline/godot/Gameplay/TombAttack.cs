using Godot;
using System;
using System.Collections.Generic;

namespace SwtorSurvivors;

/// <summary>Locked-ground patterns with three reusable markers per actor, no per-strike mesh allocation.</summary>
public partial class TombAttack : Node
{
    [Export] public string Pattern {get;set;}="spit";
    [Export] public float Damage {get;set;}=7;
    [Export] public float Windup {get;set;}=1.25f;
    [Export] public float Cooldown {get;set;}=5;
    [Export] public float TriggerRange {get;set;}=0.9f;
    [Export] public string SummonScene {get;set;}="";
    [Export] public string WarningCue {get;set;}="burrow_warning";
    [Export] public string ImpactCue {get;set;}="burrow_impact";
    [Export] public string InstructionOverride {get;set;}="";
    [Export] public float PoolSeconds {get;set;}
    [Export] public bool BlasterEffect {get;set;}
    private double _poolTick;
    public BossPhase Phase {get;private set;}=BossPhase.Pursuit;
    public string Instruction {get;private set;}="";
    public int Strikes {get;private set;}
    public int ActiveWarnings=>Phase==BossPhase.Telegraph?_count:0;
    public bool SecondPhase=>Pattern=="trial" && _actor.Health.Current<=_actor.Health.Maximum*0.5f;
    private EnemyAgent _actor=null!;
    private readonly List<MeshInstance3D> _markers=new();
    private readonly List<ShaderMaterial> _materials=new();
    private readonly List<GroundTelegraphSurface> _surfaces=new();
    private readonly Vector3[] _centers=new Vector3[3],_directions=new Vector3[3];
    private readonly int[] _shapes=new int[3];
    private int _count,_turn;
    private bool _followup;
    private double _vulnerability;
    private int Tier=>_actor.Crowd?.OperationMode.MechanicsTier??0;
    private float _radius;
    private double _remaining=2.5;
    private PackedScene? _summon;
    public override void _Ready()
    {
        _actor=GetParent<EnemyAgent>();
        if(SummonScene!="")_summon=GD.Load<PackedScene>(SummonScene);
        for(int i=0;i<3;i++)
        {
            var mat=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/TombTelegraph.gdshader")};
            var surface=new GroundTelegraphSurface();_surfaces.Add(surface);
            var marker=new MeshInstance3D {Visible=false,Mesh=surface.Mesh,MaterialOverride=mat,
                CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
            _actor.GetTree().CurrentScene.AddChild(marker);_markers.Add(marker);_materials.Add(mat);
        }
    }
    public bool Tick(double delta)
    {
        if(Phase==BossPhase.Ended || !_actor.Alive)return false;
        _vulnerability=Math.Max(0,_vulnerability-delta);
        _remaining-=delta;
        if(Phase==BossPhase.Pursuit)
        {
            if(_remaining>0 || _actor.GlobalPosition.DistanceTo(_actor.Target.GlobalPosition)>TriggerRange
                || !_actor.Navigation.HasLineOfSight(_actor.AimPosition,_actor.Target.AimPosition))return false;
            _followup=false;LockPattern();Phase=BossPhase.Telegraph;_remaining=Windup;
            AudioPresentation.Find(this)?.PlayCue(WarningCue);return true;
        }
        if(Phase==BossPhase.Telegraph)
        {
            for(int i=0;i<_count;i++)_materials[i].SetShaderParameter("progress",1-(float)(_remaining/Windup));
            if(_remaining>0)return true;
            Strike();if(Phase==BossPhase.Ended)return true;
            if(!_followup && Tier>0 && (Pattern is "trial" or "crossfire" or "cleave" || Tier==2 && Pattern=="bolt")) {
                _followup=true;LockPattern();_remaining=Windup;
                Instruction="FOLLOW-UP · MOVE OUT OF THE NEW WARNING";
                return true;
            }
            Phase=BossPhase.Recovery;_remaining=PoolSeconds>0?PoolSeconds:Pattern=="trial"?1.45:0.8;_poolTick=1;return true;
        }
        if(_remaining>0)
        {
            if(PoolSeconds>0 && (_poolTick-=delta)<=0)
            {
                _poolTick=1;var diff=_actor.Target.GlobalPosition-_centers[0];diff.Y=0;
                if(diff.Length()<=_radius && Math.Abs(_actor.Target.GlobalPosition.Y-_centers[0].Y)<.28f)_actor.DealAttack(Damage*.4f,DamageType.Kinetic);
            }
            return true;
        }
        Hide();Phase=BossPhase.Pursuit;_remaining=SecondPhase?Cooldown*0.78:Cooldown;return false;
    }
    private void LockPattern()
    {
        Hide();_count=1;_turn++;
        var actor=_actor.GlobalPosition;var target=_actor.Target.GlobalPosition;
        var direction=target-actor;direction.Y=0;direction=direction.Normalized();
        _centers[0]=target;_directions[0]=direction;_shapes[0]=0;_radius=0.15f;
        Instruction="SPIT  ·  STEP OUT OF THE MARKED GROUND";
        if(Pattern=="bolt")
        { _centers[0]=actor;_shapes[0]=1;_radius=0.95f;Instruction="TARGET LOCK  ·  SIDESTEP THE FIRING LANE"; }
        else if(Pattern=="crossfire")
        {
            _count=2;_radius=0.95f;_shapes[0]=_shapes[1]=1;
            _centers[0]=_centers[1]=target;_directions[0]=direction;_directions[1]=direction.Rotated(Vector3.Up,Mathf.Pi/2);
            Instruction="CROSSFIRE  ·  MOVE INTO A GAP";
        }
        else if(Pattern is "cleave" or "screech" || Pattern=="trial" && _turn%2==1)
        {
            _centers[0]=actor;_shapes[0]=2;_radius=Pattern=="trial"?0.52f:0.38f;
            Instruction=Pattern=="screech"?"SHYRACK CALL  ·  MOVE BEHIND THE SCREECHER":"RAVAGING CLAWS  ·  MOVE BEHIND THE BEAST";
        }
        else if(Pattern=="trial")
        {
            _count=SecondPhase?3:2;_radius=0.22f;
            for(int i=0;i<_count;i++) {_shapes[i]=0;_centers[i]=target+direction.Rotated(Vector3.Up,i*Mathf.Tau/3)*(i==0?0:0.48f);_directions[i]=direction;}
            Instruction=SecondPhase?"ANCIENT FURY  ·  THREE ERUPTIONS":"TOMB ERUPTIONS  ·  KEEP MOVING";
        }
        else if(Pattern=="brood")
        { _centers[0]=actor;_radius=0.18f;Instruction="BROOD CALL  ·  LESSER SLUGS ARE GATHERING"; }
        _radius*=_actor.Crowd?.OperationMode.MechanicRadius??1;
        if(InstructionOverride!="")Instruction=InstructionOverride;
        for(int i=0;i<_count;i++)
        {
            var p=_centers[i];if(_actor.Navigation.Arena.GroundAt(p,out var ground,out _,0.4f,0.7f))p=ground;
            _centers[i]=p;_markers[i].GlobalPosition=p+Vector3.Up*0.006f;
            _markers[i].Rotation=new Vector3(0,Mathf.Atan2(-_directions[i].X,-_directions[i].Z),0);
            _markers[i].Scale=Vector3.One*(_radius*2);_markers[i].Visible=true;
            _surfaces[i].Project(_markers[i],_actor.Navigation.Arena);
            _materials[i].SetShaderParameter("shape",_shapes[i]);_materials[i].SetShaderParameter("impact",0f);
        }
    }
    private void Strike()
    {
        Strikes++;_actor.Visual?.Attack();AudioPresentation.Find(this)?.PlayCue(ImpactCue);
        if(BlasterEffect)_actor.Visual?.FireAt(_actor.GlobalPosition+_directions[0]*_radius+Vector3.Up*_actor.AimHeight);
        bool hit=false;var player=_actor.Target.GlobalPosition;
        for(int i=0;i<_count;i++)
        {
            _materials[i].SetShaderParameter("impact",1f);
            var offset=player-_centers[i];offset.Y=0;float forward=offset.Dot(_directions[i]);
            bool inside=_shapes[i] switch
            {
                1=>Math.Abs(offset.Dot(_directions[i].Rotated(Vector3.Up,Mathf.Pi/2)))<=_radius*.079f && Math.Abs(forward)<=_radius,
                2=>offset.Length()<=_radius && (offset.Length()<0.03f || forward/offset.Length()>=0.55f),
                _=>offset.Length()<=_radius
            };
            hit|=inside && Math.Abs(player.Y-_centers[i].Y)<0.28f;
        }
        // Overlapping lanes are one attack. Applied hits always follow Health -> combat events -> flytext.
        if(hit && _actor.Target.Alive && _actor.Navigation.HasLineOfSight(_actor.AimPosition,_actor.Target.AimPosition))
            {_actor.DealAttack(Damage*(_vulnerability>0?1.25f:1),DamageType.Kinetic);if(Tier>0)_vulnerability=Tier==2?12:8;}
        if(_summon!=null && _actor.Crowd is {} crowd && crowd.Running && _actor.Target.Alive)
            for(int i=0;i<2;i++)crowd.TrySpawn(_summon);
    }
    private void Hide() {foreach(var m in _markers)m.Visible=false;}
    public void End() {Phase=BossPhase.Ended;Hide();}
    public override void _ExitTree() {foreach(var m in _markers)if(GodotObject.IsInstanceValid(m))m.QueueFree();}
}
