using Godot;

namespace SwtorSurvivors.UI;

public partial class SwtorTooltip : PanelContainer
{
    public SwtorTooltip(string text)
    {
        Theme=SwtorSkin.MakeTheme();
        AddThemeStyleboxOverride("panel",SwtorSkin.Box("bwaui_tooltip_1",8,12,8,12));
        var label=SwtorSkin.Text(text,16);
        label.AutowrapMode=TextServer.AutowrapMode.WordSmart; label.CustomMinimumSize=new Vector2(290,0);
        AddChild(label);
    }
}
