using Godot;
using System;

namespace SwtorSurvivors.UI;

/// <summary>Original bwaui_slot cooldown texture translated behind its rectangular slot mask.</summary>
public partial class CooldownWipe : Control
{
    private TextureRect _texture=null!;
    public double RemainingFraction { get; private set; }
    public override void _Ready()
    {
        ClipContents=true;MouseFilter=MouseFilterEnum.Ignore;
        _texture=new TextureRect {Texture=SwtorSkin.HudArt("cooldown_wipe"),
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,MouseFilter=MouseFilterEnum.Ignore};
        AddChild(_texture);Resized+=LayoutWipe;LayoutWipe();Visible=false;
    }
    public void UpdateRemaining(double remaining,double duration)
    {
        RemainingFraction=Math.Clamp(duration<=0?0:remaining/duration,0,1);
        Visible=RemainingFraction>0;LayoutWipe();
    }
    private void LayoutWipe()
    {
        // Source shape 118 is 50x100, masked by shape 115's 50x50 rectangle.
        // Start with the source edge at the top and advance it downward as time expires.
        _texture.Size=new Vector2(Size.X,Size.Y*2);
        _texture.Position=new Vector2(0,-Size.Y*(float)RemainingFraction);
    }
}
