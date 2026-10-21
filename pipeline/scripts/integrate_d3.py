from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]/'godot'
def edit(path,old,new):
    p=ROOT/path;s=p.read_text();assert old in s,(path,old[:50]);p.write_text(s.replace(old,new))
(ROOT/'Integration/SelectionGameplayAdapter.cs').write_text('''using SwtorSurvivors.Frontend;
namespace SwtorSurvivors.Integration;
public sealed record GameplayConfiguration(string FactionId,string BaseClassId,string AdvancedClassId,string MapId,
    string AppearancePresetId,string WorldScene,string EnvironmentScene,string CharacterScene)
{
    public ClassProfile Profile=>ClassProfile.Find(AdvancedClassId)!;
}
public static class SelectionGameplayAdapter
{
    private static GameplayConfiguration Configuration(ClassProfile p)=>new(p.Faction,p.BaseClass,p.Id,"korriban",p.Appearance,
        "res://Gameplay/GameWorld.tscn","res://Assets/Korriban/KorribanEnvironment.tscn",p.CharacterScene);
    public static GameplayConfiguration DevelopmentDefault=>Configuration(ClassProfile.Find("sorcerer")!);
    public static string? UnavailableReason(SelectionConfiguration selection)=>TryResolve(selection,out _,out string reason)?null:reason;
    public static bool TryResolve(SelectionConfiguration? selection,out GameplayConfiguration configuration,out string reason)
    {
        configuration=null!;reason="Choose a ready Imperial advanced class and Korriban.";
        if(selection==null || selection.SchemaVersion!=1 || selection.MapId!="korriban")return false;
        var p=ClassProfile.Find(selection.AdvancedClassId);
        if(p==null || p.Faction!=selection.FactionId || p.BaseClass!=selection.BaseClassId || !p.Ready)return false;
        if(!selection.Appearance.IsSupportedPreset || selection.Appearance.PresetId!=p.Appearance)
        {reason="The selected appearance does not match this class profile.";return false;}
        configuration=Configuration(p);reason="";return true;
    }
}
''')
edit('Gameplay/GameWorld.cs','    public AudioPresentation Audio', '    public PeriodicEffects Periodic {get;private set;}=null!;\n    public AudioPresentation Audio')
edit('Gameplay/GameWorld.cs','        Combat.Configure(GD.Load<AbilityDefinition>("res://Gameplay/Abilities/lightning.tres"));\n        Combat.Range=AcquisitionRange;','''        var profile=RunConfiguration.Profile;
        PlayerActor.Health.Maximum=profile.Health;PlayerActor.Health.Restore();Player.MoveSpeed*=profile.Movement;
        Combat.Configure(GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{profile.StartingAbility}.tres"));
        if(profile.Presentation=="sorcerer") Combat.Range=AcquisitionRange;
        else
        {
            Player.Presentation.LoadAbilityLibrary(profile.AnimationScene,"D3");
            if(profile.Idle!="")Player.Presentation.SetEquipmentLocomotion(new Equipment.LocomotionDefinition("D3",profile.AnimationScene,profile.Idle,profile.Run));
            var presentation=new D3AbilityPresentation {Actor=Player.Presentation,Definition=Combat.Definition};AddChild(presentation);Combat.Presentation=presentation;
            AcquisitionRange=Combat.Range;Targeting.AcquisitionRange=Combat.Range;
        }
        Periodic=new PeriodicEffects {Name="PeriodicEffects"};AddChild(Periodic);Combat.Periodic=Periodic;''')
