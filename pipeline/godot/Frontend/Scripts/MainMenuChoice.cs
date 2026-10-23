using Godot;
using SwtorSurvivors.UI;
namespace SwtorSurvivors.Frontend;

/// <summary>Allegiance-style menu entry with original, modestly sized UI artwork.</summary>
public partial class MainMenuChoice : SwtorButton
{
    public string Description {get;set;}="";
    public string IconSlice {get;set;}="";
    private Control _visual=null!;
    private ColorRect _top=null!,_bottom=null!;
    private bool _hover,_focus,_keyboard;
    private Tween? _tween;
    public override void _Ready()
    {
        CustomMinimumSize=new Vector2(580,140);
        AccessibilityName=Text;AccessibilityDescription=Description;
        foreach(string state in new[]{"normal","hover","pressed","hover_pressed","disabled","focus"})
            AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        foreach(string state in new[]{"font_color","font_hover_color","font_pressed_color","font_hover_pressed_color","font_disabled_color","font_focus_color","font_outline_color"})
            AddThemeColorOverride(state,Colors.Transparent);
        _visual=new Control {Name="MenuVisual",Size=new Vector2(580,140),PivotOffset=new Vector2(290,70),MouseFilter=MouseFilterEnum.Ignore};AddChild(_visual);
        var panel=new Panel {Size=_visual.Size,MouseFilter=MouseFilterEnum.Ignore};
        panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=new Color(.01f,.025f,.045f,.38f),CornerRadiusTopLeft=18,CornerRadiusTopRight=18,CornerRadiusBottomLeft=18,CornerRadiusBottomRight=18});_visual.AddChild(panel);
        ColorRect Rule(float y){var r=new ColorRect {Position=new Vector2(50,y),Size=new Vector2(480,2),Color=new Color(.28f,.7f,1,.45f),MouseFilter=MouseFilterEnum.Ignore};_visual.AddChild(r);return r;}
        _top=Rule(0);_bottom=Rule(138);
        var icon=new TextureRect {Name="MenuIcon",ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=SwtorSkin.Slice(IconSlice),Position=new Vector2(32,51),Size=new Vector2(38,38),StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore};_visual.AddChild(icon);
        void Line(string value,int points,float y,bool bold)
        {
            var l=FrontendSkin.Label(value,points);l.Position=new Vector2(100,y);l.Size=new Vector2(460,42);
            l.AddThemeColorOverride("font_color",bold?new Color("e4edf2"):new Color("bdcbd4"));
            if(bold)l.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));_visual.AddChild(l);
        }
        Line(Text,32,26,true);Line(Description,22,76,false);
        MouseEntered+=()=>{_hover=true;Refresh();};MouseExited+=()=>{_hover=false;Refresh();};
        FocusEntered+=()=>{_focus=true;Refresh();};FocusExited+=()=>{_focus=false;Refresh();};
        base._Ready();Icon=null;Refresh();
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
        bool active=!Disabled && (_hover || (_focus && _keyboard));
        _tween?.Kill();_tween?.Dispose();_tween=CreateTween().SetParallel();
        _tween.TweenProperty(_visual,"scale",Vector2.One*(active?1.025f:1f),.14).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        var accent=new Color(.28f,.7f,1,active?.85f:.45f);
        _tween.TweenProperty(_top,"color",accent,.14);_tween.TweenProperty(_bottom,"color",accent,.14);
    }
    public override void _ExitTree(){_tween?.Kill();_tween?.Dispose();_tween=null;}
}
