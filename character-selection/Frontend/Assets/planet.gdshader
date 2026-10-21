shader_type canvas_item;
uniform float longitude = 0.0;
uniform bool animate = false;
void fragment() {
    vec2 p = (UV - vec2(0.5)) * 2.0;
    float r = dot(p,p);
    float z = sqrt(max(0.0,1.0-r));
    vec3 n = vec3(p.x,-p.y,z);
    vec2 uv = vec2(atan(n.x,n.z)/6.2831853 + 0.5 + longitude + (animate ? TIME*0.004 : 0.0), acos(clamp(n.y,-1.0,1.0))/3.1415926);
    vec4 surface = texture(TEXTURE,uv);
    float light = 0.22 + 0.78*max(0.0,dot(n,normalize(vec3(-0.5,0.4,1.0))));
    float rim = pow(1.0-z,3.0);
    COLOR = vec4(surface.rgb*light + vec3(0.08,0.28,0.42)*rim, 1.0-smoothstep(0.98,1.0,r));
}
