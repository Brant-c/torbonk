using Godot;
using System;
using System.Linq;
using System.Text.Json;

namespace SwtorSurvivors.Equipment;

// Normal-player unlock source for cosmetic appearances. Rules are data; progress lives in CosmeticSave.Milestones.
public sealed record CosmeticMilestone(string Id, string DisplayName, string Kind, int Value,
    string[] BaseClasses, string[] Appearances)
{
    /// <summary>Wave: reach wave Value or later. Boss: defeat the run's boss encounter.</summary>
    public bool Met(string baseClass, int wave, bool bossDefeated) =>
        (BaseClasses.Length==0 || BaseClasses.Contains(baseClass))
        && Kind switch { "Wave" => wave>=Value, "Boss" => bossDefeated, _ => false };
}
public sealed class CosmeticUnlockCatalog
{
    public const string Path = "res://Gameplay/Equipment/cosmetic-unlocks.json";
    public CosmeticMilestone[] Milestones { get; set; } = [];
    public static CosmeticUnlockCatalog Load() => FileAccess.FileExists(Path)
        ? JsonSerializer.Deserialize<CosmeticUnlockCatalog>(FileAccess.GetFileAsString(Path)) ?? new() : new();
    public CosmeticMilestone? Source(string appearance) => Milestones.FirstOrDefault(m=>m.Appearances.Contains(appearance));
}

/// <summary>Watches the live run and records milestones once per save. Cosmetic only; never touches combat or loadout.</summary>
public partial class CosmeticUnlockTracker : CanvasLayer
{
    public GameWorld World { get; set; } = null!;
    // Validators and captures expect untouched Collections bytes; only normal launches (optionally --wardrobe-dev) record progress.
    public bool Enabled { get; set; } = OS.GetCmdlineUserArgs().All(a=>a is "--wardrobe-dev" or "--unlock-smoke");
    public string LastToast => _toast.Text;
    private Control _panel=null!;
    private Label _toast=null!;
    private double _toastSeconds;
    private int _wave;
    private bool _boss;
    public override void _Ready()
    {
        Layer=6;ProcessMode=ProcessModeEnum.Always;
        _panel=new UI.SwtorPanel { Theme=UI.SwtorSkin.MakeTheme(),MouseFilter=Control.MouseFilterEnum.Ignore,Visible=false };
        AddChild(_panel);_panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _panel.OffsetLeft=-210;_panel.OffsetRight=210;_panel.OffsetTop=96;
        _toast=UI.SwtorSkin.Text("",16);_toast.HorizontalAlignment=HorizontalAlignment.Center;
        _toast.AutowrapMode=TextServer.AutowrapMode.WordSmart;_panel.AddChild(_toast);
    }
    public override void _Process(double delta)
    {
        if(_toastSeconds>0 && (_toastSeconds-=delta)<=0)_panel.Visible=false;
        if(!Enabled || World.Waves==null || !World.PlayerActor.Alive)return;
        int wave=World.Waves.Wave;bool boss=World.Waves.Encounter.State==EncounterState.Defeated;
        // Re-evaluate only on a change; restart resets the wave, which is harmless because milestones are recorded once.
        if(wave==_wave && boss==_boss)return;
        _wave=wave;_boss=boss;Evaluate(wave,boss);
    }
    public CosmeticMilestone[] Evaluate(int wave,bool bossDefeated)
    {
        var collection=new CosmeticCollection();
        var earned=collection.Award(m=>m.Met(World.RunConfiguration.BaseClassId,wave,bossDefeated),out string error);
        if(error!="") {GD.PushWarning(error);Toast(error);return earned;}
        if(earned.Length==0)return earned;
        var names=earned.SelectMany(m=>m.Appearances).Select(id=>collection.Catalog.Items.FirstOrDefault(i=>i.Appearance==id)?.DisplayName??id);
        Toast("WARDROBE UNLOCKED\n"+string.Join(" · ",names)+"\nEquip it in Collections");
        return earned;
    }
    private void Toast(string text) {_toast.Text=text;_panel.Visible=true;_toastSeconds=5;}
}
