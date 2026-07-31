extends Node

# Temporary dev tool: runs a scene unattended and writes a screenshot so visual
# work can be verified without a human at the keyboard. Delete when done.
#
#   GRIN_SCENE  scene to load        (default res://scenes/game.tscn)
#   GRIN_SHOT   png path to write    (default user://capture.png)
#   GRIN_RUN    seconds to run for   (default 6)

func _ready() -> void:
	var target: String = OS.get_environment("GRIN_SCENE")
	if target == "":
		target = "res://scenes/game.tscn"

	var scene: Node = (load(target) as PackedScene).instantiate()
	add_child(scene)
	await get_tree().process_frame

	# Death would change the scene out from under this node, and the interesting
	# frames are late in an orbit, so contact damage is switched off for capture.
	# Set GRIN_MORTAL=1 to leave it on and exercise the death and recap path.
	var hitbox: Area2D = scene.get_node_or_null("player/HitBox")
	if hitbox and OS.get_environment("GRIN_MORTAL") == "":
		hitbox.monitoring = false

	_capture(hitbox != null)

func _capture(is_gameplay: bool) -> void:
	var run_seconds := float(OS.get_environment("GRIN_RUN"))
	if run_seconds <= 0.0:
		run_seconds = 6.0

	if is_gameplay:
		Input.action_press("shoot")

	# Real seconds, not frames: an unfocused window runs uncapped, so a frame
	# count would end the run before anything had spawned.
	var elapsed := 0.0
	while elapsed < run_seconds:
		if is_gameplay:
			# Standing still lets gravity stack every body on top of the world,
			# where they sit inside the muzzle offset and never get shot. Keep
			# moving, and aim at whatever is closest.
			_drive(elapsed)
			_aim_at_nearest()
			# Cash mass out now and then, so nova and venting get exercised.
			if fmod(elapsed, 12.0) < 0.05:
				Input.action_press("nova")
			else:
				Input.action_release("nova")
		await get_tree().create_timer(0.05).timeout
		elapsed += 0.05

	_release_all()


func _drive(elapsed: float) -> void:
	var heading := Vector2.from_angle(elapsed * 0.7)
	_axis("left", "right", heading.x)
	_axis("up", "down", heading.y)


func _axis(negative: String, positive: String, value: float) -> void:
	Input.action_release(negative)
	Input.action_release(positive)
	if value > 0.3:
		Input.action_press(positive)
	elif value < -0.3:
		Input.action_press(negative)


func _aim_at_nearest() -> void:
	var world: Node2D = get_tree().get_first_node_in_group("game_manager").get_node_or_null("player")
	if world == null:
		return

	var best: Node2D = null
	var best_distance := INF
	for body in get_tree().get_nodes_in_group("enemies"):
		var distance: float = world.global_position.distance_squared_to(body.global_position)
		if distance < best_distance:
			best_distance = distance
			best = body

	if best == null:
		return

	Input.warp_mouse(get_viewport().get_screen_transform() * best.global_position)


func _release_all() -> void:
	for action in ["shoot", "left", "right", "up", "down"]:
		Input.action_release(action)

	await RenderingServer.frame_post_draw
	var image: Image = get_viewport().get_texture().get_image()
	var path: String = OS.get_environment("GRIN_SHOT")
	if path == "":
		path = "user://capture.png"
	image.save_png(path)
	print("captured -> ", path)
	# Hitstop drives Engine.time_scale globally. If it ever fails to hand the
	# value back the whole game runs in slow motion, so the run reports it.
	print("time_scale after run: ", Engine.time_scale)
	get_tree().quit()
