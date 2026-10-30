shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;
// April bh_kolto_missile_aoe projects the same oil puddle luminance masks, tinted kolto green.
uniform sampler2D puddle_detail : source_color, repeat_enable, filter_linear_mipmap;
uniform sampler2D puddle_border : source_color, repeat_disable, filter_linear_mipmap;
uniform vec4 tint : source_color = vec4(0.35, 1.0, 0.55, 1.0);
uniform float fade = 1.0;
void fragment() {
    float border=texture(puddle_border,UV).r;
    float detail=texture(puddle_detail,UV*1.4+TIME*0.02).r;
    float rim=1.0-smoothstep(0.88,1.0,length(UV*2.0-1.0));
    float coverage=(1.0-smoothstep(0.12,0.82,border))*rim;
    ALBEDO=tint.rgb*(0.35+detail*0.65);
    ALPHA=coverage*0.5*fade;
}
