using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
namespace SwtorSurvivors;

/// <summary>One bounded integration session; no historical suites, archive hashing or rendered captures.</summary>
public partial class D2Smoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string text,bool pass) {if(!pass)throw new InvalidOperationException(text);_checks.Add(text);GD.Print("D2: "+text);}
    private async Task Frames(int n=1) {for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private async Task Until(Func<bool> condition)
    {for(int i=0;i<240 && !condition();i++)await Frames();if(!condition())throw new TimeoutException("D2 state transition");}
    private static string CosmeticBytes()=>FileAccess.FileExists(Equipment.CosmeticCollection.SavePath)?FileAccess.GetFileAsString(Equipment.CosmeticCollection.SavePath):"<absent>";
    public async void Run(GameWorld w)
    {
        ProcessMode=ProcessModeEnum.Always;string? failure=null;
        try
        {
            string cosmetics=CosmeticBytes();
            w.ResetCombatWorld();w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;
            w.Abilities.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.PlayerActor.Health.SetMaximum(10000,10000);
            var p=w.Progression!;p.LevelAnnouncementSeconds=.2f;
            var catalog=p.Upgrades;var items=w.Artifacts!;var itemCatalog=items.Catalog.ToArray();
            var chain=w.Abilities.Find("chain")!;var overload=w.Abilities.Find("overload")!;
            float baseStorm=w.Abilities.Find("storm")!.ChannelSeconds;
            Check("eight preserved/new abilities, one twelve-slot bar, starting Lightning icon only",w.Abilities.Abilities.Count==8 && w.Abilities.Abilities.Count(a=>a.Unlocked)==1 && w.Hud!.ActionSlotCount==12 && w.Hud.UnlockedAbilityCount==1);
            Check("both April source gesture clips loaded",w.Player.Presentation.Animator.HasAnimation("D2/Chain") && w.Player.Presentation.Animator.HasAnimation("D2/Overload"));
            foreach(string id in new[]{"shock","storm","deathfield"})Check("offensive unlock "+id,w.Abilities.Unlock(id));
            Check("four offensive slots prevent Chain unlock offers while owned upgrades remain eligible",!w.Abilities.Unlock("chain") && !p.IsEligible(catalog.Single(u=>u.Id=="unlock_chain")) && p.IsEligible(catalog.Single(u=>u.Id=="shock_damage")));
            Check("utility slots are independent",w.Abilities.Unlock("barrier") && w.Abilities.Unlock("darkheal") && !w.Abilities.Unlock("overload") && !p.IsEligible(catalog.Single(u=>u.Id=="unlock_overload")));
            w.Abilities.ResetUnlocks();w.Abilities.Unlock("chain");
            Check("specialized locked-ability talents excluded",!p.IsEligible(catalog.Single(u=>u.Id=="storm_duration")) && !p.IsEligible(catalog.Single(u=>u.Id=="storm_slow")));
            var received=new List<CombatEvent>();var fly=new List<CombatEvent>();w.Events.Resolved+=received.Add;w.Flytext.EntryCreated+=fly.Add;
            var fixtures=new List<StationaryEnemy>();
            foreach(float offset in new[]{.15f,.55f,.95f,1.35f,.25f,.35f})
            {
                var e=GD.Load<PackedScene>("res://Gameplay/StationaryEnemy.tscn").Instantiate<StationaryEnemy>();
                e.GetNode<Health>("Health").Maximum=1000;w.AddChild(e);e.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*offset;fixtures.Add(e);
            }
            fixtures[4].Health.ApplyDamage(10000);fixtures[5].CombatEnabled=false;await Frames(2);received.Clear();fly.Clear();
            Check("unlocking Chain adds a second active ability icon",w.Hud!.UnlockedAbilityCount==2);
            // A short source-terrain corridor with a deliberately reduced configurable acquisition range.
            // The last link remains outside acquisition range without placing receivers inside distant terrain.
            chain.Range=.45f;
            for(int i=0;i<4;i++)fixtures[i].GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*(.15f+i*.13f);
            Check("spread-target fixture has a clear original-world corridor",fixtures.Take(4).All(e=>w.Targeting.HasLineOfSight(e)));
            Check("Chain starts with a visible hostile",chain.TryBegin());chain.Advance(.31);await Frames(2);
            Check("Chain casts on its own cooldown without a Force Lightning cast or passive",chain.Casts==1 && chain.CooldownRemaining>chain.CooldownSeconds-1 && w.Combat.Casts==0 && w.Combat.CooldownRemaining==0 && w.Build.Talent("fork")==0);
            GD.Print($"D2 Chain evidence: {chain.LastEvent}, {chain.DamageEvents} events, HP {string.Join(',',fixtures.Select(e=>e.Health.Current))}, range {chain.Range}, bounces {chain.Definition.BounceCount}");
            Check("Chain hits four distinct living targets across initial cast range with falloff",chain.LastDistinctHits==4 && chain.DamageEvents==4 && fixtures.Take(4).Select((e,i)=>Math.Abs(e.Health.Current-(1000-chain.Definition.Damage*MathF.Pow(chain.Definition.BounceFalloff,i)))<.01f).All(x=>x));
            Check("every Chain damage event has independent flytext; dead/disabled enemies excluded",received.Count(e=>e.Change.Kind==CombatEventKind.Damage)==4 && fly.Count==4 && fixtures[5].Health.Current==1000);
            Check("four visual arcs have no additional damage authority",((D2AbilityPresentation)chain.Presentation).ArcsShown==4 && fixtures[3].GlobalPosition.DistanceTo(w.PlayerActor.GlobalPosition)>chain.Range);
            await Frames(8);Check("visual updates cannot duplicate damage",chain.DamageEvents==4);
            chain.ResetCombat();Check("Chain begins invalidation fixture",chain.TryBegin());fixtures[0].CombatEnabled=false;chain.Advance(.31);
            Check("invalid initial target cancels without a hit",chain.DamageEvents==0);
            foreach(var e in fixtures){w.RemoveChild(e);e.QueueFree();}
            // Train one real mechanical choice, then retain it while artifact equipment is replaced.
            var relay=catalog.Single(u=>u.Id=="chain_reach");
            p.Upgrades=new Godot.Collections.Array<UpgradeDefinition>{relay,catalog.Single(u=>u.Id=="health"),catalog.Single(u=>u.Id=="speed")};
            p.AddExperience(p.RequiredExperience);await Until(()=>p.CanChoose);p.Choose(p.Offers.ToList().IndexOf(relay));await Until(()=>!p.Selecting);
            p.Upgrades=catalog;p.Recalculate();Check("mechanical rank modifies bounce budget",chain.ExtraBounces==1);
            w.Abilities.Unlock("overload");
            var mobs=new List<EnemyAgent>();
            for(int i=0;i<3;i++)
            {
                w.Spawner.Running=true;Check("safe Overload mob spawn",w.Spawner.TrySpawn(ignoreLimit:true));w.Spawner.Running=false;
                var mob=w.Spawner.Enemies.Last();mob.SetPhysicsProcess(false);
                var near=w.PlayerActor.GlobalPosition+new Vector3(.13f+i*.035f,0,.045f);
                Check("mob placement is on supported collision-safe terrain",mob.Navigation.TryStandingPoint(near,out var feet));mob.Body.GlobalPosition=feet;
                mob.Health.SetMaximum(1000,1000);mobs.Add(mob);
            }
            mobs[2].KnockbackResistance=1;var positions=mobs.Select(e=>e.Body.GlobalPosition).ToArray();
            Check("Overload triggers on configured close swarm off GCD",overload.TryBegin() && !overload.Definition.UsesGlobalCooldown && chain.Definition.UsesGlobalCooldown);overload.Advance(.16);
            Check("Overload deals one modest hit per enemy including immune target",mobs.All(e=>Math.Abs(e.Health.Current-(1000-overload.DamagePerTick))<.01f) && overload.LastDistinctHits==3);
            Check("controlled knockback follows safe terrain; boss immunity prevents displacement",mobs.Take(2).Select((e,i)=>e.Body.GlobalPosition.DistanceTo(positions[i])>.02f && e.Navigation.CanWalk(positions[i],e.Body.GlobalPosition) && w.Arena.Supported(e.Body.GlobalPosition,e.CollisionRadius)).All(x=>x) && mobs[2].Body.GlobalPosition==positions[2]);
            // Exact original pillar/hole coordinates from the retained B1 terrain receipt; no world edits.
            var wallStart=new Vector3(33.600002f,-16.996994f,13.199996f);var wallDirection=new Vector3(.92387956f,0,-.38268343f);
            Check("original pillar approach has supported standing room",mobs[0].Navigation.TryStandingPoint(wallStart,out var wallFeet));
            mobs[0].Body.GlobalPosition=wallFeet;
            float pushed=mobs[0].DisplaceFrom(wallFeet-wallDirection*.1f,.3f);
            Check("Overload stops before original Korriban pillar collision",pushed<.1624f && mobs[0].Navigation.CanWalk(wallFeet,mobs[0].Body.GlobalPosition));
            Check("knockback safety rejects actual source hole and arena edge",!w.Arena.Supported(new Vector3(20.533333f,1.8183689f,17.333334f),mobs[0].CollisionRadius) &&
                !mobs[0].Navigation.CanWalk(mobs[0].Body.GlobalPosition,new Vector3(w.Arena.Maximum.X+1,mobs[0].GlobalPosition.Y,w.Arena.Maximum.Y+1)));
            foreach(var mob in mobs)mob.Health.ApplyDamage(10000);
            foreach(var pickup in p.GetChildren().OfType<ExperiencePickup>())pickup.ProcessMode=ProcessModeEnum.Disabled;
            // Trigger the existing major boss lifecycle, with an already pending level-up.
            p.Upgrades=new Godot.Collections.Array<UpgradeDefinition>{catalog.Single(u=>u.Id=="health"),catalog.Single(u=>u.Id=="speed"),catalog.Single(u=>u.Id=="damage")};
            p.AddExperience(p.RequiredExperience);Check("level choice queued before boss defeat",p.Selecting);
            items.Catalog.Clear();items.Catalog.AddRange(itemCatalog.Where(a=>a.Effect is "guardian" or "heal" or "recovery"));
            w.Spawner.Running=true;w.Waves.Encounter.Tick(.1,true);w.Spawner.Running=false;
            var boss=w.Waves.Boss!;Check("existing boss encounter spawned",GodotObject.IsInstanceValid(boss));boss.Health.ApplyDamage(100000);
            items.Defeated(boss);items.Defeated(boss);await Frames(2);
            Check("boss defeat pays once and waits for level selection",w.Waves.Encounter.State==EncounterState.Defeated && items.Pending && !items.Selecting);
            await Until(()=>p.CanChoose);p.Choose(p.Offers.ToList().FindIndex(a=>a.Id=="health"));await Until(()=>items.Selecting);
            Check("one boss produces exactly three distinct SWTOR reward cards",items.RewardsOpened==1 && items.Offers.Count==3 && items.Offers.Select(a=>a.Id).Distinct().Count()==3 && !p.Selecting && GetTree().Paused);
            items.Choose(items.Offers.ToList().FindIndex(a=>a.Effect=="guardian"));p.Upgrades=catalog;p.Recalculate();
            async Task Reward(string effect)
            {
                items.Catalog.Clear();items.Catalog.AddRange(itemCatalog.Where(a=>a.Effect==effect || a.Effect is "heal" or "recovery"));
                w.Spawner.Running=true;Check("elite reward spawn",w.Spawner.TrySpawn(ignoreLimit:true,elite:true));w.Spawner.Running=false;
                var elite=w.Spawner.Enemies.Last();elite.SetPhysicsProcess(false);elite.RewardTier=2;elite.Health.ApplyDamage(100000);
                await Until(()=>items.Selecting);Check("three distinct eligible encounter choices",items.Offers.Count==3 && items.Offers.Select(a=>a.Id).Distinct().Count()==3);
                Check("select one reward",items.Choose(items.Offers.ToList().FindIndex(a=>a.Effect==effect)));
            }
            w.Abilities.Find("storm")!.SetUnlocked(true);
            await Reward("storm_duration");await Reward("bounce");
            Check("three items equip automatically and combine with trained talent",items.Equipped.Count==3 && chain.ExtraBounces==2 && w.Abilities.Find("storm")!.ChannelSeconds==baseStorm+2);
            await Reward("sacrifice");
            Check("full equipment requires an explicit replacement before applying",items.Replacing && items.Equipped.Count==3 && w.Build.Item("sacrifice")==0);
            int replace=items.Equipped.ToList().FindIndex(a=>a.Effect=="bounce");items.Equip(replace);p.Recalculate();p.Recalculate();
            Check("replacement removes only old effect and applies new effect exactly once",items.Equipped.Count==3 && chain.ExtraBounces==1 && w.Build.Item("sacrifice")==.3f && w.Build.Item("health_cost")==.2f && w.Abilities.Find("storm")!.ChannelSeconds==baseStorm+2 && items.RewardsClaimed==4);
            Check("all reward modals close and gameplay resumes",!items.Pending && !GetTree().Paused);
            items.Catalog.Clear();items.Catalog.AddRange(itemCatalog);w.ResetCombatWorld();
            Check("restart clears artifacts, talents, effects and unlocks",items.Equipped.Count==0 && !items.Pending && p.Ranks.Count==0 && chain.ExtraBounces==0 && w.Abilities.Find("storm")!.ChannelSeconds==baseStorm && w.PlayerActor.Health.Maximum==100 && w.Abilities.Abilities.Count(a=>a.Unlocked)==1);
            Check("persistent cosmetic Collections bytes unchanged",CosmeticBytes()==cosmetics);
        }
        catch(Exception ex) {failure=ex.ToString();GD.PushError(failure);}
        string output=ProjectSettings.GlobalizePath("res://../manifests/d2-smoke.json");
        System.IO.File.WriteAllText(output,JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,failure},new JsonSerializerOptions{WriteIndented=true}));
        GetTree().Paused=false;GetTree().Quit(failure==null?0:1);
    }
}
