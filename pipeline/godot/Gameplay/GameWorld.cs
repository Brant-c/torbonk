using Godot;
using System.Linq;

namespace SwtorSurvivors;

public partial class GameWorld : Node3D
{
    [Export] public bool ExplorationOnly { get; set; }
    [Export] public string PlanetRunPath {get;set;} = "";
    public PlanetRunDefinition? PlanetRun {get;private set;}
    public PlanetJourney? Journey {get;private set;}
    [Export] public RunSlotRules? SlotRules {get;set;}
    [ExportGroup("B2 Gameplay Tuning (source units)")]
    [Export] public float AcquisitionRange { get; set; } = 0.85f;
    [Export] public float TargetRefreshSeconds { get; set; } = 0.2f;
    [ExportGroup("B3 Waves")]
    [Export] public float WaveSeconds { get; set; } = 30;
    [Export] public float InitialSpawnInterval { get; set; } = 0.75f;
    [Export] public float SpawnIntervalMultiplier { get; set; } = 0.94f;
    [Export] public float MinimumSpawnInterval { get; set; } = 0.30f;
    [Export] public int InitialActiveLimit { get; set; } = 8;
    [Export] public int AdditionalActivePerWave { get; set; } = 3;
    [Export] public int MaximumActiveLimit { get; set; } = 36;
    [Export] public int BossWave { get; set; } = 5;
    [Export] public float HealthPerWave { get; set; } = 0.12f;
    [Export] public float DamagePerWave { get; set; } = 0.10f;
    [ExportGroup("B3 Spawning (source units)")]
    [Export] public PackedScene? EnemyScene { get; set; }
    [Export] public float MinimumSpawnDistance { get; set; } = 0.55f;
    [Export] public float MaximumSpawnDistance { get; set; } = 1.05f;
    [Export] public float CorpseSeconds { get; set; } = 4;
    public PlayerController Player { get; private set; } = null!;
    public CameraRig Rig { get; private set; } = null!;
    public ArenaBounds Arena { get; private set; } = null!;
    public Node3D EnvironmentRoot { get; private set; } = null!;
    public Label Status { get; private set; } = null!;
    public bool Initialized { get; private set; }
    public bool ReadyForPlay { get; private set; }
    public Combatant PlayerActor { get; private set; } = null!;
    public AutomaticLightning Combat { get; private set; } = null!;
    public AbilityController Abilities { get; private set; } = null!;
    public TargetingService Targeting { get; private set; } = null!;
    public StationaryEnemy Enemy { get; private set; } = null!;
    public EnemySpawner? Spawner { get; private set; }
    public WaveDirector? Waves { get; private set; }
    public SwtorSurvivors.Equipment.CosmeticUnlockTracker? CosmeticUnlocks { get; private set; }
    public RunProgression? Progression { get; private set; }
    public RunArtifacts? Artifacts {get;private set;}
    public RunBuildEffects Build {get;private set;}=null!;
    public UI.SurvivorsHud? Hud { get; private set; }
    public CombatEvents Events { get; private set; } = null!;
    public FlytextService Flytext { get; private set; } = null!;
    public Equipment.CharacterAppearanceAssembly Equipment { get; private set; } = null!;
    public EquipmentAttachment PlayerSaber => Equipment.Weapon("main_hand")!;
    public PeriodicEffects Periodic {get;private set;}=null!;
    public AudioPresentation Audio { get; private set; } = null!;
    public Equipment.EvLoot? EvLoot {get;private set;}
    public UI.RewardPresentation? Rewards { get; private set; }
    public Integration.GameplayConfiguration RunConfiguration { get; private set; } = null!;
    private CanvasLayer _legacyOverlay = null!;
    private Label _combatStatus = null!;
    private Vector3 _enemySpawn;
    private MeshInstance3D? _debug;
    private bool _leavingForMenu;

    /// <summary>The environment instance is "KorribanEnvironment" in the original composition and "MapEnvironment" in map sections.</summary>
    private Node3D EnvironmentNode() => GetNodeOrNull<Node3D>("MapEnvironment") ?? GetNode<Node3D>("KorribanEnvironment");

