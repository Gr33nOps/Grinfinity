extends Node

# Frame-time sampling, to answer the object-pooling question in ROADMAP section 1
# with a measurement rather than a guess. vsync is switched off in _ready so the
# numbers are real work per frame, not the refresh rate.
const BUDGET_MS := 16.67

var _worst_ms := 0.0
var _total_ms := 0.0
var _frames := 0
var _over_budget := 0
var _peak_entities := 0

func _process(delta: float) -> void:
	var ms := delta * 1000.0
	# The first frames include scene load and shader compilation.
	if _frames > 30:
		_worst_ms = maxf(_worst_ms, ms)
		if ms > BUDGET_MS:
			_over_budget += 1
	_total_ms += ms
	_frames += 1

	var entities := get_tree().get_node_count_in_group("bodies") \
		+ get_tree().get_node_count_in_group("debris")
	_peak_entities = maxi(_peak_entities, entities)


func _report_load() -> void:
	var average := _total_ms / maxf(_frames, 1)
	print("frames=%d  avg=%.2fms  worst=%.2fms  over_budget=%d  peak_bodies+debris=%d"
		% [_frames, average, _worst_ms, _over_budget, _peak_entities])
	print("budget: %.2fms at 60fps" % BUDGET_MS)

# Temporary dev tool: runs a scene unattended and writes a screenshot so visual
# work can be verified without a human at the keyboard. Delete when done.
#
#   GRIN_SCENE  scene to load        (default res://scenes/game.tscn)
#   GRIN_SHOT   png path to write    (default user://capture.png)
#   GRIN_RUN    seconds to run for   (default 6)

func _ready() -> void:
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)

	var target: String = OS.get_environment("GRIN_SCENE")
	if target == "":
		target = "res://scenes/game.tscn"

	# GRIN_WEAPON picks the loadout by index, so each weapon can be captured.
	var weapon := OS.get_environment("GRIN_WEAPON")
	if weapon != "":
		var settings := get_node_or_null("/root/GameSettings")
		if settings:
			settings.call("SetWeapon", int(weapon))

	var scene: Node = (load(target) as PackedScene).instantiate()
	add_child(scene)
	await get_tree().process_frame

	# Death would change the scene out from under this node, and the interesting
	# frames are late in an orbit, so the world is made invulnerable for capture.
	# Set GRIN_MORTAL=1 to leave it mortal and exercise the death and recap path.
	var world: Node2D = scene.get_node_or_null("player")
	if world and OS.get_environment("GRIN_MORTAL") == "":
		world.set("Invulnerable", true)

	# GRIN_BOSS brings The Coil forward, so a boss fight can be checked without
	# sitting through three minutes of orbit first.
	var boss_at := OS.get_environment("GRIN_BOSS")
	if boss_at != "":
		scene.set("BossTime", float(boss_at))

	_capture(world != null)

func _capture(is_gameplay: bool) -> void:
	var run_seconds := float(OS.get_environment("GRIN_RUN"))
	if run_seconds <= 0.0:
		run_seconds = 6.0

	# GRIN_PACIFIST holds fire so bodies pile up to the spawn cap. That is the
	# load the pooling question is actually about; a bot that kills everything
	# never builds a crowd.
	var pacifist := OS.get_environment("GRIN_PACIFIST") != ""

	if is_gameplay and not pacifist:
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
	for body in get_tree().get_nodes_in_group("bodies"):
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
	_report_load()
	# Hitstop drives Engine.time_scale globally. If it ever fails to hand the
	# value back the whole game runs in slow motion, so the run reports it.
	print("time_scale after run: ", Engine.time_scale)
	get_tree().quit()
