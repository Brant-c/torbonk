using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using SwtorSurvivors.Talents;
using SwtorSurvivors.UI;

namespace SwtorSurvivors.Frontend;

public partial class TalentsScreen : Control
{
    public Action? Closed {get;set;}
    public string ClassId {get;set;}="sorcerer";
    public IReadOnlyDictionary<string,int> Ranks=>_ranks;
    private Dictionary<string,int> _ranks=new();
    private readonly Dictionary<string,int> _scrolls=new();
    private string _status="";
    private TalentCatalog Catalog=>TalentCatalog.Current;
    // Set IgnoreSize before assigning a texture. Otherwise the native texture minimum can
    // enlarge the control during construction and persist until the next layout pass.
    private static TextureRect Image(string path,Vector2 size)=>new()
    {
        ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=GD.Load<Texture2D>(path),
        CustomMinimumSize=size,Size=size,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter=MouseFilterEnum.Ignore
    };
    public override void _Ready()
    {
        Theme=FrontendSkin.MakeTheme();
        // Custom talent tooltips draw their own frame; leave Godot's outer wrapper empty.
        Theme.SetStylebox("panel","TooltipPanel",new StyleBoxEmpty());
        _ranks=TalentProfiles.Load(ClassId);Build();
    }
    private void Put(Control node,float x,float y,float w,float h,Node? parent=null)
    {node.Position=new(x,y);node.Size=new(w,h);(parent??this).AddChild(node);}
    private void Art(string slice,float x,float y,float w,float h,Node parent)
    {
        var art=Image("res://Assets/UI/slices/"+slice+".png",new(w,h));
        art.StretchMode=TextureRect.StretchModeEnum.Scale;Put(art,x,y,w,h,parent);
    }
    public bool Invest(string id)
    {
        var talent=Catalog.ForClass(ClassId).FirstOrDefault(t=>t.Id==id);
        if(talent==null || Catalog.Locked(ClassId,talent,_ranks)!=null)return false;
        _ranks[id]=_ranks.GetValueOrDefault(id)+1;Persist();return true;
    }
    public bool Refund(string id)
    {
        if(_ranks.GetValueOrDefault(id)==0)return false;
        var candidate=new Dictionary<string,int>(_ranks);candidate[id]--;
        var valid=Catalog.Validate(ClassId,candidate);
        if(candidate.Any(p=>p.Value!=valid.GetValueOrDefault(p.Key)))return false;
        _ranks=valid;Persist();return true;
    }
    public void Respec(){_ranks.Clear();Persist();}
    private void Persist(){_status=TalentProfiles.Save(ClassId,_ranks)??"Build saved";Build();}
    public void SelectClass(string id)
    {ClassId=id;_ranks=TalentProfiles.Load(id);_status="";_scrolls.Clear();Build();}
    private void Build()
    {
        foreach(var c in GetChildren()){RemoveChild(c);c.QueueFree();}
        var classes=FrontendCatalog.Load().Classes.SelectMany(c=>c.AdvancedClasses).ToArray();
        var chosen=classes.First(c=>c.Id==ClassId);
        var art=Image(chosen.Artwork,new(72,72));Put(art,0,-62,72,72);
        var title=FrontendSkin.Heading("Talents",32);Put(title,90,-57,300,54);
        var selector=new OptionButton {CustomMinimumSize=new(290,46)};
        foreach(var cls in classes)selector.AddItem(cls.Name+(Catalog.ForClass(cls.Id).Length==0?" — Unavailable":""));
        selector.Select(Array.FindIndex(classes,c=>c.Id==ClassId));
        selector.ItemSelected+=i=>SelectClass(classes[(int)i].Id);Put(selector,370,-48,310,46);
        string allocation=(ClassId is "marauder" or "juggernaut" or "assassin" or "sorcerer" or "operative" or "sniper" or "powertech" or "mercenary")
            ?$"{Catalog.Spent(_ranks)} / {Catalog.BudgetFor(ClassId)} shared points · {Catalog.BudgetFor(ClassId)-Catalog.Spent(_ranks)} available"
            :$"{Catalog.BudgetFor(ClassId)-Catalog.Spent(_ranks)} Points Available    {Catalog.Spent(_ranks)} Allocated";
        Put(FrontendSkin.Label(allocation,23),745,-48,650,45);
        if(Catalog.ForClass(ClassId).Length==0)
        {
            var portrait=Image(chosen.Artwork,new(450,450));Put(portrait,495,50,450,450);
            Put(FrontendSkin.Heading("Talents unavailable",28),530,490,500,50);
        }
        else
        {
            var trees=Catalog.Trees.Where(tree=>Catalog.ForClass(ClassId).Any(t=>t.Tree==tree.Id)).ToArray();
            for(int index=0;index<trees.Length;index++)
            {
                var tree=trees[index];
                float x=index*482;
                var column=new Control();Put(column,x,18,466,560);
                Art("bwaui_skilltree_30",0,0,466,560,column);
                Art("bwaui_skilltree_42",7,0,452,94,column);
                var heading=FrontendSkin.Heading(tree.Name,28);heading.HorizontalAlignment=HorizontalAlignment.Center;Put(heading,50,11,366,46,column);
                int capacity=Catalog.ForClass(ClassId).Where(t=>t.Tree==tree.Id && t.Available).Sum(t=>t.Cost*t.MaxRank);
                string treePoints=(ClassId is "marauder" or "juggernaut" or "assassin" or "sorcerer" or "operative" or "sniper" or "powertech" or "mercenary")?$"{Catalog.TreeSpent(tree.Id,_ranks)} / {capacity} points":Catalog.TreeSpent(tree.Id,_ranks)+" Points";
                var points=FrontendSkin.Label(treePoints,17);points.HorizontalAlignment=HorizontalAlignment.Center;Put(points,50,56,366,25,column);
                var scroll=new ScrollContainer {HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};
                Put(scroll,18,100,430,442,column);
                var body=new Control {CustomMinimumSize=new(410,440)};scroll.AddChild(body);
                scroll.ScrollVertical=_scrolls.GetValueOrDefault(tree.Id);
                scroll.GetVScrollBar().ValueChanged+=value=>_scrolls[tree.Id]=(int)value;
                Vector2 Position(TalentDefinition t)=>new((ClassId is "marauder" or "juggernaut" or "assassin" or "sorcerer" or "operative" or "sniper" or "powertech" or "mercenary")?33+t.Column*137:74+t.Column*212,(ClassId is "marauder" or "juggernaut" or "assassin" or "sorcerer" or "operative" or "sniper" or "powertech" or "mercenary")?22+t.Tier*145:12+t.Tier*103);
                foreach(var t in Catalog.ForClass(ClassId).Where(t=>t.Tree==tree.Id))
                    foreach(var prerequisite in t.Prerequisites)
                    {
                        var from=Catalog.Talents.First(a=>a.Id==prerequisite.Id);
                        var a=Position(from)+new Vector2(30,66);var b=Position(t)+new Vector2(30,-4);
                        bool met=_ranks.GetValueOrDefault(from.Id)>=prerequisite.Rank;
                        Art(met?"bwaui_skilltree_1":"bwaui_skilltree_5",a.X-8,a.Y,16,b.Y-a.Y,body);
                    }
                foreach(var t in Catalog.ForClass(ClassId).Where(t=>t.Tree==tree.Id))
                {
                    int rank=_ranks.GetValueOrDefault(t.Id);
                    string? reason=Catalog.Locked(ClassId,t,_ranks);
                    string requirements=t.RequiredPoints==0?"":$"Requires {t.RequiredPoints} points in earlier tiers.";
                    foreach(var p in t.Prerequisites)requirements+=$" Requires {Catalog.Talents.First(a=>a.Id==p.Id).Name} ({p.Rank}).";
                    var button=new TalentIconButton {Name=t.Id.Replace('.','_'),TooltipText=t.Name,
                        Talent=t,Rank=rank,Requirements=requirements,Availability=reason??"Click to invest",
                        MouseDefaultCursorShape=CursorShape.PointingHand};
                    foreach(string state in new[]{"normal","hover","pressed","focus","disabled","hover_pressed"})
                        button.AddThemeStyleboxOverride(state,new StyleBoxEmpty());
                    var pos=Position(t);Put(button,pos.X,pos.Y,62,68,body);
                    Art("bwaui_skilltree_9",0,-1,62,62,button);
                    var icon=Image(t.Icon,new(48,48));
                    icon.Modulate=rank>0?Colors.White:reason==null?new Color(.82f,.9f,1):new Color(.32f,.38f,.45f);
                    Put(icon,7,5,48,48,button);
                    Art(rank==t.MaxRank?"bwaui_skilltree_16":"bwaui_skilltree_13",8,48,46,27,button);
                    var counter=FrontendSkin.Label($"{rank}/{t.MaxRank}",17);counter.HorizontalAlignment=HorizontalAlignment.Center;
                    counter.AddThemeColorOverride("font_color",rank>0?new Color(.65f,1,.72f):Colors.White);Put(counter,6,49,50,25,button);
                    var name=FrontendSkin.Label(t.Name,(ClassId is "marauder" or "juggernaut" or "assassin" or "sorcerer" or "operative" or "sniper" or "powertech" or "mercenary")?15:16,true);name.HorizontalAlignment=HorizontalAlignment.Center;
                    if((ClassId is "marauder" or "juggernaut" or "assassin" or "sorcerer" or "operative" or "sniper" or "powertech" or "mercenary"))Put(name,pos.X-32,pos.Y+76,126,42,body);
                    else Put(name,pos.X-56,pos.Y+76,174,34,body);
                    button.Pressed+=()=>{if(Invest(t.Id))FindChild(t.Id.Replace('.','_'),true,false)?.CallDeferred(Control.MethodName.GrabFocus);};
                    button.GuiInput+=input=>{if(input is InputEventMouseButton {Pressed:true,ButtonIndex:MouseButton.Right})Refund(t.Id);};
                    // Tier threshold is visible while hovering and in the original-style tooltip.
                }
            }
        }
        var reset=FrontendSkin.Button("Reset / Respec",Respec);reset.Disabled=Catalog.Spent(_ranks)==0;
        Put(reset,0,594,240,52);
        var save=FrontendSkin.Button("Save Build",()=>{_status=TalentProfiles.Save(ClassId,_ranks)??"Build saved";Build();});
        save.Disabled=Catalog.ForClass(ClassId).Length==0;Put(save,260,594,220,52);
        Put(FrontendSkin.Label(_status,18),510,601,630,40);
        Put(FrontendSkin.Button("Back",()=>Closed?.Invoke()),1210,594,230,52);
    }
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if(input is InputEventKey {Pressed:true,Echo:false,Keycode:Key.Escape})
        {Closed?.Invoke();GetViewport().SetInputAsHandled();}
    }
}

public partial class TalentIconButton : Button
{
    public TalentDefinition Talent {get;set;}=null!;
    public int Rank {get;set;}
    public string Requirements {get;set;}="";
    public string Availability {get;set;}="";
    public override GodotObject _MakeCustomTooltip(string forText)
    {
        var panel=new PanelContainer {CustomMinimumSize=new(360,0),Theme=FrontendSkin.MakeTheme()};
        panel.AddThemeStyleboxOverride("panel",SwtorSkin.Box("bwaui_tooltip_1",12,12,12,12));
        var content=new VBoxContainer();panel.AddChild(content);
        content.AddChild(FrontendSkin.Heading(Talent.Name,23));
        content.AddChild(FrontendSkin.Label($"Rank {Rank}/{Talent.MaxRank}    Cost: {Talent.Cost}",18));
        foreach(string line in new[]{Talent.Description,Requirements,Availability,"Right-click to refund a point."}
            .Where(s=>!string.IsNullOrWhiteSpace(s)).DistinctBy(s=>s.Trim().TrimEnd('.')))
        {
            var label=FrontendSkin.Label(line,18,true);label.CustomMinimumSize=new(330,0);content.AddChild(label);
        }
        return panel;
    }
}
