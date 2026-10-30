extends Node3D
## Environment-only inspection. No encounter scripts or progression changes.
func _ready():
	var camera = Camera3D.new()
	add_child(camera)
	camera.position = Vector3(82.5, -33, 72)
	camera.look_at(Vector3(82.5, -38.7, 69))
	camera.near = 0.01
	camera.current = true
	if "--soa-shaft-capture" in OS.get_cmdline_user_args():
		camera.position = Vector3(82.5,-32,69)
		camera.look_at(Vector3(83,-8,70))
	add_child(load("res://Assets/Maps/soa_p3/RoomAtmosphere.tscn").instantiate())
	if "--soa-capture" in OS.get_cmdline_user_args() or "--soa-shaft-capture" in OS.get_cmdline_user_args():
		for frame in range(8):
			await get_tree().process_frame
		await RenderingServer.frame_post_draw
		var output = "soa-p3-shaft.png" if "--soa-shaft-capture" in OS.get_cmdline_user_args() else "soa-p3-import.png"
		get_viewport().get_texture().get_image().save_png("res://../manifests/"+output)
		get_tree().quit()
	if "--soa-import-check" in OS.get_cmdline_user_args():
		await get_tree().physics_frame
		await get_tree().physics_frame
		var query = PhysicsRayQueryParameters3D.create(Vector3(82.5,-37,69), Vector3(82.5,-41,69))
		var hit = get_world_3d().direct_space_state.intersect_ray(query)
		print("SOA_P3_IMPORT_CHECK floor=", hit.get("position", "MISSING"))
		get_tree().quit(1 if hit.is_empty() else 0)
