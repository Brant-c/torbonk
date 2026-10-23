shader_type spatial;
render_mode depth_draw_opaque;
uniform sampler2D carbonite : source_color, repeat_enable, filter_linear_mipmap;
uniform vec3 origin;
varying vec3 coating_position;
varying vec3 coating_normal;
void vertex() {
    coating_position=((MODEL_MATRIX*vec4(VERTEX,1.0)).xyz-origin)*16.0;
    coating_normal=normalize(MODEL_NORMAL_MATRIX*NORMAL);
}
void fragment() {
    vec3 weights=pow(abs(normalize(coating_normal)),vec3(4.0));
    weights/=max(dot(weights,vec3(1.0)),0.001);
    vec3 detail=texture(carbonite,coating_position.yz).rgb*weights.x
        +texture(carbonite,coating_position.xz).rgb*weights.y
        +texture(carbonite,coating_position.xy).rgb*weights.z;
    ALBEDO=mix(vec3(0.12,0.14,0.15),vec3(0.53,0.57,0.59),detail.r);
    METALLIC=0.55;
    ROUGHNESS=0.48;
}
