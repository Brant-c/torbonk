extends SceneTree
func _initialize():
	call_deferred("run")
func run():
	var failures=[]
	var checked=0
	var base="res://Assets/Maps/eternity_vault/"
	for file in DirAccess.get_files_at(base):
		if not file.ends_with(".png") or not "_weights" in file: continue
		var source=Image.new()
		source.load_png_from_buffer(FileAccess.get_file_as_bytes(base+file))
		var imported=load(base+file).get_image()
		if imported.is_compressed(): imported.decompress()
		source.convert(Image.FORMAT_RGBA8)
		imported.convert(Image.FORMAT_RGBA8)
		if source.get_data()!=imported.get_data(): failures.append(file)
		checked+=1
	var data=JSON.parse_string(FileAccess.get_file_as_string(base+"environment.json"))
	var scene=load(base+"MapEnvironment.tscn").instantiate()
	root.add_child(scene)
	var colors=0
	for patch in data.terrain:
		var mesh=scene.find_child("Terrain_"+patch.id,true,false)
		var arrays=mesh.mesh.surface_get_arrays(0)
		var valid=arrays[Mesh.ARRAY_COLOR] is PackedColorArray and arrays[Mesh.ARRAY_COLOR].size()==arrays[Mesh.ARRAY_VERTEX].size()
		if valid:
			for j in arrays[Mesh.ARRAY_TEX_UV].size():
				var uv=arrays[Mesh.ARRAY_TEX_UV][j]
				var index=int(round(uv.y*(patch.depth-1)))*int(patch.width)+int(round(uv.x*(patch.width-1)))
				var expected=patch.vertexColors[index]
				var actual=arrays[Mesh.ARRAY_COLOR][j]
				if abs(actual.r-expected[0]/255.0)>0.005 or abs(actual.g-expected[1]/255.0)>0.005 or abs(actual.b-expected[2]/255.0)>0.005: valid=false
		if not valid:
			failures.append("vertex tint "+patch.id)
		else: colors+=1
	var passed=failures.is_empty() and checked>=17 and colors==17
	var rendered=0
	if DisplayServer.get_name()!="headless":
		root.size=Vector2i(800,600)
		scene.queue_free()
		await process_frame
		var world=Node3D.new()
		root.add_child(world)
		var env=WorldEnvironment.new()
		env.environment=Environment.new()
		env.environment.ambient_light_source=Environment.AMBIENT_SOURCE_COLOR
		env.environment.ambient_light_color=Color.WHITE
		env.environment.ambient_light_energy=0.6
		world.add_child(env)
		var light=DirectionalLight3D.new()
		light.rotation_degrees=Vector3(-45,-20,0)
		world.add_child(light)
		var camera=Camera3D.new()
		world.add_child(camera)
		camera.position=Vector3(0,9,7)
		camera.look_at(Vector3.ZERO)
		for patch in data.terrain:
			var plane=MeshInstance3D.new()
			plane.mesh=PlaneMesh.new()
			plane.material_override=load(patch.detailMaterial)
			plane.position=Vector3((rendered%5-2)*2.05,0,(rendered/5-1.5)*2.05)
			world.add_child(plane)
			rendered+=1
		for i in 12: await process_frame
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png("res://../manifests/ev-terrain-materials.png")
	var receipt={"passed":passed,"importedBlendMapsExact":checked,"vertexTintPatches":colors,"renderedMaterials":rendered,"failures":failures}
	var f=FileAccess.open("res://../manifests/ev-blend-data-smoke.json",FileAccess.WRITE)
	f.store_string(JSON.stringify(receipt,"  "))
	print("EV_BLEND_DATA ",receipt)
	quit(0 if passed else 1)
