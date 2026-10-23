using Godot;
using System.Collections.Generic;

namespace SwtorSurvivors.UI;

/// <summary>Original April art in native Godot controls. Slice identities live in ui-conversion.json.</summary>
public static class SwtorSkin
{
    private static readonly Dictionary<string,Texture2D> Textures = new();
    public static readonly Color Gold = new("efd192");
    public static readonly Color Blue = new("71c5ed");
    public static readonly Color TextColor = new("d5e6ef");
    public static StyleBoxTexture ButtonArtwork(bool hovered=false, bool selected=false, int verticalPadding=6)
    {
        var box=new StyleBoxTexture {
            Texture=GD.Load<Texture2D>("res://Assets/UI/menu/"+((hovered || selected)?"row_active":"row")+".svg"),
            TextureMarginLeft=18,TextureMarginRight=18,TextureMarginTop=18,TextureMarginBottom=18,
            ContentMarginLeft=16,ContentMarginRight=16,
            ContentMarginTop=verticalPadding,ContentMarginBottom=verticalPadding };
        return box;
    }
    public static StyleBoxTexture MenuFrame(int top=24) => new() {
        Texture=GD.Load<Texture2D>("res://Assets/UI/menu/frame.svg"),
        TextureMarginLeft=24,TextureMarginRight=36,TextureMarginTop=32,TextureMarginBottom=24,
        ContentMarginLeft=22,ContentMarginRight=28,ContentMarginTop=top,ContentMarginBottom=22 };
    public static PanelContainer Frame(Control content)
    {
        var panel=new PanelContainer();panel.AddThemeStyleboxOverride("panel",MenuFrame());
        panel.AddChild(content);return panel;
    }
    public static string? ActionIcon(string text)
    {
        text=text.ToLowerInvariant();
        if(text.Contains("restart") || text.Contains("reset"))return "bwaui_buttons_69";
        if(text.Contains("settings"))return "bwaui_buttons_90";
        if(text.Contains("sound") || text.Contains("music"))return "bwaui_buttons_57";
        if(text.Contains("collection") || text.Contains("appearance"))return "bwaui_buttons_135";
        if(text.Contains("main menu"))return "bwaui_buttons_236";
        if(text.Contains("exit") || text.Contains("close") || text.Contains("back"))return "bwaui_buttons_66";
        if(text.Contains("run") || text.Contains("play") || text.Contains("continue"))return "bwaui_buttons_132";
        return null;
    }
    public static Texture2D Slice(string name)
    {
        if (!Textures.TryGetValue(name,out var texture))
            Textures[name]=texture=GD.Load<Texture2D>("res://Assets/UI/slices/"+name+".png");
        return texture;
    }
    public static Texture2D Icon(string name) => GD.Load<Texture2D>("res://Assets/UI/icons/"+name+".png");
    public static Texture2D HudArt(string name)
    {
        string key="hud/"+name;
        if (!Textures.TryGetValue(key,out var texture))
            Textures[key]=texture=GD.Load<Texture2D>("res://Assets/UI/"+key+".tres");
        return texture;
    }
    public static StyleBoxTexture Box(string slice, int left=12, int top=12, int right=12, int bottom=12)
    {
        var box=new StyleBoxTexture { Texture=Slice(slice), TextureMarginLeft=left, TextureMarginTop=top,
            TextureMarginRight=right, TextureMarginBottom=bottom,
            ContentMarginLeft=left, ContentMarginTop=top, ContentMarginRight=right, ContentMarginBottom=bottom };
        return box;
    }
    public static Theme MakeTheme()
    {
        var theme=new Theme { DefaultFont=GD.Load<FontFile>("res://Assets/UI/Eurostile.ttf"), DefaultFontSize=17 };
        theme.SetColor("font_color","Label",TextColor);
        theme.SetColor("font_color","Button",TextColor);
        theme.SetColor("font_hover_color","Button",Blue);
        theme.SetColor("font_focus_color","Button",Blue);
        theme.SetColor("font_pressed_color","Button",TextColor);
        theme.SetColor("font_hover_pressed_color","Button",Blue);
        theme.SetColor("font_disabled_color","Button",new Color("73818d"));
        theme.SetStylebox("normal","Button",ButtonArtwork());
        theme.SetStylebox("hover","Button",ButtonArtwork(hovered:true));
        theme.SetStylebox("pressed","Button",ButtonArtwork(selected:true));
        theme.SetStylebox("hover_pressed","Button",ButtonArtwork(selected:true));
        theme.SetStylebox("disabled","Button",ButtonArtwork());
        var focus=ButtonArtwork(hovered:true);
        theme.SetStylebox("focus","Button",focus);
        theme.SetStylebox("panel","PanelContainer",MenuFrame());
        theme.SetStylebox("panel","AcceptDialog",MenuFrame());
        theme.SetStylebox("panel","TooltipPanel",Box("bwaui_tooltip_1",8,12,8,12));
        theme.SetConstant("outline_size","Label",0);
        theme.SetConstant("shadow_offset_x","Label",1);
        theme.SetConstant("shadow_offset_y","Label",1);
        theme.SetColor("font_shadow_color","Label",new Color(0.01f,0.025f,0.04f,0.85f));
        theme.SetConstant("separation","VBoxContainer",8);
        theme.SetConstant("separation","HBoxContainer",12);
        return theme;
    }
    public static Label Text(string text,int size=17,bool heading=false)
    {
        var label=new Label { Text=text, MouseFilter=Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size",size);
        if (heading)
        {
            label.AddThemeColorOverride("font_color",Blue);
        }
        return label;
    }
}
