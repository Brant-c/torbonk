using Godot;
using SwtorSurvivors.UI;
namespace SwtorSurvivors.Frontend;

/// <summary>Clickable allegiance group assembled from original insignias and class icons.</summary>
public partial class FactionChoice : SwtorButton
{
    public FactionDefinition Faction {get;set;}=null!;
    public ClassDefinition[] Classes {get;set;}=[];
    public bool Selected {get;set;}
    private Control _visual=null!;
    private TextureRect _emblem=null!;
    private Label _cue=null!;
    private bool _hover,_focus,_keyboardNavigation;
    public bool Emphasized => Selected || _hover || (_focus && _keyboardNavigation);
    private Tween? _emphasis;
    public override void _Ready()
    {
        CustomMinimumSize=new Vector2(540,570);
        AccessibilityName=Faction.Name;
        AccessibilityDescription=Faction.Description;
        SetMeta("selected",Selected);
        // The whole group is a real button; child artwork never intercepts input.
        foreach(string state in new[]{"normal","hover","pressed","hover_pressed","disabled","focus"})
            AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        // Animate the artwork as a group while keeping its pointer target fixed.
        _visual=new Control {Name="FactionVisual",Size=new Vector2(540,570),PivotOffset=new Vector2(270,285),MouseFilter=MouseFilterEnum.Ignore};
        AddChild(_visual);
        var backing=new Panel {Name="FactionBackplate",Position=new Vector2(-8,-8),Size=new Vector2(556,590),MouseFilter=MouseFilterEnum.Ignore};
        backing.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=new Color(.01f,.025f,.045f,.38f),
            CornerRadiusTopLeft=18,CornerRadiusTopRight=18,CornerRadiusBottomLeft=18,CornerRadiusBottomRight=18});
        _visual.AddChild(backing);
        var accent=Faction.Id=="republic" ? new Color(.28f,.7f,1,.45f) : new Color(1,.25f,.22f,.45f);
        foreach(float y in new[]{0f,588f})backing.AddChild(new ColorRect {Position=new Vector2(56,y),Size=new Vector2(444,2),Color=accent,MouseFilter=MouseFilterEnum.Ignore});
        _emblem=FrontendSkin.Art(Faction.Emblem,new Vector2(290,283));
        _emblem.Position=new Vector2(125,25);_emblem.PivotOffset=_emblem.Size/2;_visual.AddChild(_emblem);
        Label Line(string value,int size,float top,bool bold=false)
        {
            var label=FrontendSkin.Label(value,size);label.Position=new Vector2(0,top);label.Size=new Vector2(540,44);
            label.HorizontalAlignment=HorizontalAlignment.Center;
            if(bold)label.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));
            _visual.AddChild(label);return label;
        }
        Line(Faction.Name,34,333,true);
        Line(Faction.Id=="republic" ? "Freedom. Diplomacy. The Jedi Order." : "Power. Order. The Sith Empire.",23,382);
        float start=(540-(Classes.Length*58+(Classes.Length-1)*18))/2f;
        for(int i=0;i<Classes.Length;i++)
        {
            var icon=new TextureRect {ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
                Texture=FrontendSkin.Texture(Classes[i].Icon),CustomMinimumSize=new Vector2(58,58),Size=new Vector2(58,58),
                StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore};
            icon.Name="ClassIcon_"+Classes[i].Id;icon.Size=new Vector2(58,58);
            icon.Position=new Vector2(start+i*76,440);_visual.AddChild(icon);
        }
        _cue=Line("Select allegiance",23,535);
        MouseEntered+=()=>{_hover=true;RefreshEmphasis();};
        MouseExited+=()=>{_hover=false;RefreshEmphasis();};
        FocusEntered+=()=>{_focus=true;RefreshEmphasis();};
        FocusExited+=()=>{_focus=false;RefreshEmphasis();};
        base._Ready();RefreshEmphasis();
    }
    public override void _ExitTree()
    {
        _emphasis?.Kill();_emphasis?.Dispose();_emphasis=null;
    }
    public override void _Input(InputEvent e)
    {
        bool keyboard=_keyboardNavigation;
        if(e is InputEventMouseMotion or InputEventMouseButton)keyboard=false;
        else if(e is InputEventKey {Pressed:true} key && key.Keycode is Key.Tab or Key.Left or Key.Right or Key.Up or Key.Down)keyboard=true;
        if(keyboard==_keyboardNavigation)return;
        _keyboardNavigation=keyboard;RefreshEmphasis();
    }
    private void RefreshEmphasis()
    {
        bool active=Emphasized;
        _emphasis?.Kill();_emphasis?.Dispose();_emphasis=CreateTween().SetParallel();
        _emphasis.TweenProperty(_visual,"scale",Vector2.One*(_hover?1.025f:1f),.14)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _emphasis.TweenProperty(_emblem,"modulate",active?Colors.White:new Color(.87f,.87f,.87f),.14);
        _cue.AddThemeColorOverride("font_color",active ? (Faction.Id=="empire" ? new Color("ff9690") : new Color("a2e6ff")) : Faction.Id=="empire"?new Color(1,.25f,.22f):SwtorSkin.Blue);
        _cue.Text=Selected ? "Allegiance selected" : active ? "Select "+Faction.Name : "Select allegiance";
    }
}
