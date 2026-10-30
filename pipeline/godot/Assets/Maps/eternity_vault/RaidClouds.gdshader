shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, fog_disabled;
uniform sampler2D clouds : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D mask_map : filter_linear_mipmap, repeat_disable;
varying vec3 projected_position;
varying vec3 projected_normal;
void vertex() {
    projected_position = VERTEX * vec3(200.0,40.0,200.0);
    projected_normal = MODEL_NORMAL_MATRIX * NORMAL;
}
void fragment() {
    // Source cloud geometry, projector extents and rotation; dual-planar falloff remains approximate.
    float angle=TIME*radians(-1.4);
    vec2 planar=mat2(vec2(cos(angle),sin(angle)),vec2(-sin(angle),cos(angle)))*projected_position.xz/60.0+0.5;
    vec4 cloud = texture(clouds, planar * 3.0 + TIME * vec2(-0.001,-0.002));
    // Both source textures carry opacity in alpha; mask RGB is uniformly white.
    float mask = texture(mask_map, planar).a;
    float facing=abs(normalize(projected_normal).y);
    float falloff=smoothstep(cos(radians(80.0)),cos(radians(20.0)),facing);
    ALBEDO = cloud.rgb * vec3(0.493,0.567,0.727) * vec3(0.584,0.658,0.776);
    ALPHA = cloud.a * mask * 0.5 * falloff;
}
