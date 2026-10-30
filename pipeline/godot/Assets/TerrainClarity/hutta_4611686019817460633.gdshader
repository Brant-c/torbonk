shader_type spatial;
render_mode cull_disabled;
varying vec3 source_pos;
void vertex(){source_pos=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;}
uniform sampler2D weights0 : filter_linear, repeat_disable;
uniform sampler2D layer0 : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D tint : source_color, filter_linear, repeat_disable;
uniform sampler2D normal_map : hint_normal, filter_linear_mipmap_anisotropic, repeat_disable;
uniform sampler2D roughness_map : filter_linear_mipmap_anisotropic, repeat_disable;
void fragment(){ALBEDO=(texture(layer0,source_pos.xz*vec2(1.75,1.75)).rgb*texture(weights0,UV).r)/max(texture(weights0,UV).r,0.001)*texture(tint,UV).rgb;NORMAL_MAP=texture(normal_map,UV).rgb;ROUGHNESS=texture(roughness_map,UV).r;SPECULAR=0.15;}
