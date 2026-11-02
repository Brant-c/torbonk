using Godot;
using System;
using System.Linq;
using System.Text.Json;
namespace SwtorSurvivors;
/// <summary>Diversion's recovered April FXSpecs: sp_diversion_grenade_01 (thrown flash-bang model + prep sound),
/// sp_diversion_impact_01 (ring burst, sparks, flashes, smoke cloud, blast/smoke sounds) and sp_diversion_target_01 (smoke on each affected enemy).</summary>
public partial class DiversionEffect : Node3D
{
    private const string Fx="res://Assets/Agent/diversion-fx-";
    private const float FxScale=.2f;
    private double _age,_life;
    private Node3D _grenade=null!;
    private Vector3 _from,_to;
    private bool _impact;
    private EternityVaultWeather[] _bursts=Array.Empty<EternityVaultWeather>();
    private (EternityVaultWeather fx,double stop)[] _timed=Array.Empty<(EternityVaultWeather,double)>();
    private EternityVaultWeather? _cloud;
    private StormAreaIndicator? _ground;
    private ShaderMaterial? _groundMaterial;
    public const float ZoneRadius=.8f;
    public static float Radius=>ZoneRadius;
    private AudioStreamPlayer3D _voice=null!;
    public const double ThrowSeconds=.3;
    public static void Spawn(GameWorld world,Vector3 at,float seconds)
    {
        var e=new DiversionEffect {Name="DiversionEffect",_life=seconds,_world=world};world.AddChild(e);e.GlobalPosition=at;e.Build(world);
    }
    private static EternityVaultWeather NewFx(Node parent,string name)
    {
        var fx=new EternityVaultWeather {Sources=new[]{Fx+name+".json"},UnitScale=FxScale,Active=false};parent.AddChild(fx);return fx;
    }
    public static void Cue(Node host,string name,bool loop=false,float db=-6)
    {
        using var audio=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Agent/audio.json"));var root=audio.RootElement;
        if(!root.GetProperty("events").TryGetProperty(name,out var id) || !root.GetProperty("graphs").TryGetProperty(id.GetString()!,out var graph))return;
        var media=graph.GetProperty("media");if(media.GetArrayLength()==0)return;
        var stream=GD.Load<AudioStreamWav>("res://Assets/Agent/Audio/"+media[0].GetInt32()+".wav");
        if(loop){stream=(AudioStreamWav)stream.Duplicate();stream.LoopMode=AudioStreamWav.LoopModeEnum.Forward;stream.LoopBegin=0;stream.LoopEnd=(int)(stream.GetLength()*stream.MixRate);}
        var player=new AudioStreamPlayer3D {Stream=stream,Bus="B7Combat",VolumeDb=db,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};
        host.AddChild(player);AudioPresentation.Find(host)?.RegisterCombatVoice(player);player.Play();
        if(!loop)player.Finished+=player.QueueFree;
    }
    private void Build(GameWorld world)
    {
        _to=GlobalPosition+Vector3.Up*.02f;_from=GlobalPosition+Vector3.Up*.22f;
        _grenade=GD.Load<PackedScene>("res://Assets/Agent/sp_flash_bang_01.glb").Instantiate<Node3D>();AddChild(_grenade);AgentStatusVisual.Fit(_grenade,.035f);_grenade.GlobalPosition=_from;
        Cue(this,"Play_ability_agent_escape_prep");
        _bursts=new[]{"sp_diversion_impact_01-stop035","sp_diversion_impact_01-stop045"}.Select(n=>NewFx(this,n)).ToArray();
        _timed=new[]{(NewFx(this,"sp_diversion_impact_01-stop100"),1.0)};
        _cloud=NewFx(this,"sp_diversion_impact_01-run");
    }
    public override void _Process(double delta)
    {
        _age+=delta;
        if(!_impact){
            float t=(float)Mathf.Clamp(_age/ThrowSeconds,0,1);_grenade.GlobalPosition=_from.Lerp(_to,t*t);_grenade.RotateY((float)delta*14);
            if(_age<ThrowSeconds)return;
            _impact=true;_grenade.QueueFree();
            foreach(var fx in _bursts){fx.Active=true;fx.Restart();}
            foreach(var (fx,_) in _timed){fx.Active=true;fx.Restart();}
            _cloud!.Active=true;_cloud.Restart();
            _groundMaterial=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/DiversionSmoke.gdshader")};
            _groundMaterial.SetShaderParameter("smoke",GD.Load<Texture2D>("res://Assets/Agent/Diversion/2x2volumesmokegradient_blur2.png"));
            _groundMaterial.SetShaderParameter("ring",GD.Load<Texture2D>("res://Assets/Agent/Diversion/blast_ring_01.png"));
            _ground=new StormAreaIndicator {Name="DiversionGround",ProjectionMaterial=_groundMaterial};AddChild(_ground);_ground.UpdateArea(GlobalPosition,ZoneRadius);
            Cue(this,"Play_ability_agent_escape_blast");Cue(this,"Play_ability_agent_escape_smoke");
        }
        double since=_age-ThrowSeconds;
        _bursts[0].Active=since<.35;_bursts[1].Active=since<.45;
        foreach(var (fx,stop) in _timed)fx.Active=since<stop;
        _groundMaterial?.SetShaderParameter("effect_time",(float)since);
        _groundMaterial?.SetShaderParameter("fade",(float)Mathf.Clamp(Mathf.Min(since/.5,(_life-since)/1.0),0,1));
        if(since>=_life-.5)_cloud!.Active=false;
        if(since>=_life+3)QueueFree();
    }
    /// <summary>Like an Oil Slick: enemies standing in the smoke cannot hit you; leaving it ends the effect shortly after.</summary>
    public override void _PhysicsProcess(double delta)
    {
        if(!_impact || _age-ThrowSeconds>=_life || !GodotObject.IsInstanceValid(_world))return;
        foreach(var t in _world.Targeting.InArea(GlobalPosition,ZoneRadius,ZoneRadius,true))
            if(t is EnemyAgent e && e.Alive){e.DiversionRemaining=Math.Max(e.DiversionRemaining,.6);AttachTarget(e);}
    }
    private GameWorld _world=null!;
    /// <summary>sp_diversion_target_01 follows an affected enemy for as long as its debuff lasts.</summary>
    public static void AttachTarget(EnemyAgent enemy)
    {
        if(enemy.GetNodeOrNull("DiversionTarget")!=null)return;
        var node=new DiversionTarget {Name="DiversionTarget",Enemy=enemy};enemy.AddChild(node);
    }
}
public partial class DiversionTarget : Node3D
{
    public EnemyAgent Enemy=null!;
    private EternityVaultWeather _fx=null!;
    private AudioStreamPlayer3D? _loop;
    public override void _Ready()
    {
        Position=Vector3.Up*.08f;
        _fx=new EternityVaultWeather {Sources=new[]{"res://Assets/Agent/diversion-fx-sp_diversion_target_01-run.json"},UnitScale=.2f,Active=true};AddChild(_fx);
        DiversionEffect.Cue(this,"Play_ability_agent_diversion_targetloop",true,-14);
        _loop=GetChildren().OfType<AudioStreamPlayer3D>().FirstOrDefault();
    }
    public override void _Process(double delta)
    {
        if(GodotObject.IsInstanceValid(Enemy) && Enemy.Alive && Enemy.DiversionRemaining>0)return;
        _loop?.Stop();_fx.Active=false;
        if(GetChildren().OfType<EternityVaultWeather>().All(f=>!f.Active))
        {Timer(2.5);}
    }
    private double _end=-1;
    private void Timer(double seconds){if(_end<0)_end=Time.GetTicksMsec()/1000.0+seconds;else if(Time.GetTicksMsec()/1000.0>=_end)QueueFree();}
}