    /// <summary>Find supported ground with full body clearance and open paths around the preferred start.</summary>
    private bool FindSpawn(Vector3 seed, out Vector3 floor)
    {
        // Walk outward from the seed and take the first ground that is open all around, using the same
        // standing/walking tests as the enemy spawner so props and canopy count. Bounded so start-up stays quick.
        float radius = Player.CapsuleRadius * 3, bestScore = -1; floor = default; bool found = false; int tried = 0;
        var navigation = new ArenaNavigation { Arena = Arena, Radius = 0.05f, Height = 0.2f };
        AddChild(navigation);
        float reach = Mathf.Max(Arena.Maximum.X - Arena.Minimum.X, Arena.Maximum.Y - Arena.Minimum.Y);
        for (float ringRadius = 0; ringRadius <= reach && tried < 150; ringRadius += 0.6f)
        {
            int samples = ringRadius == 0 ? 1 : Mathf.Max(6, (int)(ringRadius * 5));
            for (int k = 0; k < samples && tried < 150; k++)
            {
                float turn = k * Mathf.Tau / samples;
                var probe = seed + new Vector3(Mathf.Cos(turn), 0, Mathf.Sin(turn)) * ringRadius;
                // Keep Korriban starts on the approach side of the camp, away from the mountain behind it.
                if (RunConfiguration.MapId == "korriban" && (probe.X < 38 || probe.X > 42 || probe.Z < 21 || probe.Z > 26)) continue;
                if (!Arena.GroundAt(probe, out var ground, out _, 40, 60) || !Arena.Supported(ground, radius)) continue;
                if (!navigation.TryStandingPoint(ground, out var feetHere)) continue;
                tried++; int open = 0;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.Tau / 8;
                    var ring = ground + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.8f;
                    if (navigation.TryStandingPoint(ring, out var ringFeet) && navigation.CanWalk(feetHere, ringFeet)) open++;
                }
                if (open > bestScore) { bestScore = open; floor = ground; found = true; }
                if (open >= 7) { ringRadius = float.MaxValue; break; }
            }
        }
        navigation.QueueFree();
        GD.Print($"Map spawn: open ring samples {bestScore}/8 at {floor} after {tried} candidates (found={found})");
        return found && bestScore >= 7;
    }

    public override async void _Ready()
    {
        RunConfiguration = Integration.GameNavigation.TakeRunConfiguration();
        if(OS.GetCmdlineUserArgs().Contains("--eternity-vault-smoke")) ExplorationOnly=true;
        if(PlanetRunPath!="")PlanetRun=PlanetRunDefinition.Load(PlanetRunPath);
        if(PlanetRun!=null)RunConfiguration.ModeTuning.Apply(PlanetRun);
        SlotRules??=GD.Load<RunSlotRules>("res://Gameplay/run-slots.tres");
        // The adapter selects an existing composition, never substitutes assets for an unsupported key.
        if (SceneFilePath != RunConfiguration.WorldScene
            || EnvironmentNode().SceneFilePath != RunConfiguration.EnvironmentScene
            || GetNode<Node3D>("Player/Presentation/OriginalSorcerer").SceneFilePath != RunConfiguration.CharacterScene)
        { GD.PushError("Selected gameplay composition does not match its registered resources."); GetTree().Quit(1); return; }
        Player = GetNode<PlayerController>("Player"); Player.SetPhysicsProcess(false);
        Rig = GetNode<CameraRig>("CameraRig"); Arena = GetNode<ArenaBounds>("ArenaBounds");
        EnvironmentRoot = EnvironmentNode();
        TerrainClarity.Apply(EnvironmentRoot, RunConfiguration.MapId);
        if(RunConfiguration.MapId=="korriban") { var frontier=new KorribanFrontier {Name="KorribanFrontier"}; AddChild(frontier); }
        Player.Arena = Arena; Player.Rig = Rig; Rig.Target = Player;
        foreach (var body in EnvironmentRoot.FindChildren("*", "StaticBody3D", true, false).OfType<StaticBody3D>())
        {
            bool terrain = body.GetPath().ToString().Contains("/Terrain/");
            body.CollisionLayer = terrain ? 1u : 2u; body.CollisionMask = 4;
            if (!terrain)
            {
                // Source art is rendered two-sided. Give the gameplay instance matching
                // two-sided triangle collision so camera probes also see a wall's back.
                foreach (var shape in body.GetChildren().OfType<CollisionShape3D>())
                    if (shape.Shape is ConcavePolygonShape3D original)
                    {
                        var local = (ConcavePolygonShape3D)original.Duplicate();
                        local.BackfaceCollision = true; shape.Shape = local;
                    }
            }
        }
        RegisterInput(); AddLighting(); AddOverlay();
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // Authored spawn on the existing second patch, never a source NPC-placement claim.
        bool fixedSpawn=ExplorationOnly || RunConfiguration.MapId is "eternity_vault" or "soa_p3";
        var seed = new Vector3(Arena.SpawnSeed.X, fixedSpawn ? Player.Position.Y : 0, Arena.SpawnSeed.Y);
        Vector3 floor;
        bool spawnFound = fixedSpawn
            ? Arena.GroundAt(seed, out floor, out _, 1, 3)
            : FindSpawn(seed, out floor);
        if (!spawnFound)
        { GD.PushError("B1 spawn has no safe source terrain"); GetTree().Quit(1); return; }
        Player.SpawnPosition = floor + Vector3.Up * 0.002f;
        Player.ResetPlayer(); Player.SetPhysicsProcess(true); Initialized = true;
        // Existing B1 validation remains a movement-only fixture, without automatic casting.
        bool b1Validation = OS.GetCmdlineUserArgs().Any(x => x is "--gameplay-validate" or "--gameplay-capture" or "--gameplay-probe");
        if (!b1Validation)
        {
            bool b2Validation = OS.GetCmdlineUserArgs().Contains("--combat-smoke");
            if (!AddCombat(!b2Validation)) { GetTree().Quit(1); return; }
            if(!ExplorationOnly && RunConfiguration.MapId=="eternity_vault") AddChild(new EternityVaultAtmosphere());
            if (ExplorationOnly)
            {
                if (RunConfiguration.MapId == "eternity_vault") AddChild(new EternityVaultAtmosphere());
                var explore = new CanvasLayer(); AddChild(explore);
                var panel = new VBoxContainer { Position = new Vector2(18, 14) }; explore.AddChild(panel);
                panel.AddChild(new Label { Text = (RunConfiguration.MapId=="soa_p3" ? "ETERNITY VAULT · SOA · INFERNAL PRISON" : "ETERNITY VAULT · 01_Surface")+"\nWASD: move · Space: jump · Mouse: camera" });
                var back = new Button { Text = "Main Menu" }; back.Pressed += ReturnToMainMenu; panel.AddChild(back);
                _legacyOverlay.Visible = false;
                SyncCameraControls(); ReadyForPlay = true;
                GD.Print($"EXPLORATION_READY {RunConfiguration.MapId} at {Player.SpawnPosition}; no encounters");
                if (OS.GetCmdlineUserArgs().Contains("--eternity-vault-smoke")) AddChild(new EternityVaultSmoke());
                return;
            }
            if (!b2Validation && !OS.GetCmdlineUserArgs().Contains("--survival-smoke"))
            {
                Abilities.AllowHybrid=RunConfiguration.AdvancedClassId=="sorcerer";
                Build=new RunBuildEffects {Name="RunBuildEffects",World=this};AddChild(Build);
                Artifacts=new RunArtifacts {Name="RunArtifacts",World=this,SlotLimit=System.Math.Clamp(SlotRules.ArtifactSlots,1,6),
                    CatalogPath=PlanetRun?.ArtifactCatalog??"res://Gameplay/RunItems/artifacts.json"};AddChild(Artifacts);
                foreach(var ability in Abilities.Abilities)ability.Build=Build;
                Progression = new RunProgression { Name = "RunProgression", World = this,
                    FirstLevelExperience=PlanetRun?.FirstLevelExperience??12,ExperienceGrowth=PlanetRun?.ExperienceGrowth??1.3f,
                    MaximumLevel=PlanetRun?.CoreLevel??0 };
                AddChild(Progression);
                Progression.Changed += SyncCameraControls;
                string arenaName=Integration.MapRegistry.Find(RunConfiguration.MapId)?.Name ?? RunConfiguration.MapId;
                if(RunConfiguration.MapId=="eternity_vault")arenaName+=" · "+RunConfiguration.OperationDifficulty;
                Hud = new UI.SurvivorsHud { Name = "SwtorHud", World = this, ArenaName = arenaName }; AddChild(Hud);
                Rewards = new UI.RewardPresentation { Name = "RewardPresentation", World = this }; AddChild(Rewards);
                EvLoot=new Equipment.EvLoot {World=this};AddChild(EvLoot);
                AddChild(new UI.ArtifactRewardsHud {Name="RunArtifactInterface",World=this});
                if(RunConfiguration.MapId=="hutta") {AddChild(new HuttaAtmosphere());if(OS.GetCmdlineUserArgs().Contains("--d5-smoke")){var smoke=new D5Smoke();AddChild(smoke);smoke.Run(this);}}
                if(PlanetRun!=null) {Journey=new PlanetJourney {Name="PlanetJourney",World=this};AddChild(Journey);}
                if(Waves!=null) {CosmeticUnlocks=new SwtorSurvivors.Equipment.CosmeticUnlockTracker {Name="CosmeticUnlocks",World=this};AddChild(CosmeticUnlocks);}
                if(OS.GetCmdlineUserArgs().Contains("--d2-smoke")) {var smoke=new D2Smoke();AddChild(smoke);smoke.Run(this);}
                if(OS.GetCmdlineUserArgs().Contains("--difficulty-smoke")) {var smoke=new DifficultySmoke();AddChild(smoke);smoke.Run(this);}
                _legacyOverlay.Visible = false;
                if (OS.GetCmdlineUserArgs().Contains("--combat-meter-smoke"))
                { var smoke = new CombatMeterSmoke(); AddChild(smoke); smoke.Run(this); }
                if (OS.GetCmdlineUserArgs().Contains("--b6-smoke"))
                { var smoke = new B6Smoke(); AddChild(smoke); smoke.Run(this); }
                if (OS.GetCmdlineUserArgs().Contains("--b7-smoke"))
                { var smoke = new B7Smoke(); AddChild(smoke); smoke.Run(this); }
                if (OS.GetCmdlineUserArgs().Contains("--b8-smoke"))
                { var smoke = new B8Smoke(); AddChild(smoke); smoke.Run(this); }
                if (OS.GetCmdlineUserArgs().Contains("--b9-jump-smoke"))
                { var smoke = new JumpAnimationSmoke(); AddChild(smoke); smoke.Run(this); }
                if (OS.GetCmdlineUserArgs().Contains("--audio-polish-smoke"))
                { var smoke = new AudioPolishSmoke(); AddChild(smoke); smoke.Run(this); }
                if (OS.GetCmdlineUserArgs().Contains("--refinement-smoke"))
                {
                    var smoke=new RefinementSmoke();AddChild(smoke);smoke.Run(this);
                }
                if (OS.GetCmdlineUserArgs().Contains("--abilities-smoke"))
                {
                    var smoke=new AbilitySmoke();AddChild(smoke);smoke.Run(this);
                }
                if (OS.GetCmdlineUserArgs().Contains("--flytext-smoke"))
                {
                    var smoke = new FlytextSmoke(); AddChild(smoke); smoke.Run(this);
                }
                if (OS.GetCmdlineUserArgs().Contains("--storm-reticle-smoke"))
                {
                    var smoke = new StormReticleSmoke(); AddChild(smoke); smoke.Run(this);
                }
                if (OS.GetCmdlineUserArgs().Contains("--storm-source-smoke"))
                {var smoke=new StormSourceSmoke();AddChild(smoke);smoke.Run(this);}
                if(OS.GetCmdlineUserArgs().Contains("--credit-pickup-smoke")){var smoke=new CreditPickupSmoke();AddChild(smoke);smoke.Run(this);}
                if (OS.GetCmdlineUserArgs().Contains("--progression-smoke"))
                {
                    var smoke = new ProgressionSmoke(); AddChild(smoke); smoke.Run(this);
                }
            }
            if (OS.GetCmdlineUserArgs().Contains("--combat-smoke"))
            {
                var validation = new CombatValidation(); AddChild(validation); validation.Run(this);
            }
            if (OS.GetCmdlineUserArgs().Contains("--survival-smoke"))
            {
                var smoke = new SurvivalSmoke(); AddChild(smoke); smoke.Run(this);
            }
        }
        if (b1Validation)
        {
            var validation = new GameplayValidation(); AddChild(validation);
            validation.Run(this, OS.GetCmdlineUserArgs().Contains("--gameplay-capture"));
        }
        SyncCameraControls();
        if(OS.GetCmdlineUserArgs().Contains("--d3-smoke"))
        {var smoke=new D3Smoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--powertech-smoke")){var smoke=new PowertechSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--ranged-movement-smoke")){var smoke=new RangedMovementSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--sorcerer-movement-smoke")){var smoke=new SorcererMovementSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--sorcerer-smoke")){var smoke=new SorcererSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--basic-attacks-smoke")){var smoke=new BasicAttacksSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--sorcerer-refinement-smoke")){var smoke=new SorcererRefinementSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--spec-milestones-smoke")){var smoke=new SpecMilestonesSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--assassin-presentation-smoke")){var presentationSmoke=new AssassinPresentationSmoke();AddChild(presentationSmoke);presentationSmoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--assassin-refinement-smoke")){var smoke=new AssassinRefinementSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--marauder-identity-smoke")){var smoke=new MarauderIdentitySmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--carnage-rework-smoke")){var smoke=new CarnageReworkSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--juggernaut-relic-smoke")){var smoke=new JuggernautRelicSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--marauder-relic-smoke")){var smoke=new MarauderRelicSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--assassin-relic-smoke")){var smoke=new AssassinRelicSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--sorcerer-relic-smoke")){var smoke=new SorcererRelicSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--warrior-movement-smoke")){var smoke=new WarriorMovementSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--warrior-presentation-smoke")){var smoke=new WarriorPresentationSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--inquisitor-revision-smoke")){var smoke=new InquisitorRevisionSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--class-slots-smoke")){var smoke=new ClassSlotsSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--agent-spec-identity-smoke")){var smoke=new AgentSpecIdentitySmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--marauder-smoke")){var smoke=new MarauderSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--juggernaut-smoke")){var smoke=new JuggernautSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--sniper-run-powers-smoke")){var smoke=new SniperRunPowersSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--sniper-cover-smoke")){var smoke=new SniperCoverSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--agent-presentation-smoke")){var smoke=new AgentPresentationSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--agent-expansion-smoke")){var smoke=new AgentExpansionSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--assassin-run-powers-smoke")){var smoke=new AssassinRunPowersSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--assassin-expansion-smoke")){var smoke=new AssassinExpansionSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--presentation-repair-smoke")){var smoke=new PresentationRepairSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--kit-smoke"))
        {var smoke=new KitSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--korriban-endless-smoke")){var smoke=new KorribanEndlessSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--d4-smoke"))
        {var smoke=new D4Smoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--d1-smoke"))
        { var smoke=new SwtorSurvivors.Equipment.D1EquipmentSmoke();AddChild(smoke);smoke.Run(this); }
        ReadyForPlay = true;
        if(OS.GetCmdlineUserArgs().Contains("--eternity-encounter-smoke")) {var smoke=new EternityVaultEncounterSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--soa-encounter-smoke")) {var smoke=new SoaEncounterSmoke();AddChild(smoke);smoke.Run(this);}
        if(OS.GetCmdlineUserArgs().Contains("--continuous-encounter-smoke")) {var smoke=new ContinuousEncounterSmoke();AddChild(smoke);smoke.Run(this);}
    }

    private void SyncCameraControls() => Rig.SetControlsEnabled(Initialized && !_leavingForMenu && !GetTree().Paused
        && (PlayerActor == null || PlayerActor.Alive));

    private static void RegisterInput()
    {
        foreach (var entry in new[] { ("b1_forward", Key.W), ("b1_back", Key.S), ("b1_left", Key.A), ("b1_right", Key.D), ("b9_jump", Key.Space) })
        {
            if (!InputMap.HasAction(entry.Item1)) InputMap.AddAction(entry.Item1);
            if (InputMap.ActionGetEvents(entry.Item1).Count == 0)
                InputMap.ActionAddEvent(entry.Item1, new InputEventKey { PhysicalKeycode = entry.Item2 });
        }
    }

    private void AddLighting()
    {
        if (RunConfiguration.MapId == "soa_p3")
        {
            AddChild(GD.Load<PackedScene>("res://Assets/Maps/soa_p3/RoomAtmosphere.tscn").Instantiate());
            return;
        }
        var skyMaterial = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color(0.14f, 0.09f, 0.12f), SkyHorizonColor = new Color(0.64f, 0.37f, 0.22f),
            GroundBottomColor = new Color(0.16f, 0.09f, 0.065f), GroundHorizonColor = new Color(0.64f, 0.37f, 0.22f)
        };
        AddChild(new WorldEnvironment { Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky, Sky = new Sky { SkyMaterial = skyMaterial },
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.77f, 0.62f, 0.56f), AmbientLightEnergy = 0.65f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic
        }});
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-38,-32,0),
            LightColor = new Color(1,0.78f,0.57f), LightEnergy = 1.1f, ShadowEnabled = true, DirectionalShadowMaxDistance = 50 });
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-25,145,0),
            LightColor = new Color(0.52f,0.48f,0.65f), LightEnergy = 0.35f });
    }

    private void AddOverlay()
    {
        var layer = new CanvasLayer(); AddChild(layer); _legacyOverlay = layer;
        var panel = new VBoxContainer { Position = new Vector2(18,14), MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(panel);
        panel.AddChild(new Label { Text = "SWTOR SURVIVORS  /  B3 Â· Korriban Survival" });
        panel.AddChild(new Label { Text = "WASD: move   Â·   Lightning: automatic   Â·   R: restart   Â·   F3: collision/bounds   Â·   Esc: quit" });
        Status = new Label(); panel.AddChild(Status);
        _combatStatus = new Label(); panel.AddChild(_combatStatus);
    }

    private bool AddCombat(bool survival)
    {
        Events = new CombatEvents { Name = "CombatEvents" }; AddChild(Events);
        var flytextLayer = new CanvasLayer { Name = "FlytextLayer", Layer = 0 };
        AddChild(flytextLayer);
        Flytext = new FlytextService { Name = "Flytext", Events = Events, Camera = Rig.Camera };
        flytextLayer.AddChild(Flytext);
        Audio = new AudioPresentation { Name = "AudioPresentation",TuningPath=PlanetRun?.AudioTuning??"res://Gameplay/presentation-audio.json" }; AddChild(Audio);
        PlayerActor = new Combatant { Name = "PlayerCombatant", TargetId = "Player", Team = CombatTeam.Player,
            Presentation = Player.Presentation };
        PlayerActor.AddChild(new Health { Name = "Health", Maximum = 100 }); Player.AddChild(PlayerActor);
        Equipment = SwtorSurvivors.Equipment.EquipmentComposition.Create(this,
            Player.Presentation.GetNode<Node3D>("OriginalSorcerer"),Player.Presentation.Skeleton,
            RunConfiguration.BaseClassId,RunConfiguration.AdvancedClassId);
        Player.Presentation.SetEquipmentLocomotion(Equipment.Locomotion);
        Equipment.Loadout.Changed += _ => Player.Presentation.SetEquipmentLocomotion(SwtorSurvivors.Equipment.EquipmentComposition.Locomotion(Equipment,RunConfiguration.Profile));
        if(OS.GetCmdlineUserArgs().Contains("--d1-equipment"))
            AddChild(new SwtorSurvivors.Equipment.EquipmentDeveloperControls { Assembly=Equipment,
                Expanded=OS.GetCmdlineUserArgs().Contains("--d1-equipment") });
        Targeting = new TargetingService { Name = "Targeting", OwnerActor = PlayerActor,
            AcquisitionRange = AcquisitionRange, RefreshSeconds = TargetRefreshSeconds }; AddChild(Targeting);
        var visual = new LightningPresentation { Name = "LightningPresentation", Actor = Player.Presentation }; AddChild(visual);
        Combat = new AutomaticLightning { Name = "AutomaticLightning", Caster = PlayerActor, Targeting = Targeting, Visual = visual };
        var profile=RunConfiguration.Profile;
        PlayerActor.Health.Maximum=profile.Health;PlayerActor.Health.Restore();Player.MoveSpeed*=profile.Movement;
        string starter=Talents.TalentRuntime.BountyStartingAbility(profile);
        Combat.Configure(GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{(profile.Id=="marauder"?"marauder_"+starter:starter)}.tres"));
        // Bounty Hunter starters (Power Shot, or Arsenal's converted Tracer Missile) are kit resources that default to locked.
        if(profile.Id is "mercenary" or "powertech"){Combat.Definition.StartsUnlocked=true;Combat.SetUnlocked(true);}
        if(profile.Presentation=="sorcerer")
        {
            Player.Presentation.LoadAbilityAnimations();
            Player.Presentation.LoadAbilityLibrary("res://Assets/D3/casts.glb","D3");
            if(Combat.Definition.Id=="shock")
            {var shockVisual=new OriginalAbilityPresentation {Actor=Player.Presentation,Definition=Combat.Definition};AddChild(shockVisual);Combat.Presentation=shockVisual;}
            Combat.Definition.StartsUnlocked=true;Combat.SetUnlocked(true);Combat.Range=AcquisitionRange;
        }
        else
        {
            Player.Presentation.LoadAbilityLibrary(profile.AnimationScene,"D3");
            if(profile.Idle!="")Player.Presentation.SetEquipmentLocomotion(new Equipment.LocomotionDefinition("D3",profile.AnimationScene,profile.Idle,profile.Run));
            var presentation=new D3AbilityPresentation {Actor=Player.Presentation,Definition=Combat.Definition};AddChild(presentation);Combat.Presentation=presentation;
            AcquisitionRange=Combat.Range;Targeting.AcquisitionRange=Combat.Range;
        }
        if(profile.Id=="juggernaut") {Player.Presentation.LoadAbilityLibrary("res://Assets/WarriorLibrary/casts.glb","Warrior");Combat.Definition.StartsUnlocked=true;Combat.Definition.Slotless=true;Combat.Definition.ReplaceableStarter=true;Combat.SetUnlocked(true);}
        if(profile.Id is "marauder" or "juggernaut")Player.Presentation.LoadAbilityLibrary("res://Assets/WarriorPresentation/casts.glb","WarriorExtra");
        if(profile.Id is "marauder" or "juggernaut" or "sorcerer")Player.Presentation.LoadDirectionalLocomotion(profile.Id=="marauder",Player.MoveSpeed);
        if(profile.Id=="marauder"){Player.Presentation.LoadMarauderAnimations();Player.Presentation.AddChild(new MarauderBladeTrails {Actor=Player.Presentation});}
        if(profile.Id is "operative" or "sniper"){Player.Presentation.LoadAbilityLibrary("res://Assets/Agent/casts.glb","Agent");Player.Presentation.LoadRifleDirectionalLocomotion(Player.MoveSpeed);}
        if(profile.Id is "mercenary" or "powertech")Player.Presentation.LoadDirectionalLocomotion(false,Player.MoveSpeed,legsOnly:true);
        if(profile.Id is "mercenary" or "powertech")Player.Presentation.LoadAbilityLibrary("res://Assets/BountyHunter/casts.glb","BH");
        if(profile.Id=="powertech")Player.Presentation.LoadAbilityLibrary("res://Assets/Powertech/casts.glb","PT");
        if(profile.Id=="assassin") {Player.Presentation.LoadDirectionalLocomotion(false,Player.MoveSpeed,true);Player.Presentation.AddChild(new MarauderBladeTrails {Actor=Player.Presentation,Assassin=true});Player.Presentation.LoadAbilityLibrary("res://Assets/B8/casts.glb","B8");Player.Presentation.LoadAbilityLibrary("res://Assets/Assassin/casts.glb","Assassin");Combat.DamagePerTick=Combat.Definition.Damage=12;Combat.CooldownSeconds=Combat.Definition.CooldownSeconds=2.4f;Combat.Radius=Combat.Definition.Radius=.24f;Combat.Definition.Slotless=true;Combat.Definition.Description="Modest saber sweep. Spreads existing Discharge to nearby enemies hit; talents can spread other DoTs. Starts free and can be replaced.";}
        if(profile.Id is "operative" or "sniper"){Combat.Definition.Slotless=true;Combat.Definition.ReplaceableStarter=true;}
        if(profile.Id=="sniper")AddChild(new SniperCoverController {Name="SniperCover",World=this});
        Periodic=new PeriodicEffects {Name="PeriodicEffects"};AddChild(Periodic);Combat.Periodic=Periodic;
        AddChild(Combat);
        Abilities=new AbilityController { Name="AutomaticAbilities",Targeting=Targeting,
            OffensiveLimit=profile.OffensiveSlots>=0?profile.OffensiveSlots:SlotRules!.OffensiveSlots,UtilityLimit=profile.UtilitySlots>=0?profile.UtilitySlots:SlotRules!.UtilitySlots };
        if(profile.Id=="marauder")Abilities.GlobalCooldownSeconds=.6f;
        Abilities.Abilities.Add(Combat);
        if(profile.BasicAttack!="")
        {
            Player.Presentation.LoadAbilityLibrary("res://Assets/BasicAttacks/casts.glb","Basic");
            var definition=GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{profile.BasicAttack}.tres");
            var basicVisual=new D3AbilityPresentation {Name="BasicAttackPresentation",Actor=Player.Presentation,Definition=definition};AddChild(basicVisual);
            var basic=new AutomaticAbility {Name=profile.BasicAttack,Caster=PlayerActor,Targeting=Targeting,Presentation=basicVisual,Periodic=Periodic};
            basic.Configure(definition);
            basicVisual.Definition=basic.Definition;
            AddChild(basic);Abilities.Abilities.Add(basic);
        }
        // Class kits: locked until a level-up unlocks them; same lifecycle, presentation adapter and slot rules as the starter.
        foreach(string id in profile.Presentation=="sorcerer"?System.Array.Empty<string>():profile.Kit)
        {
            if(id==Combat.Definition.Id)continue; // A talent-converted starter is never also a kit unlock.
            var definition=GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{(profile.Id=="juggernaut" && id=="forcescream"?"juggernaut_forcescream":profile.Id=="marauder"?"marauder_"+id:id)}.tres");
            IAbilityPresentation kitVisual;
            if(profile.Id=="assassin" && id=="electrocute"){var electrocute=new ElectrocutePresentation {Actor=Player.Presentation,Definition=definition,World=this};AddChild(electrocute);kitVisual=electrocute;}
            else if(profile.Id=="assassin" && id is "voltaicslash" or "maul" or "spike" or "wither" or "lowslash" or "forcecloak" or "deflection") {var original=new AssassinAbilityPresentation {Actor=Player.Presentation,Definition=definition,World=this};AddChild(original);kitVisual=original;}
            else if(id is "forcespeed" or "crushingdarkness") {var speed=new SorcererRefinementPresentation {Actor=Player.Presentation,Definition=definition};AddChild(speed);kitVisual=speed;}
            else if(id=="shock") {var shock=new OriginalAbilityPresentation {Actor=Player.Presentation,Definition=definition};AddChild(shock);kitVisual=shock;}
            else if(id=="affliction")
            {
                if(!Player.Presentation.Animator.HasAnimationLibrary("B5"))Player.Presentation.LoadAbilityLibrary("res://Assets/Abilities/casts.glb","B5");
                var affliction=new SorcererAbilityPresentation {Actor=Player.Presentation,Definition=definition};AddChild(affliction);kitVisual=affliction;
            }
            else if(id=="deathfield")
            {
                var field=new SurvivalAbilityPresentation {Name=id+"Presentation",Actor=Player.Presentation,Definition=definition,Health=PlayerActor.Health};
                AddChild(field);kitVisual=field;
            }
            else {var visualKit=new D3AbilityPresentation {Name=id+"Presentation",Actor=Player.Presentation,Definition=definition};AddChild(visualKit);kitVisual=visualKit;}
            var kit=new AutomaticAbility {Name=id,Caster=PlayerActor,Targeting=Targeting,Presentation=kitVisual,Periodic=Periodic};
            kit.Configure(definition);
            if((profile.Id is "operative" or "sniper") && id=="dart"){kit.Definition.StartsUnlocked=profile.Id=="operative";kit.SetUnlocked(kit.Definition.StartsUnlocked);}
            if(profile.Id=="juggernaut"){kit.Definition.StartsUnlocked=false;kit.SetUnlocked(false);}
            if(profile.Id=="assassin") {
                if(!Player.Presentation.Animator.HasAnimationLibrary("B5"))Player.Presentation.LoadAbilityLibrary("res://Assets/Abilities/casts.glb","B5");
                if(!Player.Presentation.Animator.HasAnimationLibrary("T22"))Player.Presentation.LoadAbilityLibrary("res://Assets/SorcererRefinement/casts.glb","T22");
                if(kit.Definition.Category==AbilityCategory.Utility || id=="lowslash"){kit.Definition.Category=AbilityCategory.Utility;kit.Definition.Slotless=true;}
                if(id=="shock") {kit.Definition.Area=AbilityArea.SingleTarget;kit.Range=.45f;}
                if(id=="creepingterror") {kit.Range=.55f;kit.Definition.PeriodicHealing=2;}
                if(id=="crushingdarkness")kit.Range=.55f;
                if(id=="discharge")kit.Definition.NearbyTriggerCount=1;
                if(id=="deathfield") {kit.Range=.55f;kit.Healing=3;kit.Definition.HealingCap=10;kit.Definition.ExclusiveWith="";}
            }
            AddChild(kit);Abilities.Abilities.Add(kit);
        }
        if (survival && RunConfiguration.Profile.Presentation=="sorcerer" && !OS.GetCmdlineUserArgs().Any(x=>x is "--survival-smoke" or "--progression-smoke"))
        {
            Player.Presentation.LoadAbilityLibrary("res://Assets/Sorcerer/casts.glb","Sorc");
            Player.Presentation.LoadAbilityLibrary("res://Assets/SorcererRefinement/casts.glb","T22");
            foreach(string id in new[]{"shock","lightning","storm","thunderingblast","affliction","creepingterror","deathfield","barrier","darkheal","chain","overload","lightningstrike","crushingdarkness","polarityshift","forcespeed"})
            {
                if(id==profile.StartingAbility)continue;
                var definition=(AbilityDefinition)GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{id}.tres").Duplicate();
                definition.StartsUnlocked=id=="affliction";
                if(id is "affliction" or "polarityshift")definition.Slotless=true;
                if(id=="shock"){definition.Damage=22;definition.Area=AbilityArea.SingleTarget;}
                if(id=="affliction")definition.CooldownSeconds=12;
                if(id=="creepingterror")definition.CooldownSeconds=14;
                IAbilityPresentation presentation;
                if(id=="lightning")presentation=visual;
                else if(id is "lightningstrike" or "crushingdarkness" or "polarityshift" or "forcespeed")
                {
                    var original=new SorcererRefinementPresentation {Actor=Player.Presentation,Definition=definition};AddChild(original);presentation=original;
                }
                else if(id=="creepingterror")
                {
                    var original=new D3AbilityPresentation {Actor=Player.Presentation,Definition=definition};AddChild(original);presentation=original;
                }
                else if(id is "thunderingblast" or "affliction")
                {
                    var original=new SorcererAbilityPresentation {Actor=Player.Presentation,Definition=definition};
                    AddChild(original);presentation=original;
                }
                else if(id is "shock" or "storm")
                {
                    var original=new OriginalAbilityPresentation { Actor=Player.Presentation,Definition=definition };
                    AddChild(original);presentation=original;
                }
                else if(id is "chain" or "overload")
                {
                    var original=new D2AbilityPresentation {Actor=Player.Presentation,Definition=definition};
                    AddChild(original);presentation=original;
                }
                else
                {
                    var original=new SurvivalAbilityPresentation { Actor=Player.Presentation,Definition=definition,Health=PlayerActor.Health };
                    AddChild(original);presentation=original;
                }
                var ability=new AutomaticAbility {Name=id,Caster=PlayerActor,Targeting=Targeting,Presentation=presentation,Periodic=Periodic};
                if(id=="storm") ((OriginalAbilityPresentation)presentation).EffectiveRadius=()=>ability.Radius;
                ability.Configure(definition);AddChild(ability);Abilities.Abilities.Add(ability);
            }
        }
        Abilities.ApplyClassSlotPolicy(profile.Id,profile.StartingAbility);
        AddChild(Abilities);
        PlayerActor.Health.Died += () =>
        {
            Rig.SetControlsEnabled(false);
            Periodic.Clear();Abilities.Abort(); PlayerActor.CombatEnabled = false;
            Progression?.CancelPresentation(); Rewards?.Clear(); Audio.Clear();
            Waves?.Stop(); Player.SetPhysicsProcess(false); Player.Velocity = Vector3.Zero;
            Player.Presentation.PlayReaction("Death");
        };
        if (ExplorationOnly) return true;
        if (survival)
        {
            var navigation = new ArenaNavigation { Name = "ArenaNavigation", Arena = Arena }; AddChild(navigation);
            Spawner = new EnemySpawner { Name = "EnemySpawner", Navigation = navigation, Player = PlayerActor,
                EnemyScene = EnemyScene ?? GD.Load<PackedScene>("res://Gameplay/Enemies/Broodling.tscn"),
                MinimumSpawnDistance = MinimumSpawnDistance, MaximumSpawnDistance = MaximumSpawnDistance,
                CorpseSeconds = CorpseSeconds,ReuseAnimations=PlanetRun!=null,OperationMode=RunConfiguration.ModeTuning }; AddChild(Spawner);
            Waves = new WaveDirector { Name = "WaveDirector", Spawner = Spawner, WaveSeconds = WaveSeconds,
                PlanetRun=PlanetRun,PlayerLevel=()=>Progression?.Level??1,
                CanStartMilestone=()=>Progression!=null && !Progression.Selecting && Artifacts?.Pending!=true,
                InitialSpawnInterval = InitialSpawnInterval, SpawnIntervalMultiplier = SpawnIntervalMultiplier,
                MinimumSpawnInterval = MinimumSpawnInterval, InitialActiveLimit = InitialActiveLimit,
                AdditionalActivePerWave = AdditionalActivePerWave, BossWave = BossWave,
                MaximumActiveLimit = MaximumActiveLimit, HealthPerWave = HealthPerWave, DamagePerWave = DamagePerWave }; AddChild(Waves);
            Waves.ResetWaves();
            return true;
        }
        // Retain the exact B2 stationary fixture for the existing opt-in validator.
        Enemy = GD.Load<PackedScene>("res://Gameplay/StationaryEnemy.tscn").Instantiate<StationaryEnemy>();
        AddChild(Enemy);
        // A bounded authored placement search at initialization only; no enemy spawning or world edits.
        bool placed = false;
        foreach (var offset in new[] { new Vector3(0,0,-0.45f), new Vector3(0.45f,0,0), new Vector3(-0.45f,0,0), new Vector3(0,0,0.45f) })
        {
            var point = Player.SpawnPosition + offset;
            if (!Arena.GroundAt(point, out var floor, out _, 0.4f, 0.8f) || !Arena.Supported(floor, Player.CapsuleRadius)) continue;
            Enemy.GlobalPosition = floor + Vector3.Up * 0.002f;
            var clearance = new SphereShape3D { Radius = 0.045f };
            var query = new PhysicsShapeQueryParameters3D { Shape = clearance, CollisionMask = 2,
                Transform = new Transform3D(Basis.Identity, Enemy.AimPosition) };
            if (Enemy.GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count > 0 || !Targeting.IsValidTarget(Enemy)) continue;
            placed = true; break;
        }
        if (!placed) { GD.PushError("B2: no safe, visible nearby test enemy placement"); return false; }
        _enemySpawn = Enemy.GlobalPosition;
        return true;
    }

    public void ResetCombatWorld()
    {
        Hud?.Meter.FinishRun("Incomplete · Restarted");
        Periodic?.Clear();
        Rewards?.Clear(); Audio?.Clear();
        Flytext?.Clear();
        EvLoot?.ResetRun();
        Progression?.ResetRun();
        if (Combat != null)
        {
            Abilities.ResetCombat(); PlayerActor.Health.Restore(); PlayerActor.CombatEnabled = true;
            if (GodotObject.IsInstanceValid(Enemy)) { Enemy.GlobalPosition = _enemySpawn; Enemy.ResetEnemy(); }
        }
        Player.ResetPlayer(); Player.SetPhysicsProcess(true);
        Waves?.ResetWaves();
        Journey?.ResetJourney();
        Hud?.Meter.ResetRun();
        Hud?.AbilityMeter.ResetRun();
        SyncCameraControls();
    }

    public void ReturnToMainMenu()
    {
        Hud?.Meter.FinishRun("Incomplete · Left run");
        var error = Integration.GameNavigation.ReturnToMenu(this);
        if (error != Error.Ok) GD.PushError("Unable to return to main menu: " + error);
    }

    internal void StopForMenu()
    {
        Periodic?.Clear();
        _leavingForMenu = true;
        Rig.SetControlsEnabled(false);
        Abilities?.Abort();
        if (PlayerActor != null) PlayerActor.CombatEnabled = false;
        Waves?.Stop(); Progression?.CancelPresentation();
        Rewards?.Clear(); Flytext?.Clear(); Audio?.Clear();
        Player.SetPhysicsProcess(false); Player.Velocity = Vector3.Zero;
        // ChangeScene frees this entire world, including spawns, effects, portraits and all HUD layers.
    }

    public override void _Process(double delta)
    {
        if (!Initialized) return;
        Status.Text = $"{Player.Presentation.State} Â· {(Player.IsOnFloor() ? "grounded" : "gravity")} Â· {Player.ActualVelocity.Length():0.00} source units/s" +
            (_debug?.Visible == true ? $"\nPosition {Player.GlobalPosition} Â· bounds blocks {Arena.RejectedMoves} Â· recovery {Player.Recoveries}\n{Arena.LastRejection}" : "");
        if (Combat != null)
        {
            var target = Combat.CastTarget ?? Targeting.Current;
            bool valid = GodotObject.IsInstanceValid(target);
            _combatStatus.Text = $"Player {PlayerActor.Health.Current:0}/{PlayerActor.Health.Maximum:0} HP Â· {Combat.State} Â· cooldown {Combat.CooldownRemaining:0.0}s\n" +
                $"Target: {(valid ? target!.SelectionKey : "none")} Â· HP {(valid ? $"{target!.Health.Current:0}/{target.Health.Maximum:0}" : "â€”")} Â· damage events {Combat.DamageEvents}\n{Combat.LastEvent}";
            if (Waves != null && Spawner != null)
            {
                _combatStatus.Text = $"HEALTH  {PlayerActor.Health.Current:0}/{PlayerActor.Health.Maximum:0}   Â·   WAVE {Waves.Wave}  ({Waves.WaveRemaining:0}s)\n" +
                    $"Enemies {Spawner.ActiveCount}/{Spawner.ActiveLimit}   Â·   Defeated {Spawner.Defeated}\n" +
                    (PlayerActor.Alive ? $"Lightning: {Combat.State}   Â·   {Spawner.LastPlacement}\n{Combat.LastEvent}" : "YOU DIED Â· Press R to restart from wave 1");
            }
            if (GodotObject.IsInstanceValid(Enemy))
                Enemy.GetNode<Label3D>("Nameplate").Text = Enemy.Health.IsAlive ? $"HOSTILE Â· {Enemy.Health.Current:0}/{Enemy.Health.Maximum:0}" : "DEFEATED Â· R to reset";
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key || !Initialized) return;
        if (key.PhysicalKeycode == Key.R) ResetCombatWorld();
        if (key.PhysicalKeycode == Key.F3) ToggleDebug();
        if (key.PhysicalKeycode == Key.Escape) Hud?.Menu.OpenPause();
    }

    public void ToggleDebug()
    {
        if (Hud != null) _legacyOverlay.Visible = !_legacyOverlay.Visible;
        if (_debug != null) { _debug.Visible = !_debug.Visible; return; }
        var mesh = new ImmediateMesh(); mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        foreach (var shape in EnvironmentRoot.FindChildren("*", "CollisionShape3D", true, false).OfType<CollisionShape3D>())
        {
            if (shape.Shape is not ConcavePolygonShape3D triangles) continue;
            var faces = triangles.GetFaces();
            for (int i = 0; i < faces.Length; i += 3)
                for (int j = 0; j < 3; j++)
                { mesh.SurfaceAddVertex(shape.GlobalTransform * faces[i+j]); mesh.SurfaceAddVertex(shape.GlobalTransform * faces[i+(j+1)%3]); }
        }
        // Bounds are vertical corner posts plus rectangle at spawn height; terrain probes enforce holes.
        var corners = new[] { new Vector3(Arena.Minimum.X,Player.SpawnPosition.Y,Arena.Minimum.Y),
            new Vector3(Arena.Maximum.X,Player.SpawnPosition.Y,Arena.Minimum.Y),
            new Vector3(Arena.Maximum.X,Player.SpawnPosition.Y,Arena.Maximum.Y),
            new Vector3(Arena.Minimum.X,Player.SpawnPosition.Y,Arena.Maximum.Y) };
        for (int i=0;i<4;i++)
        {
            mesh.SurfaceAddVertex(corners[i]); mesh.SurfaceAddVertex(corners[(i+1)%4]);
            mesh.SurfaceAddVertex(corners[i]-Vector3.Up*20); mesh.SurfaceAddVertex(corners[i]+Vector3.Up*30);
        }
        mesh.SurfaceEnd();
        _debug = new MeshInstance3D { Mesh = mesh, MaterialOverride = new StandardMaterial3D
        { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(0.2f,1,0.6f), NoDepthTest = true } };
        AddChild(_debug);
    }
}
