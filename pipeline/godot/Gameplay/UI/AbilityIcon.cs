using Godot;

namespace SwtorSurvivors.UI;

public partial class AbilityIcon : Control
{
    public string IconName { get; set; }="sithsorcerer";
    public float SlotSize { get; set; }=64;
    public float IconPadding { get; set; }=8;
    public string FrameSlice { get; set; }="bwaui_buttons_381";
    public Texture2D? FrameTexture { get; set; }
    private CooldownWipe? _cooldown;
    private CooldownWipe? _gcd;
    private Label _time=null!;
    private Label _rank=null!;
    private ColorRect _rarity=null!;
    public override void _Ready()
    {
        CustomMinimumSize=new Vector2(SlotSize,SlotSize);
        var frame=new TextureRect { ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=FrameTexture??SwtorSkin.Slice(FrameSlice),Size=new Vector2(SlotSize,SlotSize),
            MouseFilter=MouseFilterEnum.Ignore };
        AddChild(frame);
        if (!string.IsNullOrEmpty(IconName))
        {
            var icon=new TextureRect { ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=SwtorSkin.Icon(IconName),Position=new Vector2(IconPadding,IconPadding),Size=new Vector2(SlotSize-2*IconPadding,SlotSize-2*IconPadding),
                MouseFilter=MouseFilterEnum.Ignore };
            AddChild(icon);
            _cooldown=new CooldownWipe { Name="IndividualCooldownSweep",Position=icon.Position,Size=icon.Size };
            AddChild(_cooldown);
        }
        // Original ConversionIconButton has separate cooldown and GCD copies of sprite 140.
        // The shared vertical wipe also covers empty quickslots.
        _gcd=new CooldownWipe { Name="GlobalCooldownSweep",Position=new Vector2(IconPadding,IconPadding),
            Size=Vector2.One*(SlotSize-2*IconPadding) };
        AddChild(_gcd);
        _time=SwtorSkin.Text("",SlotSize<64 ? 15 : 20,true); _time.HorizontalAlignment=HorizontalAlignment.Center;_time.VerticalAlignment=VerticalAlignment.Center;
        AddChild(_time); _time.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _rank=SwtorSkin.Text("",10,true);_rank.Position=new Vector2(4,SlotSize-16);_rank.Size=new Vector2(SlotSize-8,12);
        _rank.HorizontalAlignment=HorizontalAlignment.Right;_rank.MouseFilter=MouseFilterEnum.Ignore;AddChild(_rank);
        _rarity=new ColorRect {Position=new Vector2(4,SlotSize-4),Size=new Vector2(SlotSize-8,2),Visible=false,MouseFilter=MouseFilterEnum.Ignore};
        AddChild(_rarity);
    }
    public void UpdateCooldown(double remaining,double duration,double globalRemaining=0,double globalDuration=1.5,bool casting=false)
    {
        _cooldown?.UpdateRemaining(remaining,duration);
        // The ability's own cooldown takes priority over the shared cooldown sweep.
        _gcd?.UpdateRemaining(remaining>0?0:globalRemaining,globalDuration);
        _time.Text=remaining>0 ? remaining.ToString("0.0") : casting?"ACTIVE":globalRemaining>0?"GCD":"";
        _time.AddThemeFontSizeOverride("font_size",remaining>0?15:10);
    }
    public void UpdateGlobalCooldown(double remaining,double duration)
    {
        _gcd?.UpdateRemaining(remaining,duration);
    }
    public void UpdateUpgrade(int rank,PowerRarity rarity)
    {
        var color=rarity switch {PowerRarity.Prototype=>new Color("60c2ed"),PowerRarity.Artifact=>new Color("be8bf1"),_=>new Color("55d884")};
        _rank.Text=rank>0?$"+{rank}":"";_rank.AddThemeColorOverride("font_color",color);
        _rarity.Visible=rank>0;_rarity.Color=color;
    }
    public override GodotObject _MakeCustomTooltip(string forText) => new SwtorTooltip(forText);
}
