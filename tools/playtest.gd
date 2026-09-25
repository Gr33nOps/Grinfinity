extends Node
# Development-only visual playtest; release exports exclude tools/.
var frames := 0
var total_ms := 0.0
var worst_ms := 0.0
var slow_frames := 0
var previous_usec := 0
var peak_nodes := 0

func _process(_delta: float) -> void:
	var now := Time.get_ticks_usec()
	if previous_usec > 0 and frames > 60:
		var ms := float(now - previous_usec) / 1000.0
		total_ms += ms
		worst_ms = maxf(ms, worst_ms)
		if ms > 16.67:
			slow_frames += 1
	previous_usec = now
	frames += 1
	peak_nodes = maxi(peak_nodes, get_tree().get_node_count())

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
	if OS.get_environment("GRIN_UNCAPPED") != "":
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	else:
		Engine.max_fps = 60
	var world: Node2D = scene.get_node_or_null("player")
	if world:
		world.set("Invulnerable", OS.get_environment("GRIN_MORTAL") == "")
		var boss_text := OS.get_environment("GRIN_BOSS_INDEX")
		if boss_text != "":
			scene.set("NextBossIndex", int(boss_text))
			for property in ["CoilTime", "BroodTime", "BlackHoleTime"]:
				scene.set(property, 0.5)
	var duration := float(OS.get_environment("GRIN_RUN"))
	if duration <= 0:
		duration = 20
	var start := Time.get_ticks_msec()
	while float(Time.get_ticks_msec() - start) / 1000.0 < duration:
		var elapsed := float(Time.get_ticks_msec() - start) / 1000.0
		var game := get_tree().get_first_node_in_group("game_manager")
		if game:
			if get_tree().paused and game.get("IsPaused"):
				_pause_action(game)
			world = game.get_node("player")
			Input.action_press("shoot")
			if OS.get_environment("GRIN_PACIFIST") != "":
				Input.action_release("shoot")
			var heading := Vector2.from_angle(elapsed * 0.55)
			_axis("left", "right", heading.x)
			_axis("up", "down", heading.y)
			var nearest: Node2D = null
			var distance := INF
			for body in get_tree().get_nodes_in_group("bosses") + get_tree().get_nodes_in_group("bodies"):
				var candidate: float = world.global_position.distance_squared_to(body.global_position)
				if candidate < distance:
					distance = candidate
					nearest = body
			if nearest:
				Input.warp_mouse(get_viewport().get_screen_transform() * nearest.global_position)
			var prompt := game.get_node("UI/UpgradePrompt")
			if prompt.visible:
				if OS.get_environment("GRIN_CAPTURE_BREAK") != "":
					await get_tree().create_timer(0.2).timeout
					break
				if OS.get_environment("GRIN_AUTO_BUY") != "":
					if OS.get_environment("GRIN_BUILD") == "focused":
						var chosen: Button = null
						for priority in ["FASTER SHOTS", "PIERCING SHOTS", "QUICKER DASH", "BIGGER NOVA"]:
							for card in prompt.find_children("*", "Button", true, false):
								for label in card.find_children("*", "Label", true, false):
									if label.text == priority:
										chosen = card
										break
								if chosen:
									break
							if chosen:
								break
						if chosen:
							chosen.emit_signal("pressed")
						else:
							prompt.find_child("SkipUpgrade", true, false).emit_signal("pressed")
						continue
					var key := InputEventKey.new()
					key.keycode = KEY_1
					key.pressed = true
					prompt.call("_UnhandledInput", key)
		await get_tree().create_timer(0.05, true, false, true).timeout
	for action in ["shoot", "left", "right", "up", "down", "nova"]:
		Input.action_release(action)
	var game := get_tree().get_first_node_in_group("game_manager")
	if game:
		print("RUN wave=%s kills=%s score=%s survival=%s" % [game.get_node("BodySpawner").get("WaveNumber"), game.get_node("RunState").get("Kills"), game.get_node("RunState").get("Score"), game.get_node("RunState").get("SurvivalTime")])
		print("BUILD weapon=%s spread_level=%s bosses_defeated=%s" % [game.get_node("RunState").get("Weapon"), game.get_node("RunState").call("LevelOf", 13), game.get("NextBossIndex")])
	var view := OS.get_environment("GRIN_VIEW")
	if game and view in ["pause", "options"]:
		_pause_action(game)
		if view == "options":
			game.get_node("PauseLayer/PauseMenu").find_child("Options", true, false).emit_signal("pressed")
	await RenderingServer.frame_post_draw
	var path := OS.get_environment("GRIN_SHOT")
	if path == "":
		path = "user://capture.png"
	get_viewport().get_texture().get_image().save_png(path)
	print("CAPTURE ", path, " scene=", get_tree().current_scene.scene_file_path)
	print("PERF frames=%d avg=%.2fms worst=%.2fms over16.67=%d peak_nodes=%d" % [frames, total_ms / maxf(frames - 61, 1), worst_ms, slow_frames, peak_nodes])
	settings.call("QuitGame")

func _pause_action(game: Node) -> void:
	var event := InputEventAction.new()
	event.action = "pause"
	event.pressed = true
	game.call("_Input", event)

func _axis(negative: String, positive: String, value: float) -> void:
	Input.action_release(negative)
	Input.action_release(positive)
	if value > 0.3:
		Input.action_press(positive)
	elif value < -0.3:
		Input.action_press(negative)
