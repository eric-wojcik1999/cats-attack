shader_type canvas_item;
render_mode unshaded;


// =====================================================
// PIXEL / DITHER SETTINGS
// =====================================================

uniform float pixel_columns = 180.0;
uniform float pixel_rows = 100.0;

uniform float dither_scale = 90.0;
uniform float dither_strength = 0.14;


// =====================================================
// LIQUID MOTION
// =====================================================

uniform float speed = 0.18;

uniform float flow_scale = 1.8;
uniform float warp_strength = 0.22;

uniform float large_wave_strength = 0.14;
uniform float detail_strength = 0.025;


// =====================================================
// PALETTE TIMING
// =====================================================

uniform float palette_speed = 0.035;


// =====================================================
// CAT PARTICLES
// =====================================================

uniform float cat_columns = 10.0;
uniform float cat_rows = 7.0;

uniform float cat_size = 0.24;

uniform float cat_flow_speed = 0.045;
uniform float cat_sideways_drift = 0.25;

uniform float cat_opacity = 0.48;
uniform float cat_brightness = 1.10;


// =====================================================
// RANDOM / NOISE
// =====================================================

float hash(vec2 p)
{
	return fract(
		sin(dot(p, vec2(127.1, 311.7)))
		* 43758.5453123
	);
}


vec2 hash2(vec2 p)
{
	return vec2(
		hash(p),
		hash(p + vec2(37.2, 91.7))
	);
}


float noise(vec2 p)
{
	vec2 i = floor(p);
	vec2 f = fract(p);

	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));

	vec2 u = f * f * (3.0 - 2.0 * f);

	return mix(a, b, u.x)
		+ (c - a) * u.y * (1.0 - u.x)
		+ (d - b) * u.x * u.y;
}


float fbm(vec2 p)
{
	float v = 0.0;
	float a = 0.5;

	for (int i = 0; i < 5; i++)
	{
		v += noise(p) * a;

		p *= 2.0;
		a *= 0.5;
	}

	return v;
}


// =====================================================
// DITHER
// =====================================================

float dither_pattern(vec2 p)
{
	vec2 cell = floor(p);

	float pattern =
		mod(
			cell.x
			+ cell.y * 2.0,
			4.0
		);

	if (pattern < 1.0)
		return 0.15;

	if (pattern < 2.0)
		return 0.65;

	if (pattern < 3.0)
		return 0.35;

	return 0.85;
}


// =====================================================
// PALETTE STATES
// =====================================================

vec3 palette_color_a(float state)
{
	float s = mod(state, 5.0);

	if (s < 1.0)
		return vec3(0.04, 0.20, 0.78); // deep blue

	if (s < 2.0)
		return vec3(0.42, 0.13, 0.82); // purple

	if (s < 3.0)
		return vec3(0.63, 0.34, 0.92); // lavender

	if (s < 4.0)
		return vec3(0.35, 0.15, 0.73); // violet

	return vec3(0.08, 0.25, 0.82); // blue-violet
}


vec3 palette_color_b(float state)
{
	float s = mod(state, 5.0);

	if (s < 1.0)
		return vec3(1.00, 0.08, 0.66); // hot pink

	if (s < 2.0)
		return vec3(1.00, 0.25, 0.70); // pink

	if (s < 3.0)
		return vec3(1.00, 0.68, 0.78); // pale peach-pink

	if (s < 4.0)
		return vec3(0.94, 0.35, 0.82); // pink-violet

	return vec3(0.90, 0.12, 0.72); // magenta
}


vec3 palette_color_c(float state)
{
	float s = mod(state, 5.0);

	if (s < 1.0)
		return vec3(0.72, 0.12, 0.88); // purple-magenta

	if (s < 2.0)
		return vec3(0.70, 0.24, 0.93); // violet

	if (s < 3.0)
		return vec3(0.96, 0.84, 0.74); // pale peach

	if (s < 4.0)
		return vec3(0.72, 0.49, 1.00); // lavender

	return vec3(0.45, 0.15, 0.88); // purple
}


vec3 get_palette_color(
	float state,
	float blend,
	int index
)
{
	vec3 current;
	vec3 next;

	if (index == 0)
	{
		current = palette_color_a(state);
		next = palette_color_a(state + 1.0);
	}
	else if (index == 1)
	{
		current = palette_color_b(state);
		next = palette_color_b(state + 1.0);
	}
	else
	{
		current = palette_color_c(state);
		next = palette_color_c(state + 1.0);
	}

	return mix(
		current,
		next,
		smoothstep(0.0, 1.0, blend)
	);
}