edit('Gameplay/GameWorld.cs','if (survival && !OS.GetCmdlineUserArgs().Any(x=>x is "--survival-smoke" or "--progression-smoke"))','if (survival && RunConfiguration.Profile.Presentation=="sorcerer" && !OS.GetCmdlineUserArgs().Any(x=>x is "--survival-smoke" or "--progression-smoke"))')
edit('Gameplay/GameWorld.cs','        Rewards?.Clear(); Audio?.Clear();','        Periodic?.Clear();\n        Rewards?.Clear(); Audio?.Clear();')
edit('Gameplay/GameWorld.cs','            Abilities.Abort(); PlayerActor.CombatEnabled = false;','            Periodic.Clear();Abilities.Abort(); PlayerActor.CombatEnabled = false;')
edit('Gameplay/RunProgression.cs','        var rarity=GD.Load<RarityTuning>', '''        if(World.RunConfiguration.Profile.TalentPool.Length>0)
        {
            Upgrades.Clear();
            foreach(string id in World.RunConfiguration.Profile.TalentPool)
            {
                var u=(UpgradeDefinition)GD.Load<UpgradeDefinition>($"res://Gameplay/Upgrades/{id}.tres").Duplicate();
                if(u.Effect is UpgradeEffect.Damage or UpgradeEffect.Range or UpgradeEffect.Cooldown)u.AbilityId=World.Combat.Definition.Id;
                if(u.Effect==UpgradeEffect.Damage)u.Title="Combat Training";
                Upgrades.Add(u);
            }
        }
        var rarity=GD.Load<RarityTuning>''')
edit('Gameplay/RunArtifacts.cs','public bool Eligible(RunArtifactDefinition item,int tier) => item.MinimumTier<=tier &&','public bool Eligible(RunArtifactDefinition item,int tier) => (World.RunConfiguration.Profile.ArtifactEffects.Length==0 || World.RunConfiguration.Profile.ArtifactEffects.Contains(item.Effect)) && item.MinimumTier<=tier &&')
edit('Gameplay/UpgradeDefinition.cs','_=>"All abilities" };','_=>ClassProfile.All.FirstOrDefault(p=>p.StartingAbility==AbilityId) is {} p?Godot.GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{p.StartingAbility}.tres").Title:"All abilities" };')
edit('Gameplay/UpgradeDefinition.cs','using Godot;','using Godot;\nusing System.Linq;')
edit('Frontend/Scripts/CharacterPreview.cs','        var skeleton = model.FindChildren','        var profile=ClassProfile.Find(advanced.Id);\n        var skeleton = model.FindChildren')
edit('Frontend/Scripts/CharacterPreview.cs','        if (advanced.Id=="sorcerer" && skeleton!=null)\n            Assembly=EquipmentComposition.Create(model,model,skeleton,"sith_inquisitor",advanced.Id);','''        if (profile!=null && skeleton!=null)
        {
            Assembly=EquipmentComposition.Create(model,model,skeleton,profile.BaseClass,advanced.Id);
            if(profile.Idle!="" && animator!=null)
            {
                var source=GD.Load<PackedScene>(profile.AnimationScene).Instantiate<Node3D>();
                var sourcePlayer=source.FindChildren("*","AnimationPlayer",true,false).OfType<AnimationPlayer>().Single();
                var clip=(Animation)sourcePlayer.GetAnimation(profile.Idle).Duplicate(true);
                for(int t=clip.GetTrackCount()-1;t>=0;t--)
                { string bone=clip.TrackGetPath(t).GetConcatenatedSubNames();if(skeleton.FindBone(bone)<0){clip.RemoveTrack(t);continue;}clip.TrackSetPath(t,new NodePath(animator.GetNode(animator.RootNode).GetPathTo(skeleton)+":"+bone)); }
                clip.LoopMode=Animation.LoopModeEnum.Linear;var library=new AnimationLibrary();library.AddAnimation("Idle",clip);animator.AddAnimationLibrary("Class",library);animator.Play("Class/Idle");animator.Advance(0);source.Free();
            }
        }''')
edit('Frontend/Scripts/CharacterPreview.cs','advanced.Id=="sorcerer" && skeleton!=null ? AddSaberStance','ClassProfile.Find(advanced.Id)?.Idle=="" && skeleton!=null ? AddSaberStance')
