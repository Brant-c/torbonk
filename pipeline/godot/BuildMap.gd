extends SceneTree
## Offline scene compiler. Inputs come only from the bounded source conversion.
var data: Dictionary
var failures: Array = []
func check(label: String, ok: bool):
	if not ok: failures.append(label); push_error(label)
func v(a) -> Vector3: return Vector3(a[0],a[1],a[2])
var map_id: String
func _initialize():
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--map="): map_id = a.substr(6)
	if map_id == "": push_error("MAP_BUILD_FAILED no --map"); quit(1); return
	call_deferred("build")
func build():
	data = JSON.parse_string(FileAccess.get_file_as_string("res://Assets/Maps/"+map_id+"/environment.json"))
	var root = Node3D.new(); root.name = "MapEnvironment"; get_root().add_child(root)
	root.set_meta("area_id",data.area); root.set_meta("source_room",data.room); root.set_meta("map_id",map_id)
	root.set_meta("source_units","1:1; physical metres uncalibrated")
	var terrain = Node3D.new(); terrain.name = "Terrain"; root.add_child(terrain); terrain.owner = root
	var props = Node3D.new(); props.name = "Props"; root.add_child(props); props.owner = root
	var terrain_triangles = 0; var prop_instances = 0; var mesh_count = 0
	for p in data.terrain:
		var st = SurfaceTool.new(); st.begin(Mesh.PRIMITIVE_TRIANGLES)
		for i in p.indices:
			var index = int(i)
			st.set_uv(Vector2(float(index%int(p.width))/float(p.width-1),float(index/int(p.width))/float(p.depth-1)))
			st.set_smooth_group(0); st.add_vertex(v(p.vertices[index]))
		st.generate_normals(); st.generate_tangents(); st.index()
		var mesh = st.commit(); terrain_triangles += p.indices.size()/3
		var mat = StandardMaterial3D.new(); mat.resource_name = "SourceSplat_"+p.id
		mat.albedo_texture = load(p.maps.albedo); mat.normal_enabled = true; mat.normal_texture = load(p.maps.normal)
		mat.roughness_texture = load(p.maps.roughness); mat.roughness_texture_channel = BaseMaterial3D.TEXTURE_CHANNEL_RED
		mat.metallic_specular = 0.15
		mat.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC
		mesh.surface_set_material(0,mat)
		var node = MeshInstance3D.new(); node.name = "Terrain_"+p.id; node.mesh = mesh; node.position = v(p.position)
		node.set_meta("source_instance_id",p.id); node.set_meta("source_asset_id",p.asset)
		terrain.add_child(node); node.owner = root
		collision(node,root)
	var nodes = {}
	var conversion = JSON.parse_string(FileAccess.get_file_as_string("res://../manifests/map-"+map_id+"-model-conversion.json"))
	for p in data.placements:
		var node = Node3D.new(); node.name = "Instance_"+p.id
		node.set_meta("source_instance_id",p.id); node.set_meta("source_asset_id",p.asset)
		node.set_meta("source_parent_id",p.parent); node.set_meta("source_model",p.path)
		# Blender ZXY uses Ry * Rx * Rz; Godot's Euler label has different composition semantics.
		var r = v(p.rotation)*PI/180.0
		var basis = Basis(Vector3.UP,r.y)*Basis(Vector3.RIGHT,r.x)*Basis(Vector3.BACK,r.z)
		node.transform = Transform3D(basis.scaled_local(v(p.scale)),v(p.position))
		nodes[p.id] = node
	var pending: Array = data.placements.duplicate()
	while not pending.is_empty():
		var progress = false
		for p in pending.duplicate():
			var parent = props if p.parent == "0" else nodes[p.parent]
			if not parent.is_inside_tree(): continue
			parent.add_child(nodes[p.id]); nodes[p.id].owner = root
			pending.erase(p); progress = true
		if not progress:
			check("parent hierarchy cycle",false); quit(1); return
	for p in data.placements:
		var node = nodes[p.id]
		if data.modelOutputs.has(p.path) and p.fields.get("Hidden","false") != "true":
			var model = load(data.modelOutputs[p.path]).instantiate(); node.add_child(model); model.owner = root
			prop_instances += 1
			var model_info = null
			for info in conversion.models:
				if info.path == p.path: model_info = info; break
			var triangle_count = 0
			for mi in meshes(model):
				mesh_count += 1
				for s in mi.mesh.get_surface_count():
					var arrays = mi.mesh.surface_get_arrays(s)
					triangle_count += arrays[Mesh.ARRAY_INDEX].size()/3 if arrays[Mesh.ARRAY_INDEX] != null else arrays[Mesh.ARRAY_VERTEX].size()/3
					var m = mi.mesh.surface_get_material(s)
					check("material "+p.id+"/"+str(s),m is StandardMaterial3D and m.albedo_texture != null)
				collision(mi,root,node)
			check("source triangle count "+p.id,model_info != null and triangle_count == int(model_info.triangles))
	await process_frame
	var max_transform_error = 0.0
	for id in nodes:
		var t: Transform3D = nodes[id].global_transform
		var m = data.expectedMatrices[id]
		for row in range(3):
			for col in range(4):
				var value = t.origin[row] if col == 3 else t.basis[col][row]
				max_transform_error = max(max_transform_error,abs(value-m[row][col]))
	check("full source transform matrices",max_transform_error<0.00003)
	var packed = PackedScene.new(); check("pack environment",packed.pack(root)==OK)
	check("save environment",ResourceSaver.save(packed,"res://Assets/Maps/"+map_id+"/MapEnvironment.tscn")==OK)
	var report = {"passed":failures.is_empty(),"failures":failures,"terrainPatches":data.terrain.size(),
		"terrainTriangles":terrain_triangles,"sourcePlacements":nodes.size(),"propInstances":prop_instances,
		"sharedModelResources":data.modelOutputs.size(),"propMeshInstances":mesh_count,
		"maxWorldMatrixError":max_transform_error,"collision":"derived static triangle meshes; source physics not claimed"}
	var file = FileAccess.open("res://../manifests/map-"+map_id+"-godot-build.json",FileAccess.WRITE); file.store_string(JSON.stringify(report,"\t")); file.close()
	print("MAP_BUILD_PASSED" if failures.is_empty() else "MAP_BUILD_FAILED")
	quit(0 if failures.is_empty() else 1)
func meshes(node: Node) -> Array:
	var result = []
	if node is MeshInstance3D: result.append(node)
	for child in node.get_children(): result.append_array(meshes(child))
	return result
func collision(mi: MeshInstance3D, owner_root: Node, attachment: Node3D = null):
	if attachment == null: attachment = mi
	var body = StaticBody3D.new(); body.name = "DerivedCollision"; attachment.add_child(body); body.owner = owner_root
	body.global_transform = mi.global_transform
	var shape = CollisionShape3D.new(); shape.shape = mi.mesh.create_trimesh_shape()
	body.add_child(shape); shape.owner = owner_root
