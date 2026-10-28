using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SwtorSurvivors;

/// <summary>Bounded adapter for explicit April surface snow emitters. No generated textures or guessed rates.</summary>
public partial class EternityVaultWeather : Node3D
{
    private sealed class Emitter {
        public Vector3 Origin; public Basis Basis=Basis.Identity; public Dictionary<string,string> E = null!, P = null!;
        public Texture2D Texture = null!; public double Next;
    }
    private sealed class Particle {
        public Sprite3D Node = null!; public Emitter E = null!;
        public Vector3 Origin, Velocity; public float Age,Life,Size,Spin,Rotation,Buoyancy;
    }
    private readonly List<Emitter> _emitters = new();
    private readonly List<Particle> _particles = new();
    private readonly RandomNumberGenerator _random = new();
    private double _age;
    private static float N(string s,float fallback=0) => float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out float f)?f:fallback;
    private static string F(Dictionary<string,string> d,string key,string fallback="") => d.TryGetValue(key,out var v)&&v!=""?v:fallback;
    private float Sample(string s,float fallback=0) {
        if(!s.StartsWith('<'))return N(s,fallback);
        var parts=s.Trim('<','>').Split(',');return _random.RandfRange(N(parts[0]),N(parts[1]));
    }
    private Vector3 V(string s) {
        var parts=Regex.Matches(s,@"<[^>]+>|-?[\d.]+(?:[eE][+-]?\d+)?").Select(m=>Sample(m.Value)).ToArray();
        return parts.Length==3?new Vector3(parts[0],parts[1],parts[2]):Vector3.Zero;
    }
    private static float Curve(string s,float t) {
        if(!s.StartsWith('['))return N(s,1);
        var keys=Regex.Matches(s,@"\[([\d.]+):(-?[\d.]+):([A-Z])\]");
        if(keys.Count==0)return 1;
        for(int i=1;i<keys.Count;i++)if(t<=N(keys[i].Groups[1].Value)) {
            float a=N(keys[i-1].Groups[1].Value),b=N(keys[i].Groups[1].Value);
            // Preserve recovered endpoint values; native S/I/O interpolation remains unverified.
            return Mathf.Lerp(N(keys[i-1].Groups[2].Value),N(keys[i].Groups[2].Value),(t-a)/Math.Max(.00001f,b-a));
        }
        return N(keys[^1].Groups[2].Value);
    }
    private static Color ColorCurve(string s,float t) {
        var keys=Regex.Matches(s,@"\[([\d.]+):#([\d.]+),([\d.]+),([\d.]+),([\d.]+):L\]");
        Color C(Match m)=>new(N(m.Groups[2].Value),N(m.Groups[3].Value),N(m.Groups[4].Value),N(m.Groups[5].Value));
        if(keys.Count==0)return Colors.Transparent;
        for(int i=1;i<keys.Count;i++)if(t<=N(keys[i].Groups[1].Value))return C(keys[i-1]).Lerp(C(keys[i]),(t-N(keys[i-1].Groups[1].Value))/Math.Max(.00001f,N(keys[i].Groups[1].Value)-N(keys[i-1].Groups[1].Value)));
        return C(keys[^1]);
    }
    public override void _Ready() {
        _random.Seed=201204;
        using var doc=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Maps/eternity_vault/authored-weather.json"));
        var root=doc.RootElement;
        foreach(var placement in root.GetProperty("placements").EnumerateArray()) {
            var p=placement.GetProperty("position").EnumerateArray().Select(v=>v.GetSingle()).ToArray();
            var rotation=placement.GetProperty("rotation").EnumerateArray().Select(v=>v.GetSingle()).ToArray();
            var basis=new Basis(Vector3.Up,Mathf.DegToRad(rotation[1]));
            foreach(var e in root.GetProperty("emitters").EnumerateArray()) {
                var leaf=root.GetProperty("particles").GetProperty(e.GetProperty("particle").GetString()!);
                var fields=JsonSerializer.Deserialize<Dictionary<string,string>>(e.GetProperty("fields").GetRawText())!;
                _emitters.Add(new Emitter {Basis=basis,Origin=new Vector3(p[0],p[1],p[2])+basis*V(e.GetProperty("entry").GetProperty("_fxAttachPosition").GetString()!), E=fields,
                    P=JsonSerializer.Deserialize<Dictionary<string,string>>(leaf.GetProperty("fields").GetRawText())!,Texture=GD.Load<Texture2D>(leaf.GetProperty("texture").GetString()!)});
            }
        }
        GD.Print($"AUTHORED_WEATHER {_emitters.Count} source emitters; explicit intervals only");
    }
    private void Spawn(Emitter e) {
        var data=e.P;var node=new Sprite3D {Texture=e.Texture,PixelSize=1f/e.Texture.GetWidth(),Shaded=false,
            DoubleSided=true,NoDepthTest=false,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        AddChild(node);
        var p=new Particle {Node=node,E=e,Origin=e.Origin,Velocity=e.Basis*V(F(data,"Trajectory")),Life=Sample(F(data,"LifeSpan")),
            Size=Sample(F(data,"Size2D")),Spin=Sample(F(data,"DeltaRotation2D")),Rotation=Mathf.DegToRad(Sample(F(e.E,"InitialRotation2D"))),Buoyancy=Sample(F(data,"Buoyancy"))};
        _particles.Add(p);
    }
    public override void _Process(double delta) {
        var camera=GetViewport().GetCamera3D();if(camera==null)return;
        _age+=delta;
        foreach(var e in _emitters) {
            float distance=camera.GlobalPosition.DistanceTo(e.Origin);
            if(distance>N(F(e.P,"MaxDistanceFromCamera"),12)+Curve(F(e.P,"XScale2D"),1)) {e.Next=_age;continue;}
            while(e.Next<=_age) {
                int count=(int)Sample(F(e.E,"NumberToEmit","1"));for(int i=0;i<count;i++)Spawn(e);
                e.Next+=N(e.E["EmitFrequency"]);
            }
        }
        for(int i=_particles.Count-1;i>=0;i--) {
            var p=_particles[i];p.Age+=(float)delta;
            if(p.Age>=p.Life) {p.Node.Free();_particles.RemoveAt(i);continue;}
            // Evaluate authored trajectory; buoyancy's engine acceleration unit is unresolved, so it is not guessed.
            p.Node.GlobalPosition=p.Origin+p.Velocity*p.Age;
            float t=p.Age/p.Life;
            p.Node.GlobalBasis=camera.GlobalBasis;p.Node.RotateObjectLocal(Vector3.Forward,p.Rotation+p.Spin*p.Age);
            p.Node.Scale=new Vector3(p.Size*Curve(F(p.E.P,"XScale2D"),t),p.Size*Curve(F(p.E.P,"YScale2D"),t),1);
            Color color=ColorCurve(F(p.E.P,"DiffuseColor"),t);
            float d=camera.GlobalPosition.DistanceTo(p.Node.GlobalPosition);
            float near=N(F(p.E.P,"MinDistanceFromCamera")),far=N(F(p.E.P,"MaxDistanceFromCamera"));
            color.A*=Mathf.Clamp((d-near)/Math.Max(.001f,N(F(p.E.P,"AlphaFalloff"),1)),0,1)*Mathf.Clamp((far-d)/Math.Max(.001f,N(F(p.E.P,"FarAlphaFalloff"),1)),0,1);
            p.Node.Modulate=color;
        }
    }
}
