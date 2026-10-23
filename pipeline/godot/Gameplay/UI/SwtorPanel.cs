using Godot;

namespace SwtorSurvivors.UI;

public partial class SwtorPanel : PanelContainer
{
    public SwtorPanel()
    {
        AddThemeStyleboxOverride("panel",SwtorSkin.MenuFrame());
    }
}
