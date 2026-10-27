shader_type spatial;
render_mode unshaded, fog_disabled, blend_mix, depth_draw_never, cull_disabled, shadows_disabled;
uniform sampler2D source_texture : source_color, repeat_disable, filter_linear;
uniform float frame_phase = 0.0;

void vertex() {
    MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
        INV_VIEW_MATRIX[0] * length(MODEL_MATRIX[0].xyz),
        INV_VIEW_MATRIX[1] * length(MODEL_MATRIX[1].xyz),
        INV_VIEW_MATRIX[2] * length(MODEL_MATRIX[2].xyz), MODEL_MATRIX[3]);
}

float smoke_alpha(float frame, vec2 local_uv) {
    vec2 cell = vec2(mod(frame, 4.0), floor(frame / 4.0));
    vec2 half_texel = 0.5 / vec2(textureSize(source_texture, 0));
    vec2 atlas_uv = clamp((cell + local_uv) / 4.0,
        cell / 4.0 + half_texel, (cell + 1.0) / 4.0 - half_texel);
    return texture(source_texture, atlas_uv).a;
}

void fragment() {
    float frame = mod(floor(frame_phase), 16.0);
    float alpha = mix(smoke_alpha(frame, UV), smoke_alpha(mod(frame + 1.0, 16.0), UV),
        smoothstep(0.0, 1.0, fract(frame_phase)));
    // Original atlas cells have opaque wisps at their boundaries. Fade the card
    // edges and clamp samples inside each cell to prevent rectangular seams.
    vec2 edge = smoothstep(vec2(0.0), vec2(0.18), UV)
        * smoothstep(vec2(0.0), vec2(0.18), vec2(1.0) - UV);
    ALBEDO = COLOR.rgb;
    ALPHA = alpha * COLOR.a * edge.x * edge.y;
}
