extends Control

# Renders the existing game art and typography as a store cover.
func _ready() -> void:
	await get_tree().process_frame
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	DisplayServer.window_set_size(Vector2i(630, 500))
	get_window().content_scale_size = Vector2i(630, 500)
	var planet := Sprite2D.new()
	planet.texture = load("res://sprites/player 1.png")
	planet.scale = Vector2.ONE * (220.0 / planet.texture.get_width())
	planet.position = Vector2(315, 264)
	add_child(planet)
	_label("GRINFINITY", 72, 38, Color.WHITE)
	await get_tree().create_timer(0.5).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(OS.get_environment("GRIN_SHOT"))
	get_node("/root/GameSettings").call("QuitGame")

func _label(text: String, font_size: int, y: int, color: Color) -> void:
	var label := Label.new()
	label.text = text
	label.position = Vector2(0, y)
	label.size.x = 630
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.add_theme_font_override("font", load("res://fonts/LilitaOne.ttf"))
	label.add_theme_font_size_override("font_size", font_size)
	label.add_theme_color_override("font_color", color)
	add_child(label)
