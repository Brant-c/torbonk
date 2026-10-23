using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using SwtorSurvivors.Equipment;
using SwtorSurvivors.UI;

namespace SwtorSurvivors.Frontend;

/// <summary>Category host; Appearances is the sole category implemented in D1.1.</summary>
public partial class CollectionsScreen : Control
{
    public Action? Closed { get; set; }
    public string CloseLabel { get; set; } = "Main Menu";
    public void SelectClass(string baseClass,string advanced) { _base=baseClass;_advanced=advanced; }
    public CosmeticCollection Collection { get; private set; } = new();
    public CharacterPreview Preview { get; private set; } = null!;
    private readonly FrontendCatalog _catalog=FrontendCatalog.Load();
    private string _base="sith_inquisitor",_advanced="sorcerer",_slot="chest";
    private Control _body=null!;
    private VBoxContainer _choices=null!;
    private Label _status=null!;
    public override void _Ready() => Build();
    private static VBoxContainer Column() {var c=new VBoxContainer();c.AddThemeConstantOverride("separation",8);return c;}
    private void Build()
    {
        if(_body!=null) {RemoveChild(_body);_body.QueueFree();}
        _body=new Control {Size=Size};AddChild(_body);
        var category=FrontendSkin.Button("Appearances",()=>{},true);
        category.Position=new Vector2(0,-58);category.Size=new Vector2(260,48);_body.AddChild(category);
        var close=FrontendSkin.Button(CloseLabel,()=>Closed?.Invoke());
        close.Position=new Vector2(1228,-58);close.Size=new Vector2(220,48);_body.AddChild(close);
        var classes=Column();
        foreach(var cl in _catalog.Classes)
        {
            var button=FrontendSkin.Button(cl.Name,()=> {
                _base=cl.Id;_advanced=cl.AdvancedClasses[0].Id;Build();
            },cl.Id==_base);
            button.CustomMinimumSize=new Vector2(0,43);button.AddThemeFontSizeOverride("font_size",18);
            button.Icon=FrontendSkin.Texture(cl.Icon);button.ExpandIcon=true;button.AddThemeConstantOverride("icon_max_width",28);
            classes.AddChild(button);
        }
        classes.AddChild(FrontendSkin.Heading("Advanced Class",18));
        var advancedRow=new HBoxContainer();
        foreach(var a in _catalog.Class(_base).AdvancedClasses)
        {
            var b=FrontendSkin.Button(a.Name,()=> {_advanced=a.Id;Build();},a.Id==_advanced);
            b.SizeFlagsHorizontal=SizeFlags.ExpandFill;b.CustomMinimumSize=new Vector2(0,42);
            b.AddThemeFontSizeOverride("font_size",17);advancedRow.AddChild(b);
        }
        classes.AddChild(advancedRow);
        _body.AddChild(FrontendSkin.TitledPanel("Collections",classes,Vector2.Zero,new Vector2(330,650)));
        var advanced=_catalog.Advanced(_advanced);
        var previewBody=Column();
        Preview=new CharacterPreview {CustomMinimumSize=new Vector2(0,474),SizeFlagsVertical=SizeFlags.ExpandFill};
        previewBody.AddChild(Preview);
        previewBody.AddChild(FrontendSkin.Label("Drag to rotate · Scroll to zoom",16));
        _body.AddChild(FrontendSkin.Panel(previewBody,new Vector2(342,0),new Vector2(470,650),creation:true));
        Preview.ShowClass(advanced.Artwork,advanced);
        var content=Column();
        _body.AddChild(FrontendSkin.TitledPanel(advanced.Name+" · Appearances",content,new Vector2(824,0),new Vector2(624,650)));
        if(!Collection.Available(_base,_advanced) || Preview.Assembly==null)
        {
            content.AddChild(FrontendSkin.Heading("Appearance profile unavailable",22));
            content.AddChild(FrontendSkin.Label("Compatible character models and equipment have not been implemented for this advanced class.",20,true));
            content.AddChild(FrontendSkin.Label("Your other class loadouts are saved separately.",18,true));return;
        }
        content.AddChild(FrontendSkin.Label("Cosmetic wardrobe · Saved automatically",18));
        var slots=new GridContainer {Columns=4};slots.AddThemeConstantOverride("h_separation",6);slots.AddThemeConstantOverride("v_separation",6);
        foreach(string slot in new[]{"head","chest","hands","legs","feet","waist","main_hand"})
        {
            bool enabled=Collection.Catalog.Appearances.Any(a=>a.Slot==slot && Collection.Compatible(a,Preview.Assembly.Loadout.Context));
            var button=FrontendSkin.Button(SlotName(slot),()=> {_slot=slot;RefreshChoices();SelectSlots(slots);},slot==_slot);
            button.Name="Slot_"+slot;button.Disabled=!enabled;button.CustomMinimumSize=new Vector2(132,61);button.AddThemeFontSizeOverride("font_size",17);
            // The recovered square icon frame supplies the equipment cell artwork.
            button.Icon=SwtorSkin.Slice("bwaui_buttons_381");button.ExpandIcon=true;button.AddThemeConstantOverride("icon_max_width",30);
            button.TooltipText=enabled ? SlotName(slot)+" cosmetic appearances" : "Unavailable · no compatible converted appearance";
            slots.AddChild(button);
        }
        content.AddChild(slots);
        _choices=Column();content.AddChild(_choices);
        var resetRow=new HBoxContainer();
        resetRow.AddChild(FrontendSkin.Button("Reset Slot",()=>Equip(null,false)));
        resetRow.AddChild(FrontendSkin.Button("Reset Outfit",()=>Equip(null,true)));content.AddChild(resetRow);
        if(OS.GetCmdlineUserArgs().Contains("--wardrobe-dev"))
            content.AddChild(FrontendSkin.Button("DEV · Unlock SS A04",()=> {
                Collection.DeveloperUnlock(out string error);RefreshChoices();_status.Text=error=="" ? "Alternate pieces unlocked for visual review." : error;
            }));
        _status=FrontendSkin.Label(Collection.Notice=="" ? "Appearance only · No combat bonuses" : Collection.Notice,16,true);content.AddChild(_status);
        RefreshChoices();category.GrabFocus();
    }
    private void SelectSlots(GridContainer grid)
    {
        foreach(var b in grid.GetChildren().OfType<Button>())
        {
            bool selected=b.Name=="Slot_"+_slot;
            foreach(string state in new[]{"normal","hover","pressed","hover_pressed","disabled","focus"})
                b.AddThemeStyleboxOverride(state,FrontendSkin.ButtonArtwork(hovered:selected || state is "hover" or "pressed" or "hover_pressed" or "focus",selected:selected));
        }
    }
    public static string SlotName(string slot) => slot=="main_hand" ? "Weapon" : char.ToUpperInvariant(slot[0])+slot[1..];
    private const int CellSize=72;
    private Label _detail=null!;
    // Inventory cell: recovered square item frame plus icon; the name and unlock text live in the detail line and tooltip.
    private Button AppearanceCell(AppearanceDefinition a,string name,string state,bool compatible,bool unlocked,bool selected,string hint)
    {
        var b=new SwtorButton {Name="Appearance_"+a.Id,CustomMinimumSize=new Vector2(CellSize,CellSize),Disabled=!compatible || !unlocked,
            FocusMode=Control.FocusModeEnum.All};
        var frame=SwtorSkin.Box("bwaui_buttons_381",14,14,14,14);
        foreach(string st in new[]{"normal","hover","pressed","hover_pressed","disabled"})
        {
            var box=(StyleBoxTexture)frame.Duplicate();
            box.ModulateColor=selected ? SwtorSkin.Gold : st=="hover" ? Colors.White : new Color(0.85f,0.9f,1f);
            b.AddThemeStyleboxOverride(st,box);
        }
        b.AddThemeStyleboxOverride("focus",new StyleBoxEmpty());
        string icon=Collection.Catalog.Items.FirstOrDefault(i=>i.Appearance==a.Id)?.Icon ?? "";
        // Never substitute another piece's icon.
        if(icon!="" && !ResourceLoader.Exists(icon)) icon="";
        b.Icon=icon!="" ? FrontendSkin.Texture(icon) : null;b.ExpandIcon=true;b.IconAlignment=HorizontalAlignment.Center;
        b.AddThemeConstantOverride("icon_max_width",CellSize-22);
        foreach(string c in new[]{"icon_normal_color","icon_hover_color","icon_pressed_color","icon_disabled_color","icon_focus_color"})
            b.AddThemeColorOverride(c,unlocked && compatible ? Colors.White : new Color(0.35f,0.4f,0.45f,0.8f));
        string line=name+" · "+state+(compatible && !unlocked && hint!="" ? "\nUnlock: "+hint : "");
        b.TooltipText=name+" · "+SlotName(_slot)+"\n"+state+"\nCosmetic appearance only. No gameplay statistics."
            +(!compatible ? "\nNo compatible converted resource for this profile." : !unlocked ? "\n"+(hint!="" ? "Unlock: "+hint : "Not yet unlocked.") : "\nSelect to equip and save immediately.");
        b.MouseEntered+=()=>_detail.Text=line;b.FocusEntered+=()=>_detail.Text=line;
        b.Pressed+=()=>Equip(a.Id,false);
        return b;
    }
    private void RefreshChoices()
    {
        foreach(var child in _choices.GetChildren()) {_choices.RemoveChild(child);child.QueueFree();}
        _choices.AddChild(FrontendSkin.Heading(SlotName(_slot),21));
        var assembly=Preview.Assembly!;
        string equipped=assembly.Loadout.Current.Cosmetics.GetValueOrDefault(_slot) ?? Collection.DefaultAppearance(_slot,_advanced);
        var grid=new GridContainer {Columns=6};grid.AddThemeConstantOverride("h_separation",6);grid.AddThemeConstantOverride("v_separation",6);
        _detail=FrontendSkin.Label("Hover a piece for details",16,true);_detail.CustomMinimumSize=new Vector2(0,44);
        foreach(var a in Collection.Catalog.Appearances.Where(a=>a.Slot==_slot
            && Collection.State.Unlocked.Contains(a.Id) && Collection.Compatible(a,assembly.Loadout.Context)))
        {
            bool compatible=Collection.Compatible(a,assembly.Loadout.Context),unlocked=Collection.State.Unlocked.Contains(a.Id),selected=equipped==a.Id;
            string state=!compatible ? "Incompatible" : selected ? "Equipped" : unlocked ? "Unlocked" : "Locked";
            string name=Collection.Catalog.Items.FirstOrDefault(i=>i.Appearance==a.Id)?.DisplayName??a.Id;
            grid.AddChild(AppearanceCell(a,name,state,compatible,unlocked,selected,Collection.UnlockHint(a.Id)));
        }
        _choices.AddChild(grid);_choices.AddChild(_detail);
    }
    private void Equip(string? id,bool all)
    {
        var assembly=Preview.Assembly!;var before=assembly.Loadout.Current;
        var next=before.Copy();if(all) next.Cosmetics.Clear();else if(id==null)next.Cosmetics.Remove(_slot);else next.Cosmetics[_slot]=id;
        if(id!=null && !Collection.State.Unlocked.Contains(id))return;
        if(!assembly.Loadout.TryApply(next,out string error)) {_status.Text=error;return;}
        var profile=Collection.Profile(_base,_advanced);var old=profile.Equipped;profile.Equipped=new(assembly.Loadout.Current.Cosmetics);
        if(!Collection.Save(out error)) {profile.Equipped=old;assembly.Loadout.TryApply(before,out _);}
        _status.Text=error=="" ? "Saved · Appearance only · No combat bonuses" : error;RefreshChoices();
    }
}
