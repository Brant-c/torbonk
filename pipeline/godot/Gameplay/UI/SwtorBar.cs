using Godot;

namespace SwtorSurvivors.UI;

/// <summary>Original bar pixels, clipped horizontally rather than recolored. Works for health, XP and resources.</summary>
public partial class SwtorBar : Control
{
    private TextureRect _fill=null!;
    private Control _clip=null!;
    private Label _label=null!;
    private float _fraction=1;
    public string FillSlice { get; set; }="bwaui_bars_21";
    public int FillHeight { get; set; }=12;
    public bool DrawFrame { get; set; }=true;
    public int HorizontalInset { get; set; }=12;
    public int MinimumHeight { get; set; }=36;
    public int LabelSize { get; set; }=15;
    public override void _Ready()
    {
        CustomMinimumSize=new Vector2(0,MinimumHeight);
        var border=new NinePatchRect { Texture=SwtorSkin.Slice("bwaui_bars_157"),
            PatchMarginLeft=12,PatchMarginRight=12,PatchMarginTop=4,PatchMarginBottom=4,
            MouseFilter=MouseFilterEnum.Ignore };
        AddChild(border); border.Visible=DrawFrame; border.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _clip=new Control { ClipContents=true,MouseFilter=MouseFilterEnum.Ignore };
        AddChild(_clip);
        _fill=new TextureRect { Texture=SwtorSkin.Slice(FillSlice),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter=MouseFilterEnum.Ignore };
        _clip.AddChild(_fill);
        _label=SwtorSkin.Text("",LabelSize); _label.HorizontalAlignment=HorizontalAlignment.Center;
        _label.VerticalAlignment=VerticalAlignment.Center;
        AddChild(_label); _label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Resized+=LayoutBar; LayoutBar();
    }
    private void LayoutBar()
    {
        float width=Mathf.Max(0,Size.X-2*HorizontalInset);
        _clip.Position=new Vector2(HorizontalInset,(Size.Y-FillHeight)/2);
        _clip.Size=new Vector2(width*_fraction,FillHeight);
        _fill.Size=new Vector2(width,FillHeight);
    }
    public void UpdateValue(double current,double maximum,string text)
    {
        _fraction=(float)System.Math.Clamp(maximum<=0 ? 0 : current/maximum,0,1);
        _label.Text=text; LayoutBar();
    }
    public override GodotObject _MakeCustomTooltip(string forText) => new SwtorTooltip(forText);
}
