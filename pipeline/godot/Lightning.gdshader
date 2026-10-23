shader_type spatial;
render_mode unshaded, fog_disabled, blend_add, depth_draw_never, cull_disabled, shadows_disabled;
uniform sampler2D source_texture : source_color, repeat_enable, filter_linear;
uniform bool particle = false;
uniform float hue_purple_key = .4;
uniform float hue_white_key = .95;
void fragment() {
	vec4 source = texture(source_texture, UV);
	float x = source.r;
	// Recovered texture hue keys: black -> purple (.333,0,1) -> white.
	vec3 purple = vec3(.333, 0.0, 1.0);
	vec3 hue = x < hue_purple_key ? mix(vec3(0.0), purple, x/hue_purple_key) : mix(purple, vec3(1.0), clamp((x-hue_purple_key)/(hue_white_key-hue_purple_key),0.0,1.0));
	if (particle) hue = x < .554 ? mix(vec3(0.0),vec3(.114),x/.554) : mix(vec3(.114),vec3(1.0),(x-.554)/.446);
	ALBEDO = hue * COLOR.rgb;
	ALPHA = source.a * COLOR.a;
}
