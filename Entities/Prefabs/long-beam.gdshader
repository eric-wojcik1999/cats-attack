shader_type spatial;
render_mode unshaded, cull_disabled, blend_add;

// Main colors
uniform vec4 base_yellow  : source_color = vec4(1.0, 0.88, 0.10, 1.0);
uniform vec4 dark_yellow  : source_color = vec4(0.75, 0.55, 0.02, 1.0);
uniform vec4 light_yellow : source_color = vec4(1.0, 1.0, 0.55, 1.0);

// Transparency / glow
uniform float beam_alpha : hint_range(0.0, 1.0) = 0.32;
uniform float emission_strength : hint_range(0.0, 20.0) = 5.2;

// Pixelation controls
uniform float pixel_length : hint_range(4.0, 256.0) = 96.0;
uniform float pixel_around : hint_range(2.0, 64.0) = 12.0;

// Main streak controls
uniform float streak_scale : hint_range(1.0, 100.0) = 34.0;
uniform float streak_speed : hint_range(0.0, 20.0) = 8.5;
uniform float streak_contrast : hint_range(0.0, 2.0) = 1.15;

// Fast particle controls
uniform float particle_density : hint_range(8.0, 400.0) = 140.0;
uniform float particle_speed : hint_range(0.0, 40.0) = 20.0;
uniform float particle_sharpness : hint_range(1.0, 50.0) = 18.0;
uniform float particle_brightness : hint_range(0.0, 4.0) = 1.5;

// Edge glow
uniform float fresnel_power : hint_range(0.1, 8.0) = 2.2;
uniform float fresnel_strength : hint_range(0.0, 3.0) = 1.2;

// If the streaks run the wrong direction, toggle this
uniform bool flip_uv = false;

void fragment() {
	vec2 uv = UV;
	float u = flip_uv ? uv.y : uv.x;
	float v = flip_uv ? uv.x : uv.y;

	// Pixelate UVs for blocky style
	float pu = floor(u * pixel_length) / pixel_length;
	float pv = floor(v * pixel_around) / pixel_around;

	// Main fast moving streak layers
	float s1 = sin(pu * streak_scale - TIME * streak_speed) * 0.5 + 0.5;
	float s2 = sin(pu * streak_scale * 1.9 + pv * 14.0 + TIME * streak_speed * 1.45) * 0.5 + 0.5;
	float s3 = sin(pu * streak_scale * 0.8 - pv * 20.0 - TIME * streak_speed * 0.85) * 0.5 + 0.5;

	float streaks = s1 * 0.42 + s2 * 0.36 + s3 * 0.22;
	streaks = smoothstep(0.12, 0.9, streaks);

	// Tiny bright "zipping particles"
	// These appear as short bright dashes travelling very quickly along the beam.
	float particle_wave_1 = fract(pu * particle_density - TIME * particle_speed + pv * 0.35);
	float particle_wave_2 = fract(pu * (particle_density * 0.72) - TIME * (particle_speed * 1.35) - pv * 0.6);

	float particle_1 = pow(1.0 - particle_wave_1, particle_sharpness);
	float particle_2 = pow(1.0 - particle_wave_2, particle_sharpness * 0.85);

	// Mask them so they don't appear everywhere uniformly
	float particle_mask = sin(pv * 40.0 + TIME * 3.0) * 0.5 + 0.5;
	particle_mask = smoothstep(0.15, 0.95, particle_mask);

	float particles = max(particle_1, particle_2) * particle_mask;
	particles *= particle_brightness;

	// Base color variation
	vec3 col = mix(dark_yellow.rgb, base_yellow.rgb, streaks);
	col = mix(col, light_yellow.rgb, pow(streaks, 2.0) * streak_contrast);

	// Let particles brighten toward near-white yellow
	col = mix(col, light_yellow.rgb, clamp(particles * 0.7, 0.0, 1.0));

	// Pulse
	float pulse = 0.94 + 0.06 * sin(TIME * 12.0 + pu * 45.0);

	// Fresnel glow around edges
	float fresnel = pow(
		1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0),
		fresnel_power
	);

	float glow = 0.5 + streaks * 0.2 + fresnel * fresnel_strength + particles * 1.2;

	ALBEDO = col;
	EMISSION = col * emission_strength * glow * pulse;
	ALPHA = clamp(beam_alpha + fresnel * 0.16 + streaks * 0.06 + particles * 0.12, 0.0, 1.0);
}