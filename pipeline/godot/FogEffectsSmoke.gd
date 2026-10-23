extends SceneTree
## Focused renderer check: black texels of an additive effect must contribute no fog-colored card.
func _initialize():
	call_deferred("run")
func run():
	root.size=Vector2i(640,360)
	var stage=Node3D.new();root.add_child(stage)
	var camera=Camera3D.new();stage.add_child(camera);camera.position=Vector3(0,0,5);camera.projection=Camera3D.PROJECTION_ORTHOGONAL;camera.size=4;camera.current=true
	var atmosphere=WorldEnvironment.new();stage.add_child(atmosphere)
	var env=Environment.new();env.background_mode=Environment.BG_COLOR;env.background_color=Color(.04,.04,.04)
	env.fog_enabled=true;env.fog_density=.1;env.fog_light_color=Color(.634,.658,.302);atmosphere.environment=env
	var backdrop=MeshInstance3D.new();backdrop.mesh=QuadMesh.new();backdrop.mesh.size=Vector2(8,5)
	var base=StandardMaterial3D.new();base.shading_mode=BaseMaterial3D.SHADING_MODE_UNSHADED;base.albedo_color=Color(.08,.08,.08);backdrop.material_override=base;stage.add_child(backdrop)
	var pixels=Image.create(64,64,false,Image.FORMAT_RGBA8);pixels.fill(Color.BLACK)
	for x in range(32,64):
		for y in range(64):pixels.set_pixel(x,y,Color.WHITE)
	var effect=MeshInstance3D.new();effect.mesh=QuadMesh.new();effect.mesh.size=Vector2(1.4,1.4);effect.position.z=.02
	var material=ShaderMaterial.new();material.shader=load("res://Gameplay/D3Particle.gdshader");material.set_shader_parameter("source_texture",ImageTexture.create_from_image(pixels));material.set_shader_parameter("billboard",false)
	effect.material_override=material;effect.cast_shadow=GeometryInstance3D.SHADOW_CASTING_SETTING_OFF;stage.add_child(effect)
	for i in range(8):await process_frame
	await RenderingServer.frame_post_draw
	var rendered=root.get_texture().get_image()
	var black_uv=camera.unproject_position(Vector3(-.35,0,.02));var control_uv=camera.unproject_position(Vector3(-1,0,0));var white_uv=camera.unproject_position(Vector3(.35,0,.02))
	var dark=rendered.get_pixelv(Vector2i(black_uv));var control=rendered.get_pixelv(Vector2i(control_uv));var bright=rendered.get_pixelv(Vector2i(white_uv))
	var error=max(abs(dark.r-control.r),max(abs(dark.g-control.g),abs(dark.b-control.b)))
	var passed=error<.025 and bright.r>control.r+.15
	var receipt={"passed":passed,"fogDensity":env.fog_density,"blackTexelError":error,"background":str(control),"blackTexel":str(dark),"litTexel":str(bright)}
	var file=FileAccess.open("res://../manifests/fog-effects-smoke.json",FileAccess.WRITE);file.store_string(JSON.stringify(receipt,"  "));file.close()
	rendered.save_png("res://../manifests/fog-effects-smoke.png")
	print("FOG_EFFECTS_SMOKE_PASSED" if passed else "FOG_EFFECTS_SMOKE_FAILED")
	quit(0 if passed else 1)
