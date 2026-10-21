using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors.Equipment;

/// <summary>Only changes mesh visibility and sockets on the existing rig. Never writes animation or actor state.</summary>
public partial class CharacterAppearanceAssembly : Node
{
    public Skeleton3D Skeleton { get; private set; } = null!;
    public EquipmentLoadout Loadout { get; private set; } = null!;
    public string LastError { get; private set; } = "";
    public LocomotionDefinition? Locomotion { get; private set; }
    public int ActiveArmorMeshes => _meshes.Where(p=>_active.Contains(p.Key)).Sum(p=>p.Value.Count);
    private readonly Dictionary<string,List<MeshInstance3D>> _meshes=new();
    private readonly Dictionary<string,EquipmentAttachment> _weapons=new();
    private readonly Dictionary<string,List<MeshInstance3D>> _baseGroups=new();
    private HashSet<string> _active=new();
    private readonly Dictionary<string,PackedScene> _scenes=new();
    public EquipmentAttachment? Weapon(string slot) => _weapons.GetValueOrDefault(slot);

    public void Initialize(Node3D model, Skeleton3D skeleton, EquipmentCatalog catalog, CharacterEquipmentContext context)
    {
        Skeleton=skeleton; Loadout=new(catalog,context);
        var original=model.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().ToArray();
        foreach(var group in context.Rig.BaseGroups)
        {
            var matches=original.Where(m=>group.Value.Contains(m.Name.ToString())).ToList();
            if(matches.Count!=group.Value.Length)throw new InvalidOperationException("Base character mesh group missing: "+group.Key);
            _baseGroups.Add(group.Key,matches);
        }
        Loadout.ApplyAppearance=Apply;
    }

    private string? Apply(EquipmentSnapshot next)
    {
        try
        {
            var visuals=Loadout.ResolveVisuals(next);
            // Validate everything before changing the current outfit, including previously cached resources.
            foreach(var entry in visuals)
            {
                var visual=entry.Value;
                if(visual.Slot!=entry.Key)throw new InvalidOperationException("Appearance slot mismatch: "+visual.Id);
                if(!EquipmentCatalog.Compatible(visual.Compatibility,Loadout.Context,out var error))throw new InvalidOperationException(error);
                foreach(string bone in visual.RequiredBones)
                    if(Skeleton.FindBone(bone)<0)throw new InvalidOperationException($"{visual.Id}: required accessory/attachment bone {bone} absent from {Loadout.Context.Rig.Id}.");
                if(visual.Attachment is {} attach)
                {
                    if(!Loadout.Context.Rig.AttachmentBones.TryGetValue(visual.Slot,out string? bone) || bone!=attach.Bone)
                        throw new InvalidOperationException("Unsupported attachment profile for "+visual.Slot);
                    EquipmentAttachment.Validate(Skeleton,visual.Scene,attach);
                }
                else EnsureMeshes(visual);
            }
            // Prepare new sockets hidden, so failed preparation cannot remove the old weapon.
            var staged=new Dictionary<string,EquipmentAttachment>();
            try
            {
                foreach(var entry in visuals.Where(v=>v.Value.Attachment!=null && !_active.Contains(v.Value.Id)))
                { var weapon=EquipmentAttachment.Equip(Skeleton,entry.Value.Scene,entry.Value.Attachment!);weapon.Visible=false;staged.Add(entry.Key,weapon); }
            }
            catch { foreach(var weapon in staged.Values)weapon.Unequip();throw; }
            var ids=visuals.Values.Select(v=>v.Id).ToHashSet();
            foreach(var group in _baseGroups)
                foreach(var mesh in group.Value)mesh.Visible=!visuals.Values.Any(v=>v.HidesBaseGroups.Contains(group.Key));
            foreach(var group in _meshes)
                foreach(var mesh in group.Value)mesh.Visible=ids.Contains(group.Key);
            foreach(var entry in _weapons.ToArray())
                if(!visuals.TryGetValue(entry.Key,out var v) || !_active.Contains(v.Id) || staged.ContainsKey(entry.Key))
                { entry.Value.Unequip();_weapons.Remove(entry.Key); }
            foreach(var entry in staged) { _weapons[entry.Key]=entry.Value;entry.Value.Visible=true; }
            Locomotion=visuals.Values.Select(v=>v.Locomotion).FirstOrDefault(v=>v!=null);
            _active=ids;LastError="";return null;
        }
        catch(Exception ex) { LastError=ex.Message;return LastError; }
    }

