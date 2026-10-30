shader_type spatial;
render_mode cull_disabled;
varying vec3 source_pos;
void vertex(){source_pos=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;}
uniform sampler2D weights0 : filter_linear, repeat_disable;
uniform sampler2D layer0 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D layer1 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D layer2 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D tint : source_color, filter_linear, repeat_disable;
uniform sampler2D detail_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
void fragment(){ALBEDO=(texture(layer0,source_pos.xz*vec2(3.0,3.0)).rgb*texture(weights0,UV).r+texture(layer1,source_pos.xz*vec2(2.0,2.0)).rgb*texture(weights0,UV).g+texture(layer2,source_pos.xz*vec2(2.0,2.0)).rgb*texture(weights0,UV).b)*texture(tint,UV).rgb;ROUGHNESS=0.88;NORMAL_MAP=texture(detail_normal,source_pos.xz*vec2(3,3)).rgb;}