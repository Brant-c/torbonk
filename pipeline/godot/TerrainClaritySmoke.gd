extends SceneTree
## Render every new shader, then capture identical Korriban views before/after.
func _initialize():
	call_deferred("run")

func frames(n: int):
	for i in range(n): await process_frame

func run():
	root.size = Vector2i(1280,720)
	var world = Node3D.new()
	root.add_child(world)
	var env = WorldEnvironment.new()
	env.environment = Environment.new()
	env.environment.background_mode = Environment.BG_COLOR
	env.environment.background_color = Color(0.12,0.16,0.2)
	env.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.environment.ambient_light_color = Color(0.77,0.62,0.56)
	env.environment.ambient_light_energy = 0.65
	world.add_child(env)
	var light = DirectionalLight3D.new()
	light.rotation_degrees = Vector3(-38,-32,0)
	light.light_color = Color(1,0.78,0.57)
	light.light_energy = 1.1
	world.add_child(light)
	var camera = Camera3D.new()
	world.add_child(camera)
	camera.position = Vector3(0,2,2)
	camera.look_at(Vector3.ZERO)
	var plane = MeshInstance3D.new()
	plane.mesh = PlaneMesh.new()
	world.add_child(plane)
	var registry = JSON.parse_string(FileAccess.get_file_as_string("res://Assets/TerrainClarity/materials.json"))
	var checked = 0
	for map in registry:
		for id in registry[map]:
			var material = load(registry[map][id])
			assert(material is ShaderMaterial)
			plane.material_override = material
			await frames(3)
			checked += 1
	plane.queue_free()
	await frames(1)
	var terrain = load("res://Assets/Korriban/D4/KorribanTrials.tscn").instantiate()
	world.add_child(terrain)
	camera.position = Vector3(36,-15.9,14.0)
	camera.look_at(Vector3(36,-17.1,12.6))
	camera.near = 0.01
	camera.fov = 52
	await frames(12)
	root.get_texture().get_image().save_png("res://../generated/terrain-clarity-before.png")
	var applied = 0
	for mesh in terrain.find_children("Terrain_*","MeshInstance3D",true,false):
		var id = str(mesh.name).trim_prefix("Terrain_")
		if registry.korriban.has(id):
			mesh.set_surface_override_material(0,load(registry.korriban[id]))
			applied += 1
	assert(applied == 6)
	await frames(12)
	root.get_texture().get_image().save_png("res://../generated/terrain-clarity-after.png")
	var receipt = {"passed":true,"shadersRendered":checked,"korribanPatches":applied,
		"msaa3d":root.msaa_3d,"captureSize":str(root.size)}
	var file = FileAccess.open("res://../manifests/terrain-clarity-smoke.json",FileAccess.WRITE)
	file.store_string(JSON.stringify(receipt,"\t"))
	print("TERRAIN_CLARITY_SMOKE ",receipt)
	quit()
