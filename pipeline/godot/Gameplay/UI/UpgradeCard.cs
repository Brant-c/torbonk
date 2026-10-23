using Godot;

namespace SwtorSurvivors.UI;

public partial class UpgradeCard : SwtorButton
{
    private Label _select = null!;
    private bool _hover, _confirmed, _selected;
    private float _pulse;
    private Color _rarityColor;
    public void Confirm(bool selected)
    {
        _confirmed = true; _selected = selected; _pulse = 0; Disabled = true;
        if (selected) { _select.Text = "APPLIED"; _select.AddThemeColorOverride("font_color", _rarityColor); }
    }
    public override void _Ready()
    {
        base._Ready(); MouseEntered += () => _hover = true; MouseExited += () => _hover = false;
    }
    public override void _Process(double delta)
    {
        _pulse += (float)delta;
        PivotOffset = Size / 2;
        float target = _confirmed ? (_selected ? 1.025f : 0.98f) : _hover && !Disabled ? 1.012f : 1;
        Scale = Scale.Lerp(Vector2.One * target, 1-Mathf.Exp(-(float)delta*18));
        if (_confirmed)
        {
            float flash = _selected ? 1 + Mathf.Sin(Mathf.Min(1, _pulse/0.45f)*Mathf.Pi)*0.35f : 0.4f;
            Modulate = _selected ? Colors.White.Lerp(_rarityColor, 0.22f) * flash : new Color(0.4f,0.4f,0.4f);
        }
    }
    public UpgradeCard(UpgradeDefinition definition,int rank,int number,string itemType="")
    {
        CustomMinimumSize=new Vector2(228,320);
        SizeFlagsHorizontal=SizeFlags.ExpandFill;
        var (rarityColor,rarityFrame)=definition.Rarity switch
        {
            PowerRarity.Prototype => (new Color("60c2ed"),"rarity_prototype"),
            PowerRarity.Artifact => (new Color("be8bf1"),"rarity_artifact"),
            _ => (new Color("55d884"),"rarity_premium")
        };
        _rarityColor = rarityColor;
        bool unlock=definition.Effect==UpgradeEffect.UnlockAbility;
        TooltipText=definition.Title+" · "+definition.Rarity+"\n"+definition.Description+
            (unlock?"":definition.Effect==UpgradeEffect.Mechanic?"\nFixed mechanical effect; rarity identifies its tier.":$"\nBonuses last for this run. Rarity strength ×{definition.RarityRules?.Multiplier(definition.Rarity)??1:0.##}.");
        AddThemeStyleboxOverride("normal",SwtorSkin.ButtonArtwork());
        AddThemeStyleboxOverride("hover",SwtorSkin.ButtonArtwork(hovered:true));
        AddThemeStyleboxOverride("pressed",SwtorSkin.ButtonArtwork(hovered:true));
        AddThemeStyleboxOverride("disabled",SwtorSkin.ButtonArtwork());
        var focus=SwtorSkin.ButtonArtwork(hovered:true); focus.DrawCenter=false;
        AddThemeStyleboxOverride("focus",focus);
        var content=new VBoxContainer { MouseFilter=MouseFilterEnum.Ignore };
        content.AddThemeConstantOverride("separation",8);
        AddChild(content); content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        content.OffsetLeft=18;content.OffsetTop=20;content.OffsetRight=-18;content.OffsetBottom=-20;
        var title=SwtorSkin.Text(definition.Title,17);title.HorizontalAlignment=HorizontalAlignment.Center;
        title.CustomMinimumSize=new Vector2(0,42);title.VerticalAlignment=VerticalAlignment.Center;
        title.AddThemeColorOverride("font_color",rarityColor);
        title.AutowrapMode=TextServer.AutowrapMode.WordSmart;content.AddChild(title);
        var center=new CenterContainer {MouseFilter=MouseFilterEnum.Ignore}; content.AddChild(center);
        var icon=new AbilityIcon { IconName=definition.Icon,SlotSize=56,IconPadding=4,
            FrameTexture=SwtorSkin.HudArt(rarityFrame),MouseFilter=MouseFilterEnum.Ignore };center.AddChild(icon);
        var rankLabel=SwtorSkin.Text($"{definition.Rarity.ToString().ToUpperInvariant()}  ·  "+(itemType!=""?itemType:unlock?"NEW ABILITY":definition.Effect==UpgradeEffect.Mechanic?$"PASSIVE RANK {rank+1}":$"RANK {rank+1}"),12);
        rankLabel.AddThemeColorOverride("font_color",rarityColor);
        rankLabel.HorizontalAlignment=HorizontalAlignment.Center;content.AddChild(rankLabel);
        var description=SwtorSkin.Text(Summary(definition),15);description.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        description.HorizontalAlignment=HorizontalAlignment.Center;description.VerticalAlignment=VerticalAlignment.Center;
        description.SizeFlagsVertical=SizeFlags.ExpandFill;content.AddChild(description);
        _select=SwtorSkin.Text($"{(itemType!=""?"SELECT":unlock?"UNLOCK":"TRAIN")}  [{number}]",14);_select.AddThemeColorOverride("font_color",SwtorSkin.Blue);
        _select.HorizontalAlignment=HorizontalAlignment.Center;content.AddChild(_select);
    }

    private static string Summary(UpgradeDefinition power) => power.Description;
}
