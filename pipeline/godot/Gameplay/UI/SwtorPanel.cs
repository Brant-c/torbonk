using Godot;

namespace SwtorSurvivors.UI;

public partial class SwtorPanel : PanelContainer
{
    public SwtorPanel()
    {
        AddThemeStyleboxOverride("panel",SwtorSkin.Box("bwaui_standard_window_1",12,14,12,12));
    }
}
