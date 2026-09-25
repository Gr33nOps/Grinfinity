shader_type canvas_item;

// The arena's sky. Drawn on a full-screen rect behind the world and scrolled
// by the camera at a fraction of its speed, so it reads as far away and the
// player can feel themselves moving. Everything here is kept low-contrast:
// enemies, shots and pickups have to stay the brightest things on screen.
//
// Same family as the main menu: plum space, soft wavy ribbons, warm cream
// stars. Nebula uses the menu's berry and indigo rather than new colours.

uniform vec2 camera = vec2(0.0);          // top-left of the view, in world units
uniform vec2 view_size = vec2(1920.0, 1080.0);

uniform vec4 space_top : source_color = vec4(0.09, 0.066, 0.15, 1.0);
uniform vec4 space_bottom : source_color = vec4(0.114, 0.078, 0.172, 1.0);
uniform vec4 nebula_indigo : source_color = vec4(0.176, 0.161, 0.4, 1.0);
uniform vec4 nebula_plum : source_color = vec4(0.333, 0.157, 0.365, 1.0);
uniform vec4 nebula_berry : source_color = vec4(0.455, 0.196, 0.329, 1.0);
uniform vec4 ribbon_color : source_color = vec4(0.62, 0.38, 0.58, 1.0);
uniform vec4 star_tint : source_color = vec4(1.0, 0.94, 0.81, 1.0);

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
	f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
	float a = hash21(i);
	float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0));
	float d = hash21(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

const mat2 OCTAVE_ROT = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));

float fbm(vec2 p) {
	float total = 0.0;
	float amplitude = 0.5;
	float norm = 0.0;
	for (int i = 0; i < 4; i++) {
		total += amplitude * value_noise(p);
		norm += amplitude;
		p = OCTAVE_ROT * p * 2.03 + 3.1;
		amplitude *= 0.5;
	}
	return total / norm;
}

// At most one star per cell, with a soft round core.
float stars(vec2 p, float cell, float radius, float density, float t) {
	vec2 grid = p / cell;
	vec2 id = floor(grid);
	vec2 local = fract(grid);
	vec2 rnd = hash22(id);
	float present = step(1.0 - density, hash21(id + 7.13));
	float magnitude = 0.5 + 0.5 * hash21(id + 3.71);
	float twinkle = 0.75 + 0.25 * sin(t * (1.1 + rnd.x * 2.4) + rnd.y * 6.2831);
	float dist = length(local - (0.15 + rnd * 0.7)) * cell;
	return present * magnitude * twinkle * smoothstep(radius, 0.0, dist);
}

// A soft wavy band, like the ribbons across the main menu.
float ribbon(vec2 p, float y, float width, float wave, float phase) {
	float centre = y + sin(p.x * 0.0011 + phase) * wave + sin(p.x * 0.0027 + phase * 1.7) * wave * 0.35;
	float d = abs(p.y - centre);
	return smoothstep(width, width * 0.35, d);
}

void fragment() {
	vec2 screen = UV * view_size;
	float t = mod(TIME, 10000.0);

	vec3 col = mix(space_top.rgb, space_bottom.rgb, UV.y);

	// Nebula, very far: barely moves as the camera does.
	vec2 far = screen + camera * 0.08;
	float n1 = fbm(far * 0.0009 + vec2(t * 0.004, 0.0));
	float n2 = fbm(far * 0.0016 + vec2(11.0, -t * 0.003));
	float cloud = smoothstep(0.45, 0.85, n1 * 0.7 + n2 * 0.45);
	vec3 nebula = mix(nebula_indigo.rgb, nebula_plum.rgb, smoothstep(0.35, 0.7, n2));
	nebula = mix(nebula, nebula_berry.rgb, smoothstep(0.62, 0.8, n1) * 0.6);
	col = mix(col, nebula, cloud * 0.26);

	// Ribbons, a little nearer. Repeat down the arena so every view has one.
	vec2 mid = screen + camera * 0.18;
	float bands = 0.0;
	float spacing = 820.0;
	float row = floor(mid.y / spacing);
	for (int i = -1; i <= 1; i++) {
		float r = row + float(i);
		float y = (r + 0.5) * spacing;
		bands += ribbon(mid, y, 90.0 + 40.0 * hash21(vec2(r, 1.0)), 120.0, r * 2.3);
	}
	col = mix(col, ribbon_color.rgb, clamp(bands, 0.0, 1.0) * 0.035);

	// Two star layers at different depths.
	float s = stars(screen + camera * 0.22, 54.0, 1.2, 0.4, t) * 0.22
		+ stars(screen + camera * 0.45, 118.0, 1.9, 0.45, t) * 0.38;
	col += star_tint.rgb * s;

	// A soft vignette keeps the eye on the middle, where the planet is.
	vec2 v = UV - 0.5;
	col *= 1.0 - dot(v, v) * 0.55;

	COLOR = vec4(col, 1.0);
}
