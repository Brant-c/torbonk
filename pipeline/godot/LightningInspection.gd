extends "res://CharacterInspection.gd"
## Milestone A3 wrapper; original character/door inspectors remain untouched.
var lightning
var dummy: Node3D
var dummy_rig: Skeleton3D
var dummy_player: AnimationPlayer
var moving_target: bool = false
var drift_time: float = 0.0
var target_base = Vector3(0,0,-0.45)
var testing: bool = false
var checks: Array[Dictionary] = []
var failed: bool = false

func find_target_nodes(node: Node):
	if node is Skeleton3D: dummy_rig=node
	if node is AnimationPlayer: dummy_player=node
	for child in node.get_children(): find_target_nodes(child)

func _ready():
	super._ready()
	testing = "--lightning-validate" in OS.get_cmdline_user_args() or "--lightning-capture" in OS.get_cmdline_user_args()
	dummy=load("res://Assets/sorcerer.glb").instantiate(); dummy.name="OriginalRigTarget"
	add_child(dummy); dummy.position=target_base; dummy.rotation.y=PI
	find_target_nodes(dummy)
	dummy_player.get_animation("Idle").loop_mode=Animation.LOOP_LINEAR
	dummy_player.play("Idle")
	lightning=load("res://ForceLightning.gd").new(); lightning.name="ForceLightning"
	add_child(lightning)
	if not lightning.configure(skeleton,player,dummy,dummy_rig): fail(lightning.last_error); return
	center=Vector3(0,0.11,-0.21); yaw=1.25; pitch=0.25; radius=0.88; update_camera()
	var overlay=CanvasLayer.new(); add_child(overlay)
	var panel=VBoxContainer.new(); panel.position=Vector2(18,610); overlay.add_child(panel)
	var title=Label.new(); title.text="FORCE LIGHTNING · Source textures + four finger bolts + target arcs + original audio"; panel.add_child(title)
	var row=HBoxContainer.new(); panel.add_child(row)
	for item in [["Start [F]",func(): lightning.start_channel()],["Stop [G]",func(): lightning.stop_channel()],["Interrupt [X]",func(): lightning.interrupt()]]:
		var button=Button.new(); button.text=item[0]; button.pressed.connect(item[1]); row.add_child(button)
	var move=CheckButton.new(); move.text="Moving target"; move.toggled.connect(func(value): moving_target=value); row.add_child(move)
	var timed=CheckButton.new(); timed.text="3 s channel"; timed.toggled.connect(func(value): lightning.automatic_stop_seconds=3.0 if value else 0.0); row.add_child(timed)
	var notes=Label.new(); notes.text="Arrows: move target  |  PgUp/PgDn: target height  |  Drag: orbit  |  Wheel: zoom\n1–7: original clips (interrupt effect)  |  S: bones  |  F/G/X: start / end / cancel\nFX units & note timing are reconstructed. Audio is the decoded April bank media."; panel.add_child(notes)
	var scale_row=HBoxContainer.new(); panel.add_child(scale_row)
	var scale_label=Label.new(); scale_label.text="FX scale (uncalibrated): 0.10"; scale_row.add_child(scale_label)
	var scale_slider=HSlider.new(); scale_slider.min_value=0.02; scale_slider.max_value=1.0; scale_slider.step=0.01; scale_slider.value=0.1; scale_slider.custom_minimum_size=Vector2(210,20)
	scale_slider.value_changed.connect(func(value): lightning.fx_scale=value; scale_label.text="FX scale (uncalibrated): %.2f" % value); scale_row.add_child(scale_slider)
	# The base inspector's pause controls remain valid only for its clip viewer.
	for node in get_children():
		if node is CanvasLayer and node!=overlay:
			for container in node.get_children():
				for child in container.get_children():
					if child is Label and child.text.begins_with("1–7:"): child.text="Select a clip to inspect it separately, or use the Force Lightning controls below."
	if "--lightning-validate" in OS.get_cmdline_user_args(): await validate_lightning(false)
	elif "--lightning-capture" in OS.get_cmdline_user_args(): await validate_lightning(true)

func play_clip(clip: String):
	if is_instance_valid(lightning): lightning.interrupt(false)
	super.play_clip(clip)

func _process(delta):
	if not is_instance_valid(lightning): return
	if moving_target:
		drift_time+=delta
		dummy.position=target_base+Vector3(sin(drift_time*1.6)*0.10,sin(drift_time*1.1)*0.025,cos(drift_time)*0.025)
	if not testing:
		var direction=Vector3.ZERO
		if Input.is_key_pressed(KEY_LEFT): direction.x-=1
		if Input.is_key_pressed(KEY_RIGHT): direction.x+=1
		if Input.is_key_pressed(KEY_UP): direction.z-=1
		if Input.is_key_pressed(KEY_DOWN): direction.z+=1
		if Input.is_key_pressed(KEY_PAGEUP): direction.y+=1
		if Input.is_key_pressed(KEY_PAGEDOWN): direction.y-=1
		target_base+=direction*delta*0.12
		if not moving_target: dummy.position=target_base
	var snapshot: Dictionary=lightning.snapshot()
	status.text="Phase: %s  ·  Bolts: %d  ·  Particles: %d  ·  Lights: %d  ·  Audio voices: %d" % [snapshot.phase,snapshot.beams,snapshot.sparks,snapshot.lights,snapshot.audioPlaying]
	debug_mesh.visible=show_bones
	if show_bones: draw_bones()

