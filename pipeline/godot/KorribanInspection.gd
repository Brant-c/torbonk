extends Node3D
## Standalone A4 inspector: navigation operates only the camera. No gameplay.
var environment_root: Node3D
var camera: Camera3D
var center = Vector3(36,1,12.5)
var yaw = 2.8
var pitch = 0.48
var radius = 49.0
var fly = false
var collision_lines: MeshInstance3D
var reference: Node3D
var status: Label
var light: DirectionalLight3D
var overlay: CanvasLayer
var failures: Array = []
func _ready():
	environment_root = load("res://Assets/Korriban/KorribanEnvironment.tscn").instantiate(); add_child(environment_root)
	var world = WorldEnvironment.new(); world.environment = Environment.new()
	world.environment.background_mode = Environment.BG_SKY
	var sky = Sky.new(); var sky_material = ProceduralSkyMaterial.new()
	sky_material.sky_top_color = Color(0.14,0.09,0.12); sky_material.sky_horizon_color = Color(0.64,0.37,0.22)
	sky_material.ground_bottom_color = Color(0.16,0.09,0.065); sky_material.ground_horizon_color = Color(0.64,0.37,0.22)
	sky.sky_material = sky_material; world.environment.sky = sky
	world.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	world.environment.ambient_light_color = Color(0.77,0.62,0.56); world.environment.ambient_light_energy = 0.65
	world.environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC; add_child(world)
	light = DirectionalLight3D.new(); light.rotation_degrees = Vector3(-38,-32,0)
	light.light_color = Color(1,0.78,0.57); light.light_energy = 1.1; light.shadow_enabled = true
	light.directional_shadow_max_distance = 100; add_child(light)
	var fill = DirectionalLight3D.new(); fill.rotation_degrees = Vector3(-25,145,0)
	fill.light_color = Color(0.52,0.48,0.65); fill.light_energy = 0.35; add_child(fill)
	camera = Camera3D.new(); camera.current = true; camera.fov = 52; camera.near = 0.015; camera.far = 200
	add_child(camera); update_camera()
	reference = load("res://Assets/sorcerer.glb").instantiate(); add_child(reference)
	# Inspection reference placement derived from an actual selected source vertex.
	var d = JSON.parse_string(FileAccess.get_file_as_string("res://Assets/Korriban/environment.json"))
	var patch = d.terrain[1]; var sample = patch.vertices[32*64+16]
	reference.position = Vector3(sample[0],sample[1],sample[2])+Vector3(patch.position[0],patch.position[1],patch.position[2])
	for node in descendants(reference):
		if node is AnimationPlayer:
			node.play("Idle"); node.advance(0)
	overlay = CanvasLayer.new(); add_child(overlay)
	var panel = VBoxContainer.new(); panel.position = Vector2(18,14); overlay.add_child(panel)
	var title = Label.new(); title.text = "KORRIBAN · Valley of the Dark Lords · Original April-profile assets"; panel.add_child(title)
	var subtitle = Label.new(); subtitle.text = "2 source patches · 109 placements · 31 shared models · Area 4611686022825732973 / baseroom"; panel.add_child(subtitle)
	var row = HBoxContainer.new(); panel.add_child(row)
	for info in [["Terrain",environment_root.get_node("Terrain")],["Props",environment_root.get_node("Props")],["Sorcerer 1:1",reference]]:
		var box = CheckButton.new(); box.text = info[0]; box.button_pressed = true
		var target: Node3D = info[1]; box.toggled.connect(func(on): target.visible = on); row.add_child(box)
	var debug = CheckButton.new(); debug.text = "Derived collision"; debug.toggled.connect(toggle_collision); row.add_child(debug)
	var controls = Label.new(); controls.text = "Drag: orbit · Middle/right drag: pan · Wheel: zoom · F: free-fly (WASD/QE, right-drag look)\n1: overview · 2: statue · 3: source terrain · 4: Sorcerer scale · Esc: orbit\nSource geometry/transforms retained. Portable materials and authored lighting approximate SWTOR."
	panel.add_child(controls); status = Label.new(); panel.add_child(status)
	if "--korriban-validate" in OS.get_cmdline_user_args() or "--korriban-capture" in OS.get_cmdline_user_args():
		await validate("--korriban-capture" in OS.get_cmdline_user_args())
func descendants(n: Node) -> Array:
	var a = [n]
	for c in n.get_children(): a.append_array(descendants(c))
	return a
func update_camera():
	if not fly:
		camera.position = center+Vector3(sin(yaw)*cos(pitch),sin(pitch),cos(yaw)*cos(pitch))*radius
		camera.look_at(center)
func _process(delta):
	if fly:
		var motion = Vector3(float(Input.is_physical_key_pressed(KEY_D))-float(Input.is_physical_key_pressed(KEY_A)),
			float(Input.is_physical_key_pressed(KEY_E))-float(Input.is_physical_key_pressed(KEY_Q)),
			float(Input.is_physical_key_pressed(KEY_S))-float(Input.is_physical_key_pressed(KEY_W)))
		camera.position += camera.basis*motion*delta*(8 if Input.is_physical_key_pressed(KEY_SHIFT) else 2)
	if status: status.text = ("Free-fly camera" if fly else "Orbit camera")+" · Source units 1:1 · No gameplay"