// =====================================================
// PROCEDURAL PIXEL CAT
// =====================================================

float pixel_cat(vec2 p)
{
	// Draw in a 9 x 8 logical pixel grid.
	vec2 q = floor(p * vec2(9.0, 8.0));

	float x = q.x;
	float y = q.y;

	float mask = 0.0;


	// ----------------------------
	// ears
	// ----------------------------

	if (
		(y == 0.0 && x == 1.0) ||
		(y == 1.0 && (x == 1.0 || x == 2.0))
	)
	{
		mask = 1.0;
	}

	if (
		(y == 0.0 && x == 7.0) ||
		(y == 1.0 && (x == 6.0 || x == 7.0))
	)
	{
		mask = 1.0;
	}


	// ----------------------------
	// upper head
	// ----------------------------

	if (
		y >= 1.0 &&
		y <= 5.0 &&
		x >= 2.0 &&
		x <= 6.0
	)
	{
		mask = 1.0;
	}


	// ----------------------------
	// cheeks
	// ----------------------------

	if (
		y >= 3.0 &&
		y <= 4.0 &&
		(x == 1.0 || x == 7.0)
	)
	{
		mask = 1.0;
	}


	// ----------------------------
	// lower face
	// ----------------------------

	if (
		y == 5.0 &&
		x >= 3.0 &&
		x <= 5.0
	)
	{
		mask = 1.0;
	}


	// ----------------------------
	// eyes
	// ----------------------------

	if (
		y == 3.0 &&
		(x == 3.0 || x == 5.0)
	)
	{
		mask = 0.0;
	}


	// ----------------------------
	// nose / mouth
	// ----------------------------

	if (
		y == 4.0 &&
		x == 4.0
	)
	{
		mask = 0.0;
	}


	return mask;
}


// =====================================================
// CAT PARTICLE FIELD
// =====================================================

float cat_particles(
	vec2 uv,
	float time,
	float liquid_flow
)
{
	vec2 particle_grid =
		vec2(
			cat_columns,
			cat_rows
		);


	// Move the particle field downward over time.
	vec2 grid_uv =
		uv * particle_grid;

	grid_uv.y +=
		time
		* cat_flow_speed
		* particle_grid.y;


	vec2 cell =
		floor(grid_uv);

	vec2 local =
		fract(grid_uv);


	// -------------------------------------------------
	// random values per cell
	// -------------------------------------------------

	vec2 rnd =
		hash2(cell);


	float existence =
		hash(
			cell + vec2(18.4, 72.1)
		);


	// Roughly 44% of cells contain cats.
	if (existence < 0.56)
	{
		return 0.0;
	}


	// -------------------------------------------------
	// random base position
	// -------------------------------------------------

	vec2 center =
		vec2(
			0.18 + rnd.x * 0.64,
			0.18 + rnd.y * 0.64
		);


	// -------------------------------------------------
	// water-like sideways sway
	// -------------------------------------------------

	float sway =
		sin(
			time * (
				0.75 +
				rnd.y * 0.9
			)
			+
			cell.y * 2.7
			+
			cell.x * 1.3
		);


	float current =
		sin(
			uv.y * 5.0
			+
			time * 0.65
			+
			liquid_flow * 3.0
		);


	center.x +=
		sway
		* cat_sideways_drift
		* 0.22;


	center.x +=
		current
		* cat_sideways_drift
		* 0.18;


	// Slight vertical bob.
	center.y +=
		sin(
			time * 0.55
			+
			rnd.x * 8.0
		)
		* 0.035;


	// -------------------------------------------------
	// variable size
	// -------------------------------------------------

	float size =
		cat_size
		* mix(
			0.72,
			1.25,
			hash(
				cell + vec2(4.1, 19.7)
			)
		);


	vec2 cat_uv =
		(local - center)
		/ size
		+
		0.5;


	if (
		cat_uv.x < 0.0 ||
		cat_uv.x >= 1.0 ||
		cat_uv.y < 0.0 ||
		cat_uv.y >= 1.0
	)
	{
		return 0.0;
	}


	float cat =
		pixel_cat(cat_uv);


	// -------------------------------------------------
	// fade at top and bottom of cell
	// -------------------------------------------------

	float edge_fade =
		smoothstep(
			0.0,
			0.12,
			local.y
		)
		*
		smoothstep(
			0.0,
			0.12,
			1.0 - local.y
		);


	return
		cat
		* edge_fade;
}


