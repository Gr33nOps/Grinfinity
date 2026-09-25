extends Node
# Development-only: six kill pops started 0.07 s apart, captured in one frame,
# so the whole pop animation can be judged at a glance. Release exports exclude tools/.

func _ready() -> void:
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
	await get_tree().create_timer(0.8).timeout
	var centre := player.global_position + Vector2(0, -260)
	var tints := [Color(0.91, 0.35, 0.45), Color(0.62, 1.0, 0.78), Color(0.66, 0.76, 1.0)]
	for i in range(5, -1, -1):
		var pop = load("res://scripts/PopEffect.cs").new()
		pop.set("Size", 58.0)
		pop.set("Tint", tints[i % 3])
		pop.set("Stars", 7)
		pop.global_position = centre + Vector2(-750 + i * 300, 0)
		game.call("AddEntity", pop)
		await get_tree().create_timer(0.07).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(OS.get_environment("GRIN_SHOT"))
	get_tree().quit()