func _unhandled_input(event):
	if event is InputEventMouseMotion:
		if fly and Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
			camera.rotation.y -= event.relative.x*0.004; camera.rotation.x = clamp(camera.rotation.x-event.relative.y*0.004,-1.5,1.5)
		elif not fly and Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
			yaw -= event.relative.x*0.005; pitch = clamp(pitch+event.relative.y*0.005,-1.3,1.4); update_camera()
		elif not fly and (Input.is_mouse_button_pressed(MOUSE_BUTTON_MIDDLE) or Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT)):
			center += (-camera.basis.x*event.relative.x+camera.basis.y*event.relative.y)*radius*0.0015; update_camera()
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP: radius = max(0.25,radius*0.85)
		if event.button_index == MOUSE_BUTTON_WHEEL_DOWN: radius = min(150,radius/0.85)
		update_camera()
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_F: fly = not fly
		if event.keycode == KEY_ESCAPE: fly = false
		if event.keycode >= KEY_1 and event.keycode <= KEY_4: set_view(event.keycode-KEY_1)
func set_view(index: int):
	fly = false
	match index:
		0: center = Vector3(36,1,12.5); yaw = 2.8; pitch = 0.32; radius = 49
		1: center = Vector3(28,3,15); yaw = 2.8; pitch = 0.08; radius = 31
		2: center = Vector3(33,-14,12); yaw = 2.8; pitch = 0.9; radius = 17
		3: center = reference.position+Vector3(0,0.1,0); yaw = 2.8; pitch = 0.1; radius = 0.65
	update_camera()
func toggle_collision(on: bool):
	if not collision_lines and on:
		collision_lines = MeshInstance3D.new(); collision_lines.name = "DerivedTriangleDebug"; add_child(collision_lines)
		var im = ImmediateMesh.new(); im.surface_begin(Mesh.PRIMITIVE_LINES)
		for n in descendants(environment_root):
			if n is CollisionShape3D and n.shape is ConcavePolygonShape3D:
				var faces = n.shape.get_faces()
				for i in range(0,faces.size(),3):
					for edge in [[0,1],[1,2],[2,0]]:
						im.surface_add_vertex(n.global_transform*faces[i+edge[0]]); im.surface_add_vertex(n.global_transform*faces[i+edge[1]])
		im.surface_end(); collision_lines.mesh = im
		var mat = StandardMaterial3D.new(); mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		mat.albedo_color = Color(0.2,1,0.6); mat.no_depth_test = true; collision_lines.material_override = mat
	if collision_lines: collision_lines.visible = on
func validate(capture_mode: bool):
	await get_tree().process_frame
	var all = descendants(environment_root); var terrain_count = 0; var mesh_count = 0; var collision_count = 0
	var shared_meshes = {}
	for n in all:
		if n is MeshInstance3D:
			mesh_count += 1
			if n.name.begins_with("Terrain_"): terrain_count += 1
			else: shared_meshes[n.mesh.get_instance_id()] = true
			if not n.mesh: failures.append("missing mesh "+str(n.name))
		if n is CollisionShape3D: collision_count += 1
	if terrain_count != 2: failures.append("terrain count")
	if collision_count != mesh_count: failures.append("collision coverage")
	if shared_meshes.size() != 31: failures.append("shared mesh resource reuse")
	# Actual visibility/debug controls are exercised on the reloaded reusable scene.
	environment_root.get_node("Terrain").visible = false; await get_tree().process_frame
	if environment_root.get_node("Terrain").is_visible_in_tree(): failures.append("terrain toggle")
	environment_root.get_node("Terrain").visible = true
	environment_root.get_node("Props").visible = false; await get_tree().process_frame
	if environment_root.get_node("Props").is_visible_in_tree(): failures.append("props toggle")
	environment_root.get_node("Props").visible = true
	toggle_collision(true); toggle_collision(false)
	if not collision_lines or collision_lines.visible: failures.append("collision toggle")
	if capture_mode:
		overlay.visible = false
		for index in range(4):
			set_view(index); await get_tree().create_timer(0.2).timeout
			await RenderingServer.frame_post_draw
			var image = get_viewport().get_texture().get_image()
			if image.save_png("res://../generated/korriban/0"+str(index+1)+"-inspection.png") != OK: failures.append("capture")
	var result = {"passed":failures.is_empty(),"failures":failures,"terrainPatches":terrain_count,
		"sharedPropMeshResources":shared_meshes.size(),
		"meshInstances":mesh_count,"derivedCollisionShapes":collision_count,"captures":capture_mode,
		"engine":Engine.get_version_info().string,"renderer":RenderingServer.get_current_rendering_method()}
	var file = FileAccess.open("res://../manifests/korriban-godot-validation"+("-visual" if capture_mode else "")+".json",FileAccess.WRITE)
	file.store_string(JSON.stringify(result,"\t")); file.close()
	print("KORRIBAN_VALIDATION_PASSED" if failures.is_empty() else "KORRIBAN_VALIDATION_FAILED")
	get_tree().quit(0 if failures.is_empty() else 1)
