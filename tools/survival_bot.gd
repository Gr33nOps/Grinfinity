extends Node
# Development-only survival bot. Plays whole runs and prints a timeline, so the
# pacing of an endless run can be measured rather than guessed. Release exports
# exclude tools/. Run it through tools/run-bot.ps1, which uses an isolated copy
# of the project so bot runs never touch real saves, leaderboards or unlocks.
#
# Environment:
#   GRIN_RUNS      runs to play (default 1)
#   GRIN_MAX       seconds before a run is stopped (default 1500)
#   GRIN_IMMORTAL  set to play through deaths (measures boss fights and late pacing)
#   GRIN_SKILL     0..1, how well the bot dodges (default 0.7)
#   GRIN_SHOTS     comma-separated survival times to screenshot (needs a real renderer)
#   GRIN_SHOT_DIR  where screenshots go (default user://)

var game: Node
var player: Node2D
var run_state: Node
var run_number := 0
var runs := 1
var max_time := 1500.0
var immortal := false
var skill := 0.7
var next_log := 30.0
var boss_active := false
var boss_since := 0.0
# Each boss on the field, by instance id, with when it arrived.
var boss_seen := {}
var most_bosses := 0
var levels := 0
var moon_count := 0
var drops_seen := 0
var known_pickups := {}
var press_toggle := false
var results: Array[String] = []
var shot_times: Array[float] = []
var shot_dir := "user://"
var nova_shot := false

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	if OS.get_environment("GRIN_RUNS") != "":
		runs = int(OS.get_environment("GRIN_RUNS"))
	if OS.get_environment("GRIN_MAX") != "":
		max_time = float(OS.get_environment("GRIN_MAX"))
	if OS.get_environment("GRIN_SKILL") != "":
		skill = float(OS.get_environment("GRIN_SKILL"))
	immortal = OS.get_environment("GRIN_IMMORTAL") != ""
	for part in OS.get_environment("GRIN_SHOTS").split(",", false):
		shot_times.append(float(part))
	if OS.get_environment("GRIN_SHOT_DIR") != "":
		shot_dir = OS.get_environment("GRIN_SHOT_DIR")
	call_deferred("_start_run")

func _start_run() -> void:
	# Let the previous run leave the tree completely before the next one looks for it.
	await get_tree().process_frame
	await get_tree().process_frame
	run_number += 1
	next_log = 30.0
	boss_active = false
	boss_seen.clear()
	most_bosses = 0
	levels = 0
	moon_count = 0
	drops_seen = 0
	known_pickups.clear()
	game = load("res://scenes/game.tscn").instantiate()
	get_tree().root.add_child(game)
	get_tree().current_scene = game
	await get_tree().process_frame
	player = game.get_node("player")
	player.set("Invulnerable", immortal)
	run_state = game.get_node("RunState")
	run_state.connect("AbilityUnlocked", func(a): _log("UNLOCK %s" % ["DASH", "OVERDRIVE", "NOVA"][a]))
	_log("RUN %d start (immortal=%s skill=%.2f)" % [run_number, immortal, skill])

func _now() -> float:
	return run_state.get("SurvivalTime") if is_instance_valid(run_state) else 0.0

func _log(text: String) -> void:
	var t := _now()
	print("[%02d:%05.2f] %s" % [int(t / 60.0), fmod(t, 60.0), text])

func _physics_process(_delta: float) -> void:
	if not is_instance_valid(game) or not is_instance_valid(player):
		return
	if game.get("IsOver"):
		_finish("died to %s" % game.get("DeathCause"))
		return
	var t := _now()
	if t >= max_time:
		_finish("reached the time limit")
		return
	_watch(t)
	_spend_core()
	_drive()
	_capture(t)

func _finish(reason: String) -> void:
	var t := _now()
	var summary := "RUN %d END %s at %02d:%02d  kills=%d levels=%d bosses=%d most_at_once=%d" % [
		run_number, reason, int(t / 60.0), int(fmod(t, 60.0)), run_state.get("Kills"), run_state.get("TotalLevels"),
		game.get("NextBossIndex") - int(game.get("ActiveBossCount")), most_bosses]
	print(summary)
	results.append(summary)
	for action in ["left", "right", "up", "down", "aim_left", "aim_right", "aim_up", "aim_down", "shoot", "dash", "rapid_fire", "nova"]:
		Input.action_release(action)
	var dying := game
	game = null
	player = null
	dying.queue_free()
	Engine.time_scale = 1.0
	if run_number < runs:
		call_deferred("_start_run")
	else:
		print("---- SUMMARY ----")
		for line in results:
			print(line)
		get_tree().quit()

