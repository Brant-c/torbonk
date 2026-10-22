using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using SwtorSurvivors.Frontend;
using SwtorSurvivors.Integration;
namespace SwtorSurvivors;
public partial class D3Smoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string text,bool pass){if(!pass)throw new InvalidOperationException(text);_checks.Add(text);GD.Print("D3: "+text);}
    private async Task Frames(int n=1){for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    public async void Run(GameWorld w)
    {
        string? failure=null;
        try
        {
            string cosmetics=FileAccess.FileExists(Equipment.CosmeticCollection.SavePath)?FileAccess.GetFileAsString(Equipment.CosmeticCollection.SavePath):"<absent>";
            w.ResetCombatWorld();w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);
            foreach(var enemy in w.Spawner.Enemies.ToArray()){enemy.CombatEnabled=false;enemy.SetPhysicsProcess(false);}
            Check("selected gameplay profile owns active ability, HUD and equipment",w.Combat.Definition.Id==w.RunConfiguration.Profile.StartingAbility && w.Abilities.Abilities.Count(a=>a.Unlocked)==1 && w.Hud!.ActionSlotCount==12 && w.Equipment.Loadout.Current.Items.OrderBy(i=>i.Key).SequenceEqual(w.Equipment.Loadout.Catalog.Presets.Single(p=>p.Id==w.RunConfiguration.Profile.EquipmentPreset).Items.OrderBy(i=>i.Key)));
            var received=new List<CombatEvent>();w.Events.Resolved+=e=>{if(e.Change.Kind==CombatEventKind.Damage)received.Add(e);};
            var flytext=new List<CombatEvent>();w.Flytext.EntryCreated+=e=>{if(e.Change.Kind==CombatEventKind.Damage)flytext.Add(e);};
            var fixtures=new List<Combatant>();
            for(int i=0;i<3;i++)
            {
                var e=new Combatant {Name="D3Target"+i,TargetId="D3Target"+i,Team=CombatTeam.Hostile};e.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(e);
                e.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*(.12f+i*.055f);fixtures.Add(e);
            }
            await Frames(2);
            Check("nearby fixture corridor has line of sight",fixtures.All(w.Targeting.HasLineOfSight));
            var catalog=FrontendCatalog.Load();var session=new SelectionSession();session.SelectFaction("empire",catalog);
            Check("exactly eight Imperial profiles and no Republic profiles",ClassProfile.All.Length==8 && ClassProfile.All.All(p=>p.Faction=="empire"));
            foreach(var p in ClassProfile.All)
            {
                session.SelectClass(p.BaseClass,catalog);session.SelectAdvanced(p.Id,catalog);
                Check(p.Id+" selection resolves its own profile and appearance",SelectionGameplayAdapter.TryResolve(session.Configuration,out var config,out _) && config.AdvancedClassId==p.Id && config.Profile.StartingAbility==p.StartingAbility && config.AppearancePresetId==p.Appearance);
                var invalid=session.Configuration.Snapshot();invalid.MapId="tython";Check(p.Id+" invalid map rejected",!SelectionGameplayAdapter.TryResolve(invalid,out _,out _));
                var preview=new CharacterPreview();w.AddChild(preview);preview.ShowClass(catalog.Advanced(p.Id).Artwork,catalog.Advanced(p.Id));
                Check(p.Id+" preview uses its own equipment profile",preview.Assembly?.Loadout.Context.Class.AdvancedClassId==p.Id);preview.Free();
                var actor=new CharacterPresentation();actor.AddChild(GD.Load<PackedScene>(p.CharacterScene).Instantiate<Node3D>());w.AddChild(actor);
                var model=actor.GetChildren().OfType<Node3D>().First();
                var assembly=Equipment.EquipmentComposition.Create(actor,model,actor.Skeleton,p.BaseClass,p.Id);
                actor.SetEquipmentLocomotion(assembly.Locomotion);
                if(p.Id=="juggernaut")Check("Juggernaut uses original saber-ready locomotion",actor.IdleClip=="EquipmentSaber/Idle" && actor.RunClip=="EquipmentSaber/Run");
                if(p.Presentation!="sorcerer")
                {
                    actor.LoadAbilityLibrary(p.AnimationScene,"D3");
                    if(p.Idle!="")actor.SetEquipmentLocomotion(new Equipment.LocomotionDefinition("D3",p.AnimationScene,p.Idle,p.Run));
                }
                Check(p.Id+" original armor and main hand load",assembly.ActiveArmorMeshes>=5 && assembly.Weapon("main_hand")!=null && assembly.LastError=="");
                if(p.Id is "marauder" or "mercenary")Check(p.Id+" independent off hand loads",assembly.Weapon("off_hand")!=null);
                var d=GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{p.StartingAbility}.tres");
                Check(p.Id+" original idle, run, attack, hit and death load",actor.Animator.HasAnimation(actor.IdleClip) && actor.Animator.HasAnimation(actor.RunClip) && actor.Animator.HasAnimation(p.Presentation=="sorcerer"?"LightningStart":d.StartClip) && actor.Animator.HasAnimation("Hit") && actor.Animator.HasAnimation("Death"));
                IAbilityPresentation visual;
                if(p.Presentation=="sorcerer"){var v=new LightningPresentation {Actor=actor};w.AddChild(v);visual=v;}
                else {var v=new D3AbilityPresentation {Actor=actor,Definition=d};w.AddChild(v);visual=v;}
                var a=new AutomaticAbility {Caster=w.PlayerActor,Targeting=w.Targeting,Presentation=visual,Periodic=w.Periodic};a.Configure(d);w.AddChild(a);
                foreach(var f in fixtures)f.Health.Restore();received.Clear();flytext.Clear();
                Check(p.Id+" automatic ability starts",a.TryBegin());
                int pulses=d.ChannelSeconds>0?(int)Math.Ceiling(d.ChannelSeconds/d.TickSeconds):1;
                if(p.Presentation!="sorcerer" && (d.ChannelSeconds>0 || d.WindupSeconds>0)){for(double t=0;t<d.WindupSeconds+d.ChannelSeconds+.1;t+=.05)a.Advance(.05);}
                else a.Advance(p.Presentation=="sorcerer"?1.6:.02);
                if(p.Presentation=="sorcerer")a.Advance(.6);
                int expected=d.Area is AbilityArea.CasterRadial or AbilityArea.TargetSplash?3:1;
                Check(p.Id+" damages appropriate distinct targets",fixtures.Count(e=>e.Health.Current<10000)==expected && fixtures.All(e=>e.Health.DamageEvents<= (p.Presentation=="sorcerer"?3:pulses)));
                if(d.Area==AbilityArea.TargetSplash)
                {Check("Tracer primary once and secondary splash amounts",Math.Abs(fixtures[0].Health.Current-(10000-d.Damage))<.01 && Math.Abs(fixtures[1].Health.Current-(10000-d.Damage*d.SplashMultiplier))<.01);}
                if(d.PeriodicDuration>0)
                {
                    w.Periodic.Apply(a,fixtures[1]);w.Periodic.Apply(a,fixtures[0]);
                    Check(p.Id+" refresh retains one effect per target",w.Periodic.Count==2);
                    int before=received.Count;float hp=fixtures[0].Health.Current;
                    w.Periodic.Advance(2.05);
                    Check(p.Id+" independent genuine periodic ticks publish damage",received.Count-before==4 && Math.Abs(fixtures[0].Health.Current-(hp-2*d.PeriodicDamage))<.01);
                    w.Periodic.Advance(20);Check(p.Id+" DoTs expire",w.Periodic.Count==0);
                }
                int count=received.Count;await Frames(2);Check(p.Id+" presentation cannot duplicate damage",received.Count==count);
                Check(p.Id+" every applied hit and periodic tick creates flytext",flytext.Count==received.Count);
                Check(p.Id+" class upgrade pool excludes Sorcerer unlocks",p.TalentPool.Length==0 || p.TalentPool.All(id=>{var u=GD.Load<UpgradeDefinition>($"res://Gameplay/Upgrades/{id}.tres");return (u.Effect!=UpgradeEffect.UnlockAbility || p.Kit.Contains(u.AbilityId)) && (u.AbilityId=="" || u.AbilityId==p.StartingAbility || p.Kit.Contains(u.AbilityId));}));
                actor.PlayReaction("Death");Check(p.Id+" death retains full-body priority",actor.OverrideActive);
                a.Abort();w.Periodic.Clear();a.Free();if(visual is Node node)node.Free();actor.Free();
            }
            session.SelectFaction("republic",catalog);Check("Republic remains unavailable",!SelectionGameplayAdapter.TryResolve(session.Configuration,out _,out _));
            foreach(var f in fixtures)f.Free();
            var selected=w.RunConfiguration.AdvancedClassId;w.ResetCombatWorld();Check("restart retains selected profile and one starting unlock",w.RunConfiguration.AdvancedClassId==selected && w.Abilities.Abilities.Count(a=>a.Unlocked)==1 && w.Periodic.Count==0);
            Check("four offensive / two utility limits preserved",w.Abilities.OffensiveLimit==4 && w.Abilities.UtilityLimit==2);
            Check("cosmetic Collections bytes unchanged",cosmetics==(FileAccess.FileExists(Equipment.CosmeticCollection.SavePath)?FileAccess.GetFileAsString(Equipment.CosmeticCollection.SavePath):"<absent>"));
            var tree=GetTree();w.RemoveChild(this);tree.Root.AddChild(this);
            Check("return to main menu accepted",GameNavigation.ReturnToMenu(w)==Error.Ok);await Frames(3);
            Check("main menu loads after run",tree.CurrentScene is FrontendFlow);
        }
        catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/d3-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
