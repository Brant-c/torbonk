using Godot;
using System.Collections.Generic;

namespace SwtorSurvivors.UI;

/// <summary>Original April art in native Godot controls. Slice identities live in ui-conversion.json.</summary>
public static class SwtorSkin
{
    private static readonly Dictionary<string,Texture2D> Textures = new();
    public static readonly Color Gold = new("efd192");
    public static readonly Color Blue = new("71c5ed");
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
        theme.SetColor("font_color","Label",new Color("d5e6ef"));
        theme.SetColor("font_color","Button",Gold);
        theme.SetColor("font_hover_color","Button",Colors.White);
        theme.SetColor("font_focus_color","Button",Colors.White);
        var normal=Box("bwaui_standard_window_24",14,8,16,8);
        normal.ContentMarginTop=6;normal.ContentMarginBottom=6;
        theme.SetStylebox("normal","Button",normal);
        theme.SetStylebox("hover","Button",Box("bwaui_standard_window_9",12,12,12,12));
        theme.SetStylebox("pressed","Button",normal);
        theme.SetStylebox("focus","Button",new StyleBoxFlat {BgColor=Colors.Transparent,
            BorderColor=Blue,BorderWidthLeft=1,BorderWidthRight=1,BorderWidthTop=1,BorderWidthBottom=1});
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
            label.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));
            label.AddThemeColorOverride("font_color",Gold);
        }
        return label;
    }
}
