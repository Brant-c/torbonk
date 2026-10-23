shader_type spatial;
render_mode unshaded, fog_disabled, blend_add, depth_draw_never, cull_disabled, shadows_disabled;
uniform sampler2D source_texture : source_color, repeat_enable, filter_linear;
uniform sampler2D source_hue : source_color, repeat_disable, filter_linear;
void fragment() {
    vec4 source = texture(source_texture, UV);
    // The Storm FXSpec defines its own four-key hue ramp; it is not Force Lightning's ramp.
    ALBEDO = texture(source_hue, vec2(source.r, 0.5)).rgb * COLOR.rgb;
    ALPHA = source.a * COLOR.a;
}
