extends Node3D
## Environment-only inspection. No encounter scripts or progression changes.
func _ready():
	var camera = Camera3D.new()
	add_child(camera)
	camera.position = Vector3(82.5, -28, 78)
	camera.look_at(Vector3(82.5, -38.7, 69))
	camera.near = 0.01
	camera.current = true
	var light = DirectionalLight3D.new()
	add_child(light)
	light.rotation_degrees = Vector3(-55, -30, 0)
	light.light_energy = 1.2
	var world = WorldEnvironment.new()
	world.environment = Environment.new()
	world.environment.background_mode = Environment.BG_COLOR
	world.environment.background_color = Color(0.025, 0.02, 0.015)
	world.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	world.environment.ambient_light_color = Color(0.65, 0.60, 0.52)
	world.environment.ambient_light_energy = 0.6
	add_child(world)
	if "--soa-capture" in OS.get_cmdline_user_args():
		for frame in range(8):
			await get_tree().process_frame
		await RenderingServer.frame_post_draw
		get_viewport().get_texture().get_image().save_png("res://../manifests/soa-p3-import.png")
		get_tree().quit()
	if "--soa-import-check" in OS.get_cmdline_user_args():
		await get_tree().physics_frame
		await get_tree().physics_frame
		var query = PhysicsRayQueryParameters3D.create(Vector3(82.5,-37,69), Vector3(82.5,-41,69))
		var hit = get_world_3d().direct_space_state.intersect_ray(query)
		print("SOA_P3_IMPORT_CHECK floor=", hit.get("position", "MISSING"))
		get_tree().quit(1 if hit.is_empty() else 0)
