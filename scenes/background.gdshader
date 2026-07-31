shader_type canvas_item;

uniform float speed = .05;

void vertex() {
	// fract() keeps the scroll offset inside 0..1 so float precision does not
	// drift (and visibly judder) during a long session. texture_repeat is on,
	// so wrapping at 1.0 is seamless.
	float offset = fract(TIME * speed);
	UV.x += offset;
	UV.y += offset;
}
