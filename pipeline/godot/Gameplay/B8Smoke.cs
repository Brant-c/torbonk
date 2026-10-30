using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>One brief B8 integration session. Stops for manual visual/balance review.</summary>
public partial class B8Smoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string label,bool pass)
    {if(!pass)throw new InvalidOperationException(label);_checks.Add(label);GD.Print("B8: "+label);}
    private async Task Frames(int n) {for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private async Task Until(Func<bool> condition)
    {for(int i=0;i<180 && !condition();i++)await Frames(1);if(!condition())throw new TimeoutException("B8 transition");}
    private sealed class MissingPresentation : IAbilityPresentation
    {
        public CharacterPresentation Actor {get;set;}=null!;
        public bool Begin(Combatant t,Vector3 c,float r)=>false;
        public void Channel() {} public void Impact(Vector3 c) {} public void End(bool i) {} public void Release() {}
    }
    public async void Run(GameWorld world)
    {
        ProcessMode=ProcessModeEnum.Always;string? failure=null;
        var received=new List<CombatEvent>();var fly=new List<CombatEvent>();
        try
        {
            world.ResetCombatWorld();world.Waves!.SetPhysicsProcess(false);world.Spawner!.Running=false;
            var controller=world.Abilities;controller.SetPhysicsProcess(false);controller.Abort();world.Player.SetPhysicsProcess(false);
            var health=world.PlayerActor.Health;var p=world.Progression!;var catalog=p.Upgrades;
            // Static Barrier and the bundled Death Field / Dark Heal cards left the Sorcerer pool, so this ability smoke loads them directly.
            UpgradeDefinition Card(string id){var u=(UpgradeDefinition)GD.Load<UpgradeDefinition>($"res://Gameplay/Upgrades/{id}.tres").Duplicate();u.RarityRules=catalog.First().RarityRules;u.AbilityTitle=world.Abilities.Find(u.AbilityId)?.Definition.Title??"";return u;}
            p.LevelAnnouncementSeconds=0.2f;
            world.Events.Resolved+=received.Add;world.Flytext.EntryCreated+=fly.Add;
            var field=controller.Find("deathfield")!;var barrier=controller.Find("barrier")!;var heal=controller.Find("darkheal")!;
            Check("three new abilities start locked and retain the single twelve-slot bar",controller.Abilities.Count==6 &&
                controller.Abilities.Count(a=>a.Unlocked)==1 && world.Hud!.ActionSlotCount==12);
            Check("original clips and required hand/body sockets are available",new[]{"B8/DeathField","B8/Barrier","B8/DarkHeal"}.All(c=>world.Player.Presentation.Animator.HasAnimation(c)) &&
                new[]{"vfx_hand_left","LeftWrist","Chest","Bip01"}.All(b=>world.Player.Presentation.Skeleton.FindBone(b)>=0));
            foreach(var ability in new[]{field,barrier,heal})
            {
                var unlock=Card("unlock_"+ability.Definition.Id);
                Check(ability.Definition.Title+" remains gated before unlock",!ability.TryBegin() && !p.IsEligible(Card(ability.Definition.Id+"_power")));
                p.Upgrades=new Godot.Collections.Array<UpgradeDefinition> {unlock,catalog.Single(u=>u.Id=="health"),catalog.Single(u=>u.Id=="speed")};
                p.AddExperience(p.RequiredExperience);await Until(()=>p.CanChoose);
                Check(ability.Definition.Title+" unlocks through a real level choice",p.Choose(p.Offers.ToList().IndexOf(unlock)) && ability.Unlocked);
                await Until(()=>!p.Selecting);
            }
            p.Upgrades=catalog;await Frames(2);
            Check("unlocked abilities populate the existing action bar",world.Hud!.UnlockedAbilityCount==4 && world.Hud.ActionSlotCount==12);
            var enemies=new List<StationaryEnemy>();
            for(int i=0;i<9;i++)
            {
                var e=GD.Load<PackedScene>("res://Gameplay/StationaryEnemy.tscn").Instantiate<StationaryEnemy>();
                e.TargetId="B8Fixture"+i;e.GetNode<Health>("Health").Maximum=1000;world.AddChild(e);
                e.GlobalPosition=world.PlayerActor.GlobalPosition+new Vector3(i==8?0.95f:0.16f+i*0.025f,0,0);enemies.Add(e);
            }
            enemies[6].Health.ApplyShield(1000,30);enemies[7].Health.ApplyDamage(10000);await Frames(2);
            Check("the real cluster is visible and targetable",enemies.Take(7).All(e=>world.Targeting.IsValidTarget(e,field.Range)));
            health.ApplyDamage(50);received.Clear();fly.Clear();
            Check("Death Field starts on its independent cooldown",field.TryBegin() && field.CooldownRemaining==15);
            field.Advance(0.31);await Frames(2);
            Check("Death Field hits six enemies for 35 and caps actual-hit healing at 15",enemies.Take(6).All(e=>e.Health.Current==965) &&
                enemies[6].Health.Current==1000 && enemies[8].Health.Current==1000 && field.LastDistinctHits==6 && field.LastHealing==15 && health.Current==65);
            Check("each actual damage, shield absorption and healing retains its own flytext",received.Count==8 && fly.Count==received.Count &&
                received.Count(e=>e.Change.Kind==CombatEventKind.Damage)==6 && received.Count(e=>e.Change.Kind==CombatEventKind.Healing)==1);
            var ground=((SurvivalAbilityPresentation)field.Presentation).GetChildren().OfType<StormAreaIndicator>().Single();
            Check("Death Field reuses terrain projection without a standing textured box",ground.Visible && ground.ProjectedVertexCount>0 && ground.Scale==Vector3.One);
            Check("barrier initializes for the visual review",barrier.TryBegin());await Frames(2);
            var shield=((SurvivalAbilityPresentation)barrier.Presentation).GetNode<Node3D>("OriginalStaticBarrier");
            int chest=world.Player.Presentation.Skeleton.FindBone("Chest");
            var chestPosition=(world.Player.Presentation.Skeleton.GlobalTransform*world.Player.Presentation.Skeleton.GetBoneGlobalPose(chest)).Origin;
            Check("barrier follows the chest and its opaque-black artwork uses additive blending",shield.GlobalPosition.DistanceTo(chestPosition)<0.001f &&
                shield.GetChildren().OfType<Sprite3D>().All(s=>s.MaterialOverride is StandardMaterial3D m && m.BlendMode==BaseMaterial3D.BlendModeEnum.Add));
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            string review=ProjectSettings.GlobalizePath("res://../generated/b8");DirAccess.MakeDirRecursiveAbsolute(review);
            GetViewport().GetTexture().GetImage().SavePng(review+"/effects-review.png");barrier.ResetCombat();
            field.ResetCombat();foreach(var e in enemies.Skip(2))e.CombatEnabled=false;
            Check("Death Field can start again after a fixture reset",field.TryBegin());field.Advance(0.31);
            Check("two distinct hits heal six HP without counting VFX or previous hits",field.LastDistinctHits==2 && field.LastHealing==6 && health.Current==71);
            field.ResetCombat();Check("a selected cluster may move during windup",field.TryBegin());
            foreach(var e in enemies.Take(2))e.GlobalPosition+=Vector3.Right*2;
            field.Advance(0.31);Check("no successful hits means no healing",field.LastDistinctHits==0 && field.LastHealing==0 && health.Current==71);
            foreach(var e in enemies.Take(2))e.GlobalPosition-=Vector3.Right*2;
            field.ResetCombat();var original=field.Presentation;field.Presentation=new MissingPresentation {Actor=world.Player.Presentation};
            Check("gameplay starts even without presentation",field.TryBegin());field.Advance(0.31);
            Check("presentation failure cannot block damage or healing",field.LastDistinctHits==2 && field.LastHealing==6);
            field.Abort();field.Presentation=original;

            received.Clear();fly.Clear();float hp=health.Current;
            Check("barrier applies 30 shield immediately with an 18 second cooldown",barrier.TryBegin() && health.ShieldRemaining==30 && barrier.CooldownRemaining==18);
            Check("shield absorbs before HP and does not stack",health.ApplyDamage(12)==0 && health.Current==hp && health.ShieldRemaining==18 && !health.ApplyShield(30,12));
            Check("depletion passes only excess damage to HP",health.ApplyDamage(25)==7 && health.Current==hp-7 && !health.Shielded &&
                received.Count(e=>e.Change.Kind==CombatEventKind.Absorbed)==2 && received.Count(e=>e.Change.Kind==CombatEventKind.Damage)==1 && fly.Count==3);
            barrier.ResetCombat();Check("barrier reapplies after reset",barrier.TryBegin());
            health._PhysicsProcess(13);Check("shield expiry removes capacity and its persistent effect",!health.Shielded && !((SurvivalAbilityPresentation)barrier.Presentation).ShieldVisible);
            barrier.ResetCombat();foreach(var e in enemies)e.CombatEnabled=false;
            Check("barrier does not activate without a threat",!barrier.TryBegin());enemies[0].CombatEnabled=true;

            health.SetMaximum(150);health.Restore();Check("Dark Heal never activates at full HP",!heal.TryBegin());
            health.ApplyDamage(75);Check("exactly fifty percent does not trigger Dark Heal",!heal.TryBegin());health.ApplyDamage(1);
            Check("Dark Heal restores 25 missing HP below half of configured max",heal.TryBegin() && health.Current==99 && heal.LastHealing==25 && heal.CooldownRemaining==25);
            Check("Dark Heal cannot repeatedly heal during its cooldown",!heal.TryBegin());heal.ResetCombat();health.ApplyDamage(35);
            var start=world.Player.GlobalPosition;world.Player.SetPhysicsProcess(true);Input.ActionPress("b1_right");
            Check("brief self-heal can activate while movement is held",heal.TryBegin());await Frames(18);
            Input.ActionRelease("b1_right");world.Player.SetPhysicsProcess(false);
            Check("casting preserves player movement",world.Player.GlobalPosition.DistanceTo(start)>0.01f);

            controller.ResetCombat();health.Restore();health.ApplyDamage(90);
            controller.SetPhysicsProcess(true);await Frames(35);controller.SetPhysicsProcess(false);
            Check("self defenses activate during offensive cooldown and animations",barrier.Casts>0 && heal.Casts>0 && health.Current>=85 && health.Shielded);
            foreach(var id in new[]{"deathfield","barrier","darkheal"})
            {
                var power=Card(id+"_power");
                p.Upgrades=new Godot.Collections.Array<UpgradeDefinition> {power,catalog.Single(u=>u.Id=="health"),catalog.Single(u=>u.Id=="speed")};
                foreach(var previous in catalog.Where(u=>u.Effect==UpgradeEffect.AbilityPower && p.Rank(u)>0))
                {var capped=(UpgradeDefinition)previous.Duplicate();capped.MaximumRank=p.Rank(previous);p.Upgrades.Add(capped);}
                p.AddExperience(p.RequiredExperience);await Until(()=>p.CanChoose);Check(id+" improvement is eligible",p.Choose(p.Offers.ToList().IndexOf(power)));await Until(()=>!p.Selecting);
            }
            p.Upgrades=catalog;
            Check("rarity-scaled upgrades improve all three new ability profiles",field.DamagePerTick>35 && field.Radius>0.34f && field.Healing>2 &&
                barrier.ShieldCapacity>22 && barrier.ShieldDuration>12 && barrier.CooldownSeconds<22 && heal.Healing>25 && heal.CooldownSeconds<25 && heal.HealthThreshold>0.5f);
            health.ApplyDamage(10000);await Frames(2);
            Check("death clears abilities, shields, gestures, VFX and audio",controller.Abilities.All(a=>a.State==LightningState.Disabled) && !health.Shielded &&
                world.Player.Presentation.GestureOwner==null && new[]{field,barrier,heal}.All(a=>((SurvivalAbilityPresentation)a.Presentation).EffectCount==0 && ((SurvivalAbilityPresentation)a.Presentation).PlayingAudioVoices==0));
            world.ResetCombatWorld();world.Waves.SetPhysicsProcess(false);world.Spawner.Running=false;await Frames(2);
            Check("restart restores baseline HP, tuning, locks and cooldowns",health.Current==100 && !health.Shielded && p.Ranks.Count==0 &&
                controller.Abilities.Count(a=>a.Unlocked)==1 && controller.Abilities.All(a=>a.CooldownRemaining==0) && field.DamagePerTick==35 && barrier.ShieldCapacity==22 && heal.Healing==25);
            foreach(var e in enemies)e.QueueFree();
            Check("the brief rendered Godot session completed",Engine.GetFramesDrawn()>0);
        }
        catch(Exception ex) {failure=ex.ToString();GD.PushError(failure);}
        Input.ActionRelease("b1_right");GetTree().Paused=false;
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/b8-smoke.json"),JsonSerializer.Serialize(new {passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
