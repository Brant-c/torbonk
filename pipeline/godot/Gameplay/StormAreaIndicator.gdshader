shader_type spatial;
render_mode unshaded, fog_disabled, blend_add, depth_draw_never, cull_disabled, shadows_disabled;

uniform sampler2D reticle_texture : source_color, filter_linear_mipmap, repeat_disable;
uniform sampler2D imperial_texture : source_color, filter_linear_mipmap, repeat_disable;
uniform vec4 player_green : source_color = vec4(0.0, 1.0, 0.0, 1.0);
uniform float ridge_fraction;

void fragment() {
	vec4 ring = texture(reticle_texture, (UV - 0.5) * ridge_fraction + 0.5);
	// The Imperial emblem is the original persistent AoE projector's artwork.
	// Its red channel is a brightness mask, using the original green UI projector color.
	vec2 emblem_uv = (UV - 0.5) / 0.62 + 0.5;
	float inside = step(0.0, emblem_uv.x) * step(emblem_uv.x, 1.0)
		* step(0.0, emblem_uv.y) * step(emblem_uv.y, 1.0);
	vec4 emblem = texture(imperial_texture, emblem_uv);
	float intensity = ring.r * ring.a + emblem.r * emblem.a * inside * 0.65;
	ALBEDO = player_green.rgb * intensity;
	ALPHA = player_green.a;
}
