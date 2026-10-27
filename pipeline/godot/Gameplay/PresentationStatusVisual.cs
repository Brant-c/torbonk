using Godot;
using System;
using System.Text.Json;
namespace SwtorSurvivors;
// Lifetime belongs to PeriodicEffects; refresh replaces rather than stacks visuals or voices.
public partial class PresentationStatusVisual : Node3D
{
    private Func<bool> _active=null!;
    private Sprite3D _glow=null!,_smoke=null!;
    private double _age;
    private bool _afflictionSmoke;
    public static Node3D AttachTerror(Combatant target,Func<bool> active)
    {
        var node=new PresentationStatusVisual {_active=()=>GodotObject.IsInstanceValid(target) && target.Alive && active()};
        target.AddChild(node);node.Position=Vector3.Up*.09f;
        node._glow=node.Make("2x2_electrical_blast_alpha",2,.1f,new Color(.527f,.535f,.993f,.5f));
        node._smoke=node.Make("5x5_flame_lick_04_bw",5,.12f,new Color(.3f,.15f,.45f,.4f));
        return node;
    }
    public static Node3D AttachDarkness(Combatant target,Func<bool> active)
    {
        var node=new PresentationStatusVisual {_active=()=>GodotObject.IsInstanceValid(target) && target.Alive && active()};
        target.AddChild(node);node.Position=Vector3.Up*.09f;
        node._glow=node.Make("res://Assets/SorcererRefinement/2x2_dark_energy_clouds_02.png",2,.19f,new Color(.6f,.2f,.8f,.65f));
        node._smoke=node.Make("res://Assets/SorcererRefinement/2x2_dark_energy_clouds_02.png",2,.13f,new Color(.5f,.2f,.7f,.25f));
        return node;
    }
    public static Node3D AttachAffliction(Combatant target,Func<bool> active)
    {
        var node=new PresentationStatusVisual {Name="AfflictionStatus",_active=()=>GodotObject.IsInstanceValid(target) && target.Alive && active()};
        target.AddChild(node);node.Position=Vector3.Up*.10f;
        node._glow=node.Make("res://Assets/SorcererRefinement/2x2_electrical_blast_alpha.png",2,.18f,new Color(.51f,.456f,.744f,.8f));
        node._smoke=node.Make("res://Assets/SorcererRefinement/smoke_thin.png",4,.21f,new Color(.115f,.024f,.176f,.65f));
        node.SetOriginalMaterial(node._glow,false);node.SetOriginalMaterial(node._smoke,true);
        node._afflictionSmoke=true;
        // The shader owns atlas sampling and interpolation for this original smoke layer.
        node._smoke.Hframes=node._smoke.Vframes=1;node._smoke.PixelSize=.21f/node._smoke.Texture.GetWidth();
        using var audio=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/SorcererRefinement/audio.json"));
        var id=audio.RootElement.GetProperty("events").GetProperty("Play_ability_sithinquisitor_darkaffliction_loop").GetString()!;
        var media=audio.RootElement.GetProperty("graphs").GetProperty(id).GetProperty("media");
        if(media.GetArrayLength()>0 && node.GetTree().GetNodesInGroup("AfflictionAudio").Count<4)
        {
            var stream=(AudioStreamWav)GD.Load<AudioStreamWav>($"res://Assets/SorcererRefinement/Audio/{media[0].GetInt32()}.wav").Duplicate();
            stream.LoopMode=AudioStreamWav.LoopModeEnum.Forward;stream.LoopBegin=0;stream.LoopEnd=(int)(stream.GetLength()*stream.MixRate);
            var voice=new AudioStreamPlayer3D {Name="OriginalAfflictionLoop",Bus="B7Combat",Stream=stream,VolumeDb=-23,UnitSize=.6f,MaxDistance=2};
            node.AddChild(voice);voice.AddToGroup("AfflictionAudio");AudioPresentation.Find(node)?.RegisterCombatVoice(voice);voice.Play();
        }
        return node;
    }
    private void SetOriginalMaterial(Sprite3D sprite,bool smoke)
    {
        var material=new ShaderMaterial {Shader=GD.Load<Shader>(smoke?"res://Gameplay/AfflictionSmoke.gdshader":"res://Gameplay/SorcererParticle.gdshader")};
        material.SetShaderParameter("source_texture",sprite.Texture);
        if(!smoke)material.SetShaderParameter("brightness",1.8f);
        material.RenderPriority=smoke?-1:1;
        sprite.MaterialOverride=material;
    }
    private Sprite3D Make(string stem,int frames,float size,Color tint)
    {
        var tex=GD.Load<Texture2D>(stem.StartsWith("res://")?stem:"res://Assets/PresentationRepairs/"+stem+".png");
        var sprite=new Sprite3D {Texture=tex,Hframes=frames,Vframes=frames,PixelSize=size/(tex.GetWidth()/frames),Shaded=false,Modulate=tint,
            MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
                BlendMode=BaseMaterial3D.BlendModeEnum.Add,BillboardMode=BaseMaterial3D.BillboardModeEnum.Enabled,AlbedoTexture=tex,VertexColorUseAsAlbedo=true}};
        AddChild(sprite);return sprite;
    }
    public override void _Process(double delta)
    {
        if(!_active()){QueueFree();return;}
        _age+=delta;_glow.Frame=(int)(_age*12)%(_glow.Hframes*_glow.Vframes);if(_afflictionSmoke)((ShaderMaterial)_smoke.MaterialOverride).SetShaderParameter("frame_phase",(float)(_age*8));
        else _smoke.Frame=(int)(_age*20)%(_smoke.Hframes*_smoke.Vframes);
    }
}
