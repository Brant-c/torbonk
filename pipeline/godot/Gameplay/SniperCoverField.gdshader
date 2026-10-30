shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled;

// Adapter for sp_portablecover_dynamiclength_01 / proj_portable_cover1.
// Sample the recovered projector artwork, not the receiver mesh's base grid.
uniform sampler2D hex_grid : source_color, repeat_enable, filter_linear;
uniform sampler2D glow_bar : source_color, repeat_enable, filter_linear;
uniform float effect_time = 0.0;
varying vec2 projected_uv;

void vertex() {
    // Field-local coordinates before the original (90,0,0) attachment rotation.
    // Source projector centre (0,.06,0), size (.14,.11,.05).
    projected_uv = vec2(VERTEX.x / .14 + .5, (-VERTEX.z - .06) / .11 + .5);
}

void fragment() {
    // Native P curves: 6->5.9 / .05s, 5->4.9 / .02s,
    // and layer-one intensity 1.5->1.8 / .1s. No UV scrolling.
    vec2 tile = vec2(mix(6.0, 5.9, fract(effect_time / .05)),
                     mix(5.0, 4.9, fract(effect_time / .02)));
    vec3 hex = texture(hex_grid, projected_uv * tile).rgb;
    vec3 glow = texture(glow_bar, (projected_uv - .5) * 1.2 + .5).rgb;
    vec3 base = hex * vec3(.442, .614, .958) * 2.0;
    vec3 layer = glow * vec3(.114, .353, .646)
        * mix(1.5, 1.8, fract(effect_time / .1));
    // Original layer-one blend is MULT; the complete projection is ADDITIVE.
    ALBEDO = base * layer;
    ALPHA = 1.0;
}
