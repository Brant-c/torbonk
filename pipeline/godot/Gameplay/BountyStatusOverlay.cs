using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors;

/// <summary>Instance-only Bounty Hunter status coating on an NPC's actual skinned meshes: your burns and Heat Signature.
/// Never replaces another overlay (Carbonize keeps priority); restores on expiry, death or exit.</summary>
public partial class BountyStatusOverlay : Node3D
{
    private EnemyAgent _target=null!;
    private Talents.TalentRuntime _runtime=null!;
    private ShaderMaterial _coating=null!;
    private readonly List<MeshInstance3D> _meshes=new();
    private float _burn,_mark;
    private double _idle;
    public float Burn=>_burn;
    public float Mark=>_mark;
    public int CoatedMeshes=>_meshes.Count(m=>GodotObject.IsInstanceValid(m) && m.MaterialOverlay==_coating);
    public static BountyStatusOverlay Ensure(EnemyAgent target,Talents.TalentRuntime runtime)
    {
        var existing=target.GetChildren().OfType<BountyStatusOverlay>().FirstOrDefault(n=>!n.IsQueuedForDeletion());
        if(existing!=null)return existing;
        var overlay=new BountyStatusOverlay {Name="BountyStatusOverlay",_target=target,_runtime=runtime};target.AddChild(overlay);return overlay;
    }
    public override void _Ready()
    {
        _coating=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/BountyStatus.gdshader")};
        _coating.SetShaderParameter("flames",GD.Load<Texture2D>("res://Assets/Powertech/4x4_flame_lick_02_bw.png"));
        _meshes.AddRange(_target.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().Where(m=>m.Mesh!=null));
        Refresh(0);
    }
    public override void _Process(double delta)=>Refresh(delta);
    private void Refresh(double delta)
    {
        if(!GodotObject.IsInstanceValid(_target) || !_target.Alive){Restore();QueueFree();return;}
        float burn=_runtime.PowertechBurning(_target)?1:0,mark=_runtime.HeatSignature(_target)>0?Math.Min(1,.45f+.11f*_runtime.HeatSignature(_target)):0;
        // Short fades keep refreshes from flickering.
        _burn=Mathf.MoveToward(_burn,burn,(float)delta*4);_mark=Mathf.MoveToward(_mark,mark,(float)delta*4);
        _coating.SetShaderParameter("burn",_burn);_coating.SetShaderParameter("mark",_mark);_coating.SetShaderParameter("origin",_target.GlobalPosition);
        bool active=burn>0 || mark>0 || _burn>0 || _mark>0;
        foreach(var mesh in _meshes)
            if(GodotObject.IsInstanceValid(mesh) && mesh.MaterialOverlay==null && active)mesh.MaterialOverlay=_coating;
        _idle=active?0:_idle+delta;
        if(_idle>.5){Restore();QueueFree();}
    }
    private void Restore()
    {
        foreach(var mesh in _meshes)if(GodotObject.IsInstanceValid(mesh) && mesh.MaterialOverlay==_coating)mesh.MaterialOverlay=null;
    }
    public override void _ExitTree()=>Restore();
}
