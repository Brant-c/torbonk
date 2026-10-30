shader_type spatial;
render_mode cull_disabled;
varying vec3 source_pos;
void vertex(){source_pos=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;}
uniform sampler2D weights0 : filter_linear, repeat_disable;
uniform sampler2D weights1 : filter_linear, repeat_disable;
uniform sampler2D layer0 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D layer1 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D layer2 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D layer3 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D layer4 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D layer5 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D layer6 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D layer7 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D tint : source_color, filter_linear, repeat_disable;
uniform sampler2D normal_map : hint_normal, filter_linear_mipmap_anisotropic, repeat_disable;
uniform sampler2D roughness_map : filter_linear_mipmap_anisotropic, repeat_disable;
void fragment(){ALBEDO=(texture(layer0,source_pos.xz*vec2(1.5,1.5)).rgb*texture(weights0,UV).r+texture(layer1,source_pos.xz*vec2(3.0,3.0)).rgb*texture(weights0,UV).g+texture(layer2,source_pos.xz*vec2(2.25,2.25)).rgb*texture(weights0,UV).b+texture(layer3,source_pos.xz*vec2(2.0,2.0)).rgb*texture(weights0,UV).a+texture(layer4,source_pos.xz*vec2(2.0,2.0)).rgb*texture(weights1,UV).r+texture(layer5,source_pos.xz*vec2(1.0,1.0)).rgb*texture(weights1,UV).g+texture(layer6,source_pos.xz*vec2(2.5,2.5)).rgb*texture(weights1,UV).b+texture(layer7,source_pos.xz*vec2(2.0,2.0)).rgb*texture(weights1,UV).a)/max(texture(weights0,UV).r+texture(weights0,UV).g+texture(weights0,UV).b+texture(weights0,UV).a+texture(weights1,UV).r+texture(weights1,UV).g+texture(weights1,UV).b+texture(weights1,UV).a,0.001)*texture(tint,UV).rgb;NORMAL_MAP=texture(normal_map,UV).rgb;ROUGHNESS=texture(roughness_map,UV).r;SPECULAR=0.15;}
