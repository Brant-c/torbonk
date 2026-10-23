using Godot;
using SwtorSurvivors.UI;
namespace SwtorSurvivors.Frontend;

public partial class DestinationChoice : SwtorButton
{
    public MapDefinition Map {get;set;}=null!;
    private bool _selected;
    public bool Selected {get=>_selected;set{_selected=value;SetMeta("selected",value);Refresh();}}
    private Control _visual=null!;
    private ColorRect _top=null!,_bottom=null!;
    private bool _hover,_focus,_keyboard;
    private Tween? _tween;
    public override void _Ready()
    {
        CustomMinimumSize=new Vector2(310,98);AccessibilityName=Map.Name;AccessibilityDescription=Map.Description;
        foreach(string state in new[]{"normal","hover","pressed","hover_pressed","disabled","focus"})AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        _visual=new Control {Name="DestinationVisual",Size=new Vector2(310,98),PivotOffset=new Vector2(155,49),MouseFilter=MouseFilterEnum.Ignore};AddChild(_visual);
        var panel=new Panel {Size=_visual.Size,MouseFilter=MouseFilterEnum.Ignore};panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=new Color(.01f,.025f,.045f,.38f),CornerRadiusTopLeft=18,CornerRadiusTopRight=18,CornerRadiusBottomLeft=18,CornerRadiusBottomRight=18});_visual.AddChild(panel);
        ColorRect Rule(float y){var r=new ColorRect {Position=new Vector2(50,y),Size=new Vector2(210,2),MouseFilter=MouseFilterEnum.Ignore};_visual.AddChild(r);return r;}
        _top=Rule(0);_bottom=Rule(96);
        var planet=new TextureRect {Name="PlanetIcon",ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=FrontendSkin.Texture(Map.Surface),Position=new Vector2(20,22),Size=new Vector2(52,52),StretchMode=TextureRect.StretchModeEnum.Scale,Material=new ShaderMaterial {Shader=GD.Load<Shader>(FrontendSkin.Assets+"planet.gdshader")},MouseFilter=MouseFilterEnum.Ignore};_visual.AddChild(planet);
        void Line(string text,int size,float y,Color color,bool bold=false){var l=FrontendSkin.Label(text,size);l.Position=new Vector2(90,y);l.Size=new Vector2(220,38);l.AddThemeColorOverride("font_color",color);if(bold)l.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));_visual.AddChild(l);}
        Line(Map.Name,25,15,new Color("e4edf2"),true);Line(Map.Region,19,53,new Color("8cabbf"));
        MouseEntered+=()=>{_hover=true;Refresh();};MouseExited+=()=>{_hover=false;Refresh();};FocusEntered+=()=>{_focus=true;Refresh();};FocusExited+=()=>{_focus=false;Refresh();};
        base._Ready();Refresh();
    }
    public override void _Input(InputEvent e)
    {
        bool next=_keyboard;
        if(e is InputEventMouseMotion or InputEventMouseButton)next=false;
        else if(e is InputEventKey {Pressed:true} key && key.Keycode is Key.Tab or Key.Up or Key.Down or Key.Left or Key.Right)next=true;
        if(next!=_keyboard){_keyboard=next;Refresh();}
    }
    private void Refresh()
    {
        if(_visual==null)return;
        bool active=Selected || (_focus && _keyboard) || _hover;
        _tween?.Kill();_tween?.Dispose();_tween=CreateTween().SetParallel();
        _tween.TweenProperty(_visual,"scale",Vector2.One*(_hover && !Disabled?1.025f:1f),.14).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        var accent=active?new Color("a2e6ff"):new Color(.28f,.7f,1,.45f);
        _tween.TweenProperty(_top,"color",accent,.14);_tween.TweenProperty(_bottom,"color",accent,.14);
    }
    public override void _ExitTree(){_tween?.Kill();_tween?.Dispose();_tween=null;}
}
