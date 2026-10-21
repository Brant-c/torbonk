using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SwtorSurvivors.Frontend;

public static class FrontendSkin
{
    public const string Assets = "res://Frontend/Assets/";
    public static readonly Color Gold = new("efd192"), Blue = new("71c5ed"), Muted = new("91a9b9");
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
        var t = new Theme { DefaultFont = GD.Load<FontFile>(Assets + "UI/Eurostile.ttf"), DefaultFontSize = 20 };
        t.SetColor("font_color", "Label", new Color("dae7ed"));
        t.SetColor("font_color", "Button", Gold);
        t.SetColor("font_hover_color", "Button", Colors.White);
        t.SetColor("font_pressed_color", "Button", Blue);
        t.SetColor("font_disabled_color", "Button", new Color("73818d"));
        t.SetStylebox("normal", "Button", Box(Assets + "UI/bwaui_standard_window_24.png", 10));
        t.SetStylebox("hover", "Button", Box(Assets + "UI/bwaui_standard_window_9.png", 12));
        t.SetStylebox("pressed", "Button", Box(Assets + "UI/bwaui_standard_window_18.png", 12));
        t.SetStylebox("disabled", "Button", Box(Assets + "UI/bwaui_standard_window_24.png", 10));
        t.SetStylebox("focus", "Button", new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = Gold,
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2 });
        t.SetStylebox("panel", "PanelContainer", Box(Assets + "UI/bwaui_standard_window_1.png", 16));
        t.SetStylebox("panel", "TooltipPanel", Box(Assets + "UI/bwaui_tooltip_1.png", 12));
        t.SetConstant("separation", "VBoxContainer", 12);
        t.SetConstant("separation", "HBoxContainer", 12);
        return t;
    }
    public static Label Label(string text, int size = 20, bool gold = false, bool wrap = false)
    {
        var l = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off };
        l.AddThemeFontSizeOverride("font_size", size);
        if (gold) { l.AddThemeColorOverride("font_color", Gold); l.AddThemeFontOverride("font", GD.Load<FontFile>(Assets + "UI/EurostileBold.ttf")); }
        return l;
    }
    public static TextureRect Art(string path, Vector2 size) => new()
    {
        Texture = Texture(path), CustomMinimumSize = size, Size = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter = Control.MouseFilterEnum.Ignore
    };
    public static Button Button(string text, Action action, bool selected = false)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(0, 52),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        if (selected) b.AddThemeStyleboxOverride("normal", Box(Assets + "UI/bwaui_standard_window_18.png", 12));
        b.Pressed += action; return b;
    }
    public static PanelContainer Panel(Control content, Vector2 position, Vector2 size, bool galaxy = false)
    {
        var p = new PanelContainer { Position = position, Size = size };
        if (galaxy) p.AddThemeStyleboxOverride("panel", Box(Assets + "Slices/ui_galaxymap_panel_3.png", 8));
        p.AddChild(content); return p;
    }
}
