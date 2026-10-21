using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SwtorSurvivors;

/// <summary>Renders the recovered Storm lightning layers, dummy attachments and source curves.
/// The unavailable native zap primitive is adapted to Godot ribbons; no combat or animation writes.</summary>
public partial class OriginalStormLightning : Node3D
{
    public CharacterPresentation Actor {get;set;}=null!;
    public JsonElement Source {get;set;}
    private const float FxScale=0.1f;
    private JsonElement _cast,_strike,_ground;
    private ShaderMaterial _material=null!;
    private readonly RandomNumberGenerator _random=new();
    private readonly List<Strike> _strikes=new();
    private readonly List<MeshInstance3D> _meshes=new();
    private readonly List<Sprite3D> _sprites=new();
    private readonly Dictionary<string,(float t,float value)[]> _curves=new();
    private readonly Dictionary<string,(float t,Color value)[]> _colors=new();
    private readonly Dictionary<string,Texture2D> _textures=new();
    private readonly Dictionary<string,float> _casterAngles=new();
    private double _age,_nextStrike;
    public int GroundBeamCount {get;private set;}
    public int CasterBeamCount {get;private set;}
    public int StrikesStarted {get;private set;}
    public int LiveVisuals => _meshes.Count+_sprites.Count+SourceParticleCount+SourceLightCount;
    private sealed class Strike
    {
        public Vector3 Center;
        public double Age;
        public readonly Dictionary<string,float> Angles=new();
    }
    private static readonly Regex ScalarKeys=new(@"\[([\d.]+):(-?[\d.]+):[A-Z]\]",RegexOptions.Compiled);
    private static readonly Regex ColorKeys=new(@"\[([\d.]+):#([\d.,]+):[A-Z]\]",RegexOptions.Compiled);
    private static string Text(JsonElement e,string key,string fallback="") =>
        e.TryGetProperty(key,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??fallback:fallback;
    private static float Parse(string v,float fallback=0) => float.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out float n)?n:fallback;
    private static float Number(JsonElement e,string key,float fallback=0) => Parse(Text(e,key),fallback);
    private static Vector3 Vector(string text)
    {var c=text.Trim('(',')').Split(',');return c.Length==3?new Vector3(Parse(c[0]),Parse(c[1]),Parse(c[2])):Vector3.Zero;}
    private static Color Rgba(string text)
    {var c=text.TrimStart('#').Split(',');return c.Length==4?new Color(Parse(c[0]),Parse(c[1]),Parse(c[2]),Parse(c[3])):Colors.White;}
    private static float Delay(JsonElement e)
    {var p=Text(e,"_fxStartDelay","0").Split(':');return Parse(p[^1]);}
    private float Curve(string text,float t)
    {
        if(!_curves.TryGetValue(text,out var keys))
            _curves[text]=keys=ScalarKeys.Matches(text).Select(m=>(Parse(m.Groups[1].Value),Parse(m.Groups[2].Value))).ToArray();
        if(keys.Length==0)return Parse(text);
        var previous=keys[0];
        foreach(var key in keys)
        {if(t<=key.t)return key.t>previous.t?Mathf.Lerp(previous.value,key.value,Mathf.Clamp((t-previous.t)/(key.t-previous.t),0,1)):key.value;previous=key;}
        return keys[^1].value;
    }
    private Color ColorCurve(string text,float t)
    {
        if(!_colors.TryGetValue(text,out var keys))
            _colors[text]=keys=ColorKeys.Matches(text).Select(m=>(Parse(m.Groups[1].Value),Rgba(m.Groups[2].Value))).ToArray();
        if(keys.Length==0)return Rgba(text);
        var previous=keys[0];
        foreach(var key in keys)
        {if(t<=key.t)return key.t>previous.t?previous.value.Lerp(key.value,Mathf.Clamp((t-previous.t)/(key.t-previous.t),0,1)):key.value;previous=key;}
        return keys[^1].value;
    }
    public override void _Ready()
    {
        _random.Randomize();_cast=Source.GetProperty("si_forcestorm_cast");_strike=Source.GetProperty("si_forcestorm_lightningstrike");
        _ground=Source.GetProperty("si_forcestorm_caster_groundbolts_01");
        var beam=_strike.GetProperty("_fxLightningList")[0];
        var hueText=Text(beam,"_fxTextureHueRamp");var keys=ColorKeys.Matches(hueText).ToArray();
        var gradient=new Gradient {Offsets=keys.Select(m=>Parse(m.Groups[1].Value)).ToArray(),Colors=keys.Select(m=>Rgba(m.Groups[2].Value)).ToArray()};
        _material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/StormLightning.gdshader")};
        _material.SetShaderParameter("source_texture",Texture(System.IO.Path.GetFileNameWithoutExtension(Text(beam,"_fxTextureName"))));
        _material.SetShaderParameter("source_hue",new GradientTexture1D {Gradient=gradient,Width=256});
        LoadParticleSpecs();
        SetProcess(false);
    }
    private Texture2D Texture(string name)
    {
        name=name.ToLowerInvariant();if(!_textures.TryGetValue(name,out var texture))
            _textures[name]=texture=GD.Load<Texture2D>($"res://Assets/Abilities/{name}.png");return texture;
    }
    public void Begin() {Clear();_age=0;_nextStrike=0.55;StrikesStarted=0;_casterAngles.Clear();BeginParticles();}
    private Vector3 Bone(string name)
    {
        name=name.Trim();int index=Actor.Skeleton.FindBone(name);
        if(index<0)for(int i=0;i<Actor.Skeleton.GetBoneCount();i++)
            if(string.Equals(Actor.Skeleton.GetBoneName(i),name,StringComparison.OrdinalIgnoreCase)){index=i;break;}
        return index<0?Actor.GlobalPosition:(Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(index)).Origin;
    }
    private Vector3 Resolve(JsonElement spec,string name,Vector3 origin,Dictionary<string,float> angles,bool caster,int depth=0)
    {
        if(depth>5)return origin;
        if(name is "CASTER" or "TARGET" or "")return origin;
        foreach(var e in spec.GetProperty("_fxEmitterList").EnumerateArray())
        {
            if(Text(e,"_fxName")!=name)continue;
            string parent=Text(e,"_fxAttachTo"),bone=Text(e,"_fxAttachBone");
            var point=caster&&bone!=""?Bone(bone):Resolve(spec,parent,origin,angles,caster,depth+1);
            var offset=Vector(Text(e,"_fxAttachPosition","(0,0,0)"))*FxScale;
            if(parent.StartsWith("DummyRandRot",StringComparison.Ordinal))
            {
                if(!angles.TryGetValue(parent,out float angle))angles[parent]=angle=_random.RandfRange(0,Mathf.Tau);
                offset=offset.Rotated(Vector3.Up,angle);
            }
            else if(caster)offset=Actor.GlobalBasis*offset;
            return point+offset;
        }
        return origin;
    }
    private Vector3 GroundAt(Vector3 point)
    {
        var hit=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(point+Vector3.Up*0.3f,point-Vector3.Up*0.5f,1));
        return hit.Count>0?(Vector3)hit["position"]+Vector3.Up*0.002f:point;
    }
    public void Advance(double delta,Vector3 center,float radius,bool casting)
    {
        _age+=delta;ClearFrame();
        foreach(var strike in _strikes)strike.Age+=delta;
        _strikes.RemoveAll(s=>s.Age>=1); // Original EmitFXSpec particle lifespan: one second.
        if(_age>=_nextStrike)
        {
            _nextStrike=_age+0.25; // si_forcestorm_strikes_fxpec.prt
            float a=_random.RandfRange(0,Mathf.Tau),r=Mathf.Sqrt(_random.Randf())*radius;
            var strike=new Strike {Center=GroundAt(center+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r))};
            _strikes.Add(strike);AddEmitters(_strike,strike.Center,false,strike);StrikesStarted++;
        }
        foreach(var strike in _strikes)
        {
            foreach(var entry in _strike.GetProperty("_fxLightningList").EnumerateArray())
                if(Draw(_strike,entry,strike.Center,strike.Age,strike.Angles,false))GroundBeamCount++;
        }
        AdvanceParticles(delta,casting);
        if(!casting)return;
        foreach(var entry in _cast.GetProperty("_fxLightningList").EnumerateArray())
            if(Draw(_cast,entry,Actor.GlobalPosition,_age,_casterAngles,true))CasterBeamCount++;
        foreach(var side in new[]{-1f,1f})
        {
            var origin=GroundAt(Actor.GlobalPosition+Actor.GlobalBasis.X*side*0.01f);
            foreach(var entry in _ground.GetProperty("_fxLightningList").EnumerateArray())
                if(Draw(_ground,entry,origin,_age%1,_casterAngles,false))CasterBeamCount++;
        }
    }
    private bool Draw(JsonElement spec,JsonElement entry,Vector3 origin,double age,Dictionary<string,float> angles,bool caster)
    {
        float delay=GroupDelay(spec,entry)+Delay(entry);if(age<delay)return false;
        age-=delay;
        var start=Resolve(spec,Text(entry,"_fxAttachTo"),origin,angles,caster)+Vector(Text(entry,"_fxAttachPosition","(0,0,0)"))*FxScale;
        var end=Resolve(spec,Text(entry,"_fxTargetName"),origin,angles,caster);
        var delta=end-start;float length=delta.Length();if(length<0.00001f)return false;
        var direction=delta/length;var camera=GetViewport().GetCamera3D();
        var across=direction.Cross(camera==null?Vector3.Back:(camera.GlobalPosition-start).Normalized()).Normalized();
        if(across.IsZeroApprox())across=Vector3.Right;var up=direction.Cross(across).Normalized();
        int count=Math.Clamp((int)Math.Ceiling(length/Math.Max(0.001f,Number(entry,"_fxTessellationDist",0.2f)*FxScale)),8,96);
        var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip,_material);
        float width=Number(entry,"_fxLightningMaxWidth",0.05f)*FxScale;
        float envelope=Number(entry,"_fxEnvelopeMaxRadius",0.1f)*FxScale;
        float tile=Math.Max(0.001f,Number(entry,"_fxTextureTileDist",1)*FxScale);
        string ramp=Text(entry,"_fxLightningLengthColorRampStart");
        float period=Parse(ramp.Split(';')[^1],1);
        float rampStart=Curve(ramp,Mathf.Clamp((float)age/Math.Max(0.01f,period),0,1));
        float flicker=1-_random.Randf()*Number(entry,"_fxLightningRandomWidthPercent",0);
        // 4x1 is four alternate bolt frames across the sheet, not four simultaneous thin bolts.
        int frame=(int)(age*20)%4;
        for(int i=0;i<=count;i++)
        {
            float t=i/(float)count;
            float shape=Curve(Text(entry,"_fxEnvelopeShape"),t);
            var point=start.Lerp(end,t)+(across*_random.RandfRange(-1,1)+up*_random.RandfRange(-1,1))*envelope*shape*Mathf.Sin(t*Mathf.Pi);
            float w=width*Curve(Text(entry,"_fxLightningShape","1"),t)*flicker;
            var color=ColorCurve(Text(entry,"_fxLightningLengthColor","#1,1,1,1"),t-rampStart);
            float uv=t*length/tile+(float)age*Number(entry,"_fxUVScrollRate",1);
            mesh.SurfaceSetColor(color);mesh.SurfaceSetUV(new Vector2(frame/4f,uv));mesh.SurfaceAddVertex(ToLocal(point-across*w/2));
            mesh.SurfaceSetColor(color);mesh.SurfaceSetUV(new Vector2((frame+1)/4f,uv));mesh.SurfaceAddVertex(ToLocal(point+across*w/2));
        }
        mesh.SurfaceEnd();var node=new MeshInstance3D {Mesh=mesh,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};AddChild(node);_meshes.Add(node);return true;
    }
    private void Glow(string texture,Vector3 point,float size,Color color,bool flat=false,bool atlas=false)
    {
        var tex=Texture(texture);var sprite=new Sprite3D {Texture=tex,PixelSize=size/(tex.GetWidth()/(atlas?2:1)),
            Hframes=atlas?2:1,Vframes=atlas?2:1,Frame=atlas?_random.RandiRange(0,3):0,Modulate=color,
            Shaded=false,Billboard=flat?BaseMaterial3D.BillboardModeEnum.Disabled:BaseMaterial3D.BillboardModeEnum.Enabled,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        sprite.MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoTexture=tex,VertexColorUseAsAlbedo=true,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,BlendMode=BaseMaterial3D.BlendModeEnum.Add,
            BillboardMode=sprite.Billboard,BillboardKeepScale=true,CullMode=BaseMaterial3D.CullModeEnum.Disabled,DepthDrawMode=BaseMaterial3D.DepthDrawModeEnum.Disabled};
        if(flat)sprite.RotationDegrees=new Vector3(-90,0,0);AddChild(sprite);sprite.GlobalPosition=point;_sprites.Add(sprite);
    }
    private void ClearFrame()
    {foreach(var m in _meshes)m.Free();_meshes.Clear();foreach(var s in _sprites)s.Free();_sprites.Clear();GroundBeamCount=CasterBeamCount=0;}
    public void Clear() {ClearFrame();_strikes.Clear();ClearParticles();}
    public override void _ExitTree()=>Clear();
}
