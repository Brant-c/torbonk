extends Node3D
## Presentation only. Caller supplies an imported rig and a target point/rig.
## No damage, target acquisition, range rules or combat state.
signal phase_changed(phase: String)
@export var fx_scale: float = 0.1
@export var target_point: Vector3 = Vector3(0, 0.12, -0.5)
@export var automatic_stop_seconds: float = 0.0
@export var externally_driven: bool = false
var caster: Skeleton3D
var target_rig: Skeleton3D
var target_node: Node3D
var animator: AnimationPlayer
var phase: String = "idle"
var elapsed: float = 0.0
var channel_elapsed: float = 0.0
var active: bool = false
var config: Dictionary
var rng = RandomNumberGenerator.new()
var beams: Array[Dictionary] = []
var emitters: Array[Dictionary] = []
var sparks: Array[Dictionary] = []
var lights: Array[Dictionary] = []
var audio: Array[AudioStreamPlayer3D] = []
var audio_base: Array[float] = []
var seed_value: int = 201204
var bolt_material: ShaderMaterial
var ring_material: ShaderMaterial
var body_material: ShaderMaterial
var particle_clock: float = 0.0
var last_cast: int = -1
var last_error: String = ""
var scalar_curves: Dictionary = {}
var color_curves: Dictionary = {}
var aftershock_clock: float = 0.0
var aftershock_history: Array[int] = []
var target_was_assigned: bool = false

func _ready():
	config = JSON.parse_string(FileAccess.get_file_as_string("res://Assets/Lightning/visuals.json"))
	rng.seed = seed_value
	bolt_material = make_material("res://Assets/Lightning/512_singlearcb_bw.png", false)
	body_material = bolt_material.duplicate()
	body_material.set_shader_parameter("hue_purple_key",0.3); body_material.set_shader_parameter("hue_white_key",0.85)
	ring_material = make_material("res://Assets/Lightning/electric_rings_bw_02.png", true)
	# Update after AnimationPlayer and Skeleton3D have evaluated this frame.
	process_priority = 100
	for i in 12:
		var sound = AudioStreamPlayer3D.new()
		if AudioServer.get_bus_index("B7Combat") >= 0: sound.bus = "B7Combat"
		sound.unit_size = 1.0; sound.max_distance = 6.0
		sound.attenuation_model = AudioStreamPlayer3D.ATTENUATION_DISABLED
		add_child(sound); audio.append(sound); audio_base.append(0.0)
		var mixer = get_tree().get_first_node_in_group("b7_audio")
		if mixer: mixer.call("RegisterCombatVoice", sound)
	set_process(false)

func configure(rig: Skeleton3D, animation_player: AnimationPlayer, target: Node3D = null, target_skeleton: Skeleton3D = null) -> bool:
	interrupt(false); last_error=""
	caster = rig; animator = animation_player; target_node = target; target_rig = target_skeleton
	target_was_assigned = target != null or target_skeleton != null
	for entry in config.caster._fxLightningList:
		if caster.find_bone(entry._fxAttachBone) < 0:
			last_error = "Missing original attachment " + entry._fxAttachBone; return false
	for clip in ["Idle", "LightningStart", "LightningLoop", "LightningEnd"]:
		if not animator.has_animation(clip): last_error = "Missing original clip " + clip; return false
	if is_instance_valid(target_rig):
		var required = ["vfx_aim01"]
		for entry in config.body._fxEmitterList + config.body._fxLightningList: required.append(entry._fxAttachBone)
		for bone in required:
			if target_rig.find_bone(bone)<0: last_error="Missing target attachment "+bone; return false
	animator.get_animation("LightningLoop").loop_mode = Animation.LOOP_LINEAR
	return true

func set_phase(value: String):
	phase = value; elapsed = 0.0; phase_changed.emit(value)

func set_target_point(value: Vector3):
	if active and target_was_assigned: interrupt()
	target_node=null; target_rig=null; target_was_assigned=false; target_point=value

func start_channel() -> bool:
	if not is_instance_valid(caster) or not is_instance_valid(animator) or not last_error.is_empty(): return false
	interrupt(false)
	set_phase("start"); channel_elapsed = 0.0
	if not externally_driven:
		animator.play("LightningStart"); animator.advance(0)
	set_process(true)
	return true

func stop_channel():
	if phase not in ["start", "channel"]: return
	set_phase("end"); active = false
	if not externally_driven: animator.play("LightningEnd")
	clear_visuals(false)
	for i in range(lights.size()-1,-1,-1):
		if lights[i].data._fxHowToStop=="NORMAL": lights[i].node.free(); lights.remove_at(i)

func interrupt(play_idle: bool = true):
	active = false
	clear_visuals(true)
	for sound in audio: sound.stop(); sound.stream = null
	if play_idle and not externally_driven and is_instance_valid(animator): animator.play("Idle")
	set_phase("idle"); set_process(false)