func _watch(t: float) -> void:
	var live := {}
	for b in get_tree().get_nodes_in_group("bosses"):
		var id: int = b.get_instance_id()
		live[id] = true
		if not boss_seen.has(id):
			boss_seen[id] = [t, str(b.get("BossName"))]
			_log("BOSS %s arrives (cycle %d, %d hp), %d enemies alive, %d bosses up" % [b.get("BossName"), b.get("Cycle"), b.get("ScaledMaxHealth"), get_tree().get_nodes_in_group("bodies").size(), live.size()])
	for id in boss_seen.keys():
		if not live.has(id):
			_log("BOSS %s defeated after %.1fs" % [boss_seen[id][1], t - boss_seen[id][0]])
			boss_seen.erase(id)
	most_bosses = max(most_bosses, live.size())
	boss_active = live.size() > 0

	for pickup in get_tree().get_nodes_in_group("pickups"):
		var id := pickup.get_instance_id()
		if not known_pickups.has(id):
			known_pickups[id] = true
			drops_seen += 1

	var now_levels: int = run_state.get("TotalLevels")
	if now_levels != levels:
		levels = now_levels
		_log("UPGRADE -> %d levels" % levels)
	var moons: int = run_state.get("Moons")
	if moons != moon_count:
		moon_count = moons
		_log("MOONS now %d" % moons)

	if t >= next_log:
		next_log += 30.0
		_log("alive=%d kills=%d pickups=%d levels=%d bought=%d core=%d%% banked=%d moons=%d" % [get_tree().get_nodes_in_group("bodies").size(), run_state.get("Kills"), drops_seen, levels, run_state.get("UpgradesBought"), int(run_state.get("CoreFraction") * 100), run_state.get("Banked"), moon_count])

func _set_axis(negative: String, positive: String, value: float) -> void:
	Input.action_release(negative)
	Input.action_release(positive)
	if value > 0.05:
		Input.action_press(positive, clampf(value, 0.0, 1.0))
	elif value < -0.05:
		Input.action_press(negative, clampf(-value, 0.0, 1.0))

func _tap(action: String, want: bool) -> void:
	# Abilities trigger on a fresh press, so a held want becomes a stream of taps.
	if want and press_toggle:
		Input.action_press(action)
	else:
		Input.action_release(action)

