using Godot;

namespace SwtorSurvivors.UI;

public partial class SwtorButton : Button
{
    public SwtorButton() { CustomMinimumSize=new Vector2(140,38); MouseDefaultCursorShape=CursorShape.PointingHand; }
    public override void _Ready()
    {
        MouseEntered += () => { if (!Disabled) AudioPresentation.Find(this)?.PlayCue("hover"); };
        FocusEntered += () => { if (!Disabled) AudioPresentation.Find(this)?.PlayCue("hover"); };
        ButtonDown += () => { if (!Disabled) AudioPresentation.Find(this)?.PlayCue("click"); };
    }
    public override GodotObject _MakeCustomTooltip(string forText) => new SwtorTooltip(forText);
}
