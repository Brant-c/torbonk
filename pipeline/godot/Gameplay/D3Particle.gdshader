shader_type spatial;
render_mode unshaded, fog_disabled, blend_add, depth_draw_never, cull_disabled, shadows_disabled;
uniform sampler2D source_texture : source_color, repeat_disable, filter_linear;
uniform bool billboard = true;
// Sprite3D ignores its own Billboard flag under a custom shader, so face the camera here.
void vertex() {
    if (billboard) {
        MODELVIEW_MATRIX = VIEW_MATRIX * mat4(INV_VIEW_MATRIX[0], INV_VIEW_MATRIX[1], INV_VIEW_MATRIX[2], MODEL_MATRIX[3]);
    }
}
void fragment() {
    vec4 pixel = texture(source_texture, UV);
    ALBEDO = pixel.rgb * COLOR.rgb;
    ALPHA = pixel.a * COLOR.a;
}
