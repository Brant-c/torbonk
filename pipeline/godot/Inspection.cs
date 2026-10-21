using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

// Standalone inspection only. No gameplay, emulator, client or source archive dependency.
public partial class Inspection : Node3D
{
    private Camera3D _camera = null!;
    private Vector3 _center;
    private float _radius = 3.8f;
    private float _yaw = 1.85f;
    private float _pitch = 0.15f;
    private bool _spin;
    private Label _status = null!;
    private static float[] Vec(Vector3 v) => new[] { v.X, v.Y, v.Z };
    private static IEnumerable<MeshInstance3D> Meshes(Node root)
    {
        if (root is MeshInstance3D mesh) yield return mesh;
        foreach (Node child in root.GetChildren())
            foreach (var found in Meshes(child)) yield return found;
    }
    public override async void _Ready()
    {
        try
        {
            var model = GetNode<Node3D>("OriginalDoor");
            bool first = true; Aabb bounds = default;
            int triangles = 0, vertices = 0, badNormals = 0, invalidValues = 0;
            var materials = new List<object>();
            foreach (var mi in Meshes(model))
            {
                var aabb = mi.GlobalTransform * mi.GetAabb();
                bounds = first ? aabb : bounds.Merge(aabb); first = false;
                for (int s = 0; s < mi.Mesh.GetSurfaceCount(); s++)
                {
                    var arrays = mi.Mesh.SurfaceGetArrays(s);
                    var positions = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                    var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
                    var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                    vertices += positions.Length;
                    triangles += (indices.Length > 0 ? indices.Length : positions.Length) / 3;
                    invalidValues += positions.Count(v => !v.IsFinite());
                    badNormals += normals.Count(n => !n.IsFinite() || Math.Abs(n.Length() - 1) > 0.002);
                    var material = mi.GetActiveMaterial(s) as StandardMaterial3D;
                    materials.Add(new {
                        name = material?.ResourceName,
                        albedo = material?.AlbedoTexture != null,
                        normal = material?.NormalEnabled == true && material.NormalTexture != null,
                        roughness = material?.RoughnessTexture != null,
                        emission = material?.EmissionEnabled == true && material.EmissionTexture != null,
                        cullMode = material?.CullMode.ToString()
                    });
                    if (material?.AlbedoTexture == null || material.NormalTexture == null || material.RoughnessTexture == null)
                        throw new Exception("Required converted texture missing in Godot");
                }
            }
            if (first) throw new Exception("Imported scene has no meshes");
            using var recipe = JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://Assets/recipe.json"));
            var dims = recipe.RootElement.GetProperty("expectedDimensionsGodot").EnumerateArray().Select(x => x.GetSingle()).ToArray();
            var expected = new Vector3(dims[0], dims[1], dims[2]);
            var difference = (bounds.Size - expected).Abs();
            float error = Math.Max(difference.X, Math.Max(difference.Y, difference.Z));
            bool passed = error < 0.00001f && triangles == recipe.RootElement.GetProperty("expectedTriangles").GetInt32() && badNormals == 0 && invalidValues == 0;
            var result = new {
                engine = Engine.GetVersionInfo()["string"].AsString(), dotnet = System.Environment.Version.ToString(),
                passed, meshInstances = Meshes(model).Count(), vertices, triangles, badNormals, invalidValues,
                aabbPosition = Vec(bounds.Position), dimensions = Vec(bounds.Size), expectedDimensions = Vec(expected),
                maximumDimensionError = error, units = "uncalibrated source units, scale 1:1", materials,
                orientation = "+Y up; glTF standard forward; source placement rotation intentionally not applied",
                physics = "Optional collision omitted; source collision not accepted as tested"
            };
            string output = ProjectSettings.GlobalizePath("res://../manifests/godot-validation.json");
            System.IO.File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            if (!passed) throw new Exception("Geometry validation failed; see godot-validation.json");
            _center = bounds.GetCenter();
            _radius = bounds.Size.Length() * 1.8f;
            var environment = new WorldEnvironment {
                Environment = new Godot.Environment {
                    BackgroundMode = Godot.Environment.BGMode.Color,
                    BackgroundColor = new Color(0.11f, 0.12f, 0.14f),
                    AmbientLightSource = Godot.Environment.AmbientSource.Color,
                    AmbientLightColor = Colors.White, AmbientLightEnergy = 0.45f,
                    TonemapMode = Godot.Environment.ToneMapper.Linear
                }
            };
            AddChild(environment);
            var key = new DirectionalLight3D { RotationDegrees = new Vector3(-40, -35, 0), LightEnergy = 1.0f };
            var fill = new DirectionalLight3D { RotationDegrees = new Vector3(-20, 130, 0), LightEnergy = 0.45f };
            AddChild(key); AddChild(fill);
            _camera = new Camera3D { Current = true, Fov = 42, Near = 0.01f, Far = 100 };
            AddChild(_camera); UpdateCamera();
            var canvas = new CanvasLayer(); AddChild(canvas);
            _status = new Label {
                Text = "SITH ACADEMY DOOR  |  original April2012 asset\n576 triangles - 1:1 source units - PBR material approximation\nDrag: orbit   Wheel: zoom   1/2/3/4: views   Space: rotate\nPhysical scale and original-client shader parity remain unverified.",
                Position = new Vector2(22, 20)
            };
            _status.AddThemeFontSizeOverride("font_size", 17);
            canvas.AddChild(_status);
            GD.Print("INSPECTION_VALIDATION_PASSED " + JsonSerializer.Serialize(result));
            var args = OS.GetCmdlineUserArgs();
            int view = Array.IndexOf(args, "--view");
            if (view >= 0) { _yaw = float.Parse(args[view + 1], System.Globalization.CultureInfo.InvariantCulture); UpdateCamera(); }
            if (args.Contains("--validate")) { GetTree().Quit(); return; }
            int capture = Array.IndexOf(args, "--capture");
            if (capture >= 0)
            {
                for (int frame = 0; frame < 12; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Error saved = GetViewport().GetTexture().GetImage().SavePng(args[capture + 1]);
                if (saved != Error.Ok) throw new Exception("Capture failed: " + saved);
                GetTree().Quit();
            }
        }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private void UpdateCamera()
    {
        _camera.Position = _center + new Vector3(Mathf.Sin(_yaw) * Mathf.Cos(_pitch), Mathf.Sin(_pitch), Mathf.Cos(_yaw) * Mathf.Cos(_pitch)) * _radius;
        _camera.LookAt(_center, Vector3.Up);
    }
    public override void _Process(double delta)
    {
        if (_camera != null && _spin) { _yaw += (float)delta * 0.35f; UpdateCamera(); }
    }
    public override void _UnhandledInput(InputEvent ev)
    {
        if (_camera == null) return;
        if (ev is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Left)) {
            _yaw -= motion.Relative.X * .006f; _pitch = Mathf.Clamp(_pitch + motion.Relative.Y * .006f, -1.3f, 1.3f);
        }
        if (ev is InputEventMouseButton { Pressed: true } button) {
            if (button.ButtonIndex == MouseButton.WheelUp) _radius = Math.Max(.3f, _radius * .9f);
            if (button.ButtonIndex == MouseButton.WheelDown) _radius = Math.Min(30, _radius * 1.1f);
        }
        if (ev is InputEventKey { Pressed: true, Echo: false } key) {
            if (key.Keycode == Key.Space) _spin = !_spin;
            if (key.Keycode >= Key.Key1 && key.Keycode <= Key.Key4) { _yaw = ((int)key.Keycode - (int)Key.Key1) * Mathf.Pi / 2; _pitch = .05f; }
        }
        UpdateCamera();
    }
}
