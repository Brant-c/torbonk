using Godot;
using SwtorSurvivors.UI;
namespace SwtorSurvivors.Frontend;

public partial class AdvancedClassChoice : SwtorButton
{
    public AdvancedClassDefinition Advanced {get;set;}=null!;
    public string FactionId {get;set;}="";
    public bool Selected {get;set;}
    public Color Accent => FactionId=="empire"?new Color(1,.25f,.22f):new Color(.28f,.7f,1);
    private Control _visual=null!;
    private ColorRect _top=null!,_bottom=null!;
    private bool _hover,_focus,_keyboard;
    private Tween? _tween;
    public override void _Ready()
    {
        CustomMinimumSize=new Vector2(530,550);AccessibilityName=Advanced.Name;AccessibilityDescription=Advanced.Description;SetMeta("selected",Selected);
        foreach(string state in new[]{"normal","hover","pressed","hover_pressed","disabled","focus"})AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        _visual=new Control {Name="AdvancedVisual",Size=new Vector2(530,550),PivotOffset=new Vector2(265,275),MouseFilter=MouseFilterEnum.Ignore};AddChild(_visual);
        var panel=new Panel {Size=_visual.Size,MouseFilter=MouseFilterEnum.Ignore};
        panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=new Color(.01f,.025f,.045f,.38f),CornerRadiusTopLeft=18,CornerRadiusTopRight=18,CornerRadiusBottomLeft=18,CornerRadiusBottomRight=18});_visual.AddChild(panel);
        ColorRect Rule(float y){var r=new ColorRect {Position=new Vector2(50,y),Size=new Vector2(430,2),Color=Accent,MouseFilter=MouseFilterEnum.Ignore};_visual.AddChild(r);return r;}
        _top=Rule(0);_bottom=Rule(548);
        var art=new TextureRect {Name="AdvancedArtwork",ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=FrontendSkin.Texture(Advanced.Artwork),Size=new Vector2(480,275),Position=new Vector2(25,20),StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore};_visual.AddChild(art);
        void Line(string value,int points,float y,float width,float height,Color color,bool bold=false,bool wrap=false)
        {
            var label=FrontendSkin.Label(value,points,wrap);label.Position=new Vector2((530-width)/2,y);label.Size=new Vector2(width,height);label.HorizontalAlignment=HorizontalAlignment.Center;
            label.AddThemeColorOverride("font_color",color);
            if(bold)label.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));_visual.AddChild(label);
        }
        Line(Advanced.Name,34,310,530,50,new Color("e4edf2"),true);
        Line(Advanced.Description,23,380,460,85,new Color("bdcbd4"),wrap:true);
        Line("Choose "+Advanced.Name,23,486,530,40,FactionId=="empire"?Accent:new Color("a2e6ff"));
        MouseEntered+=()=>{_hover=true;Refresh();};MouseExited+=()=>{_hover=false;Refresh();};
        FocusEntered+=()=>{_focus=true;Refresh();};FocusExited+=()=>{_focus=false;Refresh();};
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
        bool active=Selected || (!Disabled && (_hover || (_focus && _keyboard)));
        _tween?.Kill();_tween?.Dispose();_tween=CreateTween().SetParallel();
        _tween.TweenProperty(_visual,"scale",Vector2.One*(_hover && !Disabled?1.025f:1f),.14).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        var accent=Accent;accent.A=active?.95f:.55f;
        _tween.TweenProperty(_top,"color",accent,.14);_tween.TweenProperty(_bottom,"color",accent,.14);
    }
    public override void _ExitTree(){_tween?.Kill();_tween?.Dispose();_tween=null;}
}
