shader_type spatial;

render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_schlick_ggx;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap, repeat_enable;

uniform vec4 base_tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 edge_color : source_color = vec4(0.55, 0.65, 1.0, 1.0);

uniform float edge_strength = 0.55;
uniform float edge_power = 1.0;

uniform float roughness_value = 0.65;
uniform float metallic_value = 0.0;


void fragment() {
    vec4 tex = texture(albedo_texture, UV);

    // Vertex color red channel is the edge mask.
    // Black = 0.0, white = 1.0.
    float edge_mask = clamp(COLOR.r, 0.0, 1.0);

    // Lets you sharpen or soften the mask in Godot.
    edge_mask = pow(edge_mask, edge_power);

    vec3 base_col = tex.rgb * base_tint.rgb;

    // Lighten/tint toward edge color where vertex mask is white.
    vec3 final_col = mix(base_col, edge_color.rgb, edge_mask * edge_strength);

    ALBEDO = final_col;
    ALPHA = tex.a * base_tint.a;

    ROUGHNESS = roughness_value;
    METALLIC = metallic_value;
}

//void fragment() {
    //ALBEDO = COLOR.rgb;
    //ROUGHNESS = 1.0;
//}

//void fragment() {
    //ALBEDO = vec3(COLOR.r);
    //ROUGHNESS = 1.0;
//}