using Godot;
using System;
namespace SwtorSurvivors;
public partial class PowertechOilSlick : Node3D
{
    public AutomaticAbility Source=null!;
    private double _remaining=8;
    private StormAreaIndicator _ground=null!;
    public int ProjectedVertexCount=>_ground?.ProjectedVertexCount??0;
    public override void _Ready()
    {
        var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/PowertechOilSlick.gdshader")};
        material.SetShaderParameter("oil_detail",GD.Load<Texture2D>("res://Assets/Powertech/proj_oilpuddle_wrap.png"));
        material.SetShaderParameter("oil_border",GD.Load<Texture2D>("res://Assets/Powertech/proj_oilpuddle_border.png"));
        _ground=new StormAreaIndicator {Name="OilTerrainProjection",ProjectionMaterial=material};AddChild(_ground);
    }
    public override void _PhysicsProcess(double delta)
    {
        _remaining-=delta;
        if(_remaining<=0 || !Source.Unlocked || !Source.Caster.Alive){QueueFree();return;}
        // Reuses terrain-only rays, slope-following vertices, hole/cliff guards and surface lift.
        // Position has been assigned by the activation hook before this first physics update.
        _ground.UpdateArea(GlobalPosition,.34f);
        foreach(var t in Source.Targeting.InArea(GlobalPosition,.34f,.34f,true))
            if(t is EnemyAgent e){e.Slow(.55f,.2f);e.OilRemaining=.2;}
    }
}
