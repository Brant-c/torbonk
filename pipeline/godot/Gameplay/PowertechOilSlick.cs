using Godot;
using System;
using System.Linq;
namespace SwtorSurvivors;
public partial class PowertechOilSlick : Node3D
{
    public AutomaticAbility Source=null!;
    private double _remaining=8;
    private double _burning,_burnTick,_flameSpawn;
    private StormAreaIndicator _ground=null!;
    private readonly RandomNumberGenerator _random=new();
    public int ProjectedVertexCount=>_ground?.ProjectedVertexCount??0;
    public bool Burning=>_burning>0;
    // The base .32 slick keeps its original .34 footprint; Shield Tech and run cards scale it.
    public float Radius=>Math.Max(.05f,(Source?.Radius??.32f)+.02f);
    private Talents.TalentRuntime? Runtime=>Source?.Build?.Pregame;
    public bool Contains(EnemyAgent enemy)=>enemy.GlobalPosition.DistanceSquaredTo(GlobalPosition)<=Radius*Radius;
    public override void _Ready()
    {
        _remaining=Runtime?.OilSlickSeconds??8;
        var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/PowertechOilSlick.gdshader")};
        material.SetShaderParameter("oil_detail",GD.Load<Texture2D>("res://Assets/Powertech/proj_oilpuddle_wrap.png"));
        material.SetShaderParameter("oil_border",GD.Load<Texture2D>("res://Assets/Powertech/proj_oilpuddle_border.png"));
        _ground=new StormAreaIndicator {Name="OilTerrainProjection",ProjectionMaterial=material};AddChild(_ground);
    }
    /// <summary>A fire hit on a soaked enemy sets the whole slick alight; reignition refreshes, never stacks.</summary>
    public void Ignite()
    {
        if(_remaining<=0)return;
        bool fresh=_burning<=0;
        _burning=Runtime?.OilFireSeconds??6;
        _remaining=Math.Max(_remaining,_burning);
        if(fresh)_burnTick=0;
    }
    public override void _PhysicsProcess(double delta)
    {
        _remaining-=delta;
        if(_remaining<=0 || !Source.Unlocked || !Source.Caster.Alive){QueueFree();return;}
        // Reuses terrain-only rays, slope-following vertices, hole/cliff guards and surface lift.
        // Position has been assigned by the activation hook before this first physics update.
        _ground.UpdateArea(GlobalPosition,Radius);
        var inside=Source.Targeting.InArea(GlobalPosition,Radius,Radius,true).ToArray();
        foreach(var t in inside)
            if(t is EnemyAgent e){e.Slow(.55f,.2f);e.OilRemaining=.2;}
        if(_burning<=0)return;
        _burning-=delta;_burnTick-=delta;_flameSpawn-=delta;
        while(_burnTick<=0){
            _burnTick+=.5;
            float amount=Runtime?.OilFireDamage??3;
            foreach(var t in inside.Where(t=>t.Alive))Source.ApplyPeriodicHit(t,amount);
        }
        while(_flameSpawn<=0){_flameSpawn+=.08;SpawnFlame();}
    }
    private void SpawnFlame()
    {
        // Original April 4x4 flame lick atlas, billboarded over the burning slick.
        var texture=GD.Load<Texture2D>("res://Assets/Powertech/4x4_flame_lick_02_bw.png");
        var sprite=new Sprite3D {Texture=texture,Hframes=4,Vframes=4,PixelSize=.11f/(texture.GetWidth()/4f),Shaded=false,
            Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Modulate=new Color(1,.6f,.25f,.9f)};
        float angle=_random.RandfRange(0,Mathf.Tau),distance=Mathf.Sqrt(_random.Randf())*Radius*.9f;
        AddChild(sprite);sprite.Position=new Vector3(Mathf.Cos(angle)*distance,.04f,Mathf.Sin(angle)*distance);
        var tween=sprite.CreateTween();
        tween.TweenMethod(Callable.From<float>(f=>{if(GodotObject.IsInstanceValid(sprite)){sprite.Frame=Math.Min(15,(int)(f*16));sprite.Modulate=new Color(1,.6f,.25f,.9f*(1-f));}}),0f,1f,.6);
        tween.TweenCallback(Callable.From(sprite.QueueFree));
    }
}
