using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors.Equipment;

/// <summary>One brief targeted run on the actual gameplay rig; no historical regressions.</summary>
public partial class D1EquipmentSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string label,bool passed)
    { if(!passed)throw new InvalidOperationException(label);_checks.Add(label);GD.Print("D1: "+label); }
    private async Task Frames(int count)
    { for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    public async void Run(GameWorld world)
    {
        string? failure=null;
        try
        {
            world.Waves!.SetPhysicsProcess(false);world.Abilities.Abort();world.Abilities.SetPhysicsProcess(false);
            foreach(var ability in world.Abilities.Abilities)ability.SetPhysicsProcess(false);
            world.Player.SetPhysicsProcess(false);world.Progression!.ProcessMode=ProcessModeEnum.Disabled;
            var assembly=world.Equipment;var loadout=assembly.Loadout;var presentation=world.Player.Presentation;
            var skeleton=presentation.Skeleton;var tree=presentation.Tree;var animator=presentation.Animator;
            var health=world.PlayerActor.Health;var progression=world.Progression;var abilityController=world.Abilities;
            float hp=health.Current;var position=world.Player.Position;
            string error="";
            void StateIntact() => Check("Live rig, animation tree, health, abilities, progression and position preserved",
                presentation.Skeleton==skeleton && presentation.Tree==tree && presentation.Animator==animator
                && world.PlayerActor.Health==health && health.Current==hp && world.Progression==progression
                && world.Abilities==abilityController && world.Player.Position==position);
            Check("All 8 frontend base classes and 16 advanced IDs registered without new gameplay",
                loadout.Catalog.Classes.Length==16 && loadout.Catalog.Classes.Select(c=>c.BaseClassId).Distinct().Count()==8);
            Check("Sorcerer menu uses artwork",Frontend.FrontendCatalog.Load().Classes.SelectMany(c=>c.AdvancedClasses).Single(c=>c.Id=="sorcerer").PreviewScene=="");
            Check("Level-1 preset loads on existing rig",assembly.ActiveArmorMeshes==6 && loadout.Current.Cosmetics.Count==0);
            Check("Red lightsaber initialized on original RightWeapon",world.PlayerSaber.BladeInitialized && world.PlayerSaber.Socket.BoneName=="RightWeapon");
            Check("Equipped saber selects original saber idle and run",presentation.IdleClip=="EquipmentSaber/Idle" && presentation.RunClip=="EquipmentSaber/Run");
            var controls=world.GetChildren().OfType<EquipmentDeveloperControls>().Single();
            controls._UnhandledInput(new InputEventKey { Pressed=true,PhysicalKeycode=Key.F5 });
            Check("Gear review opens from a normal development run",controls.OutfitButton.IsVisibleInTree());
            controls.OutfitButton.EmitSignal(Button.SignalName.Pressed);
            Check("Visible outfit button changes to alternate cosmetic",loadout.Current.Cosmetics.GetValueOrDefault("chest")=="ss_a04.chest");
            controls.OutfitButton.EmitSignal(Button.SignalName.Pressed);
            Check("Visible outfit button restores starting appearance",loadout.Current.Cosmetics.Count==0);
            Check("Original animation library retained",new[]{"Idle","Run","LightningLoop","Hit","Death"}.All(clip=>animator.HasAnimation(clip)));
            foreach(string preset in new[]{"inquisitor_level1","sorcerer_ss_a04"})
            {
                Check(preset+" loads",loadout.TryPreset(preset,true,out error));
                presentation.Present(Vector3.Zero,.1);await Frames(12);Check(preset+" idle",presentation.State=="Idle");
                presentation.Present(Vector3.Right*.3f,.1);await Frames(12);
                Check(preset+" run",presentation.Locomotion=="Run" && presentation.MovementBlend>.5f);
                Check(preset+" starts upper-body cast",presentation.TryBeginGesture(this,"LightningLoop"));await Frames(12);
                var gesture=presentation.GestureOwner;var saber=world.PlayerSaber;var socket=saber.GlobalTransform;
                var other=loadout.Catalog.Presets.Single(p=>p.Id==(preset=="inquisitor_level1" ? "sorcerer_ss_a04" : "inquisitor_level1"));
                Check("Swap during moving cast",loadout.TryApply(loadout.Current with { Cosmetics=new(other.Cosmetics) },out error));
                Check("Cast owner and saber instance retained during armor swap",presentation.GestureOwner==gesture && world.PlayerSaber==saber);
                Check("Moving cast keeps saber locomotion after outfit swap",presentation.RunClip=="EquipmentSaber/Run");
                StateIntact();await Frames(10);
                Check("Saber follows animated socket after swap",!socket.IsEqualApprox(saber.GlobalTransform));
                foreach(string bone in new[]{"LeftWrist","RightWrist","RightWeapon","LeftWeapon","vfx_hand_left","vfx_hand_right","RightIndexFinger1"})
                    Check("Original attachment bone remains: "+bone,skeleton.FindBone(bone)>=0);
                Check("Original finger bones remain",Enumerable.Range(0,skeleton.GetBoneCount()).Count(i=>skeleton.GetBoneName(i).ToString().Contains("Finger"))>10);
                presentation.ReleaseGesture(this);await Frames(15);
                presentation.PlayReaction("Hit");await Frames(2);
                Check("Swap during hit reaction",loadout.TryPreset(preset,true,out error) && presentation.OverrideActive);
                await ToSignal(GetTree().CreateTimer(animator.GetAnimation("Hit").Length+.2),SceneTreeTimer.SignalName.Timeout);
            }
            Check("A default cannot unlock developer preset",!loadout.TryPreset("sorcerer_ss_a04",false,out error));
            Check("Single-slot cosmetic applies",loadout.TrySetCosmetic("chest","ss_a04.chest",out error));
            Check("Cosmetic removal restores functional visual",loadout.TrySetCosmetic("chest",null,out error));
            Check("Armor unequip uses rig fallback without geometry holes",loadout.TryUnequip("chest",out error) && assembly.ActiveArmorMeshes>=6);
            Check("Armor re-equip",loadout.TryEquip("chest","level1.chest",out error));
            Check("Weapon visual unequips",loadout.TryUnequip("main_hand",out error) && assembly.Weapon("main_hand")==null);
            Check("Unequip returns to base unarmed locomotion",presentation.IdleClip=="Idle" && presentation.RunClip=="Run");
            Check("Weapon visual restores",loadout.TryEquip("main_hand","d1.red_saber",out error) && world.PlayerSaber.BladeInitialized);
            Check("Re-equip restores saber locomotion without rebuilding tree",presentation.IdleClip=="EquipmentSaber/Idle" && presentation.RunClip=="EquipmentSaber/Run" && presentation.Tree==tree);
            controls.SaberButton.EmitSignal(Button.SignalName.Pressed);
            Check("Visible saber button removes weapon",assembly.Weapon("main_hand")==null && presentation.IdleClip=="Idle");
            controls.SaberButton.EmitSignal(Button.SignalName.Pressed);
            Check("Visible saber button restores weapon pose",world.PlayerSaber.BladeInitialized && presentation.IdleClip=="EquipmentSaber/Idle");
            Check("Hidden weapon supported",loadout.TryHide("main_hand",true,out error) && assembly.Weapon("main_hand")==null);
            Check("Unhide restores weapon",loadout.TryHide("main_hand",false,out error) && world.PlayerSaber.Socket.BoneName=="RightWeapon");
            var before=JsonSerializer.Serialize(loadout.Current);int meshCount=assembly.ActiveArmorMeshes;
            Check("Wrong item slot rejected",!loadout.TryEquip("off_hand","d1.red_saber",out error));
            Check("Unknown equipment rejected",!loadout.TryEquip("chest","missing.item",out error));
            // Inject an incompatible appearance in this local smoke catalog only, then verify atomic rejection.
            var source=loadout.Catalog.Appearances.First();
            loadout.Catalog.Appearances=loadout.Catalog.Appearances.Append(source with { Id="smoke.wrong_rig",Compatibility=source.Compatibility with { RigProfiles=["creature"] } }).ToArray();
            Check("Incompatible rig rejected clearly",!loadout.TrySetCosmetic(source.Slot,"smoke.wrong_rig",out error) && error.Contains("Requires rig"));
            loadout.Catalog.Appearances=loadout.Catalog.Appearances.Append(source with { Id="smoke.missing_bone",RequiredBones=["AbsentAccessoryBone"] }).ToArray();
            Check("Missing accessory bone rejected",!loadout.TrySetCosmetic(source.Slot,"smoke.missing_bone",out error) && error.Contains("AbsentAccessoryBone"));
            Check("Failed operations leave loadout and visuals intact",JsonSerializer.Serialize(loadout.Current)==before && assembly.ActiveArmorMeshes==meshCount);
            // A pure loadout with synthetic stats demonstrates modifiers are counted from functional items once.
            var item=loadout.Catalog.Items.First(i=>i.Id=="level1.chest");
            loadout.Catalog.Items=loadout.Catalog.Items.Append(item with { Id="smoke.stats",StatModifiers=new() { ["test"]=3 } }).ToArray();
            var logic=new EquipmentLoadout(loadout.Catalog,loadout.Context);
            Check("Functional stats counted once with separate cosmetic",logic.TryEquip("chest","smoke.stats",out error)
                && logic.TrySetCosmetic("chest","ss_a04.chest",out error) && logic.StatModifiers()["test"]==3);
            presentation.PlayReaction("Death");await Frames(4);
            Check("Outfit swap preserves death animation",loadout.TryPreset("inquisitor_level1",false,out error) && presentation.State=="Death");
            StateIntact();presentation.ResetPresentation();
            Check("One live skeleton; no competing imported animator",world.Player.Presentation.FindChildren("*","Skeleton3D",true,false).Count==1
                && world.Player.Presentation.FindChildren("*","AnimationPlayer",true,false).Count==1);
        }
        catch(Exception ex) { failure=ex.ToString();GD.PushError("D1: "+failure); }
        // Existing engine workaround: finalize temporary Godot collections before native shutdown.
        GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/d1-smoke.json"),
            JsonSerializer.Serialize(new { passed=failure==null,checks=_checks,failure },new JsonSerializerOptions { WriteIndented=true }));
        GetTree().Quit(failure==null ? 0 : 1);
    }
}