func _exit_tree():
	for sound in audio: sound.stop()
	active=false; phase="idle"

# B5.1 gameplay controls phase timing; the inspection scene retains the original standalone behavior.
func external_channel():
	if not externally_driven or phase != "start": return
	if not active: activate()
	set_phase("channel")

func bone_world(rig: Skeleton3D, bone: String) -> Transform3D:
	if is_instance_valid(rig):
		var index = rig.find_bone(bone)
		if index >= 0: return rig.global_transform * rig.get_bone_global_pose(index)
	return Transform3D(Basis.IDENTITY, aim_point())

func aim_point() -> Vector3:
	if is_instance_valid(target_rig):
		var index = target_rig.find_bone("vfx_aim01")
		if index >= 0: return (target_rig.global_transform * target_rig.get_bone_global_pose(index)).origin
	if is_instance_valid(target_node): return target_node.global_position
	return target_point

func make_material(path: String, particle: bool) -> ShaderMaterial:
	var material = ShaderMaterial.new(); material.shader = load("res://Lightning.gdshader")
	material.set_shader_parameter("source_texture", load(path))
	material.set_shader_parameter("particle", particle)
	return material

func make_beam(entry: Dictionary, body: bool):
	var mesh = MeshInstance3D.new(); mesh.mesh = ImmediateMesh.new()
	mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(mesh)
	beams.append({"node":mesh,"data":entry,"body":body,"points":[],"uv":rng.randf(),"next":0.0})

func activate():
	active = true; particle_clock = 0.0
	for entry in config.caster._fxLightningList: make_beam(entry, false)
	if is_instance_valid(target_rig):
		for entry in config.body._fxLightningList: make_beam(entry, true)
		for entry in config.body._fxEmitterList:
			if target_rig.find_bone(entry._fxAttachBone) >= 0: emitters.append({"data":entry})
	for group in [config.caster._fxLightList, config.body._fxLightList if is_instance_valid(target_rig) else []]:
		for entry in group:
			var light = OmniLight3D.new()
			light.omni_range = float(entry._fxRange) * fx_scale
			light.light_specular = 0.0; light.shadow_enabled = false
			add_child(light); lights.append({"node":light,"data":entry,"energy":float(entry._fxIntensity)})
	start_audio()

func _process(delta: float):
	if not is_instance_valid(caster) or not is_instance_valid(animator): interrupt(false); return
	# Losing an explicitly assigned target cancels the presentation.
	if target_was_assigned and not is_instance_valid(target_node) and not is_instance_valid(target_rig): interrupt(); return
	elapsed += delta
	if phase == "start":
		if not active and elapsed >= float(config.adapter.startFxSeconds): activate()
		if not externally_driven and elapsed >= animator.get_animation("LightningStart").length:
			set_phase("channel"); animator.play("LightningLoop")
	elif phase == "channel":
		channel_elapsed += delta
		if automatic_stop_seconds > 0 and channel_elapsed >= automatic_stop_seconds: stop_channel()
	elif phase == "end":
		var fade = maxf(0, 1.0 - elapsed / 0.5)
		for i in audio.size(): audio[i].volume_linear = db_to_linear(audio_base[i]) * fade
		for light in lights: light.node.light_energy = light.energy * maxf(0, 1.0 - elapsed / 0.3)
		if elapsed >= maxf(0.5, animator.get_animation("LightningEnd").length): interrupt(); return
	if not active: return
	particle_clock += delta
	if is_instance_valid(target_rig):
		aftershock_clock += delta
		while aftershock_clock >= 0.2:
			aftershock_clock -= 0.2; trigger_aftershock()
	for i in audio.size(): audio[i].global_position = aim_point() if i>=3 else bone_world(caster,"vfx_hand_right").origin
	for beam in beams: draw_beam(beam, delta)
	for light in lights:
		var entry: Dictionary = light.data
		light.node.global_position = aim_point() if entry._fxAttachTo == "TARGET" else bone_world(caster,"vfx_hand_right").origin
		var period = float(entry._fxColor.split(";")[-1]) if ";" in entry._fxColor else 0.2
		light.node.light_color = curve_color(entry._fxColor, fmod(channel_elapsed + elapsed, period) / period)
		light.node.light_energy = light.energy
	if particle_clock >= 0.1:
		particle_clock = fmod(particle_clock,0.1)
		for emitter in emitters: spawn_spark(emitter.data)
	update_sparks(delta)

func vector_text(text: String) -> Vector3:
	var values = text.trim_prefix("(").trim_suffix(")").split(",")
	return Vector3(float(values[0]),float(values[1]),float(values[2])) if values.size()==3 else Vector3.ZERO

