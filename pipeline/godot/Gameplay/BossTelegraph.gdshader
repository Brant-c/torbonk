shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float impact : hint_range(0.0, 1.0) = 0.0;
void fragment() {
    vec2 p = UV * 2.0 - 1.0;
    float r = length(p);
    float edge = smoothstep(0.90, 0.94, r) * (1.0-smoothstep(0.985, 1.0, r));
    float angle = atan(p.y, p.x) + 3.14159265;
    float ticks = step(0.45, fract(angle * 12.0 / 6.2831853));
    float inner = smoothstep(0.79, 0.82, r) * (1.0-smoothstep(0.85, 0.88, r));
    float countdown = step(angle / 6.2831853, progress);
    float pulse = 0.74 + 0.26 * sin(TIME * (5.0 + progress * 10.0));
    float warning = edge * pulse + inner * ticks * (0.25 + countdown * 0.75);
    // Open center preserves the player silhouette. Impact is a separate short white/amber flash.
    float strike = smoothstep(0.2, 0.4, r) * (1.0-smoothstep(0.92, 1.0, r));
    ALBEDO = mix(vec3(1.0, 0.38, 0.035), vec3(1.0, 0.88, 0.53), impact);
    ALPHA = mix(warning * 0.85, strike * impact, step(0.001, impact));
}
