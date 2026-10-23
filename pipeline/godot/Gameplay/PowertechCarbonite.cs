using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors;

/// <summary>Instance-only carbonite coating on the NPC's actual skinned meshes.</summary>
public partial class PowertechCarbonite : Node3D
{
    private EnemyAgent _target=null!;
    private ShaderMaterial _coating=null!;
    private readonly List<(MeshInstance3D mesh,Material? previous)> _meshes=new();
    public int CoatedMeshes=>_meshes.Count;
    public static PowertechCarbonite Attach(EnemyAgent target)
    {
        var existing=target.GetChildren().OfType<PowertechCarbonite>().FirstOrDefault(n=>!n.IsQueuedForDeletion());
        if(existing!=null)return existing;
        var coating=new PowertechCarbonite {_target=target};target.AddChild(coating);return coating;
    }
    public override void _Ready()
    {
        _coating=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/PowertechCarbonite.gdshader")};
        _coating.SetShaderParameter("carbonite",GD.Load<Texture2D>("res://Assets/Powertech/proj_carbonite_01.png"));
        _coating.SetShaderParameter("origin",_target.GlobalPosition);
        foreach(var mesh in _target.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
        {
            if(mesh.Mesh==null)continue;
            _meshes.Add((mesh,mesh.MaterialOverlay));mesh.MaterialOverlay=_coating;
        }
    }
    public override void _Process(double delta)
    {
        if(!GodotObject.IsInstanceValid(_target) || !_target.Alive || _target.StunRemaining<=0){Restore();QueueFree();return;}
        _coating.SetShaderParameter("origin",_target.GlobalPosition);
    }
    private void Restore()
    {
        foreach(var (mesh,previous) in _meshes)
            if(GodotObject.IsInstanceValid(mesh) && mesh.MaterialOverlay==_coating)mesh.MaterialOverlay=previous;
        _meshes.Clear();
    }
    public override void _ExitTree()=>Restore();
}
