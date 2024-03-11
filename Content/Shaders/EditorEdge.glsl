VERTEX:
#version 330

layout(location=0) in vec2 a_pos;
layout(location=1) in vec2 a_tex;


out vec2 v_tex;

void main(void)
{
    gl_Position = vec4(a_pos, 0, 1);
    v_tex = a_tex;

}

FRAGMENT:
#version 330
#include Partials/Methods.gl

uniform sampler2D u_objectID;
uniform float     u_selectedID;

uniform vec2 u_pixel;
uniform vec4 u_edge;

in vec2 v_tex;
out vec4 o_color;

float objectID(vec2 uv)
{
	return texture(u_objectID, uv).r;
}

void main(void)
{
    float a = objectID(v_tex + vec2(u_pixel.x, 0));
    float b = objectID(v_tex + vec2(-u_pixel.x, 0));
    float c = objectID(v_tex + vec2(0, u_pixel.y));
    float d = objectID(v_tex + vec2(0, -u_pixel.y));
    
    float it = objectID(v_tex);
    float other =
        a * 0.25 +
        b * 0.25 +
        c * 0.25 +
        d * 0.25;
    
    float edge = step(0.0001, other - it);
    
    if (u_selectedID == 0 || edge == 0 || !(a == u_selectedID || b == u_selectedID || c == u_selectedID || d == u_selectedID || it == u_selectedID))
        discard;

    o_color = vec4(vec3(u_edge), 1);
}
        