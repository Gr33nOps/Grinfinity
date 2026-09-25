extends Node
# Development-only screen capture; release exports exclude tools/. For whole
# played runs with a timeline, use tools/survival_bot.gd instead.
#
# Environment:
#   GRIN_SCENE    scene to open (default res://scenes/game.tscn)
#   GRIN_RUN      seconds to wait before the capture (default 3)
#   GRIN_VIEW     "pause" or "options" to open the pause menu first (game only)
#   GRIN_SHOT     output path (default user://capture.png)
#   GRIN_SIZE     window size, e.g. 1280x720
#   GRIN_UI_SCALE UI scale to apply first
#   GRIN_PLAYER_AT place the planet first, e.g. 400,500 (game only)
var frames := 0
var total_ms := 0.0
var worst_ms := 0.0
var previous_usec := 0

func _process(_delta: float) -> void:
	var now := Time.get_ticks_usec()
	if previous_usec > 0 and frames > 60:
		var ms := float(now - previous_usec) / 1000.0
		total_ms += ms
		worst_ms = maxf(ms, worst_ms)
	previous_usec = now
	frames += 1

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	if DisplayServer.get_name() == "headless":
		printerr("Use a real renderer for screenshots.")
		get_tree().quit(2)
		return
	call_deferred("_run")

func _run() -> void:
	var settings := get_node("/root/GameSettings")
	var scale_text := OS.get_environment("GRIN_UI_SCALE")
	if scale_text != "":
		settings.call("SetUiScale", float(scale_text))
	var target := OS.get_environment("GRIN_SCENE")
	if target == "":
		target = "res://scenes/game.tscn"
	var scene: Node = load(target).instantiate()
	get_tree().current_scene = null
	get_tree().root.add_child(scene)
	get_tree().current_scene = scene
	await get_tree().process_frame
	var size_text := OS.get_environment("GRIN_SIZE")
	if size_text != "":
		var parts := size_text.split("x")
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
		DisplayServer.window_set_size(Vector2i(int(parts[0]), int(parts[1])))
	var world: Node = scene.get_node_or_null("player")
	if world:
		world.set("Invulnerable", true)
		var at := OS.get_environment("GRIN_PLAYER_AT")
		if at != "":
			var xy := at.split(",")
			world.global_position = Vector2(float(xy[0]), float(xy[1]))
	var duration := float(OS.get_environment("GRIN_RUN"))
	if duration <= 0:
		duration = 3
	await get_tree().create_timer(duration, true, false, true).timeout
	var view := OS.get_environment("GRIN_VIEW")
	if scene.has_method("_Input") and view in ["pause", "options"]:
		var event := InputEventAction.new()
		event.action = "pause"
		event.pressed = true
		scene.call("_Input", event)
		await get_tree().create_timer(0.4, true, false, true).timeout
	await RenderingServer.frame_post_draw
	var path := OS.get_environment("GRIN_SHOT")
	if path == "":
		path = "user://capture.png"
	get_viewport().get_texture().get_image().save_png(path)
	print("CAPTURE ", path, " scene=", target)
	print("PERF frames=%d avg=%.2fms worst=%.2fms" % [frames, total_ms / maxf(frames - 61, 1), worst_ms])
	settings.call("QuitGame")
