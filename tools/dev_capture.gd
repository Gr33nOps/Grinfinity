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
# Do NOT pass --headless: it picks the dummy renderer, which can never produce
# an image and hangs the run (see _release_all). Park the window offscreen
# instead, which captures fine and stays out of the way:
#
#   godot --path . --audio-driver Dummy --position 4000,4000 tools/dev_capture.tscn
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

	# GRIN_MODE picks the mode by index (0 Endless, 1 Flyby, 2 Daily Alignment,
	# 3 Convergence, 4 Glass Planet). GRIN_DIFFICULTY picks Easy/Normal/Hard
	# the same way (0/1/2).
	var mode := OS.get_environment("GRIN_MODE")
	if mode != "":
		var settings := get_node_or_null("/root/GameSettings")
		if settings:
			settings.call("SetMode", int(mode))
	var difficulty := OS.get_environment("GRIN_DIFFICULTY")
	if difficulty != "":
		var settings := get_node_or_null("/root/GameSettings")
		if settings:
			settings.call("SetDifficulty", int(difficulty))

	# GameManager's death path calls change_scene_to_file(), which frees
	# whatever current_scene points to. Since this wrapper IS current_scene (it
	# is the CLI-launched main scene), a mortal run that reaches death loses the
	# capture coroutine along with the game, silently: no screenshot, no report.
	# An earlier attempt to fix this by reparenting the loaded scene under root
	# and reassigning current_scene broke ordinary (non-mortal) capture outright
	# — the scene never finished initialising, nothing ever spawned. Reverted;
	# use GRIN_MORTAL to exercise the death path in the live editor instead
	# (mcp__godot__run_project on scenes/game.tscn directly), not through this
	# tool.
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
	# GRIN_BOSS brings the Coil forward; GRIN_BROOD brings the Brood forward
	# (relative to whenever the Coil is dealt with, same as in real play).
	var boss_at := OS.get_environment("GRIN_BOSS")
	if boss_at != "":
		scene.set("CoilTime", float(boss_at))
	var brood_at := OS.get_environment("GRIN_BROOD")
	if brood_at != "":
		scene.set("BroodTime", float(brood_at))
		# Skip straight to the Brood so it can be checked without first beating
		# the Coil in an unattended run.
		scene.set("NextBossIndex", 1)

	var blackhole_at := OS.get_environment("GRIN_BLACKHOLE")
	if blackhole_at != "":
		scene.set("BlackHoleTime", float(blackhole_at))
		scene.set("NextBossIndex", 2)

	# GRIN_EVENT brings the first arena event forward from its usual minute.
	var event_at := OS.get_environment("GRIN_EVENT")
	if event_at != "":
		await get_tree().process_frame
		var director := scene.get_node_or_null("EventDirector")
		if director:
			director.set("FirstEventTime", float(event_at))
			director.set("GapMin", float(event_at))
			director.set("GapMax", float(event_at) + 2.0)

	# GRIN_WELL / GRIN_COMET bring the environmental hazards forward.
	var well_at := OS.get_environment("GRIN_WELL")
	var comet_at := OS.get_environment("GRIN_COMET")
	if well_at != "" or comet_at != "":
		await get_tree().process_frame
		var hazards := scene.get_node_or_null("HazardDirector")
		if hazards:
			if well_at != "":
				hazards.set("FirstWellTime", float(well_at))
				hazards.set("WellGapMin", float(well_at))
				hazards.set("WellGapMax", float(well_at) + 2.0)
			if comet_at != "":
				hazards.set("FirstCometTime", float(comet_at))
				hazards.set("CometGapMin", float(comet_at))
				hazards.set("CometGapMax", float(comet_at) + 2.0)

	# GRIN_DROPS forces the pickup drop rate, so all five can be seen at once.
	# GRIN_WORLD_NAV clicks Next this many times on the weapon/world select
	# screen, so a locked world's card can be captured directly.
	var world_nav := OS.get_environment("GRIN_WORLD_NAV")
	if world_nav != "":
		await get_tree().process_frame
		var next_button := scene.get_node_or_null("Layout/WorldRow/Next")
		if next_button:
			for i in int(world_nav):
				next_button.emit_signal("pressed")
				await get_tree().process_frame

	# GRIN_BUY clicks the first upgrade's buy button this many times, on the
	# upgrades screen, so a purchase can be verified end to end.
	var buy_count := OS.get_environment("GRIN_BUY")
	if buy_count != "":
		await get_tree().process_frame
		var rows := scene.get_node_or_null("Layout/Rows")
		if rows and rows.get_child_count() > 0:
			var first_card: Node = rows.get_child(0)
			var buy_button: Node = first_card.get_child(first_card.get_child_count() - 1)
			for i in int(buy_count):
				buy_button.emit_signal("pressed")
				await get_tree().process_frame

	var drops := OS.get_environment("GRIN_DROPS")
	if drops != "":
		scene.set("PowerUpDropChance", float(drops))
		scene.set("HeavyDropBonus", 0.0)

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
	# A mortal run can die mid-loop to an ordinary body, not just a boss. Once
	# that happens the game scene is gone, so both lookups have to be guarded —
	# calling get_node_or_null on a null game_manager was spamming a script
	# error every tick for the rest of a mortal run instead of quietly no-oping.
	var manager: Node = get_tree().get_first_node_in_group("game_manager")
	if manager == null:
		return

	var world: Node2D = manager.get_node_or_null("player")
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

	# --headless selects the dummy rendering driver, which never emits
	# frame_post_draw. Awaiting it there blocks forever: no screenshot, no
	# report, no exit, and nothing in the log after the version banner. Bail
	# out loudly instead of hanging, and say what to run instead.
	if DisplayServer.get_name() == "headless":
		printerr("dev_capture: --headless cannot render, so no screenshot is possible.")
		printerr("             Run without --headless and park the window offscreen:")
		printerr("             godot --path . --audio-driver Dummy --position 4000,4000 tools/dev_capture.tscn")
		_report_load()
		get_tree().quit(1)
		return

	await RenderingServer.frame_post_draw
	var image: Image = get_viewport().get_texture().get_image()
	var path: String = OS.get_environment("GRIN_SHOT")
	if path == "":
		path = "user://capture.png"
	image.save_png(path)
	print("captured -> ", path)
	var landed: Node = get_tree().current_scene
	print("landed on -> ", landed.scene_file_path if landed else "<none>")
	_report_load()
	# Hitstop drives Engine.time_scale globally. If it ever fails to hand the
	# value back the whole game runs in slow motion, so the run reports it.
	print("time_scale after run: ", Engine.time_scale)
	get_tree().quit()
