extends Node3D
## Source room lights and bounded ember adapter. See room-environment.json for limits.
var rng = RandomNumberGenerator.new()
var emitters: Array = []
var particles: Array = []
var clock := 0.0
var data: Dictionary
var hue := Gradient.new()
func vector(value: String) -> Vector3:
	var a = value.trim_prefix("(").trim_suffix(")").split(",")
	return Vector3(float(a[0]),float(a[1]),float(a[2]))
func sample(value: String) -> float:
	if not value.begins_with("<"): return float(value)
	var a = value.trim_prefix("<").trim_suffix(">").split(",")
	return rng.randf_range(float(a[0]),float(a[1]))
func _ready():
	rng.seed = 201204
	data = JSON.parse_string(FileAccess.get_file_as_string("res://Assets/Maps/soa_p3/room-environment.json"))
	var hue_keys = RegEx.new()
	hue_keys.compile("\\[([0-9.]+):#([0-9.]+),([0-9.]+),([0-9.]+),([0-9.]+):[A-Z]\\]")
	var offsets = PackedFloat32Array()
	var colors = PackedColorArray()
	for key in hue_keys.search_all(data.embers.particle.ParticleHueColors):
		offsets.append(float(key.get_string(1)))
		colors.append(Color(float(key.get_string(2)),float(key.get_string(3)),float(key.get_string(4)),float(key.get_string(5))))
	hue.offsets = offsets
	hue.colors = colors
	var world = WorldEnvironment.new()
	world.environment = Environment.new()
	world.environment.background_mode = Environment.BG_COLOR
	world.environment.background_color = Color(0.012,0.015,0.02)
	world.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	world.environment.ambient_light_color = Color(0.55,0.62,0.7)
	world.environment.ambient_light_energy = 0.3
	add_child(world)
	for record in data.lights:
		var f: Dictionary = record.fields
		var light: Light3D = DirectionalLight3D.new() if f.LightType == "DIRECTIONAL" else OmniLight3D.new()
		add_child(light)
		light.name = "SourceLight_"+record.id
		light.position = vector(f.Position)
		light.rotation_degrees = vector(f.Rotation)
		# Source projector forward is adapted to Godot's -Z light direction.
		if light is DirectionalLight3D: light.rotate_object_local(Vector3.RIGHT,PI)
		var color: PackedStringArray = f.Color.trim_prefix("#").split(",")
		light.light_color = Color(float(color[0]),float(color[1]),float(color[2]))
		light.light_energy = float(f.Intensity)
		light.shadow_enabled = f.CastsShadows == "true"
		if light is OmniLight3D:
			var scale_source = vector(f.Scale).abs()
			light.omni_range = float(f.Range)*maxf(scale_source.x,maxf(scale_source.y,scale_source.z))
		light.set_meta("source_fields",f)
	for record in data.fxPlacements:
		if record.fields.FxSpecName == "env_ord_rising_embers" and record.fields.FxPause == "false":
			emitters.append({"origin":vector(record.fields.Position),"next":0.0})
	print("SOA_ROOM_ENVIRONMENT ",data.lights.size()," source lights; ",emitters.size()," ember placements")
func curve(text: String, t: float) -> float:
	var regex = RegEx.new()
	regex.compile("\\[([0-9.]+):([0-9.]+):[A-Z]\\]")
	var keys = regex.search_all(text)
	for i in range(1,keys.size()):
		if t <= float(keys[i].get_string(1)):
			var a = float(keys[i-1].get_string(1))
			var b = float(keys[i].get_string(1))
			return lerpf(float(keys[i-1].get_string(2)),float(keys[i].get_string(2)),(t-a)/(b-a))
	return float(keys[-1].get_string(2)) if keys.size() else 1.0
func _process(delta):
	clock += delta
	var camera = get_viewport().get_camera_3d()
	if camera == null: return
	var e: Dictionary = data.embers.emitter
	var p: Dictionary = data.embers.particle
	for emitter in emitters:
		if clock < emitter.next: continue
		emitter.next = clock + sample(e.EmitFrequency)
		if camera.global_position.distance_to(emitter.origin)>float(p.MaxDistanceFromCamera): continue
		for i in range(int(e.NumberToEmit)):
			var sprite = Sprite3D.new()
			sprite.texture = load(data.embers.texture)
			sprite.pixel_size = 1.0/sprite.texture.get_width()
			sprite.billboard = BaseMaterial3D.BILLBOARD_ENABLED
			sprite.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
			var material = StandardMaterial3D.new()
			material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
			material.blend_mode = BaseMaterial3D.BLEND_MODE_ADD
			material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
			material.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
			material.albedo_texture = sprite.texture
			material.vertex_color_use_as_albedo = true
			sprite.material_override = material
			add_child(sprite)
			var trajectory = RegEx.new()
			trajectory.compile("<[^>]+>")
			var values = trajectory.search_all(p.Trajectory)
			particles.append({"node":sprite,"origin":emitter.origin,"age":0.0,"life":sample(p.LifeSpan),"size":sample(p.XScale2D),"velocity":Vector3(sample(values[0].get_string()),sample(values[1].get_string()),sample(values[2].get_string()))})
	for i in range(particles.size()-1,-1,-1):
		var particle: Dictionary = particles[i]
		particle.age += delta
		if particle.age >= particle.life:
			particle.node.queue_free();particles.remove_at(i);continue
		var t: float = particle.age/particle.life
		particle.node.global_position = particle.origin + particle.velocity*particle.age
		particle.node.scale = Vector3.ONE * maxf(0.00001,particle.size*curve(p.Size2D,t))
		particle.node.modulate = hue.sample(t)
