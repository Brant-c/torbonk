using Godot;

namespace SwtorSurvivors.Frontend;

public static class FrontendSkin
{
    public const string Assets = "res://Frontend/Assets/";
    public static readonly Color TextColor = new("dae7ed");
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
        t.SetColor("font_color", "Label", TextColor);
        t.SetColor("font_color", "Button", TextColor);
        t.SetColor("font_hover_color", "Button", TextColor);
        t.SetColor("font_pressed_color", "Button", TextColor);
        t.SetColor("font_focus_color", "Button", TextColor);
        t.SetColor("font_hover_pressed_color", "Button", TextColor);
        t.SetConstant("outline_size", "Label", 0);
        t.SetConstant("outline_size", "Button", 0);
        t.SetColor("font_disabled_color", "Button", new Color("73818d"));
        t.SetStylebox("normal", "Button", ButtonArtwork());
        t.SetStylebox("hover", "Button", ButtonArtwork(hovered:true));
        t.SetStylebox("pressed", "Button", ButtonArtwork(hovered:true,selected:true));
        t.SetStylebox("hover_pressed", "Button", ButtonArtwork(hovered:true,selected:true));
        t.SetStylebox("disabled", "Button", ButtonArtwork());
        // Original texture states supply the chrome; no drawn focus border is added.
        t.SetStylebox("focus", "Button", new StyleBoxEmpty());
        t.SetStylebox("panel", "PanelContainer", Box(Assets + "UI/bwaui_standard_window_1.png", 16));
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
        // Reverse the original C1 rest/hover bindings for every button, including
        // selected destinations. Identical content insets prevent label movement.
        string image = hovered ? (selected ? "18" : "24") : "9";
        var box = Box(Assets+"UI/bwaui_standard_window_"+image+".png",12);
        box.ContentMarginLeft=22; box.ContentMarginRight=22;
        box.ContentMarginTop=verticalPadding; box.ContentMarginBottom=verticalPadding;
        return box;
    }
    public static Button Button(string text, Action action, bool selected = false)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(0, 52),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        if (selected) b.AddThemeStyleboxOverride("hover", ButtonArtwork(hovered:true,selected:true));
        b.Pressed += action; return b;
    }
    public static PanelContainer Panel(Control content, Vector2 position, Vector2 size, bool galaxy = false, bool creation = false, bool insetTitle = false)
    {
        var p = new PanelContainer { Position = position, Size = size };
        if (galaxy) p.AddThemeStyleboxOverride("panel", Box(Assets + "Slices/ui_galaxymap_panel_3.png", 8));
        else if (creation)
        {
            var box=Box(Assets+"Slices/bwaui_character_create_panels_9.png",28);
            box.TextureMarginTop=52; box.TextureMarginBottom=52;
            box.ContentMarginLeft=32; box.ContentMarginRight=32;
            box.ContentMarginTop=36; box.ContentMarginBottom=32;
            p.AddThemeStyleboxOverride("panel",box);
        }
        else if (insetTitle)
        {
            var box=Box(Assets+"UI/bwaui_standard_window_1.png",16);
            box.ContentMarginLeft=28; box.ContentMarginRight=28;
            box.ContentMarginTop=52; box.ContentMarginBottom=28;
            p.AddThemeStyleboxOverride("panel",box);
        }
        p.AddChild(content); return p;
    }
}