func _unhandled_input(event):
	if testing: return
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode in [KEY_F,KEY_SPACE]: lightning.start_channel(); return
		if event.keycode==KEY_G: lightning.stop_channel(); return
		if event.keycode==KEY_X: lightning.interrupt(); return
		if event.keycode in [KEY_LEFT,KEY_RIGHT,KEY_UP,KEY_DOWN,KEY_PAGEUP,KEY_PAGEDOWN]: return
		if event.keycode==KEY_B: lightning.interrupt(false)
	super._unhandled_input(event)

func check(label: String, passed: bool, evidence: Variant = null):
	checks.append({"check":label,"passed":passed,"evidence":evidence})
	if not passed: failed=true; push_error("LIGHTNING CHECK FAILED: "+label)

func settle(seconds: float):
	await get_tree().create_timer(seconds).timeout
	await get_tree().process_frame
	skeleton.force_update_all_bone_transforms(); dummy_rig.force_update_all_bone_transforms()

func capture(name: String):
	await RenderingServer.frame_post_draw
	var error=get_viewport().get_texture().get_image().save_png("res://../generated/lightning/"+name+".png")
	check("capture "+name,error==OK)

func assert_clean(label: String):
	var state: Dictionary=lightning.snapshot()
	check(label,state.phase=="idle" and state.beams==0 and state.sparks==0 and state.lights==0 and state.audioPlaying==0 and state.emitters==0,state)

func verify_endpoints(label: String):
	var max_start_error=0.0; var max_end_error=0.0
	for beam in lightning.beams:
		if not beam.node.has_meta("start"): continue
		var rig=dummy_rig if beam.body else skeleton
		var expected_start: Vector3=lightning.bone_world(rig,beam.data._fxAttachBone).origin
		var expected_end: Vector3=lightning.aim_point()
		if beam.body:
			for model in lightning.config.body._fxModelList:
				if model._fxName==beam.data._fxTargetName: expected_end=lightning.bone_world(dummy_rig,model._fxAttachBone).origin
		max_start_error=maxf(max_start_error,expected_start.distance_to(beam.node.get_meta("start")))
		max_end_error=maxf(max_end_error,expected_end.distance_to(beam.node.get_meta("end")))
	check(label,max_start_error<0.0001 and max_end_error<0.0001,{"maxStartError":max_start_error,"maxEndError":max_end_error})

