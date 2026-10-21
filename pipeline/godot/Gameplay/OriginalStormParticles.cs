using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SwtorSurvivors;

/// <summary>Particle/light portion of the recovered Storm FXSpec adapter.</summary>
public partial class OriginalStormLightning
{
    private readonly Dictionary<string,Dictionary<string,string>> _particleSpecs=new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string,ShaderMaterial> _particleMaterials=new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SourceEmitter> _emitters=new();
    private readonly List<SourceParticle> _particles=new();
    private readonly Dictionary<string,int> _emittedLayers=new();
    private readonly Dictionary<MeshInstance3D,uint> _casterLightLayers=new();
    private OmniLight3D? _castLight;
    private JsonElement _lightEntry;
    public IReadOnlyDictionary<string,int> EmittedParticleLayers => _emittedLayers;
    public int SourceParticleCount => _particles.Count;
    public int SourceLightCount => GodotObject.IsInstanceValid(_castLight)?1:0;
    public int CasterParticleEmitterCount => _emitters.Count(e=>e.Caster);
    private sealed class SourceEmitter
    {
        public string Name="";
        public JsonElement Spec,Entry;
        public Dictionary<string,string> Emitter=null!,Particle=null!;
        public double Age,Next;
        public Vector3 Origin;
        public bool Caster;
        public Strike? Strike;
    }
    private sealed class SourceParticle
    {
        public Sprite3D Node=null!;
        public SourceEmitter Emitter=null!;
        public Dictionary<string,string> Data=null!;
        public Vector3 Position,Velocity,Offset;
        public float Life,Age,Width,Height,Rotation,Spin;
        public bool Linked,Flat;
    }
    private static string Field(Dictionary<string,string> fields,string key,string fallback="") =>
        fields.TryGetValue(key,out string? value)&&value!=""?value:fallback;
    private static string Stem(string path) => System.IO.Path.GetFileName(path.Replace('\\','/')).ToLowerInvariant();
    private float Sample(string value,float fallback=0)
    {
        if(value.StartsWith('<'))
        {var parts=value.Trim('<','>').Split(',');return parts.Length>=2?_random.RandfRange(Parse(parts[0]),Parse(parts[1])):fallback;}
        return Parse(value,fallback);
    }
    private Vector3 SampleVector(string value)
    {
        var parts=Regex.Matches(value,@"<[^>]+>|-?[\d.]+").Select(m=>Sample(m.Value)).ToArray();
        return parts.Length==3?new Vector3(parts[0],parts[1],parts[2]):Vector3.Zero;
    }
    private float GroupDelay(JsonElement spec,JsonElement entry)
    {
        string owner=Text(entry,"_fxOwnerName");float result=0;
        if(spec.TryGetProperty("_fxList",out var groups)&&groups.ValueKind==JsonValueKind.Array)
            foreach(var group in groups.EnumerateArray())if(Text(group,"_fxName")==owner){result=Delay(group);break;}
        return result;
    }
    private void LoadParticleSpecs()
    {
        using var config=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Abilities/source-parameters.json"));
        foreach(var property in config.RootElement.EnumerateObject())
        {
            if(!property.Name.EndsWith(".prt",StringComparison.OrdinalIgnoreCase))continue;
            var fields=new Dictionary<string,string>();
            foreach(var pair in property.Value.EnumerateArray())fields[pair[0].GetString()!]=pair[1].GetString()??"";
            _particleSpecs[Stem(property.Name)]=fields;
        }
    }
    private void BeginParticles()
    {
        ClearParticles();_emittedLayers.Clear();
        AddEmitters(_cast,Actor.GlobalPosition,true,null);
        _lightEntry=_cast.GetProperty("_fxLightList")[0];
        _castLight=new OmniLight3D {Name="OriginalStormFrontLight",ShadowEnabled=false,
            OmniRange=Math.Max(0.01f,Number(_lightEntry,"_fxRange",0.2f)*FxScale),
            LightEnergy=Number(_lightEntry,"_fxIntensity",1),LightSpecular=0,LightCullMask=1u<<19};
        AddChild(_castLight);
        // The source enables the caster only. Keep other characters/environment out of this light.
        foreach(var mesh in Actor.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
        {_casterLightLayers[mesh]=mesh.Layers;mesh.Layers|=1u<<19;}
    }
    private void AddEmitters(JsonElement spec,Vector3 origin,bool caster,Strike? strike)
    {
        foreach(var entry in spec.GetProperty("_fxEmitterList").EnumerateArray())
        {
            string key=Stem(Text(entry,"_fxResourceName"));
            if(!_particleSpecs.TryGetValue(key,out var emitter))continue;
            if(!_particleSpecs.TryGetValue(Stem(Field(emitter,"EmitSpec")),out var particle))continue;
            if(Field(particle,"ParticleType")!="BILLBOARD_POINT" || Field(particle,"DoChildEmitter")=="true")continue;
            _emitters.Add(new SourceEmitter {Name=Text(entry,"_fxName"),Spec=spec,Entry=entry,
                Emitter=emitter,Particle=particle,Origin=origin,Caster=caster,Strike=strike,
                Next=GroupDelay(spec,entry)+Delay(entry)});
        }
    }
    private Vector3 EmitterOrigin(SourceEmitter emitter)
    {
        if(emitter.Caster)return Resolve(emitter.Spec,emitter.Name,Actor.GlobalPosition,_casterAngles,true);
        return Resolve(emitter.Spec,emitter.Name,emitter.Origin,emitter.Strike!.Angles,false);
    }
    private void AdvanceParticles(double delta,bool casting)
    {
        for(int i=_emitters.Count-1;i>=0;i--)
        {
            var emitter=_emitters[i];emitter.Age+=delta;
            bool live=emitter.Caster?casting:emitter.Strike!.Age<1;
            if(!live) {if(!emitter.Caster)_emitters.RemoveAt(i);else emitter.Next=Math.Max(emitter.Next,emitter.Age);continue;}
            // Catch up only the elapsed source intervals, never a frame-rate-dependent spawn count.
            while(emitter.Age>=emitter.Next)
            {
                int count=(int)Sample(Field(emitter.Emitter,"NumberToEmit","1"),1);
                for(int n=0;n<count;n++)SpawnParticle(emitter);
                emitter.Next+=Math.Max(0.025f,Sample(Field(emitter.Emitter,"EmitFrequency","1"),1));
            }
        }
        var camera=GetViewport().GetCamera3D();
        for(int i=_particles.Count-1;i>=0;i--)
        {
            var p=_particles[i];p.Age+=(float)delta;
            if(p.Age>=p.Life) {p.Node.Free();_particles.RemoveAt(i);continue;}
            float t=p.Age/p.Life;
            p.Position+=p.Velocity*(float)delta;
            p.Velocity+=Vector3.Up*Sample(Field(p.Data,"Buoyancy","0"))*0.001f*(float)delta;
            p.Node.GlobalPosition=p.Position+(p.Linked?EmitterOrigin(p.Emitter):Vector3.Zero);
            p.Node.GlobalBasis=p.Flat?new Basis(Vector3.Right,-Mathf.Pi/2):camera?.GlobalBasis??Basis.Identity;
            p.Node.RotateObjectLocal(Vector3.Forward,p.Rotation+p.Spin*p.Age);
            float width=Field(p.Data,"XScale2D").StartsWith('[')?Curve(Field(p.Data,"XScale2D"),t)*FxScale:p.Width;
            float height=Field(p.Data,"YScale2D").StartsWith('[')?Curve(Field(p.Data,"YScale2D"),t)*FxScale:p.Height;
            p.Node.Scale=new Vector3(Math.Max(0.00001f,width),Math.Max(0.00001f,height),1);
            p.Node.Modulate=ColorCurve(Field(p.Data,"DiffuseColor","#1,1,1,1"),t);
        }
        if(_castLight!=null)
        {
            _castLight.Visible=casting && _age>=GroupDelay(_cast,_lightEntry)+Delay(_lightEntry);
            _castLight.GlobalPosition=Bone(Text(_lightEntry,"_fxAttachBone"))+Actor.GlobalBasis*Vector(Text(_lightEntry,"_fxAttachPosition"))*FxScale;
            string color=Text(_lightEntry,"_fxColor");float period=Parse(color.Split(';')[^1],0.2f);
            _castLight.LightColor=ColorCurve(color,(float)(_age%period/period));
        }
    }
    private ShaderMaterial ParticleMaterial(string key,Dictionary<string,string> data)
    {
        if(_particleMaterials.TryGetValue(key,out var cached))return cached;
        bool additive=Field(data,"DestBlend")=="D3DBLEND_ONE";
        var material=new ShaderMaterial {Shader=GD.Load<Shader>(additive?"res://Gameplay/StormParticleAdd.gdshader":"res://Gameplay/StormParticleAlpha.gdshader")};
        string texture=System.IO.Path.GetFileNameWithoutExtension(Stem(Field(data,"TextureName")));
        material.SetShaderParameter("source_texture",Texture(texture));
        bool hue=Field(data,"UseParticleHueing").Equals("true",StringComparison.OrdinalIgnoreCase);
        material.SetShaderParameter("use_hue",hue);material.SetShaderParameter("source_one",Field(data,"SourceBlend")=="D3DBLEND_ONE");
        var hueKeys=ColorKeys.Matches(Field(data,"ParticleHueColors")).ToArray();
        var gradient=hueKeys.Length>0?new Gradient {Offsets=hueKeys.Select(m=>Parse(m.Groups[1].Value)).ToArray(),Colors=hueKeys.Select(m=>Rgba(m.Groups[2].Value)).ToArray()}:new Gradient();
        material.SetShaderParameter("source_hue",new GradientTexture1D {Gradient=gradient,Width=256});
        _particleMaterials[key]=material;return material;
    }
    private void SpawnParticle(SourceEmitter emitter)
    {
        var data=emitter.Particle;string key=Stem(Field(data,"Name",Field(data,"TextureName")));
        string texture=System.IO.Path.GetFileNameWithoutExtension(Stem(Field(data,"TextureName")));
        var tex=Texture(texture);
        // TextureNumber is a frame-selection bitmask (15 -> all four, 10 -> frames one/three).
        int mask=(int)Sample(Field(data,"TextureNumber","0"));bool atlas=mask>0;
        var frames=Enumerable.Range(0,4).Where(f=>(mask&(1<<f))!=0).ToArray();
        var sprite=new Sprite3D {Texture=tex,PixelSize=1f/(tex.GetWidth()/(atlas?2:1)),
            Hframes=atlas?2:1,Vframes=atlas?2:1,Frame=atlas?frames[_random.RandiRange(0,frames.Length-1)]:0,
            Billboard=BaseMaterial3D.BillboardModeEnum.Disabled,Shaded=false,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,MaterialOverride=ParticleMaterial(key,data)};
        AddChild(sprite);
        var radius=SampleVector(Field(emitter.Emitter,"EmitterRadius","(0,0,0)"))*FxScale;
        var offset=new Vector3(_random.RandfRange(-radius.X,radius.X),_random.RandfRange(-radius.Y,radius.Y),_random.RandfRange(-radius.Z,radius.Z));
        bool linked=Field(emitter.Emitter,"LinkChildren")=="true";
        var direction=new Vector3(_random.RandfRange(-1,1),_random.RandfRange(-1,1),_random.RandfRange(-1,1)).Normalized();
        var velocity=SampleVector(Field(data,"Trajectory"))*FxScale+direction*Sample(Field(emitter.Emitter,"InitialParticleVelocity"))*FxScale;
        _particles.Add(new SourceParticle {Node=sprite,Emitter=emitter,Data=data,Offset=offset,Linked=linked,
            Position=offset+(linked?Vector3.Zero:EmitterOrigin(emitter)),Velocity=velocity,
            Life=Math.Max(0.025f,Sample(Field(data,"LifeSpan","1"),1)),Flat=Field(data,"LieFlat")=="true",
            Width=Sample(Field(data,"XScale2D","1"),1)*FxScale,Height=Sample(Field(data,"YScale2D","1"),1)*FxScale,
            Rotation=Mathf.DegToRad(Sample(Field(data,"InitialRotation2D",Field(emitter.Emitter,"InitialRotation2D")))),
            Spin=Mathf.DegToRad(Sample(Field(data,"DeltaRotation2D")))});
        _emittedLayers[emitter.Name]=_emittedLayers.GetValueOrDefault(emitter.Name)+1;
    }
    private void ClearParticles()
    {
        foreach(var p in _particles)p.Node.Free();_particles.Clear();_emitters.Clear();
        if(GodotObject.IsInstanceValid(_castLight))_castLight!.Free();_castLight=null;
        foreach(var pair in _casterLightLayers)if(GodotObject.IsInstanceValid(pair.Key))pair.Key.Layers=pair.Value;
        _casterLightLayers.Clear();
    }
}
