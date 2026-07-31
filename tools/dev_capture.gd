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
	var hitbox: Area2D = scene.get_node_or_null("player/HitBox")
	if hitbox:
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
			# Sweep the aim so kills happen all around, not down one lane.
			var angle := elapsed * 1.1
			Input.warp_mouse(Vector2(1280.0, 700.0) + Vector2(cos(angle), sin(angle)) * 520.0)
		await get_tree().create_timer(0.05).timeout
		elapsed += 0.05

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
