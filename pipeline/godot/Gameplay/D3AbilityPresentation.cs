using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors;
/// <summary>Engine-specific adapter using recovered April artwork and PCM. No combat authority: every effect is finite and bounded.</summary>
public partial class D3AbilityPresentation : Node3D,IAbilityPresentation
{
    public CharacterPresentation Actor {get;set;}=null!;
    public AbilityDefinition Definition {get;set;}=null!;
    private sealed class Fx { public Node3D Node=null!;public double Age,Life;public Action<Fx>? Update; }
    private readonly List<Fx> _effects=new();
    private readonly List<(double at,Action run)> _delayed=new();
    private readonly List<AudioStreamPlayer3D> _voices=new();
    private static ShaderMaterial? _additive;
    private readonly Dictionary<string,ShaderMaterial> _materials=new();
    private readonly RandomNumberGenerator _random=new();
    private float _radius;
    private Vector3 _forward;
    private double _flame,_flameSpawn;
    private double _carbon,_carbonSpawn;
    public int PlayingAudioVoices=>_voices.Count(v=>v.Playing);
    public string LastPlayedAudioPath {get;private set;}="";
    public float LastPlayedAudioGainDb {get;private set;}
    public int PendingAudioEvents=>_delayed.Count;
    public bool CarbonizeEmitting=>_carbon>0;
    public bool CarbonizeAudioPlaying=>Definition.Id=="carbonize" && _voices.Any(v=>v.Playing);
    private const int MaxEffects=90;
    private Node3D? _bubble;private Health? _shieldHealth;private bool _bubbleSeen;private double _bubbleAge;
    private double _gas,_gasSpawn;private Vector3 _gasAt;private float _gasRadius;
    public bool ShieldVisible=>_bubble!=null && GodotObject.IsInstanceValid(_bubble);
    public int EffectCount=>_effects.Count;
    public bool AdditiveBound{get;private set;}
    public bool Emitting=>_flame>0;
    // Voices 0..4 are fixed events; 5..12 preserve overlapping shot tails. Allocated once.
    public override void _Ready()
    {
        _random.Seed=(ulong)(Definition?.Id.GetHashCode()??1);
        for(int i=0;i<13;i++)
        {
            var voice=new AudioStreamPlayer3D {Bus="B7Combat",VolumeDb=-18,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};
            AddChild(voice);AudioPresentation.Find(this)?.RegisterCombatVoice(voice);_voices.Add(voice);
        }
    }
    public bool Begin(Combatant target,Vector3 center,float radius)
    {
        _forward=(target.GlobalPosition-Actor.GlobalPosition).Normalized();
        _radius=radius;Play(Definition.CastAudio,Actor.GlobalPosition,0);
        // The swing sounds when the blade lands, not when a long leap or wind-up begins.
        double swing=Math.Max(0,Definition.WindupSeconds-.15);
        if(Definition.WeaponAudio!="" && !(WarriorVisual && Definition.ChannelSeconds>0))
        { if(swing<=.01)Play(Definition.WeaponAudio,Actor.GlobalPosition,1);else _delayed.Add((swing,()=>Play(Definition.WeaponAudio,Actor.GlobalPosition,1))); }
        if(Definition.OffhandAudio!="" && !(WarriorVisual && Definition.ChannelSeconds>0))_delayed.Add((swing+.3,()=>Play(Definition.OffhandAudio,Actor.GlobalPosition,2)));
        bool agentVisual=BeginAgent(target);
        if(Definition.Effect==AbilityEffect.Shield)ShowShield(target);
        else if(!agentVisual && Definition.Effect!=AbilityEffect.Damage && !(WarriorVisual && Definition.Id=="enrageddefense"))Pulse(Actor.GlobalPosition+Vector3.Up*.06f);
        if(Definition.PresentationKind=="gas" && Definition.Effect==AbilityEffect.Damage)Projectile(MuzzleWorld(),target.AimPosition,Math.Max(.2,Definition.WindupSeconds));
        BeginMarauder(target);
        BeginRepair(target);
        BeginBountyHunter(target);
        return Definition.StartClip=="" && !Definition.RequireGesture || Actor.Animator.HasAnimation(Definition.StartClip);
    }
    public void Channel(){ ChannelRepair();if(Definition.Id=="carbonize"){_carbon=1.2;_carbonSpawn=0;} if(Definition.Id=="flamethrower"){_flame=Definition.ChannelSeconds;_flameSpawn=0;} if(Definition.PresentationKind=="flame"){_flame=Math.Max(.1,Definition.ChannelSeconds);_flameSpawn=0;} }
    public void Impact(Vector3 center)
    {
        if(Definition.Effect!=AbilityEffect.Damage){
            // Endure Pain's recovered cast FXSpec layers the cast and boom events.
            if(WarriorVisual && Definition.Id=="endurepain")Play(Definition.ImpactAudio,Actor.GlobalPosition,3);
            return;
        }
        bool radial=Definition.Area==AbilityArea.CasterRadial;
        var at=radial?Actor.GlobalPosition+Vector3.Up*.06f:center;
        if(ImpactAgent(at)){if(!AgentKnifeAbility && Definition.Id!="explosiveprobe")Play(Definition.ImpactAudio,at,3);return;}
        if(ImpactMarauder(at))return;
        if(ImpactBountyHunter(at))return;
        if(ImpactRepair(at))return;
        if(ImpactAssassin())return;
        Play(Definition.ImpactAudio,at,3);
        if(Definition.Id=="flameburst"){Burst(center,"res://Assets/Powertech/4x4_flame_lick_02_bw.png",.16f);return;}
        if(Definition.Id=="flamethrower")return;
        if(Definition.Id=="rocketpunch"){Burst(center,"res://Assets/Powertech/bw_fx_2x2_sparknglow_01.png",.18f);return;}
        switch(Definition.PresentationKind)
        {
            case "slam": Ring(at,Definition.ImpactTexture,_radius*2.2f);break;
            case "gas": GasCloud(at,Math.Max(.12f,_radius));break;
            case "flame": break; // Flames come from the emitter, never a central billboard.
            case "saber": break; // The real blades are the effect; audio only.
            case "rifle": MuzzleFlash();Bolt(MuzzleWorld(),center,.012f,.14);break;
            case "bracer": MuzzleFlash();Projectile(MuzzleWorld(),center);break;
            default: if(Definition.ImpactTexture!="")Burst(at,Definition.ImpactTexture,Math.Max(.08f,_radius*2));break;
        }
    }
    private Vector3 MuzzleWorld()
    {
        int bone=Actor.Skeleton.FindBone(Definition.MuzzleBone);
        return bone>=0?(Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(bone)).Origin:Actor.GlobalPosition+Vector3.Up*.12f;
    }
    private ShaderMaterial Material(string texture,bool billboard=true)
    {
        string key=texture+(billboard?"|b":"|f");
        if(_materials.TryGetValue(key,out var m))return m;
        _additive??=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/D3Particle.gdshader")};
        m=(ShaderMaterial)_additive.Duplicate();m.SetShaderParameter("source_texture",GD.Load<Texture2D>(texture));m.SetShaderParameter("billboard",billboard);
        AdditiveBound=true;return _materials[key]=m;
    }
    private static ShaderMaterial? _alpha;
    private ShaderMaterial AlphaMaterial(string texture,bool billboard=true)
    {
        string key=texture+(billboard?"|ab":"|af");
        if(_materials.TryGetValue(key,out var m))return m;
        _alpha??=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/D3ParticleAlpha.gdshader")};
        m=(ShaderMaterial)_alpha.Duplicate();m.SetShaderParameter("source_texture",GD.Load<Texture2D>(texture));m.SetShaderParameter("billboard",billboard);
        return _materials[key]=m;
    }
    private Sprite3D AlphaSprite(string texture,float size,int frames=1,bool billboard=true)
    {
        var tex=GD.Load<Texture2D>(texture);
        return new Sprite3D {Texture=tex,Hframes=frames,Vframes=frames,PixelSize=size/(tex.GetWidth()/frames),Shaded=false,MaterialOverride=AlphaMaterial(texture,billboard)};
    }
    private void ShowShield(Combatant target)
    {
        DetachProbe();
        if(_bubble!=null && GodotObject.IsInstanceValid(_bubble))_bubble.QueueFree();
        _shieldHealth=target.Health;_bubbleSeen=false;_bubbleAge=0;
        if(ShowDistinctShield())return;
        var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/D3Shield.gdshader")};
        material.SetShaderParameter("hexagons",GD.Load<Texture2D>("res://Assets/D3/hexagon_blurred_tiled.png"));
        material.SetShaderParameter("scanlines",GD.Load<Texture2D>("res://Assets/D3/scanlines_blurred_wrap.png"));
        material.SetShaderParameter("tint",Definition.EffectTint);
        // Original sphere_hexagon_shield.gr2 from the bh_energy_shield_02 FXSpec, scaled to wrap the character.
        var scene=GD.Load<PackedScene>("res://Assets/D3/sphere_hexagon_shield.glb").Instantiate<Node3D>();
        var source=scene.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().First();
        var mesh=source.Mesh;float radius=mesh.GetAabb().Size.X/2;scene.Free();
        _bubble=new MeshInstance3D {Name="EnergyShieldBubble",Mesh=mesh,MaterialOverride=material,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,
            Scale=Vector3.One*(.115f/Math.Max(.0001f,radius))};
        AddChild(_bubble);_shieldHealth=target.Health;_bubbleSeen=false;_bubbleAge=0;
        _bubble.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.09f;
    }
    private void GasCloud(Vector3 at,float radius)
    {
        // Original expl_poison_gas parameters: ground projection tint (.323,.792,.008) for 8 s plus tinted smoke atlases.
        _gas=5;_gasSpawn=0;_gasAt=at;_gasRadius=radius;
        var ground=AlphaSprite("res://Assets/D3/splash_01.png",radius*2.6f,1,false);ground.RotationDegrees=new Vector3(-90,0,0);
        ground.Modulate=new Color(.323f,.792f,.008f,.9f);
        Add(ground,8,fx=>{float f=(float)(fx.Age/fx.Life);((Sprite3D)fx.Node).Modulate=new Color(.323f,.792f,.008f,.9f*Math.Min(1f,(float)fx.Age*4)*(f>.75f?(1-f)*4:1));});
        ground.GlobalPosition=at+Vector3.Down*.095f;
        for(int i=0;i<16;i++)SmokePuff(1.4f);
    }
    private void SmokePuff(float spread=1f)
    {
        string tex=_random.Randf()<.5f?"res://Assets/D3/2x2_smoke_screen_clouds.png":"res://Assets/D3/2x2_fx_wispy_smoke.png";
        var s=AlphaSprite(tex,_gasRadius*_random.RandfRange(1.0f,1.6f),2);s.Frame=_random.RandiRange(0,3);
        var tint=new Color(.16f,.713f,.007f,.7f);s.Modulate=tint;
        var start=_gasAt+new Vector3(_random.RandfRange(-1,1),_random.RandfRange(-.9f,-.6f),_random.RandfRange(-1,1))*_gasRadius*.7f*spread;
        var drift=new Vector3(_random.RandfRange(-.02f,.02f),_random.RandfRange(.03f,.08f),_random.RandfRange(-.02f,.02f));
        double life=_random.RandfRange(2.4f,4f);
        Add(s,life,fx=>{float f=(float)(fx.Age/fx.Life);fx.Node.GlobalPosition=start+drift*(float)fx.Age;
            ((Sprite3D)fx.Node).Scale=Vector3.One*(.8f+f*.7f);((Sprite3D)fx.Node).Modulate=new Color(tint,tint.A*Math.Min(1f,(float)fx.Age*5)*(1-f));});
        s.GlobalPosition=start;
    }
    private Fx Add(Node3D node,double life,Action<Fx>? update=null)
    {
        // Bounded: the oldest effect yields before the budget is exceeded.
        if(_effects.Count>=MaxEffects){var old=_effects[0];if(GodotObject.IsInstanceValid(old.Node))old.Node.QueueFree();_effects.RemoveAt(0);}
        AddChild(node);var fx=new Fx {Node=node,Life=life,Update=update};_effects.Add(fx);return fx;
    }
    private Sprite3D Sprite(string texture,float size,int frames=1,bool billboard=true)
    {
        var tex=GD.Load<Texture2D>(texture);
        return new Sprite3D {Texture=tex,Hframes=frames,Vframes=frames,PixelSize=size/(tex.GetWidth()/frames),Shaded=false,MaterialOverride=Material(texture,billboard)};
    }
    private void Burst(Vector3 at,string texture,float size)
    {
        int frames=texture.Contains("4x4")?4:texture.Contains("2x2")||texture.Contains("flames")?2:1;
        var s=Sprite(texture,size,frames);s.Modulate=Definition.EffectTint;
        Add(s,.5,fx=>{
            var sp=(Sprite3D)fx.Node;float f=(float)(fx.Age/fx.Life);sp.Modulate=new Color(Definition.EffectTint,1-f);
            if(sp.Hframes>1)sp.Frame=Math.Min(sp.Hframes*sp.Vframes-1,(int)(f*sp.Hframes*sp.Vframes));
        });
        s.GlobalPosition=at;
    }
    private void Ring(Vector3 at,string texture,float size)
    {
        if(texture=="")return;
        var s=Sprite(texture,size,1,false);s.RotationDegrees=new Vector3(-90,0,0);s.Modulate=Definition.EffectTint;
        Add(s,.55,fx=>{float f=(float)(fx.Age/fx.Life);((Sprite3D)fx.Node).Scale=Vector3.One*(.5f+f);((Sprite3D)fx.Node).Modulate=new Color(Definition.EffectTint,1-f);});
        s.GlobalPosition=at;
    }
    private void Pulse(Vector3 at)
    {
        const string tex="res://Assets/D3/glow_basic_alpha.png";
        if(!ResourceLoader.Exists(tex))return;
        var s=Sprite(tex,.2f);s.Modulate=Definition.EffectTint;
        Add(s,.6,fx=>{float f=(float)(fx.Age/fx.Life);((Sprite3D)fx.Node).Scale=Vector3.One*(1+f);((Sprite3D)fx.Node).Modulate=new Color(Definition.EffectTint,1-f);});
        s.GlobalPosition=at;
    }
    private void MuzzleFlash()
    {
        const string tex="res://Assets/D3/bw_orange_flare02.png";
        var s=Sprite(tex,.07f);s.Modulate=Definition.EffectTint;
        Add(s,.1,fx=>((Sprite3D)fx.Node).Modulate=new Color(Definition.EffectTint,1-(float)(fx.Age/fx.Life)));
        s.GlobalPosition=MuzzleWorld();
    }
    private void Bolt(Vector3 from,Vector3 to,float width,double life)
    {
        float length=from.DistanceTo(to);if(length<.001f)return;
        var mesh=new MeshInstance3D {Mesh=new BoxMesh {Size=new Vector3(width,width,length)},CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,DisableFog=true,BlendMode=BaseMaterial3D.BlendModeEnum.Add,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,AlbedoColor=Definition.EffectTint}};
        Add(mesh,life,fx=>((StandardMaterial3D)((MeshInstance3D)fx.Node).MaterialOverride).AlbedoColor=new Color(Definition.EffectTint,1-(float)(fx.Age/fx.Life)));
        mesh.GlobalPosition=(from+to)/2;mesh.LookAt(to,Vector3.Up);
    }
    private void Projectile(Vector3 from,Vector3 to,double life=.16)
    {
        const string tex="res://Assets/D3/glow_basic_alpha.png";
        var s=Sprite(tex,.045f);s.Modulate=Definition.EffectTint;
        Add(s,life,fx=>fx.Node.GlobalPosition=from.Lerp(to,Math.Min(1f,(float)(fx.Age/fx.Life))));
        s.GlobalPosition=from;
        if(Definition.PresentationKind!="gas")_delayed.Add((life,()=>Burst(to,tex,.1f)));
    }
    private void EmitCarbonizeSpray()
    {
        // Original bh_carbonize_cast spray texture, using its real alpha channel.
        // Do not display projector/color textures as opaque particle cards.
        var s=AlphaSprite("res://Assets/Powertech/carbonite_spray_02.png",.065f);
        var origin=MuzzleWorld();var direction=origin-Actor.GlobalPosition;direction.Y=0;
        if(direction.LengthSquared()<.0001f)direction=-Actor.GlobalBasis.Z;
        direction=direction.Normalized();var velocity=direction*.65f+Vector3.Up*.025f;
        Add(s,.4,fx=>{float f=(float)(fx.Age/fx.Life);fx.Node.GlobalPosition=origin+velocity*(float)fx.Age;s.Scale=new Vector3(1+f,1.4f+f,1);s.Modulate=new Color(.74f,.8f,.84f,.65f*Math.Min(1,f*6)*(1-f));});
        s.GlobalPosition=origin;
    }
    private void EmitConeFlame()
    {
        var s=Sprite("res://Assets/Powertech/4x4_flame_lick_02_bw.png",.09f,4);
        var origin=MuzzleWorld();var direction=(_forward+new Vector3(_random.RandfRange(-.2f,.2f),.08f,_random.RandfRange(-.2f,.2f))).Normalized();
        Add(s,.45,fx=>{float f=(float)(fx.Age/fx.Life);fx.Node.GlobalPosition=origin+direction*f*.45f;s.Frame=Math.Min(15,(int)(f*16));s.Modulate=new Color(1,1,1,1-f);});s.GlobalPosition=origin;
    }
    private void EmitFlame()
    {
        const string tex="res://Assets/D3/fx_2x2_flames_emmissive_bw_01_d.png";
        var s=Sprite(tex,.06f,2);s.Frame=_random.RandiRange(0,3);
        var velocity=new Vector3(_random.RandfRange(-.12f,.12f),_random.RandfRange(.03f,.1f),_random.RandfRange(-.12f,.12f));
        var origin=MuzzleWorld();
        Add(s,.45,fx=>{float f=(float)(fx.Age/fx.Life);fx.Node.GlobalPosition=origin+velocity*(float)fx.Age;
            ((Sprite3D)fx.Node).Scale=Vector3.One*(.7f+f*.8f);((Sprite3D)fx.Node).Modulate=new Color(1,.45f+.25f*(1-f),.1f,1-f);});
        s.GlobalPosition=origin;
    }
    private void Play(string path,Vector3 point,int slot)
    {
        if(path=="" || !ResourceLoader.Exists(path) || _voices.Count==0)return;
        var voice=_voices[Math.Clamp(slot,0,_voices.Count-1)];
        voice.VolumeDb=Definition.Id=="laceration"?-4:AgentKnifeAbility || Definition.Id=="explosiveprobe"?-10:Definition.Id=="carbonize"?-9:-18;
        LastPlayedAudioPath=path;LastPlayedAudioGainDb=voice.VolumeDb;
        voice.Stop();voice.Stream=GD.Load<AudioStream>(path);voice.GlobalPosition=point;voice.Play();
    }
    public override void _Process(double delta)
    {
        _agentOrbitalLock=Math.Max(0,_agentOrbitalLock-delta);
        AdvanceMarauderEffects(delta);
        AdvanceRepair(delta);
        AdvanceBountyHunter();
        for(int i=_delayed.Count-1;i>=0;i--){var d=_delayed[i];d.at-=delta;if(d.at<=0){_delayed.RemoveAt(i);d.run();}else _delayed[i]=d;}
        if(_gas>0)
        {
            _gas-=delta;_gasSpawn-=delta;
            while(_gasSpawn<=0){_gasSpawn+=.22;SmokePuff();}
        }
        if(_bubble!=null)
        {
            if(!GodotObject.IsInstanceValid(_bubble)){_bubble=null;}
            else
            {
                _bubbleAge+=delta;_bubble.GlobalPosition=ShieldPosition();
                if(_shieldHealth!=null && GodotObject.IsInstanceValid(_shieldHealth) && _shieldHealth.Shielded)_bubbleSeen=true;
                if((_bubbleSeen && !(_shieldHealth?.Shielded??false)) || (!_bubbleSeen && _bubbleAge>1)){_bubble.QueueFree();_bubble=null;}
            }
        }
        if(_carbon>0){_carbon-=delta;_carbonSpawn-=delta;while(_carbonSpawn<=0){_carbonSpawn+=.035;EmitCarbonizeSpray();}}
        if(_flame>0)
        {
            _flame-=delta;_flameSpawn-=delta;
            while(_flameSpawn<=0){_flameSpawn+=.045;if(Definition.Id=="flamethrower")EmitConeFlame();else EmitFlame();}
        }
        for(int i=_effects.Count-1;i>=0;i--)
        {
            var fx=_effects[i];fx.Age+=delta;
            if(fx.Age>=fx.Life || !GodotObject.IsInstanceValid(fx.Node)){if(GodotObject.IsInstanceValid(fx.Node))fx.Node.QueueFree();_effects.RemoveAt(i);continue;}
            fx.Update?.Invoke(fx);
        }
    }
    public void End(bool interrupted){ _flame=0;EndRepair();if(interrupted)Release();EndBountyHunter(interrupted); }
    public void Complete(){ _flame=0;_carbon=0;if(GodotObject.IsInstanceValid(_bountyArea))_bountyArea!.Visible=false; }
    public void Release()
    {
        if(GodotObject.IsInstanceValid(ActiveKnife))ActiveKnife!.Stop();ActiveKnife=null;
        ReleaseWarrior();_marauderAura="";_flame=0;_carbon=0;_gas=0;_delayed.Clear();
        ReleaseRepair();if(GodotObject.IsInstanceValid(_bountyArea))_bountyArea!.Visible=false;
        if(_bubble!=null && GodotObject.IsInstanceValid(_bubble))_bubble.QueueFree();_bubble=null;
        foreach(var e in _effects)if(GodotObject.IsInstanceValid(e.Node))e.Node.QueueFree();
        _effects.Clear();foreach(var v in _voices)if(GodotObject.IsInstanceValid(v))v.Stop();
    }
    public override void _ExitTree()=>Release();
}
