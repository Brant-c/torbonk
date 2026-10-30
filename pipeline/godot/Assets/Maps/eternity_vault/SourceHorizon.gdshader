shader_type spatial;
render_mode unshaded, depth_draw_never, fog_disabled;
uniform sampler2D source_texture : source_color, filter_linear_mipmap_anisotropic, repeat_disable;
uniform float minimum_distance = 0.0;
uniform float maximum_distance = 1000.0;
uniform float minimum_opacity = 0.0;
uniform float maximum_opacity = 1.0;
void fragment() {
    vec4 texel = texture(source_texture, UV);
    float distance_from_camera = length(VERTEX);
    float fade = clamp((distance_from_camera-minimum_distance)/max(maximum_distance-minimum_distance,0.0001),0.0,1.0);
    ALBEDO = texel.rgb;
    ALPHA = texel.a * mix(minimum_opacity,maximum_opacity,fade);
}
