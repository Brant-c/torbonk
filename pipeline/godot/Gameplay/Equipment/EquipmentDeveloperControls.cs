using Godot;
using System.Linq;

namespace SwtorSurvivors.Equipment;

/// <summary>Development-only gear inspection. No unlock or persistence writes.</summary>
public partial class EquipmentDeveloperControls : CanvasLayer
{
    public CharacterAppearanceAssembly Assembly { get; set; } = null!;
    public bool Expanded { get; set; }
    public Button OutfitButton { get; private set; } = null!;
    public Button SaberButton { get; private set; } = null!;
    private Label _label=null!;
    private Control _root=null!,_details=null!;
    private string _error="";
    public override void _Ready()
    {
        Layer=5;ProcessMode=ProcessModeEnum.Always;
        _root=new Control { Theme=UI.SwtorSkin.MakeTheme(),MouseFilter=Control.MouseFilterEnum.Ignore };
        AddChild(_root);_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var dock=new VBoxContainer { MouseFilter=Control.MouseFilterEnum.Ignore };
        _root.AddChild(dock);dock.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        dock.OffsetLeft=-312;dock.OffsetRight=-16;dock.OffsetTop=52;
        var toggle=new UI.SwtorButton { Text="GEAR REVIEW [F5]",CustomMinimumSize=new Vector2(296,32),FocusMode=Control.FocusModeEnum.None };
        dock.AddChild(toggle);toggle.Pressed+=()=> { Expanded=!Expanded;Refresh(); };
        var panel=new UI.SwtorPanel();dock.AddChild(panel);_details=panel;
        var controls=new VBoxContainer();controls.AddThemeConstantOverride("separation",6);panel.AddChild(controls);
        controls.AddChild(UI.SwtorSkin.Text("DEVELOPMENT · APPEARANCE",14,true));
        _label=UI.SwtorSkin.Text("",13);_label.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        _label.CustomMinimumSize=new Vector2(268,44);controls.AddChild(_label);
        Button Action(string label,Key key)
        {
            var button=new UI.SwtorButton { Text=label,CustomMinimumSize=new Vector2(268,30),FocusMode=Control.FocusModeEnum.None };
            controls.AddChild(button);button.Pressed+=()=>Apply(key);return button;
        }
        OutfitButton=Action("Switch outfit [F6]",Key.F6);
        SaberButton=Action("Remove saber [F7]",Key.F7);
        Action("Toggle chest cosmetic [F8]",Key.F8);
        Assembly.Loadout.Changed+=OnLoadoutChanged;Refresh();
    }
    private void OnLoadoutChanged(EquipmentSnapshot _) => Refresh();
    public override void _ExitTree() => Assembly.Loadout.Changed-=OnLoadoutChanged;
    public override void _Process(double delta) => _root.Visible=!GetTree().Paused;
    private bool Alternate => Assembly.Loadout.Current.Cosmetics.Count>0;
    private void Refresh()
    {
        _details.Visible=Expanded;
        _label.Text=_error!="" ? "Rejected: "+_error : Alternate ? "SS A04 cosmetic / mixed gear" : "Level 1 Inquisitor outfit";
        SaberButton.Text=Assembly.Loadout.Current.Items.ContainsKey("main_hand") ? "Remove saber [F7]" : "Restore saber [F7]";
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if(GetTree().Paused || e is not InputEventKey { Pressed:true,Echo:false } key)return;
        if(key.PhysicalKeycode==Key.F5) { Expanded=!Expanded;Refresh();GetViewport().SetInputAsHandled();return; }
        if(key.PhysicalKeycode is not (Key.F6 or Key.F7 or Key.F8))return;
        Apply(key.PhysicalKeycode);GetViewport().SetInputAsHandled();
    }
    private void Apply(Key key)
    {
        if(GetTree().Paused)return;
        string error="";bool ok;
        switch(key)
        {
            case Key.F6:
                // Cosmetic-only preset change keeps the currently equipped functional items (and unequipped saber) intact.
                var preset=Assembly.Loadout.Catalog.Presets.Single(p=>p.Id==(Alternate ? "inquisitor_level1" : "sorcerer_ss_a04"));
                ok=Assembly.Loadout.TryApply(Assembly.Loadout.Current with { Cosmetics=new(preset.Cosmetics) },out error);
                break;
            case Key.F7:
                ok=Assembly.Loadout.Current.Items.ContainsKey("main_hand") ? Assembly.Loadout.TryUnequip("main_hand",out error)
                    : Assembly.Loadout.TryEquip("main_hand","d1.red_saber",out error);break;
            case Key.F8:
                ok=Assembly.Loadout.TrySetCosmetic("chest",Assembly.Loadout.Current.Cosmetics.ContainsKey("chest") ? null : "ss_a04.chest",out error);break;
            default:return;
        }
        _error=ok ? "" : error;Expanded=true;Refresh();
    }
}
