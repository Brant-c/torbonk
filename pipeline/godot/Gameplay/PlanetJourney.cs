using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SwtorSurvivors;

/// <summary>Optional exploration, atmosphere and area announcements on top of the existing run director.</summary>
public partial class PlanetJourney : Node3D
{
    public GameWorld World {get;set;}=null!;
    public IReadOnlyList<TombCache> Caches=>_caches;
    public PlanetStage CurrentStage {get;private set;}=null!;
    public string Region {get;private set;}="";
    private readonly List<TombCache> _caches=new();
    private Label _journeyLabel=null!;
    private CanvasLayer _layer=null!;
    private UI.SwtorButton _beginPhase=null!;
    private bool _won;
    private double _regionCheck;
    public override void _Ready()
    {
        _layer=new CanvasLayer();AddChild(_layer);
        _journeyLabel=UI.SwtorSkin.Text("",14,true);_journeyLabel.HorizontalAlignment=HorizontalAlignment.Right;
        _journeyLabel.Size=new Vector2(420,100);_journeyLabel.MouseFilter=Control.MouseFilterEnum.Ignore;
        _layer.AddChild(_journeyLabel);
        _journeyLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        _journeyLabel.OffsetLeft=-440;_journeyLabel.OffsetRight=-20;_journeyLabel.OffsetTop=170;_journeyLabel.OffsetBottom=270;
        _beginPhase=new UI.SwtorButton {Text="BEGIN NEXT PHASE",Visible=false};
        _layer.AddChild(_beginPhase);
        _beginPhase.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        _beginPhase.OffsetLeft=-260;_beginPhase.OffsetRight=-20;_beginPhase.OffsetTop=404;_beginPhase.OffsetBottom=440;
        _beginPhase.Pressed+=()=>World.Waves!.BeginNextPhase();
        foreach(var stage in World.PlanetRun!.Stages)
            if(FindOpen(stage.Focus,out var destination))stage.Focus=[destination.X,destination.Y,destination.Z];
        foreach(var d in World.PlanetRun!.Caches)
        {
            if(!FindOpen(d.Position,out var floor)) {GD.PushError("No reachable planet cache site: "+d.Id);continue;}
            var cache=new TombCache {Definition=d,World=World};AddChild(cache);cache.GlobalPosition=floor;_caches.Add(cache);
        }
        if(World.RunConfiguration.MapId=="korriban")BuildBurialCourts();ResetJourney();
    }
    public bool FindOpen(float[] seed,out Vector3 floor)
    {
        floor=default;var origin=new Vector3(seed[0],seed[1],seed[2]);
        var nav=World.Spawner!.Navigation;
        for(int ring=0;ring<=8;ring++)for(int k=0;k<(ring==0?1:12);k++)
        {
            float angle=k*Mathf.Tau/12;var p=origin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*ring*.16f;
            if(!World.Arena.GroundAt(p,out var g,out _,10,30) || !nav.TryStandingPoint(g,out var feet))continue;
            int open=0;
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.Tau/8;var q=feet+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.22f;
                if(nav.TryStandingPoint(q,out var end) && nav.CanWalk(feet,end))open++;
            }
            if(open<6)continue;floor=feet;return true;
        }
        return false;
    }
    public override void _PhysicsProcess(double delta)
    {
        if(!World.PlayerActor.Alive){_beginPhase.Visible=false;return;}
        var stage=World.PlanetRun!.StageAt(World.Progression!.Level);
        if(CurrentStage!=stage)
        {
            CurrentStage=stage;
            World.Rewards!.Announce(Integration.MapRegistry.Find(World.RunConfiguration.MapId)?.Name.ToUpperInvariant()??"",stage.Region.ToUpperInvariant(),stage.Description,"d4_region");
            UpdateAmbience();
            // Original area/tomb scheme colors, adapted as gentle ambient changes to preserve telegraph contrast.
            var env=World.GetChildren().OfType<WorldEnvironment>().FirstOrDefault()?.Environment;
            if(env!=null && World.RunConfiguration.MapId=="korriban")env.AmbientLightColor=World.Progression.Level>=26?new Color(.43f,.362f,.678f):
                World.Progression.Level>=11?new Color(.60f,.46f,.47f):new Color(.77f,.62f,.56f);
        }
        _regionCheck-=delta;
        if(_regionCheck<=0)
        {
            _regionCheck=.4;
            // Spatial titles describe the adapted playable regions; stage enemy mixtures remain level-driven.
            var p=World.Player.GlobalPosition;
            string region=p.X>49 && p.Z>24?"Forbidden Tombs":p.X>44 && p.Z>17?"Lower Wilds":p.Z>16?"Tomb Precinct":"Valley of the Dark Lords";
            if(World.RunConfiguration.MapId!="korriban")region=stage.Region;
            if(region!=Region) {Region=region;UpdateAmbience();World.Rewards!.Announce("AREA DISCOVERED",region.ToUpperInvariant());}
            int next=World.Waves!.MilestonesCompleted;
            string objective=next<World.PlanetRun.Milestones.Length?$"NEXT TRIAL  ·  LEVEL {World.PlanetRun.Milestones[next].Level}":"TRIALS COMPLETE";
            if(World.RunConfiguration.MapId!="korriban")objective=objective.Replace("TRIAL","ENCOUNTER");
            _journeyLabel.Text=$"{Region.ToUpperInvariant()}\n{objective}";
            _journeyLabel.OffsetTop=World.Waves.Recovering?320:170;
            _journeyLabel.OffsetBottom=_journeyLabel.OffsetTop+100;
            _beginPhase.Visible=false;
            if(World.Waves.Recovering && World.Waves.TravelDestination is {} destination)
            {
                var target=new Vector3(destination.Focus[0],destination.Focus[1],destination.Focus[2]);
                var offset=target-p;
                string[] directions={"N","NE","E","SE","S","SW","W","NW"};
                int heading=((int)Math.Round(Math.Atan2(offset.X,-offset.Z)/(Math.PI/4))+8)%8;
                float distance=new Vector2(offset.X,offset.Z).Length();
                _journeyLabel.Text=$"RECOVERY  ·  {Math.Ceiling(World.Waves.RecoveryRemaining)}s\n"+
                    $"{destination.Region.ToUpperInvariant()}  ·  {directions[heading]}\n"+
                    (distance<=.65f?"DESTINATION REACHED":$"DISTANCE  {distance:0.0}  ·  COLLECT REWARDS AND RELOCATE");
                _beginPhase.Visible=offset.Length()<=.65f;
            }
            else if(World.Waves.ReturnRampRemaining>0)
                _journeyLabel.Text+="\nHOSTILES REGROUPING";
        }
        if(!_won && World.Waves!.CoreComplete)
        {_won=true;World.Rewards!.Announce(World.RunConfiguration.MapId=="korriban"?"TRIALS OF THE SITH COMPLETE":"UNDERWORLD ENCOUNTERS COMPLETE",(Integration.MapRegistry.Find(World.RunConfiguration.MapId)?.Name.ToUpperInvariant()??"")+" SURVIVED","Choose your final reward. Restart or return to the galaxy.","boss_victory");}
    }
    public void ResetJourney()
    {
        _won=false;CurrentStage=null!;Region="";_regionCheck=0;
        _beginPhase.Visible=false;
        foreach(var c in _caches)c.ResetCache();
    }
    private void UpdateAmbience() { if(World.RunConfiguration.MapId!="korriban")return; World.Audio.SetWorldAmbience(
        "res://Assets/Korriban/D4/Audio/"+(Region is "Tomb Precinct" or "Forbidden Tombs"?"tomb":"valley")+".wav",CurrentStage?.AmbientGain??-24); }
    public int BurialProps {get;private set;}
    private void BuildBurialCourts()
    {
        // Open-air Survivors arrangements; these are explicitly authored placements of original tomb objects.
        // Keep a broad central fighting space and no roof/camera enclosure.
        foreach(var cache in _caches.Where(c=>c.Definition.Id is "precinct" or "forbidden"))
        {
            string[] models=["kor_tomb_item_sarcophagus","kor_tomb_item_sarcophagus_ruin","kor_tomb_item_brazier_ancient","kor_tomb_item_statue_03"];
            for(int i=0;i<4;i++)
            {
                var center=cache.GlobalPosition;
                Vector3 feet=default;bool found=false;
                // Scenery may sit on gently uneven source ground; the player support/drop policy is too strict
                // for a wide static sarcophagus. Keep the full footprint inside the crop and outside other props.
                for(int attempt=0;attempt<18;attempt++)
                {
                    float angle=i*Mathf.Tau/4+(attempt%6-2.5f)*.18f;
                    var near=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(.9f+attempt/6*.3f);
                    if(!World.Arena.Contains(near,.45f) || !World.Arena.GroundAt(near,out var ground,out var normal,1,2) || normal.Y<.85f)continue;
                    if(!World.Spawner!.Navigation.TryStandingPoint(ground,out feet))continue;
                    float radius=i==2?.14f:.38f;
                    var q=new PhysicsShapeQueryParameters3D {Shape=new SphereShape3D {Radius=radius},CollisionMask=2,
                        Transform=new Transform3D(Basis.Identity,feet+Vector3.Up*.35f)};
                    if(GetWorld3D().DirectSpaceState.IntersectShape(q,1).Count>0)continue;
                    found=true;break;
                }
                if(!found)continue;
                var node=GD.Load<PackedScene>($"res://Assets/Korriban/D4/{models[i]}.glb").Instantiate<Node3D>();
                World.EnvironmentRoot.AddChild(node);node.GlobalPosition=feet;node.RotationDegrees=new Vector3(0,90*i,0);
                node.SetMeta("source_model","/resources/art/static/area/kor_korriban/item/"+models[i]+".gr2");
                node.SetMeta("placement","D4 authored open burial court; not an original MMO placement");
                if(models[i].Contains("brazier"))node.AddChild(new OriginalTorch());
                foreach(var mesh in node.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
                {
                    var body=new StaticBody3D {CollisionLayer=2,CollisionMask=4};mesh.AddChild(body);
                    body.AddChild(new CollisionShape3D {Shape=mesh.Mesh.CreateConvexShape(true,true)});
                }
                BurialProps++;
            }
        }
    }
}

