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
            Check("Republic classes earn nothing",fresh.Award(m=>m.Met("jedi_knight",9,true),out _).Length==0);
            var lockedMenu=new Frontend.CollectionsScreen();AddChild(lockedMenu);await Frames();
            Check("Locked appearances are absent from the menu",!Walk(lockedMenu).OfType<Button>().Any(b=>b.Name.ToString().StartsWith("Appearance_wardrobe_") || b.Name.ToString().StartsWith("Appearance_ss_a04_")));
            lockedMenu.QueueFree();await Frames();
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
            var empire=new CosmeticCollection();
            Check("Twenty new pieces start locked",empire.Catalog.Appearances.Count(a=>a.Id.StartsWith("wardrobe."))==20
                && !empire.State.Unlocked.Any(a=>a.StartsWith("wardrobe.")));
            var expectedUnlocks=new HashSet<string>();
            foreach(var (baseClass,family) in new[]{("sith_inquisitor","inq"),("sith_warrior","war"),("imperial_agent","spy"),("bounty_hunter","bh")})
            {
                var rewards=empire.Award(m=>m.Met(baseClass,10,false),out error);
                var pieces=rewards.SelectMany(m=>m.Appearances).Where(a=>a.StartsWith("wardrobe.")).ToArray();
                Check(baseClass+" earns only its own five pieces",error=="" && pieces.Length==5 && pieces.All(a=>a.StartsWith("wardrobe."+family+".")));
                expectedUnlocks.UnionWith(pieces);
                empire=new CosmeticCollection();
                Check(baseClass+" unlocks persist without granting other families",expectedUnlocks.SetEquals(empire.State.Unlocked.Where(a=>a.StartsWith("wardrobe."))));
                foreach(var a in empire.Catalog.Appearances.Where(a=>pieces.Contains(a.Id)))
                    foreach(var pairedClass in a.Compatibility.BaseClasses)
                    {
                        var cl=empire.Catalog.Classes.First(c=>c.BaseClassId==pairedClass);
                        Check("Earned piece shared with "+pairedClass+": "+a.Id,empire.Compatible(a,empire.Context(cl.BaseClassId,cl.AdvancedClassId)));
                    }
            }
            foreach(string baseClass in new[]{"sith_warrior","sith_inquisitor","imperial_agent","bounty_hunter"})
            {
                var collection=new CosmeticCollection();
                var expected=collection.Unlocks.Milestones.Where(m=>m.Id.StartsWith("collectible.") && !collection.State.Milestones.Contains(m.Id) && m.Met(baseClass,13,false)).SelectMany(m=>m.Appearances).ToArray();
                Check("Collectible wave rewards: "+baseClass,expected.Length>0 && collection.Award(m=>m.Met(baseClass,13,false),out error).SelectMany(m=>m.Appearances).Order().SequenceEqual(expected.Order()));
                Check("Collectible rewards persist: "+baseClass,new CosmeticCollection().State.Unlocked.IsSupersetOf(expected));
            }
            var frontend=Frontend.FrontendCatalog.Load();
            foreach(var cl in empire.Catalog.Classes.Where(c=>new[]{"sith_warrior","sith_inquisitor","imperial_agent","bounty_hunter"}.Contains(c.BaseClassId)))
            {
                var preview=new Frontend.CharacterPreview();AddChild(preview);
                var advanced=frontend.Advanced(cl.AdvancedClassId);preview.ShowClass(advanced.Artwork,advanced);
                var assembly=preview.Assembly!;
                foreach(var a in empire.Catalog.Appearances.Where(a=>a.Id.StartsWith("wardrobe.")))
                {
                    bool expected=a.Id.StartsWith("wardrobe.war.") || a.Id.StartsWith("wardrobe.inq.")
                        ? cl.BaseClassId.StartsWith("sith_") : !cl.BaseClassId.StartsWith("sith_");
                    bool applied=assembly.Loadout.TrySetCosmetic(a.Slot,a.Id,out error);
                    Check(cl.AdvancedClassId+" pairing and mesh: "+a.Id,applied==expected);
                    var item=empire.Catalog.Items.Single(i=>i.Appearance==a.Id);
                    Check("Exact icon loads: "+a.Id,GD.Load<Texture2D>(item.Icon)!=null && item.Icon.Contains(a.Id));
                }
                foreach(var a in empire.Catalog.Appearances.Where(a=>a.Id.StartsWith("collectible.")))
                {
                    bool expected=empire.Compatible(a,assembly.Loadout.Context);
                    var before=assembly.Loadout.Current;
                    bool applied=assembly.Loadout.TrySetCosmetic(a.Slot,a.Id,out error);
                    Check(cl.AdvancedClassId+" collectible compatibility and mesh: "+a.Id,applied==expected);
                    if(expected && a.Slot=="main_hand" && cl.AdvancedClassId is "marauder" or "mercenary")
                        Check("Dual cosmetic mirrors left socket: "+a.Id,assembly.Loadout.Current.Cosmetics.GetValueOrDefault("off_hand")==a.Id[..^10]+".off_hand");
                    Check("Restore after collectible",assembly.Loadout.TryApply(before,out error));
                }
                foreach(string slot in new[]{"head","main_hand"})
                {
                    string family=cl.BaseClassId switch {"sith_warrior"=>"war","sith_inquisitor"=>"inq","imperial_agent"=>"spy",_=>"bh"};
                    var a=empire.Catalog.Appearances.Last(a=>a.Id.StartsWith("collectible.") && a.Slot==slot && empire.Compatible(a,assembly.Loadout.Context) && (slot!="head" || a.Id=="collectible."+family+".head"));
                    Check("Capture outfit: "+a.Id,assembly.Loadout.TrySetCosmetic(slot,a.Id,out error));
                }
                await Frames(3);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                Walk(preview).OfType<SubViewport>().Single().GetTexture().GetImage().SavePng("res://../work/collectible-"+cl.AdvancedClassId+".png");
                preview.QueueFree();await Frames();
            }
            var unlockedMenu=new Frontend.CollectionsScreen();AddChild(unlockedMenu);await Frames();
            foreach(var a in empire.Catalog.Appearances.Where(a=>a.Id.StartsWith("wardrobe.") && a.Slot=="chest"))
            {
                var button=Walk(unlockedMenu).OfType<Button>().SingleOrDefault(b=>b.Name=="Appearance_"+a.Id.Replace('.','_'));
                bool sith=a.Id.StartsWith("wardrobe.war.") || a.Id.StartsWith("wardrobe.inq.");
                Check("Unlocked menu pairing: "+a.Id,(button!=null)==sith);
                if(button!=null)Check("Menu uses gear icon: "+a.Id,button.Icon.ResourcePath==empire.Catalog.Items.Single(i=>i.Appearance==a.Id).Icon);
            }
            unlockedMenu.QueueFree();await Frames();
            // Exercise the in-run toolbar through real pointer events, using the isolated save above.
            async Task Toolbar(string name)
            {
                var button=Walk(world.Hud!.Menu).OfType<Button>().Single(b=>b.Name==name);
                var p=button.GetGlobalRect().GetCenter();
                GetViewport().PushInput(new InputEventMouseMotion {Position=p,GlobalPosition=p},true);
                foreach(bool down in new[]{true,false})GetViewport().PushInput(new InputEventMouseButton {Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,Pressed=down},true);
                await Frames();
            }
            world.ResetCombatWorld();await Frames();
            Check("Compact toolbar has three actions",Walk(world.Hud!.Menu).OfType<Button>().Count()==3);
            if(OS.GetCmdlineUserArgs().Contains("--run-menu-capture"))
            {
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng("res://../work/run-menu-gameplay.png");
            }
            await Toolbar("RunCollections");
            Check("Toolbar opens Collections and pauses combat",world.Hud!.Menu.IsOpen && GetTree().Paused);
            double elapsed=world.Progression!.ElapsedSeconds;await Frames(8);
            Check("Wardrobe freezes run time",world.Progression.ElapsedSeconds==elapsed);
            var wardrobe=Walk(world.Hud.Menu).OfType<Frontend.CollectionsScreen>().Single();
            var chest=Walk(wardrobe).OfType<Button>().Single(b=>b.Name=="Appearance_wardrobe_inq_chest");
            chest.EmitSignal(BaseButton.SignalName.Pressed);await Frames();
            if(OS.GetCmdlineUserArgs().Contains("--run-menu-capture"))
            {
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng("res://../work/run-menu-collections.png");
            }
            await Click("Resume Run");
            Check("Closing Collections resumes the same world with the saved outfit",!GetTree().Paused && !world.Hud.Menu.IsOpen
                && world.Equipment.Loadout.Current.Cosmetics.GetValueOrDefault("chest")=="wardrobe.inq.chest");
            world.Progression.AddExperience(world.Progression.RequiredExperience);await Until(()=>world.Progression.CanChoose);
            var phase=world.Progression.PresentationPhase;
            await Toolbar("RunCollections");await Frames(8);await Click("Resume Run");
            Check("Collections preserves a pending upgrade pause",GetTree().Paused && world.Progression.PresentationPhase==phase);
            await Toolbar("RunRestart");
            Check("Toolbar restarts and clears pending upgrades",!GetTree().Paused && world.Progression.Level==1 && world.Waves!.Wave==1);
            GetViewport().PushInput(new InputEventKey {PhysicalKeycode=Key.Escape,Pressed=true},true);await Frames();
            Check("Escape opens pause menu instead of quitting",world.Hud.Menu.IsOpen && GetTree().Paused);
            await Click("Resume Run");
            await Toolbar("RunReturn");await Until(()=>Scene is Frontend.FrontendFlow);
            Check("Toolbar returns to menu and releases the run",!GetTree().Paused && !GodotObject.IsInstanceValid(world));
        }
        catch(Exception ex) {failure=ex.ToString();GD.PushError("Unlocks: "+failure);}
        GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/unlock-smoke.json"),JsonSerializer.Serialize(new {passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null ? 0 : 1);
    }
}
