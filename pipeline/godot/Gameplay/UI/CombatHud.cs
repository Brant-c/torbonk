using Godot;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors.UI;

/// <summary>One player quickbar between joined native unit frames. Coordinates are design pixels.</summary>
public partial class CombatHud : Control
{
    public GameWorld World { get; set; }=null!;
    public const float DesignWidth=800;
    public const float DesignHeight=148;
    private SwtorBar _health=null!, _targetHealth=null!, _readiness=null!, _xp=null!;
    private readonly List<(AbilityIcon slot,AutomaticAbility ability)> _abilitySlots=new();
    private readonly List<AbilityIcon> _slotNodes=new();
    public int UnlockedAbilityCount => _abilitySlots.Count;
    private CharacterPortrait _targetPortrait=null!;
    private Label _level=null!, _targetName=null!;
    public int SlotCount { get; private set; }

    private void Art(Texture2D texture,Vector2 at,Vector2 size,bool flip=false)
    {
        AddChild(new TextureRect {ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=texture,
            Position=at,Size=size,FlipH=flip,MouseFilter=MouseFilterEnum.Ignore});
    }
    private Label LabelAt(string text,int font,Vector2 at,Vector2 size)
    {
        var label=SwtorSkin.Text(text,font);label.Position=at;label.Size=size;
        label.HorizontalAlignment=HorizontalAlignment.Center;label.VerticalAlignment=VerticalAlignment.Center;
        AddChild(label);return label;
    }
    private SwtorBar Bar(string slice,Vector2 at,Vector2 size,int fillHeight,int font=11,bool frame=false)
    {
        var bar=new SwtorBar {Position=at,Size=size,FillSlice=slice,FillHeight=fillHeight,DrawFrame=frame,
            MinimumHeight=0,HorizontalInset=frame?8:0,LabelSize=font};AddChild(bar);return bar;
    }

    public override void _Ready()
    {
        MouseFilter=MouseFilterEnum.Ignore;Size=new Vector2(DesignWidth,DesignHeight);
        // Keep each complete 342x108 contour at native aspect. The left unit mirrors the right.
        var unit=SwtorSkin.HudArt("unit_frame");
        Art(unit,new Vector2(0,12),unit.GetSize(),true);
        Art(unit,new Vector2(458,12),unit.GetSize());
        _health=Bar("bwaui_bars_21",new Vector2(136,44),new Vector2(196,11),7);
        _health.TooltipText="Player health. Mark of Power increases maximum health.";
        _readiness=Bar("bwaui_bars_60",new Vector2(136,57),new Vector2(196,7),5);
        _targetHealth=Bar("bwaui_bars_21",new Vector2(470,44),new Vector2(196,11),7);
        var name=LabelAt("SURVIVOR",14,new Vector2(136,23),new Vector2(196,18));
        name.AddThemeColorOverride("font_color",SwtorSkin.Blue);
        _targetName=LabelAt("",14,new Vector2(470,23),new Vector2(196,18));
        _targetName.AddThemeColorOverride("font_color",SwtorSkin.Blue);
        _level=LabelAt("1",13,new Vector2(100,42),new Vector2(28,24));

        // Native quickbar end/page controls, without the companion command module.
        // Only the connecting rules extend horizontally; detailed frame art keeps its aspect.
        Art(SwtorSkin.Slice("bwaui_bars_183"),new Vector2(118,74),new Vector2(564,2));
        Art(SwtorSkin.Slice("bwaui_bars_183"),new Vector2(118,124),new Vector2(564,2));
        Art(SwtorSkin.Slice("bwaui_quickslots_1"),new Vector2(94,72),new Vector2(27,54));
        Art(SwtorSkin.Slice("bwaui_quickslots_3"),new Vector2(680,72),new Vector2(27,54));
        RefreshAbilitySlots(World.Abilities.Abilities.Where(a=>a.Unlocked).Take(12).ToArray());
        Art(SwtorSkin.Slice("bwaui_quickslots_6"),new Vector2(685,72),new Vector2(19,54));
        LabelAt("1",11,new Vector2(687,91),new Vector2(15,16));
        _xp=Bar("bwaui_bars_45",new Vector2(118,130),new Vector2(564,12),3,10,true);

        AddChild(new CharacterPortrait {Position=new Vector2(11,28),
            TooltipText="Survivor · Sith Inquisitor\nWASD move · Abilities attack automatically\nR restart · F3 diagnostics · Esc quit"});
        _targetPortrait=new CharacterPortrait {Position=new Vector2(711,28),Mirrored=true,TooltipText="Current Lightning target"};
        AddChild(_targetPortrait);_targetPortrait.FaceVisible=false;
    }

