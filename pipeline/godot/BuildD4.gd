extends SceneTree
## Offline scene compiler. Inputs come only from the bounded source conversion.
var data: Dictionary
var failures: Array = []
func check(label: String, ok: bool):
	if not ok: failures.append(label); push_error(label)
func v(a) -> Vector3: return Vector3(a[0],a[1],a[2])
var map_id: String
var skipped_collision: Array = []
var convex_collision: Array = []
func _initialize(): call_deferred("build")
func build():
	data = JSON.parse_string(FileAccess.get_file_as_string("res://Assets/Korriban/D4/environment.json"))
	var root = load("res://Assets/Korriban/KorribanEnvironment.tscn").instantiate(); get_root().add_child(root)
	root.set_meta("area_id",data.area); root.set_meta("source_room",data.room); root.set_meta("map_id",map_id)
	root.set_meta("source_units","1:1; physical metres uncalibrated")
	var terrain = root.get_node("Terrain")
	var props = root.get_node("Props")
	var terrain_triangles = 0; var prop_instances = 0; var mesh_count = 0
	for p in data.terrain:
		if terrain.has_node("Terrain_"+p.id): continue
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
	for node in props.get_children():
		index_existing(node,nodes)
	var existing_ids = nodes.keys()
	var conversion = JSON.parse_string(FileAccess.get_file_as_string("res://../manifests/d4-environment-model-conversion.json"))
	for p in data.placements:
		if nodes.has(p.id): continue
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
			if nodes[p.id].is_inside_tree(): pending.erase(p); continue
			if not parent.is_inside_tree(): continue
			parent.add_child(nodes[p.id]); nodes[p.id].owner = root
			pending.erase(p); progress = true
		if not progress:
			check("parent hierarchy cycle",false); quit(1); return
	for p in data.placements:
		var node = nodes[p.id]
		if p.id in existing_ids: continue
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
				collision(mi,root,node,p.path.get_file().get_basename())
			check("source triangle count "+p.id,model_info != null and triangle_count == int(model_info.triangles))
	await process_frame
	var max_transform_error = 0.0
	for id in nodes:
		if not data.expectedMatrices.has(id): continue
		var t: Transform3D = nodes[id].global_transform
		var m = data.expectedMatrices[id]
		for row in range(3):
			for col in range(4):
				var value = t.origin[row] if col == 3 else t.basis[col][row]
				max_transform_error = max(max_transform_error,abs(value-m[row][col]))
	check("full source transform matrices",max_transform_error<0.00003)
	var packed = PackedScene.new(); check("pack environment",packed.pack(root)==OK)
	check("save environment",ResourceSaver.save(packed,"res://Assets/Korriban/D4/KorribanTrials.tscn")==OK)
	var report = {"passed":failures.is_empty(),"failures":failures,"terrainPatches":data.terrain.size(),
		"terrainTriangles":terrain_triangles,"sourcePlacements":nodes.size(),"propInstances":prop_instances,
		"sharedModelResources":data.modelOutputs.size(),"propMeshInstances":mesh_count,
		"maxWorldMatrixError":max_transform_error,"collision":"derived static triangle meshes; source physics not claimed","collisionSkippedMeshes":skipped_collision.size(),"collisionConvexMeshes":convex_collision.size()}
	var file = FileAccess.open("res://../manifests/d4-environment-godot-build.json",FileAccess.WRITE); file.store_string(JSON.stringify(report,"\t")); file.close()
	print("MAP_BUILD_PASSED" if failures.is_empty() else "MAP_BUILD_FAILED")
	quit(0 if failures.is_empty() else 1)
func meshes(node: Node) -> Array:
	var result = []
	if node is MeshInstance3D: result.append(node)
	for child in node.get_children(): result.append_array(meshes(child))
	return result
const STEP_HEIGHT := 0.055
func collision(mi: MeshInstance3D, owner_root: Node, attachment: Node3D = null, model_name: String = ""):
	if attachment == null: attachment = mi
	if model_name != "":
		# Gameplay walks on terrain only (ground probe is terrain-layer). Walkable decks and flat clutter would
		# otherwise act as walls, so they carry no collision; supports, rails-free arches and solid rocks still do.
		var size: Vector3 = (mi.global_transform * mi.get_aabb()).size
		var walkable := model_name.contains("bridge") and not (model_name.contains("support") or model_name.contains("pillar"))
		walkable = walkable or model_name.contains("plank") or model_name.contains("walkway") or model_name.contains("catwalk") or model_name.contains("ramp")
		if walkable or size.y < STEP_HEIGHT:
			skipped_collision.append(model_name); return
	var body = StaticBody3D.new(); body.name = "DerivedCollision"; attachment.add_child(body); body.owner = owner_root
	body.global_transform = mi.global_transform
	var shape = CollisionShape3D.new()
	var extent: Vector3 = (mi.global_transform * mi.get_aabb()).size
	var rounded := model_name.contains("rock") or model_name.contains("boulder") or model_name.contains("debris") or model_name.contains("log") or model_name.contains("gravestone") or model_name.contains("crate")
	if rounded and model_name != "" and (model_name.contains("boulder") or max(extent.x, max(extent.y, extent.z)) < 1.0):
		# Small jagged rocks snag a capsule on concave triangle edges; a convex hull slides.
		shape.shape = mi.mesh.create_convex_shape(true, true); convex_collision.append(model_name)
	else:
		shape.shape = mi.mesh.create_trimesh_shape()
	body.add_child(shape); shape.owner = owner_root

func index_existing(node: Node,nodes: Dictionary):
	if node.has_meta("source_instance_id"): nodes[str(node.get_meta("source_instance_id"))] = node
	for child in node.get_children(): index_existing(child,nodes)