func curve_scalar(text: String, t: float) -> float:
	if not scalar_curves.has(text):
		var regex = RegEx.new(); regex.compile("\\[([0-9.]+):(-?[0-9.]+):[A-Z]\\]")
		scalar_curves[text] = regex.search_all(text)
	var keys: Array = scalar_curves[text]
	if keys.is_empty(): return float(text) if text.is_valid_float() else 0.0
	var previous = keys[0]
	for key in keys:
		if t <= float(key.get_string(1)):
			var span = float(key.get_string(1)) - float(previous.get_string(1))
			return lerpf(float(previous.get_string(2)),float(key.get_string(2)),clampf((t-float(previous.get_string(1)))/span,0,1)) if span>0 else float(key.get_string(2))
		previous = key
	return float(keys[-1].get_string(2))

func curve_color(text: String, t: float) -> Color:
	if not color_curves.has(text):
		var regex = RegEx.new(); regex.compile("\\[([0-9.]+):#([0-9.,]+):[A-Z]\\]")
		color_curves[text] = regex.search_all(text)
	var keys: Array = color_curves[text]
	if keys.is_empty(): return Color.WHITE
	var previous = keys[0]
	for key in keys:
		if t <= float(key.get_string(1)):
			var span = float(key.get_string(1))-float(previous.get_string(1))
			return color_text(previous.get_string(2)).lerp(color_text(key.get_string(2)),clampf((t-float(previous.get_string(1)))/span,0,1)) if span>0 else color_text(key.get_string(2))
		previous = key
	return color_text(keys[-1].get_string(2))

func color_text(text: String) -> Color:
	var v = text.split(","); return Color(float(v[0]),float(v[1]),float(v[2]),float(v[3]))

func draw_beam(beam: Dictionary, delta: float):
	var entry: Dictionary = beam.data
	var origin = bone_world(target_rig if beam.body else caster,entry._fxAttachBone).origin
	var end = aim_point()
	if beam.body:
		for dummy in config.body._fxModelList:
			if dummy._fxName == entry._fxTargetName: end = bone_world(target_rig,dummy._fxAttachBone).origin
	var length = origin.distance_to(end)
	if length < 0.00001: return
	var direction = (end-origin).normalized()
	var side = direction.cross(Vector3.UP).normalized()
	if side.length_squared()<0.001: side=Vector3.RIGHT
	var up = side.cross(direction).normalized()
	beam.next -= delta
	if beam.next <= 0 or beam.points.is_empty():
		beam.next = rng.randf_range(0.1,0.6) if not beam.body else rng.randf_range(0.09,0.11)
		beam.points.clear()
		var count = clampi(int(ceil(length / maxf(0.002,float(entry._fxTessellationDist)*fx_scale))),8,128)
		for i in range(count+1): beam.points.append(Vector2(rng.randf_range(-1,1),rng.randf_range(-1,1)))
	var points: Array[Vector3] = []
	for i in beam.points.size():
		var t = float(i)/(beam.points.size()-1)
		var envelope = curve_scalar(entry._fxEnvelopeShape,t)*float(entry._fxEnvelopeMaxRadius)*fx_scale
		var offset: Vector2 = beam.points[i]
		points.append(origin.lerp(end,t) + (side*offset.x + up*offset.y)*envelope*sin(t*PI))
	points[0]=origin; points[-1]=end
	beam.uv += delta * (0.0 if beam.body else 1.0)
	var mesh: ImmediateMesh = beam.node.mesh; mesh.clear_surfaces()
	mesh.surface_begin(Mesh.PRIMITIVE_TRIANGLE_STRIP,body_material if beam.body else bolt_material)
	var camera = get_viewport().get_camera_3d()
	for i in points.size():
		var t = float(i)/(points.size()-1)
		var facing = (camera.global_position-points[i]).normalized() if camera else Vector3.BACK
		var across = direction.cross(facing).normalized()
		var width = lerpf(float(entry._fxLightningMinWidth),float(entry._fxLightningMaxWidth),curve_scalar(entry._fxLightningShape,t))*fx_scale
		var tint = curve_color(entry._fxLightningLengthColor,t)
		mesh.surface_set_color(tint)
		mesh.surface_set_uv(Vector2(0,t*length/maxf(0.001,float(entry._fxTextureTileDist)*fx_scale)+beam.uv))
		mesh.surface_add_vertex(to_local(points[i]-across*width*0.5))
		mesh.surface_set_color(tint)
		mesh.surface_set_uv(Vector2(1,t*length/maxf(0.001,float(entry._fxTextureTileDist)*fx_scale)+beam.uv))
		mesh.surface_add_vertex(to_local(points[i]+across*width*0.5))
	mesh.surface_end()
	beam.node.set_meta("start",origin); beam.node.set_meta("end",end)