    private void EnsureMeshes(AppearanceDefinition visual)
    {
        if(_meshes.ContainsKey(visual.Id))return;
        if(!_scenes.TryGetValue(visual.Scene,out var packed))
        {
            if(!ResourceLoader.Exists(visual.Scene))throw new InvalidOperationException("Unconverted appearance: "+visual.Scene);
            packed=GD.Load<PackedScene>(visual.Scene);_scenes.Add(visual.Scene,packed);
        }
        var source=packed.Instantiate<Node3D>();var copies=new List<MeshInstance3D>();
        try
        {
            var sourceRig=source.FindChildren("*","Skeleton3D",true,false).OfType<Skeleton3D>().Single();
            var matches=source.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
                .Where(m=>visual.MeshNames.Contains(m.Name.ToString())).ToArray();
            if(matches.Length!=visual.MeshNames.Length || matches.Length==0)throw new InvalidOperationException("Missing modular mesh in "+visual.Id);
            foreach(var mesh in matches)
            {
                if(mesh.Skin==null)throw new InvalidOperationException("Unskinned armor: "+mesh.Name);
                var skin=(Skin)mesh.Skin.Duplicate();
                for(int bind=0;bind<skin.GetBindCount();bind++)
                {
                    string name=skin.GetBindName(bind).ToString();
                    int sourceBone=name!="" ? sourceRig.FindBone(name) : skin.GetBindBone(bind);
                    if(sourceBone<0 || sourceBone>=sourceRig.GetBoneCount())throw new InvalidOperationException("Unknown source skin bind in "+visual.Id);
                    name=sourceRig.GetBoneName(sourceBone);int target=Skeleton.FindBone(name);
                    if(target<0)throw new InvalidOperationException($"{visual.Id}: skin needs absent bone {name}.");
                    int sp=sourceRig.GetBoneParent(sourceBone),tp=Skeleton.GetBoneParent(target);
                    if((sp<0 ? "" : sourceRig.GetBoneName(sp).ToString())!=(tp<0 ? "" : Skeleton.GetBoneName(tp).ToString())
                        || !RestMatches(sourceRig.GetBoneRest(sourceBone),Skeleton.GetBoneRest(target)))
                        throw new InvalidOperationException($"{visual.Id}: incompatible rest/hierarchy for {name}; rig left unchanged.");
                    skin.SetBindName(bind,name);skin.SetBindBone(bind,target);
                }
                var copy=new MeshInstance3D { Name="Equipment_"+mesh.Name,Mesh=mesh.Mesh,Skin=skin,Visible=false,
                    Transform=RelativeTransform(mesh,sourceRig) };
                // The resource keeps its original inverse binds, weights, surfaces and material resources.
                Skeleton.AddChild(copy);copies.Add(copy);copy.Skeleton=copy.GetPathTo(Skeleton);
            }
            _meshes.Add(visual.Id,copies);
        }
        catch { foreach(var mesh in copies)mesh.Free();throw; }
        finally { source.Free(); }
    }
    private static Transform3D RelativeTransform(Node3D node, Node3D ancestor)
    {
        var transform=node.Transform;var parent=node.GetParent();
        while(parent!=ancestor)
        { if(parent is not Node3D p)throw new InvalidOperationException("Mesh is outside source skeleton");transform=p.Transform*transform;parent=p.GetParent(); }
        return transform;
    }
    private static bool RestMatches(Transform3D a,Transform3D b) =>
        a.Origin.DistanceTo(b.Origin)<0.00001f && a.Basis.X.DistanceTo(b.Basis.X)<0.0001f
        && a.Basis.Y.DistanceTo(b.Basis.Y)<0.0001f && a.Basis.Z.DistanceTo(b.Basis.Z)<0.0001f;
}
