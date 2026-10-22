shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled;
// Original bh_energy_shield_02 inputs: tiled hexagon symbol, scrolling scanlines and a circular gradient, shaded as a rim-lit shell.
uniform sampler2D hexagons : source_color, repeat_enable, filter_linear;
uniform sampler2D scanlines : source_color, repeat_enable, filter_linear;
uniform vec4 tint : source_color = vec4(1.0, 0.45, 0.12, 1.0);
uniform float strength = 1.0;
void fragment() {
    float rim = pow(1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0), 2.0);
    vec2 uv = UV * vec2(8.0, 4.0);
    float hex = texture(hexagons, uv).r;
    float scan = texture(scanlines, vec2(UV.x * 2.0, UV.y * 3.0 - TIME * 0.35)).r;
    float v = (0.15 + rim * 0.9 + hex * (0.35 + rim)) * (0.7 + 0.5 * scan);
    ALBEDO = tint.rgb * v * strength;
    ALPHA = clamp(v * 0.8, 0.0, 1.0) * strength;
}
