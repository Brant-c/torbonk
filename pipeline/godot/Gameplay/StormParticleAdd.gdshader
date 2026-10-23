shader_type spatial;
render_mode unshaded, fog_disabled, blend_add, depth_draw_never, cull_disabled, shadows_disabled;
uniform sampler2D source_texture : source_color, repeat_disable, filter_linear;
uniform sampler2D source_hue : source_color, repeat_disable, filter_linear;
uniform bool use_hue = false;
uniform bool source_one = false;
void fragment() {
    vec4 pixel = texture(source_texture, UV);
    ALBEDO = (use_hue ? texture(source_hue, vec2(pixel.r, 0.5)).rgb : pixel.rgb) * COLOR.rgb;
    ALPHA = (source_one ? 1.0 : pixel.a) * COLOR.a;
}
