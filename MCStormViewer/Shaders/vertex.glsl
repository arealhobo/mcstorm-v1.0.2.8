#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec4 aColor;
layout (location = 2) in vec3 aNormal;

uniform mat4 uView;
uniform mat4 uProjection;

out vec4 vColor;
out vec3 vNormal;
out float vDist;
out vec3 vWorldPos;

void main()
{
    vec4 viewPos = uView * vec4(aPos, 1.0);
    gl_Position = uProjection * viewPos;
    vColor = aColor;
    vNormal = aNormal;
    vDist = length(viewPos.xyz);
    vWorldPos = aPos;
}
