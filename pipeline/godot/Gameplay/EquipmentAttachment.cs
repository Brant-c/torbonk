using Godot;
using System;
using System.Text.Json;
using SwtorSurvivors.Equipment;

namespace SwtorSurvivors;

/// <summary>Original weapon coordinates attach directly to the original right weapon socket.</summary>
public partial class EquipmentAttachment : Node3D
{
    public BoneAttachment3D Socket { get; private set; } = null!;
    public bool BladeInitialized { get; private set; }
    public static EquipmentAttachment Equip(Skeleton3D skeleton, string weapon, bool redBlade)
    {
        string bone = "";
        foreach (var candidate in new[] { "RightWeapon", "weapon_r", "weapon_right", "WeaponRight", "r_weapon" })
            if (skeleton.FindBone(candidate) >= 0) { bone = candidate; break; }
        if (bone == "") throw new InvalidOperationException("No original right weapon socket in " + skeleton.GetPath());
        return Equip(skeleton, $"res://Assets/Enemies/{weapon}.glb", new AttachmentDefinition(
            bone,"res://Assets/Enemies/weapon-socket.json",[0,0,0],[0,0,0],1,redBlade));
    }
    public static void Validate(Skeleton3D skeleton,string scene,AttachmentDefinition profile)
    {
        if(skeleton.FindBone(profile.Bone)<0)throw new InvalidOperationException("Missing weapon bone: "+profile.Bone);
        if(!ResourceLoader.Exists(scene))throw new InvalidOperationException("Unconverted weapon: "+scene);
        if(profile.Position.Length!=3 || profile.RotationDegrees.Length!=3 || !float.IsFinite(profile.Scale) || profile.Scale<=0
            || Array.Exists(profile.Position,x=>!float.IsFinite(x)) || Array.Exists(profile.RotationDegrees,x=>!float.IsFinite(x)))
            throw new InvalidOperationException("Invalid weapon offset: "+scene);
        Correction(profile);
    }
    private static Basis Correction(AttachmentDefinition profile)
    {
        if(profile.CorrectionResource=="")return Basis.Identity;
        using var config=JsonDocument.Parse(FileAccess.GetFileAsString(profile.CorrectionResource));
        var matrix=config.RootElement.GetProperty("attachmentCorrection");
        Vector3 Column(int c)=>new(matrix[0][c].GetSingle(),matrix[1][c].GetSingle(),matrix[2][c].GetSingle());
        return new Basis(Column(0),Column(1),Column(2)).Orthonormalized();
    }
    public static EquipmentAttachment Equip(Skeleton3D skeleton,string scene,AttachmentDefinition profile)
    {
        Validate(skeleton,scene,profile);
        var model=GD.Load<PackedScene>(scene).Instantiate<Node3D>();
        var socket = new BoneAttachment3D { Name = "B6WeaponSocket", BoneName = profile.Bone };
        skeleton.AddChild(socket);
        var equipment = new EquipmentAttachment { Name = "EquippedWeapon", Socket = socket };
        socket.AddChild(equipment);
        var rotation=new Vector3(profile.RotationDegrees[0],profile.RotationDegrees[1],profile.RotationDegrees[2]);
        equipment.Basis=(Correction(profile)*Basis.FromEuler(rotation*Mathf.Pi/180)).Scaled(Vector3.One*profile.Scale);
        equipment.Position=new Vector3(profile.Position[0],profile.Position[1],profile.Position[2]);
        equipment.AddChild(model);
        if (profile.RedBlade) equipment.AddBlade();
        return equipment;
    }
    public void Unequip()
    {
        // Detach synchronously; queued objects cannot overlap a newly restored visual for one frame.
        Visible=false;Socket.GetParent()?.RemoveChild(Socket);Socket.QueueFree();
    }
    private void AddBlade()
    {
        // Reconstructed blade renderer; hilt mesh and weapon socket are original April resources.
        // Converted GR2 bounds place the emitter at -Z; preserve that original weapon axis.
        foreach (var layer in new[] { (0.0018f, new Color(1, 0.77f, 0.72f), 4f), (0.0038f, new Color(1, 0.015f, 0.005f, 0.45f), 2f) })
        {
            var mat = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = layer.Item2, EmissionEnabled = true, Emission = new Color(1, 0.015f, 0.005f),
                EmissionEnergyMultiplier = layer.Item3, Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
            AddChild(new MeshInstance3D { Name = "RedBlade", Position = new Vector3(0, 0, -0.0695f), RotationDegrees = new Vector3(90, 0, 0),
                Mesh = new CylinderMesh { TopRadius = layer.Item1, BottomRadius = layer.Item1, Height = 0.105f, RadialSegments = 10 },
                MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        }
        // Original April red-base particle references this recovered soft tip texture.
        // Two crossed additive planes keep a soft visible red halo in the compatibility renderer.
        var haloMaterial = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoTexture = GD.Load<Texture2D>("res://Assets/Enemies/saber-tip.png"), AlbedoColor = new Color(1, 0.01f, 0.002f),
            BlendMode = BaseMaterial3D.BlendModeEnum.Add, Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled, NoDepthTest = false };
        foreach (float angle in new[] { 0f, Mathf.Pi / 2 })
            AddChild(new MeshInstance3D { Name = "OriginalTextureHalo", Mesh = new QuadMesh { Size = new Vector2(0.018f, 0.135f) },
                Transform = new Transform3D(new Basis(Vector3.Forward, angle) * new Basis(Vector3.Right, Mathf.Pi / 2), new Vector3(0, 0, -0.0695f)),
                MaterialOverride = haloMaterial, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        BladeInitialized = true;
    }
}
