using Godot;
using System;

namespace SwtorSurvivors;

/// <summary>Bounded atlas adapter for env_kor_torch; original flame/smoke artwork, persistent billboards.</summary>
public partial class OriginalTorch : Node3D
{
    private Sprite3D _fire=null!,_smoke=null!;
    private double _clock;
    public override void _Ready()
    {
        _fire=new Sprite3D {Texture=GD.Load<Texture2D>("res://Assets/Korriban/D4/5x5_flame_lick_04_bw.png"),
            Hframes=5,Vframes=5,PixelSize=.0022f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate=new Color(1,.70f,.34f,.85f),Position=Vector3.Up*.64f};AddChild(_fire);
        _smoke=new Sprite3D {Texture=GD.Load<Texture2D>("res://Assets/Korriban/D4/smoke_thin.png"),
            PixelSize=.0004f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Modulate=new Color(.44f,.44f,.44f,.12f)};AddChild(_smoke);
    }
    public override void _Process(double delta)
    {
        _clock+=delta;_fire.Frame=(int)(_clock*10)%25;
        float phase=(float)(_clock%4)/4;_smoke.Position=Vector3.Up*(.7f+phase*.3f);
        _smoke.Modulate=new Color(.44f,.44f,.44f,Mathf.Sin(phase*Mathf.Pi)*.12f);
    }
}
