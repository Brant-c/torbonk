extends Node3D
func _ready():
	var names = ["manka","rakata","watcher","acklay","soa","luthro","alarii","fahren","yshaar","zaheen","kahesh","jael","serrod"]
	for i in range(names.size()):
		var actor = load("res://Assets/Maps/soa_p3/Enemies/"+names[i]+".glb").instantiate()
		add_child(actor)
		actor.position = Vector3((i%4-1.5)*0.4,(3-i/4)*0.36,0)
		var animation = actor.find_children("*","AnimationPlayer",true,false)[0]
		animation.get_animation("Idle").loop_mode = Animation.LOOP_LINEAR
		animation.play("Idle")
		var label = Label3D.new()
		label.text = names[i]
		label.pixel_size = 0.0005
		label.font_size = 24
		add_child(label)
		label.position = actor.position+Vector3(0,-0.025,0)
	var camera = Camera3D.new()
	add_child(camera)
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = 1.65
	camera.position = Vector3(0,0.67,3)
	camera.look_at(Vector3(0,0.67,0))
	camera.current = true
	var world = WorldEnvironment.new()
	world.environment = Environment.new()
	world.environment.background_mode = Environment.BG_COLOR
	world.environment.background_color = Color(0.04,0.05,0.07)
	world.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	world.environment.ambient_light_color = Color.WHITE
	world.environment.ambient_light_energy = 0.7
	add_child(world)
	var light = DirectionalLight3D.new()
	add_child(light)
	light.rotation_degrees = Vector3(-30,-30,0)
	for frame in range(30):await get_tree().process_frame
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png("res://../manifests/soa-roster-review.png")
	get_tree().quit()
