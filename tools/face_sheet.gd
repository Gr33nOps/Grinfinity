extends Node2D
# Development-only: lays every face out large on one sheet and saves it, so the
# moods from tools/make_moods.py can be judged side by side. Release exports
# exclude tools/.
#   GRIN_SHOT  output path (default user://faces.png)

const NAMES := ["face", "face_blink", "face_happy",
	"body_drifter", "body_drifter_2", "body_drifter_3", "body_shard", "body_shard_2",
	"body_planetoid", "body_planetoid_2", "body_fracture", "body_fracture_mini", "body_satellite",
	"body_flare", "body_bulwark", "boss_coil", "boss_brood", "boss_black_hole"]
const CELL := 300.0

func _ready() -> void:
	RenderingServer.set_default_clear_color(Color("1d1426"))
	var font: Font = load("res://fonts/LilitaOne.ttf")
	for i in NAMES.size():
		var at := Vector2(40 + (i % 6) * CELL, 30 + (i / 6) * (CELL + 30))
		if NAMES[i].begins_with("face"):
			# The planet's face is drawn over a planet, so show it on one.
			var planet := Sprite2D.new()
			planet.texture = load("res://art/cosmic/planet_2.svg")
			planet.position = at + Vector2(CELL, CELL) * 0.45
			planet.scale = Vector2.ONE * 1.05
			add_child(planet)
		var sprite := Sprite2D.new()
		sprite.texture = load("res://art/cosmic/%s.svg" % NAMES[i])
		sprite.position = at + Vector2(CELL, CELL) * 0.45
		sprite.scale = Vector2.ONE * 1.05
		add_child(sprite)
		var label := Label.new()
		label.text = NAMES[i]
		label.position = at + Vector2(0, CELL - 18)
		label.add_theme_font_override("font", font)
		label.add_theme_font_size_override("font_size", 22)
		add_child(label)
	await get_tree().process_frame
	await RenderingServer.frame_post_draw
	var path := OS.get_environment("GRIN_SHOT")
	if path == "":
		path = "user://faces.png"
	get_viewport().get_texture().get_image().save_png(path)
	print("SHOT ", path)
	get_tree().quit()
