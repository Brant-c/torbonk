shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled;
uniform sampler2D source_texture : source_color, repeat_disable, filter_linear;
uniform bool billboard = true;
// Smoke and gas: ordinary alpha blending so dark original smoke atlases read as volume, not additive glare.
void vertex() {
    if (billboard) {
        MODELVIEW_MATRIX = VIEW_MATRIX * mat4(INV_VIEW_MATRIX[0] * length(MODEL_MATRIX[0].xyz), INV_VIEW_MATRIX[1] * length(MODEL_MATRIX[1].xyz), INV_VIEW_MATRIX[2] * length(MODEL_MATRIX[2].xyz), MODEL_MATRIX[3]);
    }
}
void fragment() {
    vec4 pixel = texture(source_texture, UV);
    ALBEDO = COLOR.rgb;
    ALPHA = pixel.a * COLOR.a;
}
