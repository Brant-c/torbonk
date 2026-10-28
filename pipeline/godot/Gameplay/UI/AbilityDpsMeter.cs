using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
namespace SwtorSurvivors.UI;

/// <summary>Actual damage per ability, divided by the same run clock as Combat.</summary>
public partial class AbilityDpsMeter : Control
{
    public GameWorld World {get;set;}=null!;
    public Dictionary<string,double> Damage {get;}=new();
    private readonly List<Label> _names=new(),_values=new();
    private int _scroll;
    public double Dps(string id)=>World.Progression!.ElapsedSeconds>0?Damage.GetValueOrDefault(id)/World.Progression.ElapsedSeconds:0;
    public override void _Ready()
    {
        Name="AbilityDpsMeter";Size=new Vector2(240,184);MouseFilter=MouseFilterEnum.Stop;
        AddChild(new NinePatchRect {Texture=SwtorSkin.Slice("bwaui_standard_window_1"),Size=Size*2,Scale=Vector2.One*.5f,
            PatchMarginLeft=128,PatchMarginRight=184,PatchMarginTop=72,PatchMarginBottom=64,MouseFilter=MouseFilterEnum.Ignore});
        var heading=SwtorSkin.Text("ABILITY DPS",14,true);heading.Position=new Vector2(15,11);heading.MouseFilter=MouseFilterEnum.Ignore;AddChild(heading);
        var bold=GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf");
        for(int i=0;i<4;i++)
        {
            var name=SwtorSkin.Text("",14);name.Position=new Vector2(16,44+26*i);name.Size=new Vector2(150,22);
            name.VerticalAlignment=VerticalAlignment.Center;name.TextOverrunBehavior=TextServer.OverrunBehavior.TrimEllipsis;
            name.MouseFilter=MouseFilterEnum.Ignore;AddChild(name);_names.Add(name);
            var value=SwtorSkin.Text("",16);value.Position=new Vector2(170,44+26*i);value.Size=new Vector2(54,22);
            value.HorizontalAlignment=HorizontalAlignment.Right;value.VerticalAlignment=VerticalAlignment.Center;
            value.AddThemeFontOverride("font",bold);value.AddThemeColorOverride("font_color",new Color("df4e4e"));
            value.MouseFilter=MouseFilterEnum.Ignore;AddChild(value);_values.Add(value);
        }
        World.Events.Resolved+=Record;Refresh();
    }
    private void Record(CombatEvent e)
    {
        if(e.Change.Kind!=CombatEventKind.Damage || e.RecipientTeam!=CombatTeam.Hostile || !float.IsFinite(e.Change.Amount) || e.Change.Amount<=0)return;
        string id=string.IsNullOrEmpty(e.Change.AbilityId)?"other":e.Change.AbilityId;
        Damage[id]=Damage.GetValueOrDefault(id)+e.Change.Amount;
    }
    private string Title(string id)=>id=="other"?"Other damage":World.Abilities.Find(id)?.Definition.Title??id;
    private static string Amount(double n)=>n>=9999.5?(n>=999999.5?(n/1000000).ToString("0.#",CultureInfo.InvariantCulture)+"M":(n/1000).ToString("0.#",CultureInfo.InvariantCulture)+"K"):n.ToString("N0",CultureInfo.InvariantCulture);
    private void Refresh()
    {
        var rows=Damage.OrderByDescending(p=>p.Value).ThenBy(p=>p.Key).ToArray();
        _scroll=Math.Clamp(_scroll,0,Math.Max(0,rows.Length-4));
        for(int i=0;i<4;i++)
        {
            bool present=i+_scroll<rows.Length;string id=present?rows[i+_scroll].Key:"";
            _names[i].Text=present?Title(id):(i==0?"No damage yet":"");_values[i].Text=present?Amount(Dps(id)):"";
        }
        TooltipText=string.Join("\n",rows.Select(p=>$"{Title(p.Key)}: {Dps(p.Key):N0} DPS · {p.Value:N0} damage"));
        if(rows.Length>4)TooltipText+="\nScroll for more abilities";
    }
    public void ResetRun(){Damage.Clear();_scroll=0;Refresh();}
    public override void _Process(double delta)=>Refresh();
    public override void _GuiInput(InputEvent input)
    {
        if(input is InputEventMouseButton {Pressed:true} b && b.ButtonIndex is MouseButton.WheelDown or MouseButton.WheelUp)
        {_scroll+=b.ButtonIndex==MouseButton.WheelDown?1:-1;Refresh();AcceptEvent();}
    }
    public override void _ExitTree(){if(GodotObject.IsInstanceValid(World.Events))World.Events.Resolved-=Record;}
}
