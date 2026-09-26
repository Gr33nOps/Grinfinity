extends Control
# Development-only: every upgrade's preview loop side by side, captured at two
# moments so each animation can be judged. Release exports exclude tools/.
#   GRIN_SHOT_DIR  where the images go (default user://)

const NAMES := ["Faster Shots", "Spread Shot", "Piercing", "Dash Reach", "Dash Blink",
	"Overdrive Power", "Overdrive Time", "Bigger Nova", "Nova Power"]

func _ready() -> void:
	RenderingServer.set_default_clear_color(Color("392339"))
	var font: Font = load("res://fonts/LilitaOne.ttf")
	var preview_script = load("res://scripts/UpgradePreview.cs")
	for i in NAMES.size():
		var at := Vector2(40 + (i % 3) * 300, 30 + (i / 3) * 190)
		var preview: Control = preview_script.new()
		preview.position = at
		preview.size = Vector2(240, 132)
		add_child(preview)
		preview.call("Play", i)
		var label := Label.new()
		label.text = NAMES[i]
		label.position = at + Vector2(0, 138)
		label.add_theme_font_override("font", font)
		label.add_theme_font_size_override("font_size", 20)
		add_child(label)
	var dir := OS.get_environment("GRIN_SHOT_DIR")
	if dir == "":
		dir = "user://"
	for moment in [0.75, 1.45]:
		await get_tree().create_timer(moment if moment < 1.0 else moment - 0.75).timeout
		await RenderingServer.frame_post_draw
		var path := "%s/previews_%d.png" % [dir, int(moment * 100)]
		get_viewport().get_texture().get_image().save_png(path)
		print("SHOT ", path)
	get_tree().quit()
