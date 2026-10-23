using Godot;
using System.Linq;
using SwtorSurvivors.UI;
namespace SwtorSurvivors.Frontend;

public partial class ClassChoice : SwtorButton
{
    public ClassDefinition Class {get;set;}=null!;
    public bool Selected {get;set;}
    public Color Accent => Class.FactionId=="empire"?new Color(1,.25f,.22f):new Color(.28f,.7f,1);
    private Control _visual=null!;
    private ColorRect _top=null!,_bottom=null!;
    private bool _hover,_focus,_keyboard;
    private Tween? _tween;
    public override void _Ready()
    {
        CustomMinimumSize=new Vector2(343,540);AccessibilityName=Class.Name;AccessibilityDescription=Class.Description;SetMeta("selected",Selected);
        foreach(string state in new[]{"normal","hover","pressed","hover_pressed","disabled","focus"})AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        _visual=new Control {Name="ClassVisual",Size=new Vector2(343,540),PivotOffset=new Vector2(171.5f,270),MouseFilter=MouseFilterEnum.Ignore};AddChild(_visual);
        var panel=new Panel {Size=_visual.Size,MouseFilter=MouseFilterEnum.Ignore};
        panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=new Color(.01f,.025f,.045f,.38f),CornerRadiusTopLeft=18,CornerRadiusTopRight=18,CornerRadiusBottomLeft=18,CornerRadiusBottomRight=18});_visual.AddChild(panel);
        ColorRect Rule(float y){var r=new ColorRect {Position=new Vector2(50,y),Size=new Vector2(243,2),Color=Accent,MouseFilter=MouseFilterEnum.Ignore};_visual.AddChild(r);return r;}
        _top=Rule(0);_bottom=Rule(538);
        var art=new TextureRect {Name="ClassArtwork",ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=FrontendSkin.Texture(Class.Artwork),Size=new Vector2(319,245),Position=new Vector2(12,22),StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore};_visual.AddChild(art);
        var icon=new TextureRect {ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=FrontendSkin.Texture(Class.Icon),Size=new Vector2(40,40),Position=new Vector2(151,276),StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore};_visual.AddChild(icon);
        void Line(string value,int size,float y,float width,bool bold=false,bool wrap=false)
        {
            var l=FrontendSkin.Label(value,size,wrap);l.Position=new Vector2((343-width)/2,y);l.Size=new Vector2(width,wrap?64:46);l.HorizontalAlignment=HorizontalAlignment.Center;
            l.AddThemeColorOverride("font_color",y==460?Accent: bold?new Color("e4edf2"):new Color("bdcbd4"));
            if(bold)l.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));_visual.AddChild(l);
        }
        Line(Class.Name,28,329,343,true);
        string summary=Class.Id switch {
            "sith_warrior"=>"Strength and lightsaber mastery.","sith_inquisitor"=>"Force lightning and forbidden knowledge.",
            "imperial_agent"=>"Precision, technology, and deception.","bounty_hunter"=>"Heavy firepower and ruthless pursuit.",
            "jedi_knight"=>"Courage and lightsaber mastery.","jedi_consular"=>"Wisdom and mastery of the Force.",
            "smuggler"=>"Quick thinking and a quicker draw.","trooper"=>"Heavy weapons and frontline resolve.",_=>Class.Description};
        Line(summary,21,390,299,wrap:true);
        Line(string.Join("  ·  ",Class.AdvancedClasses.Select(a=>a.Name)),21,460,343);
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
        var accent=Accent;accent.A=active?.95f:.45f;
        _tween.TweenProperty(_top,"color",accent,.14);_tween.TweenProperty(_bottom,"color",accent,.14);
    }
    public override void _ExitTree(){_tween?.Kill();_tween?.Dispose();_tween=null;}
}