func spawn_spark(entry: Dictionary):
	var node = MeshInstance3D.new(); var quad = QuadMesh.new()
	var size = rng.randf_range(0.016,0.03); quad.size = Vector2.ONE*size
	node.mesh = quad; node.material_override = ring_material
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(node)
	sparks.append({"node":node,"bone":entry._fxAttachBone,"offset":vector_text(entry._fxAttachPosition),
		"age":0.0,"life":rng.randf_range(0.05,0.25),"rotation":rng.randf_range(-PI,PI),"size":size})

func update_sparks(delta: float):
	for i in range(sparks.size()-1,-1,-1):
		var spark: Dictionary = sparks[i]; spark.age += delta
		if spark.age>=spark.life:
			spark.node.queue_free(); sparks.remove_at(i); continue
		var pose = bone_world(target_rig,spark.bone)
		spark.node.global_position = pose * spark.offset
		var camera = get_viewport().get_camera_3d()
		if camera: spark.node.global_basis = camera.global_basis; spark.node.rotate_object_local(Vector3.FORWARD,spark.rotation)
		var ratio = spark.age/spark.life
		spark.node.scale = Vector3.ONE*lerpf(0.8,1.2,ratio)
		spark.node.transparency = ratio

func clear_visuals(immediate: bool):
	for beam in beams: beam.node.free()
	beams.clear(); emitters.clear()
	for spark in sparks: spark.node.free()
	sparks.clear()
	if immediate:
		for light in lights: light.node.free()
		lights.clear()

func wav(mid: int, looping: bool = false) -> AudioStreamWAV:
	var stream = load("res://Assets/Lightning/Audio/%d.wav" % mid).duplicate() as AudioStreamWAV
	if looping:
		stream.loop_mode = AudioStreamWAV.LOOP_FORWARD
		stream.loop_begin = 0; stream.loop_end = roundi(stream.get_length()*stream.mix_rate)
	return stream

func sequence(ids: Array, looping: bool) -> AudioStreamPlaylist:
	var order = ids.duplicate()
	for i in range(order.size()-1,0,-1):
		var j = rng.randi_range(0,i); var value = order[i]; order[i]=order[j]; order[j]=value
	var stream = AudioStreamPlaylist.new(); stream.stream_count = order.size()
	stream.loop = looping; stream.fade_time = 0.0; stream.shuffle = false
	for i in order.size(): stream.set_list_stream(i,wav(int(order[i])))
	return stream

func play_audio(index: int, stream: AudioStream, gain: float, pitch_cents: float):
	var mixer = get_tree().get_first_node_in_group("b7_audio")
	if mixer and not mixer.call("PermitCombat", "lightning/%d" % index): return
	audio_base[index]=gain; audio[index].stream=stream; audio[index].volume_db=gain
	audio[index].pitch_scale=pow(2.0,pitch_cents/1200.0); audio[index].play()

func start_audio():
	var casts = [43766601,60467976,676198]
	var eligible = [0,1,2]; eligible.erase(last_cast)
	var choice: int = eligible[rng.randi_range(0,eligible.size()-1)]
	last_cast=choice
	play_audio(0,wav(casts[choice]),0,rng.randf_range(-100,100))
	var grains=[3124562,33815473,58948851,925098,2005362,6921508,30550027,10503425,47992697,17811174,9282082,92890106,37764586,26605792]
	play_audio(1,sequence(grains,true),-6,-560+rng.randf_range(-150,150))
	play_audio(2,wav(51508336,true),0,-500+rng.randf_range(-100,150))
	if is_instance_valid(target_rig):
		aftershock_clock=0.0; aftershock_history.clear(); trigger_aftershock()
		play_audio(11,wav(grains[rng.randi_range(0,13)]),-6+rng.randf_range(-4,2),rng.randf_range(-150,150))

func trigger_aftershock():
	# Original TriggerRate=200 ms, equal weights, avoid previous two selections.
	var candidates: Array[int] = [135833827,8262027,32918848,191183975,43710584,57320707]
	for previous in aftershock_history: candidates.erase(previous)
	var selected = candidates[rng.randi_range(0,candidates.size()-1)]
	aftershock_history.append(selected)
	if aftershock_history.size()>2: aftershock_history.pop_front()
	for i in range(3,11):
		if not audio[i].playing:
			play_audio(i,wav(selected),-9+rng.randf_range(-5,0),rng.randf_range(-75,100)); return

func snapshot() -> Dictionary:
	var playing = 0
	for sound in audio:
		if sound.playing: playing += 1
	return {"phase":phase,"active":active,"beams":beams.size(),"sparks":sparks.size(),"emitters":emitters.size(),"lights":lights.size(),"audioPlaying":playing}
