VERTEX:
#version 330
#include Partials/Methods.gl

uniform mat4 u_mvp;
uniform mat4 u_model;
uniform mat4 u_view;

layout(location=0) in vec3 a_position;
layout(location=1) in vec2 a_tex;
layout(location=2) in vec3 a_color;
layout(location=3) in vec3 a_normal;

out vec2 v_tex;
out vec3 v_color;
out vec3 v_normal;
out vec3 v_world;

void main(void)
{
    gl_Position = u_mvp * vec4(a_position, 1.0);

    v_tex = a_tex;
    v_color = a_color;
    v_normal = TransformNormal(a_normal, u_view * u_model);
    v_world = vec3(u_model * vec4(a_position, 1.0));
}

FRAGMENT:
#version 330
#include Partials/Methods.gl

uniform sampler2D u_texture;
uniform vec4      u_color;
uniform float     u_near;
uniform float     u_far;
uniform vec3      u_sun;
//uniform float     u_cutout;
uniform float u_objectID;

in vec2 v_tex;
in vec3 v_normal;
in vec3 v_color;
in vec3 v_world;

layout(location = 0) out vec4 o_color;
layout(location = 1) out float o_objectID;

void main(void)
{
    // Get texture color
    vec4 src = texture(u_texture, v_tex) * u_color * vec4(v_color, 1);

    // TODO: only enable if you want ModelFlags.Cutout types to work, didn't end up using
//    if (src.a < u_cutout)
//        discard;

    float depth = LinearizeDepth(gl_FragCoord.z, u_near, u_far);
    float fall = Map(v_world.z, 50, 0, 0, 1);
    float fade = Map(depth, 0.9, 1, 1, 0);
    vec3  col = src.rgb;

    // Apply depth values
    gl_FragDepth = depth;

    // Apply shading based on normal relative to the camera
    float shade = clamp(dot(v_normal, vec3(0, 0, 1)), 0.2, 1.0);
    col *= vec3(shade);
    
    o_color = vec4(col, src.a) * fade;
    // TODO: Support object IDs above 255, since its just 8bits
    o_objectID = u_objectID / 255.0;
}