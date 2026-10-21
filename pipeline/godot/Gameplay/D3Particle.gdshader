shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled;
uniform sampler2D source_texture : source_color, repeat_disable, filter_linear;
void fragment() {
    vec4 pixel = texture(source_texture, UV);
    ALBEDO = pixel.rgb * COLOR.rgb;
    ALPHA = pixel.a * COLOR.a;
}