// =====================================================
// MAIN
// =====================================================

void fragment()
{
	// -------------------------------------------------
	// PIXELATED UV
	// -------------------------------------------------

	vec2 grid =
		vec2(
			pixel_columns,
			pixel_rows
		);


	vec2 uv =
		floor(UV * grid)
		/ grid;


	float t =
		TIME * speed;


	// =================================================
	// LARGE LIQUID FIELD
	// =================================================

	vec2 p =
		vec2(
			uv.x * flow_scale,
			uv.y * flow_scale * 0.8
		);


	float wave =
		sin(
			uv.x * 5.0
			+
			t * 0.9
		)
		* large_wave_strength;


	wave +=
		sin(
			uv.x * 8.0
			-
			t * 0.55
			+
			uv.y * 2.0
		)
		*
		large_wave_strength
		*
		0.45;


	// -------------------------------------------------
	// domain warp
	// -------------------------------------------------

	vec2 warp_pos =
		p
		+
		vec2(
			t * 0.08,
			-t * 0.055
		);


	vec2 warp =
		vec2(
			fbm(
				warp_pos
				+
				vec2(3.2, 6.1)
			),

			fbm(
				warp_pos
				+
				vec2(8.5, 1.7)
			)
		);


	warp -= 0.5;


	float field =
		uv.y
		+
		wave
		+
		warp.x * warp_strength
		+
		warp.y * warp_strength * 0.55;


	// -------------------------------------------------
	// smaller liquid variation
	// -------------------------------------------------

	float detail =
		fbm(
			p * 2.1
			+
			vec2(
				-t * 0.04,
				t * 0.05
			)
		);


	field +=
		(detail - 0.5)
		* detail_strength;


	// =================================================
	// LARGE COLOUR REGIONS
	// =================================================

	float liquid =
		smoothstep(
			0.20,
			0.80,
			fract(
				field * 1.45
			)
		);


	// =================================================
	// PALETTE EVOLUTION
	// =================================================

	float palette_time =
		TIME
		* palette_speed;


	float palette_state =
		floor(
			palette_time
		);


	float palette_blend =
		fract(
			palette_time
		);


	vec3 col_a =
		get_palette_color(
			palette_state,
			palette_blend,
			0
		);


	vec3 col_b =
		get_palette_color(
			palette_state,
			palette_blend,
			1
		);


	vec3 col_c =
		get_palette_color(
			palette_state,
			palette_blend,
			2
		);


	// =================================================
	// PRIMARY LIQUID GRADIENT
	// =================================================

	vec3 colour;


	if (liquid < 0.5)
	{
		colour =
			mix(
				col_a,
				col_b,
				smoothstep(
					0.0,
					0.5,
					liquid
				)
			);
	}
	else
	{
		colour =
			mix(
				col_b,
				col_c,
				smoothstep(
					0.5,
					1.0,
					liquid
				)
			);
	}


	// =================================================
	// PIXEL DITHER
	// =================================================

	float dither =
		dither_pattern(
			uv
			* dither_scale
		);


	float edge =
		abs(
			liquid
			-
			0.5
		);


	float dither_mask =
		1.0
		-
		smoothstep(
			0.05,
			0.38,
			edge
		);


	float dither_offset =
		(dither - 0.5)
		*
		dither_strength
		*
		dither_mask;


	colour +=
		dither_offset;


	// =================================================
	// CAT PARTICLES
	// =================================================

	float cats =
		cat_particles(
			uv,
			TIME,
			field
		);


	// Use the current palette rather than a fixed white.
	vec3 cat_colour =
		mix(
			colour,
			vec3(1.0),
			0.42
		);


	cat_colour *=
		cat_brightness;


	colour =
		mix(
			colour,
			cat_colour,
			cats
			*
			cat_opacity
		);


	// =================================================
	// OUTPUT
	// =================================================

	COLOR =
		vec4(
			clamp(
				colour,
				vec3(0.0),
				vec3(1.0)
			),
			1.0
		);
}