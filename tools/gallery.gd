extends Node
# Development-only asset gallery; release exports exclude tools/. Lays out one of
# every enemy and every pickup next to the planet, then each boss in turn, and
# saves a screenshot of each so the art can be reviewed side by side.
#   GRIN_SHOT_DIR  where the images go (default user://)

var dir := "user://"

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	if OS.get_environment("GRIN_SHOT_DIR") != "":
		dir = OS.get_environment("GRIN_SHOT_DIR")
	call_deferred("_run")

func _run() -> void:
	var game: Node = load("res://scenes/game.tscn").instantiate()
	get_tree().root.add_child(game)
	get_tree().current_scene = game
	await get_tree().process_frame
	for node in ["BodySpawner", "HazardDirector", "EventDirector"]:
		game.get_node(node).process_mode = Node.PROCESS_MODE_DISABLED
	var player: Node2D = game.get_node("player")
	player.set("Invulnerable", true)
	player.process_mode = Node.PROCESS_MODE_DISABLED
	var run = game.get_node("RunState")
	run.call("GrantShield")
	await get_tree().create_timer(1.0).timeout
	var centre := player.global_position

	var body_scene: PackedScene = load("res://scenes/body.tscn")
	for i in 8:
		var body = body_scene.instantiate()
		body.call("Configure", i)
		body.global_position = centre + Vector2(-760 + i * 215, -300)
		game.call("AddEntity", body)
		body.process_mode = Node.PROCESS_MODE_DISABLED

	for i in 9:
		game.call("PlaceRewardForTools", i, centre + Vector2(-700 + i * 175, 430))
	await get_tree().create_timer(0.8).timeout
	await _shoot("gallery_bestiary")

	for index in 3:
		for b in get_tree().get_nodes_in_group("bosses"):
			b.queue_free()
		game.set("NextBossIndex", index)
		game.set("NextBossAt", 0.0)
		for i in 90:
			await get_tree().process_frame
			if game.get("BossActive"):
				break
		await get_tree().create_timer(1.6).timeout
		await _shoot("gallery_boss_%d" % index)
	get_tree().quit()

func _shoot(label: String) -> void:
	await RenderingServer.frame_post_draw
	var path := "%s/%s.png" % [dir, label]
	get_viewport().get_texture().get_image().save_png(path)
	print("SHOT ", path)
