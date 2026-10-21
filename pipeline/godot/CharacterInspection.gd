extends Node3D
## Isolated asset inspector. Source scale, root tracks and imported scene retained.
var player: AnimationPlayer
var skeleton: Skeleton3D
var camera: Camera3D
var status: Label
var debug_mesh: MeshInstance3D
var clips = ["Idle", "Run", "LightningStart", "LightningLoop", "Hit", "Death", "LightningEnd"]
var current_clip = "Idle"
var yaw = PI + 0.25
var pitch = 0.12
var radius = 0.43
var center = Vector3(0, 0.11, 0)
var paused = false
var show_bones = false
var capture_mode = false
var recipe: Dictionary
var expected: Dictionary
var mesh_nodes: Array[MeshInstance3D] = []

func collect(node: Node):
	if node is AnimationPlayer: player = node
	if node is Skeleton3D: skeleton = node
	if node is MeshInstance3D: mesh_nodes.append(node)
	for child in node.get_children(): collect(child)

func _ready():
	collect($OriginalSorcerer)
	if player == null or skeleton == null:
		fail("Imported AnimationPlayer or Skeleton3D missing"); return
	player.animation_finished.connect(func(_name): paused = true)
	recipe = JSON.parse_string(FileAccess.get_file_as_string("res://Assets/character-recipe.json"))
	expected = JSON.parse_string(FileAccess.get_file_as_string("res://Assets/character-validation-input.json"))
	for info in recipe.animations:
		if not player.has_animation(info.name): fail("Missing clip " + info.name); return
		player.get_animation(info.name).loop_mode = Animation.LOOP_LINEAR if info.loop else Animation.LOOP_NONE
	var environment = WorldEnvironment.new()
	environment.environment = Environment.new()
	environment.environment.background_mode = Environment.BG_COLOR
	environment.environment.background_color = Color(0.055, 0.065, 0.085)
	environment.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.environment.ambient_light_color = Color(0.8, 0.85, 1)
	environment.environment.ambient_light_energy = 0.65
	add_child(environment)
	for info in [[Vector3(-40, -30, 0), 1.3], [Vector3(-20, 140, 0), 0.8]]:
		var light = DirectionalLight3D.new(); light.rotation_degrees = info[0]; light.light_energy = info[1]; add_child(light)
	camera = Camera3D.new(); camera.current = true; camera.fov = 40; camera.near = 0.001; camera.far = 10; add_child(camera)
	update_camera()
	debug_mesh = MeshInstance3D.new(); debug_mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF; add_child(debug_mesh)
	var overlay = CanvasLayer.new(); add_child(overlay)
	var panel = VBoxContainer.new(); panel.position = Vector2(18, 16); overlay.add_child(panel)
	var title = Label.new(); title.text = "SWTOR SORCERER · Original April2012 assets · Human male type 2"; panel.add_child(title)
	var row = HBoxContainer.new(); panel.add_child(row)
	for clip in clips:
		var button = Button.new(); button.text = clip; button.pressed.connect(func(): play_clip(clip)); row.add_child(button)
	var controls = Label.new()
	controls.text = "1–7: clips  |  Space: pause  |  B: bind pose  |  S: bones  |  R: restart\nDrag: orbit  |  Wheel: zoom  |  Left/Right: step 1/30 s when paused\nSkeletal root retained. Separate JBA trajectory is logged. No cloth physics."
	panel.add_child(controls)
	status = Label.new(); panel.add_child(status)
	var args = OS.get_cmdline_user_args()
	capture_mode = "--character-capture" in args
	if "--character-validate" in args:
		validate_character(); return
	if capture_mode:
		await capture_sequence(); return
	play_clip("Idle")

func fail(message: String):
	push_error(message); get_tree().quit(1)

func play_clip(clip: String):
	current_clip = clip; paused = false; player.play(clip); player.seek(0, true); player.advance(0)

