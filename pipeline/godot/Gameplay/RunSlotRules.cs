using Godot;
namespace SwtorSurvivors;
[GlobalClass]
public partial class RunSlotRules : Resource
{
    [Export(PropertyHint.Range,"1,12,1")] public int OffensiveSlots {get;set;}=4;
    [Export(PropertyHint.Range,"0,12,1")] public int UtilitySlots {get;set;}=2;
    [Export(PropertyHint.Range,"1,6,1")] public int ArtifactSlots {get;set;}=3;
}
