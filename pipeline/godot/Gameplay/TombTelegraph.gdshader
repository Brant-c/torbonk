shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, shadows_disabled;
uniform float progress = 0.0;
uniform float impact = 0.0;
uniform int shape = 0;
void fragment() {
    vec2 p=UV*2.0-1.0;
    float r=length(p);
    float inside=1.0-smoothstep(.98,1.0,r);
    float edge=smoothstep(.91,.95,r)*inside;
    if(shape==1) {
        inside=(1.0-step(.079,abs(p.x)))*(1.0-step(1.0,abs(p.y)));
        edge=inside*step(.055,abs(p.x));
    }
    if(shape==2) {
        float cone=step(.55,-p.y/max(r,.001));inside*=cone;
        edge=max(edge*cone,inside*step(.54,-p.y/max(r,.001))*(1.0-step(.61,-p.y/max(r,.001))));
    }
    ALBEDO=mix(vec3(1.0,.32,.035),vec3(1.0,.84,.46),impact);
    ALPHA=inside*(.10+.18*progress)+edge*(.65+.15*sin(TIME*9.0))+inside*impact*.40;
}