func bind_pose():
	player.stop(); skeleton.reset_bone_poses(); paused = true; current_clip = "Bind pose"

func update_camera():
	camera.position = center + Vector3(sin(yaw) * cos(pitch), sin(pitch), cos(yaw) * cos(pitch)) * radius
	camera.look_at(center)

func _process(_delta):
	if player == null or status == null: return
	status.text = "%s  ·  %.3f / %.3f s  ·  %d bones  ·  %s" % [current_clip, player.current_animation_position if current_clip in clips else 0.0, player.get_animation(current_clip).length if current_clip in clips else 0.0, skeleton.get_bone_count(), "PAUSED" if paused else "PLAYING"]
	debug_mesh.visible = show_bones
	if show_bones: draw_bones()

func draw_bones():
	var lines = ImmediateMesh.new(); var material = StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED; material.albedo_color = Color(0.2, 1, 0.6)
	material.no_depth_test = true
	lines.surface_begin(Mesh.PRIMITIVE_LINES, material)
	for i in skeleton.get_bone_count():
		var parent = skeleton.get_bone_parent(i)
		if parent < 0: continue
		lines.surface_add_vertex(skeleton.global_transform * skeleton.get_bone_global_pose(parent).origin)
		lines.surface_add_vertex(skeleton.global_transform * skeleton.get_bone_global_pose(i).origin)
	lines.surface_end(); debug_mesh.mesh = lines

func _unhandled_input(event):
	if capture_mode or camera == null: return
	if event is InputEventMouseMotion and Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		yaw -= event.relative.x * 0.006; pitch = clamp(pitch + event.relative.y * 0.006, -1.3, 1.3); update_camera()
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP: radius = max(0.06, radius * 0.9)
		if event.button_index == MOUSE_BUTTON_WHEEL_DOWN: radius = min(1.5, radius * 1.1)
		update_camera()
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode >= KEY_1 and event.keycode <= KEY_7: play_clip(clips[event.keycode - KEY_1])
		if event.keycode == KEY_SPACE:
			if current_clip == "Bind pose": play_clip("Idle")
			else:
				paused = not paused
				if paused: player.pause()
				else: player.play(current_clip)
		if event.keycode == KEY_R: play_clip(current_clip if current_clip in clips else "Idle")
		if event.keycode == KEY_B: bind_pose()
		if event.keycode == KEY_S: show_bones = not show_bones
		if paused and event.keycode in [KEY_LEFT, KEY_RIGHT] and current_clip in clips:
			player.seek(clamp(player.current_animation_position + (-1.0 if event.keycode == KEY_LEFT else 1.0) / 30.0, 0, player.get_animation(current_clip).length), true)

func from_blender(values): return Vector3(values[0], values[2], -values[1])

