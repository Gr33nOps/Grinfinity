shader_type canvas_item;

// High-contrast accessibility outline: a solid line traced around a sprite's
// alpha edge, so a shape reads against the deep-space background even when
// colour alone does not carry enough contrast for a given player.

uniform vec4 line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float line_thickness : hint_range(0.0, 4.0) = 1.5;

void fragment() {
	vec4 base = texture(TEXTURE, UV);

	// Godot's shading language does not allow an early return from fragment(),
	// so the two cases are an if/else rather than a guard clause.
	if (base.a > 0.5) {
		COLOR = base;
	} else {
		vec2 texel = TEXTURE_PIXEL_SIZE * line_thickness;
		float neighbour_alpha = 0.0;
		neighbour_alpha = max(neighbour_alpha, texture(TEXTURE, UV + vec2(texel.x, 0.0)).a);
		neighbour_alpha = max(neighbour_alpha, texture(TEXTURE, UV - vec2(texel.x, 0.0)).a);
		neighbour_alpha = max(neighbour_alpha, texture(TEXTURE, UV + vec2(0.0, texel.y)).a);
		neighbour_alpha = max(neighbour_alpha, texture(TEXTURE, UV - vec2(0.0, texel.y)).a);
		neighbour_alpha = max(neighbour_alpha, texture(TEXTURE, UV + texel).a);
		neighbour_alpha = max(neighbour_alpha, texture(TEXTURE, UV - texel).a);
		neighbour_alpha = max(neighbour_alpha, texture(TEXTURE, UV + vec2(texel.x, -texel.y)).a);
		neighbour_alpha = max(neighbour_alpha, texture(TEXTURE, UV + vec2(-texel.x, texel.y)).a);

		COLOR = vec4(line_color.rgb, line_color.a * step(0.5, neighbour_alpha));
	}
}
