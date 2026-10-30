shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_back, shadows_disabled, fog_disabled;
// Triplanar status coating on the enemy's own skinned surfaces (see PowertechCarbonite).
// Burn: original 4x4 flame-lick atlas licking upward. Heat Signature: red rim like bh_missletracking_redoverlay.
uniform sampler2D flames : source_color, repeat_enable, filter_linear_mipmap;
uniform vec3 origin;
uniform float burn = 0.0;
uniform float mark = 0.0;
varying vec3 coat_position;
varying vec3 coat_normal;
void vertex() {
    coat_position=((MODEL_MATRIX*vec4(VERTEX,1.0)).xyz-origin)*9.0;
    coat_normal=normalize(MODEL_NORMAL_MATRIX*NORMAL);
}
float flame(vec2 p) {
    float frame=floor(mod(TIME*18.0,16.0));
    vec2 cell=vec2(mod(frame,4.0),floor(frame/4.0))*0.25;
    vec2 local=fract(vec2(p.x,-p.y+TIME*0.6));
    return texture(flames,cell+local*0.25).r;
}
void fragment() {
    vec3 w=pow(abs(coat_normal),vec3(4.0));
    w/=max(dot(w,vec3(1.0)),0.001);
    float f=flame(coat_position.zy)*w.x+flame(coat_position.xz)*w.y+flame(coat_position.xy)*w.z;
    float rim=pow(1.0-clamp(dot(NORMAL,VIEW),0.0,1.0),2.0);
    ALBEDO=burn*vec3(1.0,0.42,0.1)*f*1.5+mark*vec3(1.0,0.1,0.04)*(0.12+rim*1.3);
}
