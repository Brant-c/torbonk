using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using SwtorSurvivors.Equipment;

namespace SwtorSurvivors.Integration;

/// <summary>Gameplay cosmetic unlocks: legacy save compatibility, milestone rules, persistence and the live run tracker.</summary>
public partial class UnlockSmoke : Node
{
    private readonly List<string> _checks=new();
    private static IEnumerable<Node> Walk(Node n) {foreach(var c in n.GetChildren()) {yield return c;foreach(var d in Walk(c))yield return d;}}
    private Node Scene=>GetTree().CurrentScene;
    private void Check(string label,bool ok) {if(!ok)throw new InvalidOperationException(label);_checks.Add(label);GD.Print("Unlocks: "+label);}
    private async Task Frames(int n=3) {for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private async Task Until(Func<bool> predicate) {for(int i=0;i<180 && !predicate();i++)await Frames(1);Check("Transition completed",predicate());await Frames();}
    private async Task Click(string text)
    {
        var b=Walk(Scene).OfType<Button>().Single(b=>b.IsVisibleInTree() && b.Text==text);Check("Enabled: "+text,!b.Disabled);
        var p=b.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion {Position=p,GlobalPosition=p},true);
        foreach(bool down in new[]{true,false}) GetViewport().PushInput(new InputEventMouseButton {Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,Pressed=down},true);
        await Frames();
    }
    private string Page=>Walk(Scene).OfType<Frontend.SelectionPage>().Single().StepId;
    private async Task Choose(string text,string page) {await Click(text);await Until(()=>Page==page);}
    private static void WriteSave(string json) => System.IO.File.WriteAllText(ProjectSettings.GlobalizePath(CosmeticCollection.SavePath),json);
    private static void DeleteSave() {if(FileAccess.FileExists(CosmeticCollection.SavePath))DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(CosmeticCollection.SavePath));}
    public override async void _Ready()
    {
        string? failure=null;
        try
        {
            await Frames(8);
            // A D1.1-era save: no Milestones field, alternate chest unlocked via the developer option and equipped.
            WriteSave("{\"SchemaVersion\":1,\"Unlocked\":[\"ss_a04.chest\"],\"Profiles\":{\"sith_inquisitor/sorcerer\":{\"DefaultLoadout\":\"inquisitor_level1\",\"Equipped\":{\"chest\":\"ss_a04.chest\"}}}}");
            var legacy=new CosmeticCollection();
            Check("Legacy save loads without notice",legacy.Notice=="");
            Check("Legacy save keeps unlocked and equipped chest",legacy.State.Unlocked.Contains("ss_a04.chest")
                && legacy.Profile("sith_inquisitor","sorcerer").Equipped.GetValueOrDefault("chest")=="ss_a04.chest");
            Check("Legacy save starts with no milestones",legacy.State.Milestones.Count==0);

            DeleteSave();var fresh=new CosmeticCollection();
            Check("Every SS A04 piece has a gameplay unlock",fresh.Catalog.Presets.Single(p=>p.Id=="sorcerer_ss_a04").Cosmetics.Values.All(id=>fresh.UnlockHint(id)!=""));
            Check("Milestones reference known appearances",fresh.Unlocks.Milestones.SelectMany(m=>m.Appearances).All(id=>fresh.Catalog.Appearances.Any(a=>a.Id==id)));
            Check("Alternate pieces start locked",!fresh.State.Unlocked.Any(id=>id.StartsWith("ss_a04.")));
            Check("Feet hint names its milestone",fresh.UnlockHint("ss_a04.feet")=="Reach wave 2 on Korriban");
            Check("Other base classes earn nothing",fresh.Award(m=>m.Met("sith_warrior",9,true),out _).Length==0);
            var earned=fresh.Award(m=>m.Met("sith_inquisitor",3,false),out string error);
            Check("Wave 3 earns waves 2 and 3",error=="" && earned.Select(m=>m.Id).SequenceEqual(new[]{"korriban.wave2","korriban.wave3"}));
            Check("Earned pieces unlock, later pieces stay locked",fresh.State.Unlocked.IsSupersetOf(new[]{"ss_a04.feet","ss_a04.hands"})
                && !fresh.State.Unlocked.Contains("ss_a04.legs") && !fresh.State.Unlocked.Contains("ss_a04.chest"));
            Check("Milestones award once",fresh.Award(m=>m.Met("sith_inquisitor",3,false),out _).Length==0);
            var reloaded=new CosmeticCollection();
            Check("Milestones and unlocks persist",reloaded.State.Milestones.SetEquals(new[]{"korriban.wave2","korriban.wave3"}) && reloaded.State.Unlocked.Contains("ss_a04.hands"));
            reloaded.State.Unlocked.Remove("ss_a04.feet");reloaded.Save(out _);
            Check("Completed milestones re-grant missing pieces",new CosmeticCollection().State.Unlocked.Contains("ss_a04.feet"));

            await Choose("New Run","Faction");await Choose("Sith Empire","BaseClass");
            await Click("Sith Inquisitor");await Choose("Continue","AdvancedClass");await Click("Sorcerer");
            await Choose("Continue","Galaxy");await Click("Korriban");await Choose("Continue","Ready");await Click("Play");
            await Until(()=>Scene is GameWorld {Hud:not null});var world=(GameWorld)Scene;
            var tracker=world.CosmeticUnlocks;
            Check("Run hosts the unlock tracker",tracker!=null && tracker.Enabled);
            var outfit=JsonSerializer.Serialize(world.Equipment.Loadout.Current);float health=world.PlayerActor.Health.Maximum;
            var live=tracker!.Evaluate(5,true);
            Check("Wave 5 and boss defeat earn the remaining pieces",live.Select(m=>m.Id).SequenceEqual(new[]{"korriban.wave4","korriban.wave5","korriban.boss"}));
            Check("Unlock toast names the pieces",tracker.LastToast.Contains("WARDROBE UNLOCKED") && tracker.LastToast.Contains("SS A04 Chest"));
            Check("All five alternate pieces unlocked on disk",new CosmeticCollection().State.Unlocked.IsSupersetOf(world.Equipment.Loadout.Catalog.Presets.Single(p=>p.Id=="sorcerer_ss_a04").Cosmetics.Values));
            Check("Unlocking leaves the live outfit and combat untouched",JsonSerializer.Serialize(world.Equipment.Loadout.Current)==outfit && world.PlayerActor.Health.Maximum==health);
            Check("Restart re-awards nothing",tracker.Evaluate(5,true).Length==0);
        }
        catch(Exception ex) {failure=ex.ToString();GD.PushError("Unlocks: "+failure);}
        GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/unlock-smoke.json"),JsonSerializer.Serialize(new {passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null ? 0 : 1);
    }
}