public partial class TombCache : Node3D
{
    public PlanetCacheDefinition Definition {get;set;}=null!;
    public GameWorld World {get;set;}=null!;
    public bool Claimed {get;private set;}
    public EnemyAgent? Guard {get;private set;}
    public bool Available=>!Claimed && World.Progression!.Level>=Definition.Level;
    private Node3D _model=null!;
    private Label3D _label=null!;
    private MeshInstance3D _glow=null!;
    private PackedScene? _guardScene;
    private bool _guarded;
    private double _retry;
    public override void _Ready()
    {
        _model=GD.Load<PackedScene>(Definition.Model).Instantiate<Node3D>();AddChild(_model);
        _model.SetMeta("source_model","/resources/art/static/item/container/item_con_crate_ancient_01.gr2");
        _label=new Label3D {Font=GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"),Text=Definition.Title,FontSize=20,OutlineSize=6,
            PixelSize=.00034f,Position=Vector3.Up*.22f,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled};AddChild(_label);
        _glow=new MeshInstance3D {Position=Vector3.Up*.006f,Mesh=new PlaneMesh {Size=Vector2.One*.22f},
            MaterialOverride=new StandardMaterial3D {AlbedoTexture=GD.Load<Texture2D>("res://Assets/Presentation/loot_ring.png"),
                AlbedoColor=new Color(.35f,.8f,1,.8f),ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency=BaseMaterial3D.TransparencyEnum.Alpha,DisableFog=true,BlendMode=BaseMaterial3D.BlendModeEnum.Add},
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};AddChild(_glow);
        if(Definition.Guard!="")_guardScene=GD.Load<PackedScene>(Definition.Guard);
    }
    public override void _PhysicsProcess(double delta)
    {
        _model.Visible=_label.Visible=_glow.Visible=Available;
        if(!Available || !World.PlayerActor.Alive)return;
        float distance=World.Player.GlobalPosition.DistanceTo(GlobalPosition);
        _retry-=delta;
        if(_guardScene!=null && !_guarded && distance<1.4f && _retry<=0)
        {
            _retry=1;
            if(World.Spawner!.TrySpawn(_guardScene,anchor:GlobalPosition))
            {_guarded=true;Guard=World.Spawner.Enemies.Last();}
        }
        bool blocked=_guardScene!=null && (!_guarded || GodotObject.IsInstanceValid(Guard) && Guard!.Alive);
        _label.Text=Definition.Title+(blocked?"\nDEFEAT THE GUARDIAN":"");
        if(!blocked && distance<=.24f)Claim();
    }
    public bool Claim()
    {
        if(!Available || !World.PlayerActor.Alive || _guardScene!=null && (!_guarded || GodotObject.IsInstanceValid(Guard) && Guard!.Alive))return false;
        Claimed=true;_model.Visible=_label.Visible=_glow.Visible=false;
        if(Definition.Reward=="artifact")World.Artifacts!.EnqueueCache(Definition.Id);
        else if(Definition.Reward=="heal")World.PlayerActor.Health.ApplyHealing(40);
        else World.Progression!.AddExperience(28);
        World.Audio.PlayCue("upgrade_prototype");
        World.Rewards!.Announce(World.RunConfiguration.MapId=="hutta"?"CONTRABAND CACHE":"ANCIENT CACHE",Definition.Title,Definition.Reward=="artifact"?"CHOOSE ONE RUN REWARD":Definition.Reward=="heal"?"HEALTH RESTORED":"EXPERIENCE RECOVERED");
        return true;
    }
    public void ResetCache() {Claimed=false;Guard=null;_guarded=false;_retry=0;}
}
