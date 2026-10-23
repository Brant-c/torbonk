shader_type spatial;
render_mode unshaded, fog_disabled, blend_add, depth_draw_never, cull_disabled, shadows_disabled;

uniform sampler2D field_texture : source_color, filter_linear_mipmap, repeat_disable;
uniform sampler2D ring_texture : source_color, filter_linear_mipmap, repeat_disable;
uniform float opacity : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    vec4 field = texture(field_texture, UV);
    vec4 ring = texture(ring_texture, UV);
    float radius = length((UV - 0.5) * 2.0);
    // The source lightning tile is opaque. Its black pixels contribute no light;
    // the circular falloff keeps the terrain projection inside the gameplay area.
    float edge = 1.0 - smoothstep(0.72, 1.0, radius);
    ALBEDO = (field.rgb * field.a * edge * 0.8
        + ring.rgb * ring.a * edge * vec3(0.49, 0.19, 0.65)) * opacity;
    ALPHA = 0.75;
}
