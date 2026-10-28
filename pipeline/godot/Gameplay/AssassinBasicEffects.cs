using Godot;
namespace SwtorSurvivors;
public partial class D3AbilityPresentation
{
    public int AssassinTargetImpacts {get;private set;}
    private bool ImpactAssassin()
    {
        if(GetParent() is not GameWorld world || world.RunConfiguration.AdvancedClassId!="assassin" || Definition.Id!="discharge")return false;
        Play(Definition.ImpactAudio,Actor.GlobalPosition,3);
        // The area burst belongs on the floor. Each enemy gets its electrical contact separately.
        Ring(Actor.GlobalPosition+Vector3.Up*.006f,"res://Assets/Assassin/fx_shockwave_ring.png",_radius*2);
        return true;
    }
    private bool AssassinTargetImpact(Combatant target)
    {
        if(GetParent() is not GameWorld world || world.RunConfiguration.AdvancedClassId!="assassin" || Definition.Id is not ("lacerate" or "discharge"))return false;
        AssassinTargetImpacts++;
        var at=target.AimPosition;
        if(Definition.Id=="lacerate") {
            Burst(at,"res://Assets/Assassin/bw_fx_2x2_sparknglow_01.png",.085f);
            Burst(at,"res://Assets/Assassin/bw_flare_gray.png",.055f);
        } else {
            Burst(at,"res://Assets/D3/bw_2x2_electricity_multi_01.png",.16f);
            Burst(at,"res://Assets/Assassin/2x2_elec_blast_ring_alpha.png",.13f);
        }
        return true;
    }
}
