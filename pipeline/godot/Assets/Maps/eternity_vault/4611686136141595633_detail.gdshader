shader_type spatial;
render_mode cull_disabled;
varying vec3 source_pos;
void vertex(){source_pos=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;}
uniform sampler2D weights0 : filter_linear, repeat_disable;
uniform sampler2D layer0 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D normal0 : filter_linear_mipmap_anisotropic, repeat_enable;
void fragment(){vec2 blend_uv=(UV*vec2(128.0,128.0)+vec2(0.5))/vec2(129.0,129.0);vec4 w0=texture(weights0,blend_uv);float b0=w0.r;float total=b0;vec4 surface0=texture(normal0,source_pos.xz*vec2(1.5,1.5));ALBEDO=(texture(layer0,source_pos.xz*vec2(1.5,1.5)).rgb*b0)/max(total,0.00001)*COLOR.rgb;ROUGHNESS=max(0.88,(surface0.a*b0)/max(total,0.00001));SPECULAR=0.12;NORMAL_MAP=normalize((surface0.rgb*2.0-1.0)*b0)*0.5+0.5;}