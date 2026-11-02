shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, shadows_disabled;
// Ground projection for Diversion: the recovered 2x2 volume smoke gradient and blast ring used as luminance masks, never opaque cards.
uniform sampler2D smoke : source_color, repeat_enable, filter_linear_mipmap;
uniform sampler2D ring : source_color, repeat_disable, filter_linear_mipmap;
uniform float fade = 1.0;
uniform float effect_time = 0.0;
void fragment() {
    float r=length(UV*2.0-1.0);
    float disc=1.0-smoothstep(0.62,0.98,r);
    vec2 drift=vec2(effect_time*0.03,effect_time*0.02);
    float puff=texture(smoke,fract(UV*1.3+drift)*0.5).a;
    float puff2=texture(smoke,fract(UV*0.9-drift)*0.5+vec2(0.5,0.0)).a;
    float edge=texture(ring,UV).r*(1.0-smoothstep(0.85,1.0,r));
    ALBEDO=mix(vec3(0.45,0.74,0.68),vec3(0.80,0.95,0.90),puff*0.6);
    ALPHA=clamp(disc*(0.18+0.38*max(puff,puff2))+edge*0.35,0.0,0.75)*fade;
}
