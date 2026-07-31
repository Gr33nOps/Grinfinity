shader_type canvas_item;

// Deep space, drawn procedurally: a slow nebula wash with three parallax star
// layers over it. Doing it in a shader instead of four 1920x1080 PNGs keeps the
// look resolution-independent and costs no VRAM, which matters because the
// stretch aspect is "expand" and the real viewport is not always the 1920 base.

uniform vec4 space_color : source_color = vec4(0.064, 0.011, 0.083, 1.0);
uniform vec4 nebula_warm : source_color = vec4(0.44, 0.13, 0.56, 1.0);
uniform vec4 nebula_cool : source_color = vec4(0.11, 0.09, 0.42, 1.0);
// Deliberately low. Standing rule 3: readability beats spectacle, and pink
// bodies have to stay legible against whatever this draws.
uniform float nebula_strength : hint_range(0.0, 1.0) = 0.17;
uniform float nebula_speed = 0.006;

// Per-layer: far, mid, near. Larger cells mean sparser stars.
uniform vec3 star_cell = vec3(38.0, 72.0, 138.0);
uniform vec3 star_scroll = vec3(3.5, 9.0, 20.0);
uniform vec3 star_radius = vec3(1.1, 1.8, 3.0);
uniform vec3 star_brightness = vec3(0.30, 0.55, 0.95);
uniform vec4 star_tint : source_color = vec4(0.93, 0.86, 1.0, 1.0);

// The cheap fract(sin(dot(...))) style hashes leave straight, correlated edges
// that fbm turns into visible polygons. This one decorrelates all three
// components against each other and shows no structure at these scales.
float hash21(vec2 p) {
	vec3 p3 = fract(vec3(p.x, p.y, p.x) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

vec2 hash22(vec2 p) {
	vec3 p3 = fract(vec3(p.x, p.y, p.x) * vec3(0.1031, 0.1030, 0.0973));
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.xx + p3.yz) * p3.zy);
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	// Quintic, not smoothstep. Cubic leaves a second-derivative kink at every
	// cell edge, which the nebula's smoothstep then turns into a visible grid.
	f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
	float a = hash21(i);
	float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0));
	float d = hash21(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

// Rotating between octaves stops every layer sharing the same axes, which is
// the other half of why value-noise fbm reads as rectangles.
const mat2 OCTAVE_ROT = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));

float fbm(vec2 p) {
	float total = 0.0;
	float amplitude = 0.5;
	float norm = 0.0;
	for (int i = 0; i < 5; i++) {
		total += amplitude * value_noise(p);
		norm += amplitude;
		p = OCTAVE_ROT * p * 2.07 + 3.1;
		amplitude *= 0.52;
	}
	return total / norm;
}

// One star per grid cell, at most. The radius is far smaller than a cell, so a
// single cell lookup is enough — no neighbourhood loop needed.
float star_layer(vec2 px, float cell, float radius, float scroll, float t) {
	// Wrapping the scroll at an exact cell multiple keeps the offset small
	// forever without a visible jump: the grid maps onto itself.
	vec2 offset = mod(vec2(scroll * t, scroll * t * 0.38), cell);
	vec2 grid = (px + offset) / cell;
	vec2 id = floor(grid);
	vec2 local = fract(grid);

	vec2 rnd = hash22(id);
	float present = step(0.52, hash21(id + 7.13));
	float magnitude = 0.55 + 0.45 * hash21(id + 3.71);
	float twinkle = 0.78 + 0.22 * sin(t * (1.4 + rnd.x * 3.2) + rnd.y * 6.2831);

	float dist = length(local - rnd) * cell;
	return present * magnitude * twinkle * smoothstep(radius, 0.0, dist);
}

void fragment() {
	// UV spans the full viewport, so this recovers real pixel coordinates and
	// keeps star density and size constant at any resolution.
	vec2 px = UV / SCREEN_PIXEL_SIZE;
	float t = mod(TIME, 100000.0);

	vec2 np = px * 0.0011;
	float drift = t * nebula_speed;
	float n1 = fbm(np + vec2(drift, drift * 0.62));
	float n2 = fbm(np * 1.73 + vec2(-drift * 0.7, drift * 0.31) + 11.0);
	// Multiplying two fields carves voids as well as clouds, so the wash reads
	// as structure rather than fog.
	float cloud = smoothstep(0.42, 1.02, n1 * 0.66 + n2 * 0.56);

	vec3 col = space_color.rgb;
	col = mix(col, mix(nebula_cool.rgb, nebula_warm.rgb, n2), cloud * nebula_strength);

	float stars =
		star_layer(px, star_cell.x, star_radius.x, star_scroll.x, t) * star_brightness.x +
		star_layer(px, star_cell.y, star_radius.y, star_scroll.y, t) * star_brightness.y +
		star_layer(px, star_cell.z, star_radius.z, star_scroll.z, t) * star_brightness.z;

	col += star_tint.rgb * stars;

	COLOR = vec4(col, 1.0);
}
