shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, fog_disabled;
// April mat_si_thunderous_blast: original textures, tints and AnimatedUV rates.
uniform sampler2D energy_texture : source_color;
uniform sampler2D caustics_texture : source_color;
uniform sampler2D blast_texture : source_color;
uniform float brightness = 1.35;
vec2 spin(vec2 uv,float angle){float c=cos(angle);float s=sin(angle);return mat2(vec2(c,s),vec2(-s,c))*(uv-vec2(.5))+vec2(.5);}
void fragment(){
    vec4 energy=texture(energy_texture,spin(UV,-TIME));
    vec4 caustics=texture(caustics_texture,UV+vec2(0.0,TIME));
    vec4 blast=texture(blast_texture,spin(UV,TIME*3.0));
    vec3 light=energy.rgb*vec3(.639216,.47451,.964706)+caustics.rgb*.5*blast.rgb;
    ALBEDO=light*brightness;
    ALPHA=clamp(max(light.r,max(light.g,light.b))*brightness,0.0,1.0);
}
