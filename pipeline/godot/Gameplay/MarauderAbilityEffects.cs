namespace SwtorSurvivors;
public partial class D3AbilityPresentation
{
 private string _marauderAura="";
 private void AdvanceMarauderEffects(double delta)=>AdvanceWarriorEffects(delta);
 private void BeginMarauder(Combatant target)=>BeginWarrior(target);
 private bool ImpactMarauder(Godot.Vector3 at)=>ImpactWarrior(at);
}