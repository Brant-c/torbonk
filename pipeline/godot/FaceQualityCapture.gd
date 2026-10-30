extends SceneTree

const OUTPUT = "C:/Users/brant/.codex/visualizations/2026/10/30/01a18a43-a5bb-74e1-97f4-da0da7d0d50e"

func _initialize():
	call_deferred("capture")

func capture():
	root.size = Vector2i(600,600)
	var receipt = JSON.parse_string(FileAccess.get_file_as_string("res://../manifests/face-quality.json"))
	var original = ""
	for entry in receipt.assets:
		if entry.path.replace("\\", "/") == "godot/Assets/sorcerer.glb": original = entry.before
	assert(original != "")
	var world = Node3D.new()
	root.add_child(world)
	var env = WorldEnvironment.new()
	env.environment = Environment.new()
	env.environment.background_mode = Environment.BG_COLOR
	env.environment.background_color = Color("26313d")
	env.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.environment.ambient_light_color = Color.WHITE
	env.environment.ambient_light_energy = 0.4
	world.add_child(env)
	for setting in [[Vector3(-25,145,0),0.8],[Vector3(-10,-40,0),0.25]]:
		var light = DirectionalLight3D.new()
		light.rotation_degrees = setting[0];light.light_energy = setting[1];world.add_child(light)
	var camera = Camera3D.new()
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL;camera.size = 0.048
	camera.near = 0.001;camera.far = 2;world.add_child(camera)
	var kinds = ["current", "rest-face"] if "--facial-probe" in OS.get_cmdline_user_args() else ["before", "after"]
	for kind in kinds:
		var doc = GLTFDocument.new()
		var state = GLTFState.new()
		var path = OUTPUT + "/face-quality-backup/" + original + ".glb" if kind == "before" else ProjectSettings.globalize_path("res://Assets/sorcerer.glb")
		assert(doc.append_from_file(path,state) == OK)
		var model = doc.generate_scene(state) if kind == "before" else load("res://Assets/sorcerer.glb").instantiate()
		world.add_child(model)
		var player = model.find_children("*","AnimationPlayer",true,false)[0]
		player.play("Idle");player.advance(0);player.pause()
		var skeleton = model.find_children("*","Skeleton3D",true,false)[0]
		if kind != "before":
			for clip in ["Idle", "Run", "LightningStart", "LightningLoop", "Hit", "Death", "LightningEnd"]:
				if not player.has_animation(clip): continue
				player.play(clip);player.advance(player.get_animation(clip).length * 0.5);player.pause()
				for bone in range(skeleton.get_bone_count()):
					if skeleton.get_bone_name(bone).to_lower().begins_with("fc_"):
						assert(skeleton.get_bone_pose_position(bone).distance_to(skeleton.get_bone_rest(bone).origin) < 0.000001, clip + ": facial position differs from source rest")
			player.play("Idle");player.advance(0);player.pause()
		if kind == "rest-face":
			for bone in range(skeleton.get_bone_count()):
				if skeleton.get_bone_name(bone).to_lower().begins_with("fc_"):
					skeleton.set_bone_pose_position(bone,skeleton.get_bone_rest(bone).origin)
		var focus = skeleton.global_transform * skeleton.get_bone_global_pose(skeleton.find_bone("Head")).origin + Vector3(0,0.006,0)
		camera.position = focus + Vector3(0.016,0.003,-0.14);camera.look_at(focus)
		for frame in range(8): await process_frame
		await RenderingServer.frame_post_draw
		assert(root.get_texture().get_image().save_png(OUTPUT + "/face-" + kind + ".png") == OK)
		world.remove_child(model);model.free()
	print("FACE_QUALITY_CAPTURE_PASSED")
	quit()
