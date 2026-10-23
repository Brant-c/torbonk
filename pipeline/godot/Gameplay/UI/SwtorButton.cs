using Godot;

namespace SwtorSurvivors.UI;

public partial class SwtorButton : Button
{
    public SwtorButton() { CustomMinimumSize=new Vector2(140,38); MouseDefaultCursorShape=CursorShape.PointingHand; }
    public override void _Ready()
    {
        if (!string.IsNullOrEmpty(Text))
        {
            if(Icon==null && SwtorSkin.ActionIcon(Text) is {} icon) Icon=SwtorSkin.Slice(icon);
            ExpandIcon=true;AddThemeConstantOverride("icon_max_width",28);
            AddThemeConstantOverride("h_separation",14);
            Alignment=HorizontalAlignment.Left;IconAlignment=HorizontalAlignment.Left;
        }
        MouseEntered += () => { if (!Disabled) AudioPresentation.Find(this)?.PlayCue("hover"); };
        FocusEntered += () => { if (!Disabled) AudioPresentation.Find(this)?.PlayCue("hover"); };
        ButtonDown += () => { if (!Disabled) AudioPresentation.Find(this)?.PlayCue("click"); };
    }
    public override GodotObject _MakeCustomTooltip(string forText) => string.IsNullOrWhiteSpace(forText) ? null! : new SwtorTooltip(forText);
}
