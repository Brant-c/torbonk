using Godot;
using System.Linq;
namespace SwtorSurvivors;

// Ground-owned protection: membership is evaluated at the moment damage arrives.
public partial class BallisticShieldField : Node3D
{
    public AutomaticAbility Source {get;set;}=null!;
    public double Remaining {get;set;}
    public float Radius {get;set;}
    public bool Active=>!IsQueuedForDeletion() && Remaining>0 && GodotObject.IsInstanceValid(Source) &&
        Source.IsInsideTree() && Source.Unlocked && Source.CombatEnabled && Source.Caster.Alive &&
        Source.Presentation.Actor.SniperCoverActive;
    public override void _Ready()
    {
        AddToGroup("ballistic_fields");
        var material=new StandardMaterial3D {
            AlbedoTexture=GD.Load<Texture2D>("res://Assets/Agent/proj_hexgrid_tile.png"),
            AlbedoColor=new Color(.25f,.55f,1,.22f),Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,CullMode=BaseMaterial3D.CullModeEnum.Disabled};
        AddChild(new MeshInstance3D {Name="BallisticDome",Mesh=new SphereMesh {Radius=Radius,Height=Radius,IsHemisphere=true},
            MaterialOverride=material,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});
    }
    public override void _PhysicsProcess(double delta)
    {
        Remaining-=delta;
        if(!Active)QueueFree();
    }
    public static bool Protects(Combatant recipient)=>recipient.IsInsideTree() &&
        recipient.GetTree().GetNodesInGroup("ballistic_fields").OfType<BallisticShieldField>().Any(field=>
            field.Active && field.Source.Caster.Team!=CombatTeam.Neutral && recipient.Team==field.Source.Caster.Team &&
            recipient.GlobalPosition.DistanceSquaredTo(field.GlobalPosition)<=field.Radius*field.Radius);
}