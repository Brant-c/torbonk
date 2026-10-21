using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using SwtorSurvivors.Equipment;
using SwtorSurvivors.Frontend;

namespace SwtorSurvivors.Integration;

/// <summary>One short wardrobe session plus a restart to verify disk persistence and run handoff.</summary>
public partial class D11Smoke : Node
{
    private readonly List<string> _checks=new();
    private static IEnumerable<Node> Walk(Node n) {foreach(var c in n.GetChildren()) {yield return c;foreach(var d in Walk(c))yield return d;}}
    private Node Scene=>GetTree().CurrentScene;
    private CollectionsScreen Screen=>Walk(Scene).OfType<CollectionsScreen>().Single();
    private void Check(string label,bool ok) {if(!ok)throw new InvalidOperationException(label);_checks.Add(label);GD.Print("D1.1: "+label);}
    private async Task Frames(int n=3) {for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private async Task Until(Func<bool> predicate) {for(int i=0;i<180 && !predicate();i++)await Frames(1);Check("Transition completed",predicate());await Frames();}
    private Button Button(string text)=>Walk(Scene).OfType<Button>().Single(b=>b.IsVisibleInTree() && b.Text==text);
    private async Task Click(string text)
    {
        var b=Button(text);Check("Enabled: "+text,!b.Disabled);
        var p=b.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion {Position=p,GlobalPosition=p},true);
        foreach(bool down in new[]{true,false}) GetViewport().PushInput(new InputEventMouseButton {Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,Pressed=down},true);
        await Frames();
    }
    private string Page=>Walk(Scene).OfType<SelectionPage>().Single().StepId;
    private async Task Choose(string text,string page) {await Click(text);await Until(()=>Page==page);}
    public override async void _Ready()
    {
        bool restore=OS.GetCmdlineUserArgs().Contains("--d11-smoke-load");string? failure=null;
        try
        {
            await Frames(8);
            Check("Main menu contains New Run, Collections, Settings, Exit",new[]{"New Run","Collections","Settings","Exit"}.All(t=>Button(t)!=null));
            await Click("Collections");
            Check("Collections opens from menu with shared assembly",Screen.Preview.Assembly!=null);
            Check("16 distinct profiles for 8 base classes",Screen.Collection.State.Profiles.Count==16 && Screen.Collection.Catalog.Classes.Select(c=>c.BaseClassId).Distinct().Count()==8);
            Check("Head disabled without converted content",Button("Head").Disabled);
            if(!restore)
            {
                Check("Starter equipped by default",Screen.Preview.Assembly!.Loadout.Current.Cosmetics.Count==0);
                Check("Alternate locked",Button("Sorcerer SS A04 · Locked").Disabled);
                await Click("DEV · Unlock SS A04");
                await Click("Sorcerer SS A04 · Unlocked");
                Check("Immediate chest preview",Screen.Preview.Assembly!.Loadout.Current.Cosmetics["chest"]=="ss_a04.chest");
                await Click("Reset Slot");Check("Per-slot reset",Screen.Preview.Assembly.Loadout.Current.Cosmetics.Count==0);
                await Click("Sorcerer SS A04 · Unlocked");
                await Click("Reset Outfit");Check("Full reset retains unlock",Screen.Preview.Assembly.Loadout.Current.Cosmetics.Count==0 && Screen.Collection.State.Unlocked.Contains("ss_a04.chest"));
                await Click("Sorcerer SS A04 · Unlocked");await Click("Feet");await Click("Sorcerer SS A04 · Unlocked");
                await Click("Sith Warrior");Check("Other class uses artwork and unavailable state",Screen.Preview.Assembly==null && Walk(Screen).OfType<Label>().Any(l=>l.Text=="Appearance profile unavailable"));
                await Click("Sith Inquisitor");
                Check("Sorcerer profile survives class switching",Screen.Preview.Assembly!.Loadout.Current.Cosmetics.Count==2);
                var preview=Screen.Preview;
                var camera=Walk(preview).OfType<Camera3D>().Single();float fov=camera.Fov;
                preview.EmitSignal(Control.SignalName.GuiInput,new InputEventMouseButton {ButtonIndex=MouseButton.WheelUp,Pressed=true});
                Check("Preview zoom responds",camera.Fov<fov);
                var rotation=camera.GetParent().GetNode<Node3D>("PreviewRotation");
                var before=rotation.Rotation;
                preview.EmitSignal(Control.SignalName.GuiInput,new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=true});
                preview.EmitSignal(Control.SignalName.GuiInput,new InputEventMouseMotion {Relative=new Vector2(30,0)});
                preview.EmitSignal(Control.SignalName.GuiInput,new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=false});
                Check("Preview rotation responds",rotation.Rotation!=before);
                preview.ResetView();await Frames(12);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng("res://../work/d11-collections.png");
            }
            else
            {
                Check("Equipped cosmetics survive actual application restart",Screen.Preview.Assembly!.Loadout.Current.Cosmetics.GetValueOrDefault("chest")=="ss_a04.chest"
                    && Screen.Preview.Assembly.Loadout.Current.Cosmetics.GetValueOrDefault("feet")=="ss_a04.feet");
                Check("Unlocks survive application restart",Screen.Collection.State.Unlocked.Contains("ss_a04.chest"));
                await Click("Main Menu");await Choose("New Run","Faction");await Choose("Sith Empire","BaseClass");
                await Click("Sith Inquisitor");await Choose("Continue","AdvancedClass");await Click("Sorcerer");
                await Choose("Continue","Galaxy");await Click("Korriban");await Choose("Continue","Ready");await Click("Play");
                await Until(()=>Scene is GameWorld {Hud:not null});var world=(GameWorld)Scene;
                Check("Run applies saved compatible wardrobe",world.Equipment.Loadout.Current.Cosmetics.GetValueOrDefault("chest")=="ss_a04.chest"
                    && world.Equipment.Loadout.Current.Cosmetics.GetValueOrDefault("feet")=="ss_a04.feet");
                Check("No standard gameplay gear controls",!Walk(world).OfType<EquipmentDeveloperControls>().Any());
                var health=world.PlayerActor.Health;var ability=world.Combat;float speed=world.Player.MoveSpeed;
                var stats=JsonSerializer.Serialize(world.Equipment.Loadout.StatModifiers());
                var definition=GD.Load<AbilityDefinition>("res://Gameplay/Abilities/lightning.tres");
                Check("Starting health, movement, damage, cooldown, shield and ability availability unchanged",health.Maximum==100 && speed==.6f
                    && health.ShieldCapacity==0 && ability.DamagePerTick==definition.Damage && ability.CooldownSeconds==definition.CooldownSeconds && ability.Unlocked==definition.StartsUnlocked);
                var outfit=JsonSerializer.Serialize(world.Equipment.Loadout.Current);
                foreach(var key in new[]{Key.F5,Key.F6,Key.F7,Key.F8}) GetViewport().PushInput(new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=true},true);
                await Frames();Check("F5–F8 leave wardrobe intact",JsonSerializer.Serialize(world.Equipment.Loadout.Current)==outfit);
                Check("Cosmetics contain no stat modifiers",stats=="{}" && world.Equipment.Loadout.Current.Items.All(p=>p.Value==world.Equipment.Loadout.Catalog.Presets.Single(p=>p.Id=="inquisitor_level1").Items[p.Key]));
                Check("Saber socket and animation presentation preserved",world.PlayerSaber.BladeInitialized && world.PlayerSaber.Socket.BoneName=="RightWeapon" && world.Player.Presentation.IdleClip=="EquipmentSaber/Idle");
                var collection=new CosmeticCollection();var profile=collection.Profile("sith_inquisitor","sorcerer");
                profile.Equipped["head"]="ss_a04.chest";profile.Equipped["chest"]="missing.saved.id";
                profile.Equipped["feet"]="ss_a04.chest";profile.Equipped["main_hand"]="red_saber";
                collection.Save(out _);var sanitized=new CosmeticCollection();
                Check("Unknown and wrong-slot saved IDs fall back",!sanitized.Profile("sith_inquisitor","sorcerer").Equipped.ContainsKey("chest")
                    && !sanitized.Profile("sith_inquisitor","sorcerer").Equipped.ContainsKey("head") && !sanitized.Profile("sith_inquisitor","sorcerer").Equipped.ContainsKey("feet"));
                // Restore the review outfit after the corrupt-save probe.
                profile.Equipped=new(world.Equipment.Loadout.Current.Cosmetics);collection.Save(out _);
                world.ResetCombatWorld();Check("Reset run retains cosmetic save",new CosmeticCollection().Profile("sith_inquisitor","sorcerer").Equipped.GetValueOrDefault("chest")=="ss_a04.chest");
            }
        }
        catch(Exception ex) {failure=ex.ToString();GD.PushError("D1.1: "+failure);}
        GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/d11-"+(restore?"load":"save")+"-smoke.json"),JsonSerializer.Serialize(new {passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null ? 0 : 1);
    }
}
