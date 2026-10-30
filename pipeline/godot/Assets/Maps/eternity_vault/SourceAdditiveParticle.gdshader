shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, fog_disabled;
uniform sampler2D source_texture : source_color, filter_linear, repeat_disable;
void fragment() {
    vec4 texel=texture(source_texture,UV);
    ALBEDO=texel.rgb*COLOR.rgb;
    ALPHA=texel.a*COLOR.a;
}
