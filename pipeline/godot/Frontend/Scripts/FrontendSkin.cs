using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwtorSurvivors.UI;

namespace SwtorSurvivors.Frontend;

public static class FrontendSkin
{
    public const string Assets = "res://Frontend/Assets/";
    public static readonly Color TextColor = SwtorSkin.TextColor;
    public static Texture2D Texture(string path) => GD.Load<Texture2D>(path);
    public static StyleBoxTexture Box(string path, int margin = 14) => new()
    {
        Texture = Texture(path), TextureMarginLeft = margin, TextureMarginRight = margin,
        TextureMarginTop = margin, TextureMarginBottom = margin,
        ContentMarginLeft = margin + 8, ContentMarginRight = margin + 8,
        ContentMarginTop = margin + 4, ContentMarginBottom = margin + 4
    };
    public static Theme MakeTheme()
    {
        var t = SwtorSkin.MakeTheme(); t.DefaultFontSize = 20;
        // Preserve the already approved C1 menu button mapping and proportions.
        foreach (string color in new[] {"font_color","font_hover_color","font_pressed_color","font_focus_color","font_hover_pressed_color"})
            t.SetColor(color,"Button",TextColor);
        t.SetStylebox("normal","Button",ButtonArtwork());
        t.SetStylebox("hover","Button",ButtonArtwork(hovered:true));
        t.SetStylebox("pressed","Button",ButtonArtwork(hovered:true,selected:true));
        t.SetStylebox("hover_pressed","Button",ButtonArtwork(hovered:true,selected:true));
        t.SetStylebox("disabled","Button",ButtonArtwork());
        t.SetStylebox("focus","Button",ButtonArtwork(hovered:true));
        t.SetStylebox("panel", "PanelContainer", SwtorSkin.MenuFrame());
        t.SetStylebox("panel", "TooltipPanel", Box(Assets + "UI/bwaui_tooltip_1.png", 12));
        t.SetConstant("separation", "VBoxContainer", 12);
        t.SetConstant("separation", "HBoxContainer", 12);
        return t;
    }
    public static Label Label(string text, int size = 20, bool wrap = false)
    {
        var l = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off };
        l.AddThemeFontSizeOverride("font_size", size);
        return l;
    }
    public static Label Heading(string text, int size = 22)
    {
        var label=Label(text,size); label.AddThemeColorOverride("font_color",SwtorSkin.Blue);
        label.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));
        label.VerticalAlignment=VerticalAlignment.Center; return label;
    }
    public static TextureRect Art(string path, Vector2 size) => new()
    {
        Texture = Texture(path), CustomMinimumSize = size, Size = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter = Control.MouseFilterEnum.Ignore
    };
    public static TextureRect Planet(string surface, float diameter) => new()
    {
        Texture = Texture(surface), Size = new Vector2(diameter,diameter),
        CustomMinimumSize = new Vector2(diameter,diameter),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        // The shader projects a 2:1 surface onto a sphere. Its output quad must
        // stay square; fitting the source image's aspect would flatten the globe.
        StretchMode = TextureRect.StretchModeEnum.Scale,
        Material = new ShaderMaterial { Shader = GD.Load<Shader>(Assets+"planet.gdshader") },
        MouseFilter = Control.MouseFilterEnum.Ignore
    };
    public static StyleBoxTexture ButtonArtwork(bool hovered = false, bool selected = false, int verticalPadding = 6)
    {
        return SwtorSkin.ButtonArtwork(hovered,selected,verticalPadding);
    }
    public static Button Button(string text, Action action, bool selected = false)
    {
        var b = new SwtorButton { Text = text, CustomMinimumSize = new Vector2(0, 52),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        b.SetMeta("selected",selected);
        if (selected)
        {
            foreach (string state in new[] {"normal","hover","pressed","hover_pressed","disabled"})
                b.AddThemeStyleboxOverride(state,ButtonArtwork(hovered:true,selected:true));
            b.AddThemeColorOverride("font_disabled_color",TextColor);
        }
        b.Pressed += action; return b;
    }
    // The standard window has a shallow title strip at y=22..52. Keep the cap,
    // shoulders and bottom bevel intact instead of stretching the entire texture.
    public static Control TitledPanel(string title, Control content, Vector2 position, Vector2 size)
    {
        var root=new Control { Position=position, Size=size };
        var box=SwtorSkin.MenuFrame(58);
        var panel=new PanelContainer { Size=size }; panel.AddThemeStyleboxOverride("panel",box);
        root.AddChild(panel); panel.AddChild(content);
        var heading=Heading(title,20); heading.Position=new Vector2(26,24);
        heading.Size=new Vector2(size.X-82,28); root.AddChild(heading);
        return root;
    }
    public static PanelContainer Panel(Control content, Vector2 position, Vector2 size, bool galaxy = false, bool creation = false, bool insetTitle = false)
    {
        var p = new PanelContainer { Position = position, Size = size };
        if (galaxy) p.AddThemeStyleboxOverride("panel", Box(Assets + "Slices/ui_galaxymap_panel_3.png", 8));
        else p.AddThemeStyleboxOverride("panel",SwtorSkin.MenuFrame(creation || insetTitle ? 36 : 24));
        p.AddChild(content); return p;
    }
}