func _drive() -> void:
	press_toggle = not press_toggle
	var here := player.global_position
	var bodies := get_tree().get_nodes_in_group("bodies")
	var bosses := get_tree().get_nodes_in_group("bosses")
	var push := Vector2.ZERO
	var nearest: Node2D = null
	var nearest_d := INF
	var close := 0
	var crowd := 0
	var sense := lerpf(380.0, 720.0, skill)

	for body in bodies:
		var offset: Vector2 = here - body.global_position
		var d := offset.length()
		if d < nearest_d:
			nearest_d = d
			nearest = body
		if d < 180.0:
			close += 1
		if d < 650.0:
			crowd += 1
		if d < sense and d > 1.0:
			var w := pow((sense - d) / sense, 2.0) * (2.2 if body.get("Kind") == 6 else 1.0)
			push += offset / d * w
	var shot_close := false
	for shot in get_tree().get_nodes_in_group("hostile_bullets"):
		var offset: Vector2 = here - shot.global_position
		var d := offset.length()
		var incoming: Vector2 = shot.get("Direction")
		if d < lerpf(70.0, 120.0, skill) and incoming.dot(offset) > 0.0:
			shot_close = true
		if d < 300.0 * skill + 60.0 and d > 1.0:
			var dir: Vector2 = shot.get("Direction")
			var side := dir.orthogonal()
			if side.dot(offset) < 0.0:
				side = -side
			push += side * pow((360.0 - d) / 360.0, 2.0) * 1.6
	var boss: Node2D = null
	for b in bosses:
		if boss == null or here.distance_to(b.global_position) < here.distance_to(boss.global_position):
			boss = b
		var offset: Vector2 = here - b.global_position
		var d := offset.length()
		var keep := 520.0
		if d < keep and d > 1.0:
			push += offset / d * pow((keep - d) / keep, 1.5) * 2.5
		elif d > 900.0:
			push -= offset / d * 0.35

	# Step off any comet's warned line, the way a player reading it would.
	for child in game.get_node("Entities").get_children():
		var script = child.get_script()
		if script and script.resource_path.ends_with("CometFlyby.cs"):
			var a: Vector2 = child.get("PathStart")
			var b: Vector2 = child.get("PathEnd")
			var closest := Geometry2D.get_closest_point_to_segment(here, a, b)
			var off := here - closest
			if off.length() < 220.0 * skill + 40.0:
				push += (off.normalized() if off.length() > 1.0 else (b - a).orthogonal().normalized()) * 3.0

	var play := Rect2(Vector2(320, 320), Vector2(5860 - 640, 3860 - 640))
	var margin := 420.0
	push.x += clampf((play.position.x + margin - here.x) / margin, 0.0, 1.0) * 1.8
	push.x -= clampf((here.x - (play.end.x - margin)) / margin, 0.0, 1.0) * 1.8
	push.y += clampf((play.position.y + margin - here.y) / margin, 0.0, 1.0) * 1.8
	push.y -= clampf((here.y - (play.end.y - margin)) / margin, 0.0, 1.0) * 1.8

	var danger := push.length()
	var nearest_pickup: Node2D = null
	var pickup_d := INF
	for pickup in get_tree().get_nodes_in_group("pickups"):
		var d := here.distance_to(pickup.global_position)
		if d < pickup_d:
			pickup_d = d
			nearest_pickup = pickup
	if nearest_pickup and pickup_d < 1100.0 and danger < 0.9:
		push += (nearest_pickup.global_position - here).normalized() * 0.8
	elif danger < 0.25:
		# Nothing pressing: circle the arena centre rather than parking.
		var to_centre := Vector2(2930, 1930) - here
		push += to_centre.normalized().orthogonal() * 0.5 + to_centre / 4000.0

	var move := push.normalized() if push.length() > 0.05 else Vector2.ZERO
	_set_axis("left", "right", move.x)
	_set_axis("up", "down", move.y)

	var target: Node2D = nearest
	# Like a player would: the boss is the target unless something is about to touch you.
	if boss and here.distance_to(boss.global_position) < 950.0 and (nearest == null or nearest_d > 240.0):
		target = boss
	if target:
		var aim := (target.global_position - here).normalized()
		_set_axis("aim_left", "aim_right", aim.x)
		_set_axis("aim_up", "aim_down", aim.y)
		Input.action_press("shoot")
	else:
		Input.action_release("shoot")

	var boss_near := boss != null and here.distance_to(boss.global_position) < 700.0
	_tap("dash", (close > 0 and nearest_d < lerpf(110.0, 170.0, skill)) or shot_close)
	_tap("rapid_fire", crowd >= 6 or boss_near)
	_tap("nova", crowd >= 10 or boss_near or (close >= 2 and nearest_d < 120.0))


func _capture(t: float) -> void:
	if DisplayServer.get_name() == "headless":
		return
	if not shot_times.is_empty() and t >= shot_times[0]:
		shot_times.pop_front()
		_save("t%04d" % int(t))
	if not nova_shot:
		for child in game.get_node("Entities").get_children():
			var script = child.get_script()
			if script and script.resource_path.ends_with("NovaBlast.cs"):
				nova_shot = true
				await get_tree().create_timer(0.12, true, false, true).timeout
				_save("nova")
				break

func _save(label: String) -> void:
	await RenderingServer.frame_post_draw
	var path := "%s/run%d_%s.png" % [shot_dir, run_number, label]
	get_viewport().get_texture().get_image().save_png(path)
	_log("SHOT %s" % path)

# Buys an upgrade the moment the CORE bar is full, the way a player who taps
# the upgrade key straight away would. Order: gun first, then the abilities.
# Ids follow RunUpgradeId: 0 FireRate, 1 Spread, 2 Piercing, 3 DashReach,
# 4 DashBlink, 5 OverdrivePower, 6 OverdriveTime, 7 BiggerNova, 8 NovaPower.
const BUY_ORDER := [0, 1, 0, 2, 5, 7, 0, 1, 3, 5, 8, 2, 7, 6, 3, 4, 5, 7, 8, 6, 4, 3]

func _spend_core() -> void:
	if not run_state.get("CoreReady"):
		return
	for id in BUY_ORDER:
		if game.call("BuyUpgradeForTools", id):
			return