    private void RefreshAbilitySlots(AutomaticAbility[] unlocked)
    {
        foreach(var slot in _slotNodes) {RemoveChild(slot);slot.QueueFree();}
        _slotNodes.Clear();_abilitySlots.Clear();SlotCount=0;
        for(int column=0;column<12;column++)
        {
            var ability=column<unlocked.Length?unlocked[column]:null;
            var slot=new AbilityIcon {IconName=ability?.Definition.Icon??"",SlotSize=46,IconPadding=4,
                FrameTexture=SwtorSkin.HudArt("player_slot"),Position=new Vector2(118+column*47,77),Size=new Vector2(46,46),
                TooltipText=ability!=null?ability.Definition.Title+" · automatic":"Empty player quickbar slot"};
            AddChild(slot);_slotNodes.Add(slot);SlotCount++;
            if(ability!=null) _abilitySlots.Add((slot,ability));
        }
    }

    public void RefreshAbilities()
    {
        var unlocked=World.Abilities.Abilities.Where(a=>a.Unlocked).Take(12).ToArray();
        if(!_abilitySlots.Select(x=>x.ability).SequenceEqual(unlocked)) RefreshAbilitySlots(unlocked);
    }

    public void UpdateHud()
    {
        RefreshAbilities();
        var p=World.Progression!;var health=World.PlayerActor.Health;var combat=World.Combat;
        _health.UpdateValue(health.Current,health.Maximum,$"{health.Current:0} / {health.Maximum:0}");
        double ready=1-World.Abilities.GlobalCooldownRemaining/System.Math.Max(0.001,World.Abilities.GlobalCooldownDuration);
        _readiness.UpdateValue(World.PlayerActor.Alive?ready:0,1,"");
        _readiness.TooltipText=$"{World.Abilities.Active?.Definition.Title??"Ready"}\nGlobal cooldown {World.Abilities.GlobalCooldownRemaining:0.0}s. No Force cost.";
        _xp.UpdateValue(p.Experience,p.RequiredExperience,"");
        _xp.TooltipText=$"LEVEL {p.Level} · {p.Experience} / {p.RequiredExperience} XP\nCollect XP markers to gain another upgrade.";
        _level.Text=p.Level.ToString();
        foreach(var slot in _slotNodes)
            slot.UpdateGlobalCooldown(World.Abilities.GlobalCooldownRemaining,World.Abilities.GlobalCooldownDuration);
        foreach(var (slot,ability) in _abilitySlots)
        {
            slot.UpdateCooldown(ability.CooldownRemaining,ability.CooldownDuration,
                ability.Definition.UsesGlobalCooldown?World.Abilities.GlobalCooldownRemaining:0,
                World.Abilities.GlobalCooldownDuration,ability.Casting);
            int ranks=p.AbilityRanks(ability.Definition.Id);var rarity=p.AbilityRarity(ability.Definition.Id);
            slot.UpdateUpgrade(ranks,rarity);
            string cadence=ability.ChannelSeconds>0?$"per tick · {ability.TickSeconds:0.00}s interval\n{(ability.Definition.PersistentArea?"Persistent area":"Burst channel")} {ability.ChannelSeconds:0.0}s":"burst";
            string area=ability.Definition.Area==AbilityArea.GroundSphere?$"\nArea radius {ability.Radius:0.00}":"";
            string tooltip=$"{ability.Definition.Title} · automatic · {ability.State}\n{ability.DamagePerTick:0.#} {ability.Definition.DamageType} damage {cadence}\nRange {ability.Range:0.00}{area}\nCooldown {ability.CooldownSeconds:0.00}s\n"+(ranks>0?$"{rarity} · {ranks} trained ranks":"Unlocked · no stat upgrades")+
                $"\n{(ability.Definition.UsesGlobalCooldown?"Waits for shared GCD":"Ignores shared GCD")} · {(ability.Definition.TriggersGlobalCooldown?"triggers GCD":"no GCD trigger")}."+
                "\nIndividual cooldown seconds. Shared vertical wipe: GCD. Clear slot: ready.";
            if(slot.TooltipText!=tooltip)slot.TooltipText=tooltip;
        }
        var target=World.Abilities.Active?.CastTarget??World.Targeting.Current;
        bool valid=GodotObject.IsInstanceValid(target)&&target!.Alive;
        _targetPortrait.FaceVisible=valid;
        _targetHealth.UpdateValue(valid?target!.Health.Current:0,valid?target!.Health.Maximum:1,valid?$"{target!.Health.Current:0} / {target.Health.Maximum:0}":"");
        _targetName.Text=valid?"HOSTILE":"";
        _targetPortrait.TooltipText=valid?target!.SelectionKey:"No hostile target";
    }
}

