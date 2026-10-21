using Godot;
namespace SwtorSurvivors.Frontend;

/// <summary>Reusable page surface. The owning flow supplies a shared session and catalog.</summary>
public partial class SelectionPage : Control
{
    [Export] public string StepId { get; set; } = "Faction";
}
