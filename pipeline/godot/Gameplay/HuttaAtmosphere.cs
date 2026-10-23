using Godot;
using System.Linq;
namespace SwtorSurvivors;
/// <summary>Hutta-only adaptation of hut_main Area lighting and original cover rocks.</summary>
public partial class HuttaAtmosphere : Node3D
{
    public override async void _Ready()
    {
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        var world=GetParent<GameWorld>();
        // April area.dat Area scheme: front .993,.968,.646; fog .634,.658,.302 -> .802,.574,.158.
        var env=new Godot.Environment {BackgroundMode=Godot.Environment.BGMode.Color,
            BackgroundColor=new Color(.802f,.574f,.158f),AmbientLightSource=Godot.Environment.AmbientSource.Color,
            AmbientLightColor=new Color(.668f,.58f,.32f),AmbientLightEnergy=.65f,
            FogEnabled=true,FogLightColor=new Color(.634f,.658f,.302f),FogDensity=.10f};
        foreach(var old in world.FindChildren("*","WorldEnvironment",true,false).OfType<WorldEnvironment>())old.Environment=env;
        if(!world.FindChildren("*","WorldEnvironment",true,false).Any())AddChild(new WorldEnvironment {Environment=env});
        var sky=GD.Load<PackedScene>("res://Assets/Maps/hutta/Enemies/sky.glb").Instantiate<Node3D>();
        AddChild(sky);sky.GlobalPosition=world.Player.GlobalPosition;
        sky.SetMeta("source_model","/resources/art/static/area/hut_hutta/sky/hut_skyscene01.gr2");
    }
}