func validate_lightning(captures: bool):
	await settle(0.1)
	var recording: AudioEffectRecord
	if captures:
		recording=AudioEffectRecord.new(); recording.format=AudioStreamWAV.FORMAT_16_BITS
		AudioServer.add_bus_effect(0,recording); recording.set_recording_active(true)
	check("source finger and VFX bones",lightning.configure(skeleton,player,dummy,dummy_rig),{"casterBones":skeleton.get_bone_count(),"targetBones":dummy_rig.get_bone_count()})
	for entry in lightning.config.body._fxEmitterList:
		check("target emitter bone "+entry._fxAttachBone,dummy_rig.find_bone(entry._fxAttachBone)>=0)
	for entry in lightning.config.body._fxLightningList:
		check("target arc bone "+entry._fxAttachBone,dummy_rig.find_bone(entry._fxAttachBone)>=0)
	lightning.start_channel(); await settle(0.18)
	check("cast-start pending cue",lightning.phase=="start" and not lightning.active,lightning.snapshot())
	if captures: await capture("01-cast-start")
	await settle(0.4)
	check("four fingers and four target arcs",lightning.active and lightning.beams.size()==8,lightning.snapshot())
	verify_endpoints("animated start attachment positions")
	var starting_finger: Vector3=lightning.bone_world(skeleton,"RightIndexFinger1").origin
	if captures: await capture("02-caster-active")
	await settle(0.6)
	check("start transitions to original loop",lightning.phase=="channel" and player.current_animation=="LightningLoop",lightning.snapshot())
	var old_aim: Vector3=lightning.aim_point()
	dummy.position+=Vector3(0.07,0.02,-0.03)
	await settle(0.08)
	check("movable target followed",old_aim.distance_to(lightning.aim_point())>0.05)
	verify_endpoints("moving target and loop hand attachment positions")
	for i in 4:
		await settle(0.15); verify_endpoints("loop alignment sample %d" % i)
	check("hand endpoint actually animates",starting_finger.distance_to(lightning.bone_world(skeleton,"RightIndexFinger1").origin)>0.001)
	check("original loop audio active",lightning.audio[1].playing and lightning.audio[2].playing)
	if captures: await capture("03-channel-moving-target")
	moving_target=true
	for i in 4:
		await settle(0.15); verify_endpoints("continuous target motion sample %d" % i)
	moving_target=false
	lightning.stop_channel(); await settle(0.12)
	check("end animation, beams and emitters stopped",lightning.phase=="end" and lightning.beams.is_empty() and lightning.emitters.is_empty() and player.current_animation=="LightningEnd",lightning.snapshot())
	check("Wwise stop fade underway",lightning.audio[1].volume_linear<db_to_linear(lightning.audio_base[1]) and lightning.audio[1].playing)
	if captures: await capture("04-cast-end")
	await settle(0.5); assert_clean("normal stop cleaned all resources")
	if captures:
		await capture("05-clean"); await settle(0.15)
		recording.set_recording_active(false)
		var recorded=recording.get_recording()
		check("original audio mixed by Godot",recorded!=null and recorded.data.size()>100000)
		if recorded: check("audio capture saved",recorded.save_to_wav("res://../generated/lightning/godot-channel-audio.wav")==OK)
		AudioServer.remove_bus_effect(0,AudioServer.get_bus_effect_count(0)-1)
	lightning.start_channel(); await settle(0.15); lightning.interrupt(); assert_clean("interrupt during wind-up")
	lightning.start_channel(); await settle(0.7); lightning.interrupt(); assert_clean("interrupt during active cast")
	lightning.start_channel(); await settle(1.2); lightning.interrupt(); player.play("Hit"); assert_clean("hit interruption")
	lightning.start_channel(); await settle(1.2); lightning.interrupt(); player.play("Death"); assert_clean("death interruption")
	lightning.start_channel(); await settle(0.7); lightning.stop_channel(); await settle(0.1); lightning.start_channel(); await settle(0.7)
	check("restart during end has one bounded effect",lightning.beams.size()==8 and lightning.lights.size()==3,lightning.snapshot())
	for i in 20:
		lightning.start_channel(); lightning.activate(); lightning.interrupt()
	assert_clean("20 repeated starts and cancellations")
	check("no orphan nodes outside bookkeeping",lightning.get_child_count()==12,{"children":lightning.get_child_count(),"expectedIdleAudioPool":12})
	lightning.set_target_point(Vector3(0.08,0.15,-0.4))
	lightning.start_channel(); await settle(0.6)
	check("configurable point mode",lightning.beams.size()==4 and lightning.emitters.is_empty(),lightning.snapshot())
	verify_endpoints("point target alignment")
	lightning.interrupt(); lightning.configure(skeleton,player,dummy,dummy_rig)
	var temporary_target=Marker3D.new(); add_child(temporary_target); temporary_target.position=Vector3(0,0.1,-0.4)
	lightning.configure(skeleton,player,temporary_target)
	lightning.start_channel(); await settle(0.6); temporary_target.free(); await settle(0.05)
	assert_clean("removed target cancels presentation")
	lightning.configure(skeleton,player,dummy,dummy_rig)
	lightning.automatic_stop_seconds=0.25; lightning.start_channel(); await settle(1.9)
	assert_clean("timed demonstration channel stops")
	var removed=load("res://ForceLightning.gd").new(); add_child(removed)
	removed.configure(skeleton,player,dummy,dummy_rig); removed.start_channel(); await settle(0.6)
	var owned_children=removed.get_children(); removed.queue_free(); await settle(0.05)
	var all_freed=true
	for child in owned_children:
		if is_instance_valid(child): all_freed=false
	check("component deletion frees every owned visual and voice",not is_instance_valid(removed) and all_freed)
	var audio_manifest=JSON.parse_string(FileAccess.get_file_as_string("res://Assets/Lightning/audio.json"))
	for media in audio_manifest.media:
		var stream=load("res://Assets/Lightning/Audio/%d.wav" % int(media.mediaId)) as AudioStreamWAV
		var hash=HashingContext.new(); hash.start(HashingContext.HASH_SHA256); hash.update(stream.data)
		check("lossless Godot PCM %d" % int(media.mediaId), stream.format==AudioStreamWAV.FORMAT_16_BITS and stream.mix_rate==int(media.sampleRate) and stream.data.size()==int(media.frames)*int(media.channels)*2 and hash.finish().hex_encode()==media.pcmSha256)
	var bed=lightning.wav(51508336,true)
	check("original bed full-sample loop boundaries",bed.loop_begin==0 and bed.loop_end==240096,{"begin":bed.loop_begin,"end":bed.loop_end,"rate":bed.mix_rate})
	var result={"engine":Engine.get_version_info().string,"passed":not failed,"checks":checks,"captures":captures,
		"fidelity":"Source-derived reconstruction; no running SWTOR client comparison", "audioBackend":AudioServer.get_driver_name()}
	FileAccess.open("res://../manifests/lightning-godot-validation"+("-visual" if captures else "")+".json",FileAccess.WRITE).store_string(JSON.stringify(result,"\t"))
	print("LIGHTNING_VALIDATION_", "PASSED" if not failed else "FAILED", " checks=",checks.size())
	get_tree().quit(1 if failed else 0)
