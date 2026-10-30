extends SceneTree

func _initialize():
	call_deferred("run")

func run():
	for i in range(12): await process_frame
	assert(root.content_scale_mode == Window.CONTENT_SCALE_MODE_CANVAS_ITEMS)
	assert(root.content_scale_size == Vector2i(1280,720))
	assert(DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN)
	var rendered = root.get_texture().get_image().get_size()
	print("DISPLAY_CLARITY_PIXELS: ",rendered," window=",DisplayServer.window_get_size())
	assert(Vector2i(rendered) == DisplayServer.window_get_size())
	assert(root.msaa_3d == Viewport.MSAA_4X)
	print("DISPLAY_CLARITY_PASSED: design=",root.content_scale_size," actual=",rendered)
	quit()