func validate_character():
	var result = {"engine": Engine.get_version_info().string, "bones": skeleton.get_bone_count(), "meshes": mesh_nodes.size(), "clips": [], "maxBonePositionError": 0.0, "maxBoneRotationRadians": 0.0, "materials": [], "passed": false}
	if skeleton.get_bone_count() != expected.boneCount: fail("Bone count changed"); return
	for bone in expected.bones:
		var i = skeleton.find_bone(bone.name)
		if i < 0: fail("Lost bone " + bone.name); return
		var parent = skeleton.get_bone_parent(i)
		var parent_name = skeleton.get_bone_name(parent) if parent >= 0 else null
		if parent_name != bone.parent: fail("Hierarchy changed at " + bone.name); return
	for mesh in mesh_nodes:
		if mesh.skin == null: fail("Unskinned mesh " + mesh.name); return
		for surface in mesh.mesh.get_surface_count():
			var material = mesh.get_active_material(surface) as StandardMaterial3D
			if material == null or material.albedo_texture == null or material.normal_texture == null or material.roughness_texture == null:
				fail("Texture assignment missing: " + mesh.name); return
			result.materials.append({"mesh":mesh.name,"material":material.resource_name,"albedo":true,"normal":true,"roughness":true})
	for info in recipe.animations:
		var animation = player.get_animation(info.name)
		if abs(animation.length - info.duration) > 0.034: fail("Duration changed " + info.name); return
		play_clip(info.name)
		var worst = {"position":0.0,"rotation":0.0,"bone":"","rotationBone":"","time":0.0}
		for sample in expected.samples[info.name]:
			player.seek(min(sample.time, animation.length), true); player.advance(0)
			skeleton.force_update_all_bone_transforms()
			for bone in sample.bones:
				var pose = skeleton.get_bone_global_pose(skeleton.find_bone(bone.name))
				var error = pose.origin.distance_to(from_blender(bone.position))
				result.maxBonePositionError = max(result.maxBonePositionError, error)
				# glTF exports bone axes with its bone basis correction. World
				# positions and each bone basis column convert from Blender Y/Z.
				var basis = Basis(from_blender(bone.x),from_blender(bone.y),from_blender(bone.z))
				var angle = pose.basis.get_rotation_quaternion().angle_to(basis.get_rotation_quaternion())
				if error > worst.position: worst.position=error;worst.bone=bone.name;worst.time=sample.time
				if angle > worst.rotation: worst.rotation=angle;worst.rotationBone=bone.name
				result.maxBoneRotationRadians = max(result.maxBoneRotationRadians, angle)
		result.clips.append({"name":info.name,"duration":animation.length,"sourceDuration":info.duration,"tracks":animation.get_track_count(),"samples":5,"worst":worst})
	# glTF is sampled at 60 fps; source clip durations are preserved.
	if result.maxBonePositionError > 0.0001 or result.maxBoneRotationRadians > 0.003:
		FileAccess.open("res://../manifests/character-godot-validation.json",FileAccess.WRITE).store_string(JSON.stringify(result,"\t"))
		fail("Godot pose differs from Blender samples: " + str(result.maxBonePositionError) + " / " + str(result.maxBoneRotationRadians)); return
	result.passed = true
	FileAccess.open("res://../manifests/character-godot-validation.json",FileAccess.WRITE).store_string(JSON.stringify(result,"\t"))
	print("CHARACTER_GODOT_VALIDATION_PASSED ", JSON.stringify(result)); get_tree().quit()

func capture_sequence():
	for clip in clips:
		play_clip(clip)
		for frame in 12: await get_tree().process_frame
		player.pause(); paused = true
		player.seek(player.get_animation(clip).length * (0.9 if clip == "Death" else 0.5), true); player.advance(0)
		await RenderingServer.frame_post_draw
		var path = "res://../generated/character/godot-" + clip + ".png"
		if get_viewport().get_texture().get_image().save_png(path) != OK: fail("Capture failed"); return
	# Run every source clip in real time, retaining timed frames through its end.
	var playback_evidence = []
	for clip in clips:
		play_clip(clip)
		var captures = int(ceil(player.get_animation(clip).length / 0.2)) + 2
		for step in captures:
			await get_tree().create_timer(0.2).timeout
			await RenderingServer.frame_post_draw
			var path = "res://../generated/character/play-"+clip+"-%02d.png" % step
			if get_viewport().get_texture().get_image().save_png(path) != OK: fail("Playback capture failed"); return
			playback_evidence.append({"clip":clip,"step":step,"animationTime":player.current_animation_position,"playing":player.is_playing(),"image":path})
	bind_pose(); yaw = 0.25; update_camera()
	await get_tree().process_frame; await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png("res://../generated/character/godot-BindPose.png")
	show_bones = true; yaw = PI / 2; update_camera()
	await get_tree().process_frame; await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png("res://../generated/character/godot-BonesSide.png")
	FileAccess.open("res://../manifests/character-playback-captures.json",FileAccess.WRITE).store_string(JSON.stringify(playback_evidence,"\t"))
	get_tree().quit()
