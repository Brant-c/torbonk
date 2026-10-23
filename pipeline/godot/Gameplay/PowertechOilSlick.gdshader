shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, shadows_disabled;
uniform sampler2D oil_detail : source_color, repeat_enable, filter_linear_mipmap;
uniform sampler2D oil_border : source_color, repeat_disable, filter_linear_mipmap;
void fragment() {
    float border=texture(oil_border,UV).r;
    float detail=texture(oil_detail,UV*1.4).r;
    float rim=1.0-smoothstep(0.88,1.0,length(UV*2.0-1.0));
    float coverage=(1.0-smoothstep(0.12,0.82,border))*rim;
    // Original projector maps are luminance masks, not opaque color cards.
    ALBEDO=mix(vec3(0.025,0.029,0.035),vec3(0.12,0.10,0.075),detail*0.5);
    ALPHA=coverage*0.86;
}
